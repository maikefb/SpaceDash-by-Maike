using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Generated;
using LcdMod.Client.SurfaceScripts.Abstract;
using LcdMod.Client.Config;
using LcdMod.Client.FactionColors;
using LcdMod.Client.GridData;
using LcdMod.Client.Gui.Styling.DataTemplates;
using LcdMod.Client.Helpers;
using LcdMod.Client.ScreenAreas;
using LcdMod.Client.Terminal;
using LcdMod.Common.Helpers;
using LcdMod.Common.Networking;
using ParallelTasks;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage;
using VRage.Game.ModAPI;

namespace LcdMod.Client
{
    /// <summary>
    /// Ciclo de vida do lado cliente. O que é específico de cada mod (módulos de dados, caches de sprites)
    /// entra pelos métodos parciais implementados em ClientComponent.Mod.cs de cada mod.
    /// </summary>
    public partial class LcdModClientComponent : ISingleton<LcdModClientComponent>
    {
        public static readonly List<Action> RunNextFrame = new List<Action>();
        static readonly List<Action> RunThisFrame = new List<Action>();

        public static readonly List<Task> Blocker = new List<Task>();

        sealed class ScheduledOnePerFrameAction
        {
            public Action Action;
            public long DueFrame;
        }

        static readonly List<ScheduledOnePerFrameAction> RunOnePerFrame =
            new List<ScheduledOnePerFrameAction>();

        readonly LcdModSessionComponent _session;
        readonly TerminalManager _terminalManager;

        public LcdModClientComponent(LcdModSessionComponent session)
        {
            RegisterSingleton();
            _session = session;
            _terminalManager = new TerminalManager(session);
        }

        public static event Action OnUpdateBeforeSimulation;

        partial void LoadModData();
        partial void UnloadModData();
        partial void UpdateModData(long frame);
        partial void ClearModCaches();

        public void LoadData()
        {
            LocalConfigManager.Load();
            LoadTextures();
            LoadModData();

            MyAPIGateway.Session.Factions.FactionCreated += FactionUpdated;
            MyAPIGateway.Session.Factions.FactionEdited += FactionUpdated;
            MyAPIGateway.Session.Factions.FactionStateChanged += FactionStateChanged;
        }

        public void BeforeStart()
        {
            LoadLocalization();
            MyAPIGateway.Gui.GuiControlRemoved += OnGuiControlRemoved;
            _terminalManager.Initialize();
        }

        public void UnloadData()
        {
            ConfigManager.FlushPendingSyncs(true);
            FactionColorsSync.Flush(true);
            LocalConfigManager.Save();
            UnloadTextures();
            UnloadModData();
            _terminalManager.Unload();
            MyAPIGateway.Gui.GuiControlRemoved -= OnGuiControlRemoved;

            MyAPIGateway.Session.Factions.FactionCreated -= FactionUpdated;
            MyAPIGateway.Session.Factions.FactionEdited -= FactionUpdated;
            MyAPIGateway.Session.Factions.FactionStateChanged -= FactionStateChanged;

            _session._grids.Clear();
            LcdModSessionComponent.Components.Clear();
            LcdModSessionComponent.Components = null;
            LcdModSessionComponent.DebugSnapshot = SessionDebugSnapshot.Empty;

            ClearModCaches();
            LcdModSessionComponent.ClearClientEvents();

            SurfaceScriptBase.Instances.Clear();
            LcdModSessionComponent.LastSelectedBlock = null;
            ConfigManager.Clear();
            TextureHelper.Clear();
            FactionColorsSync.Clear();
            FormatingHelper.ClearCache();
            ScreenAreaGeometry.ClearCache();
            ItemDataTemplate.InvalidateItemAssets();
            ListBoxItemHelper.PerTypeCache.Clear();
            RunNextFrame.Clear();
            RunThisFrame.Clear();
            Blocker.Clear();
            RunOnePerFrame.Clear();
            InventoryWorkScheduler.Clear();
            OnUpdateBeforeSimulation = null;
            UnregisterSingleton();
        }

        public void UpdateBeforeSimulation()
        {
            try
            {
                _session._updateTick++;
                foreach (var grid in _session._grids.Values)
                {
                    if (grid.Item1.MarkedForClose)
                        continue;

                    grid.Item2.Update();
                }

                UpdateModData(MyAPIGateway.Session.GameplayFrameCounter);
                UpdateDebugSnapshot();
                OnUpdateBeforeSimulation?.Invoke();
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, _session);
            }
        }

        static void RunNextFrameActions()
        {
            if (RunNextFrame.Count == 0)
                return;

            RunThisFrame.Clear();
            RunThisFrame.AddRange(RunNextFrame);
            RunNextFrame.Clear();

            for (int i = 0; i < RunThisFrame.Count; i++)
            {
                var action = RunThisFrame[i];
                if (action == null)
                    continue;

                try
                {
                    action();
                }
                catch (Exception e)
                {
                    ErrorHandlerHelper.LogError(e, nameof(RunNextFrameActions));
                }
            }

            RunThisFrame.Clear();
        }

        public static void ScheduleOnePerFrame(Action action)
        {
            ScheduleOnePerFrame(action, 0);
        }

        public static void ScheduleOnePerFrame(Action action, int delayTicks)
        {
            if (action == null)
                return;

            long currentFrame = MyAPIGateway.Session != null
                ? MyAPIGateway.Session.GameplayFrameCounter
                : 0L;
            RunOnePerFrame.Add(new ScheduledOnePerFrameAction
            {
                Action = action,
                DueFrame = currentFrame + Math.Max(0, delayTicks)
            });
        }

        static void RunOnePerFrameAction()
        {
            if (RunOnePerFrame.Count == 0)
                return;

            long currentFrame = MyAPIGateway.Session != null
                ? MyAPIGateway.Session.GameplayFrameCounter
                : long.MaxValue;
            int scheduledIndex = -1;
            for (int i = 0; i < RunOnePerFrame.Count; i++)
            {
                if (RunOnePerFrame[i].DueFrame > currentFrame)
                    continue;

                scheduledIndex = i;
                break;
            }

            if (scheduledIndex < 0)
                return;

            var scheduled = RunOnePerFrame[scheduledIndex];
            RunOnePerFrame.RemoveAt(scheduledIndex);
            var action = scheduled.Action;
            if (action == null)
                return;

            try
            {
                action();
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, nameof(RunOnePerFrameAction));
            }
        }

        public void Simulate()
        {
            RunNextFrameActions();
            InventoryWorkScheduler.RunFrame();
            RunOnePerFrameAction();
            ConfigManager.FlushPendingSyncs(false);
            FactionColorsSync.Flush(false);
        }

        public void UpdateAfterSimulation()
        {
            try
            {
                LcdModSessionComponent.RaiseAfterSimulationUpdate();
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, _session);
            }

            WaitForBlockers();
        }

        void WaitForBlockers()
        {
            foreach (var task in Blocker)
            {
                if (task.valid && !task.IsComplete)
                    task.Wait();
            }

            Blocker.Clear();
        }

        internal void HandleSyncConfig(NetworkPackageSyncComponentConfig packet)
        {
            if (packet == null || packet.Config == null)
                return;

            var block = MyEntities.GetEntityById(packet.BlockId) as IMyFunctionalBlock;
            if (block == null || ConfigManager.HasPendingSync(block.EntityId))
                return;

            var settings = ConfigManager.GetConfigForBlock(block);
            if (settings == null)
            {
                ConfigManager.RememberReceivedConfig(block.EntityId, packet.Config);
                return;
            }

            LcdModSessionComponent.ApplySyncedConfig(block, settings, packet.Config);
        }

        void FactionStateChanged(MyFactionStateChange change, long faction1, long faction2, long player, long client)
        {
            if (change < MyFactionStateChange.FactionMemberSendJoin)
                return;

            FactionUpdated(faction1);
            FactionUpdated(faction2);
        }

        void FactionUpdated(long obj)
        {
            var affected = SurfaceScriptBase.Instances.Where(a => a.Faction != null && a.Faction.FactionId == obj)
                .ToList();

            var faction = MyAPIGateway.Session.Factions.TryGetFactionById(obj);

            if (faction != null)
                affected.AddRange(
                    SurfaceScriptBase.Instances.Where(a => a.Block != null &&
                                                           (faction.FounderId == a.Block.OwnerId ||
                                                            faction.Members.ContainsKey(a.Block.OwnerId))));

            affected.ForEach(a => a.UpdateFaction(faction));
        }

        void OnGuiControlRemoved(object obj)
        {
            if (obj.ToString().EndsWith("ScreenOptionsSpace"))
                LoadLocalization();
        }

        void UpdateDebugSnapshot()
        {
            int trackedGrids = _session._grids.Count;
            int trackedLogic = LcdModSessionComponent.Components != null ? LcdModSessionComponent.Components.Count : 0;
            LcdModSessionComponent.DebugSnapshot = new SessionDebugSnapshot(_session._updateTick, trackedGrids, trackedLogic);
        }

        void LoadLocalization()
        {
            var path = Path.Combine(_session.ModContext.ModPathData, "Localization");

            var supportedLanguages = new HashSet<MyLanguagesEnum>();
            MyTexts.LoadSupportedLanguages(path, supportedLanguages);

            var configuredLanguage = MyAPIGateway.Session.Config.Language;
            var currentLanguage = supportedLanguages.Contains(configuredLanguage)
                ? configuredLanguage
                : MyLanguagesEnum.English;

            if (_session._loadedLanguage.HasValue && _session._loadedLanguage.Value == currentLanguage)
                return;

            _session._loadedLanguage = currentLanguage;

            var languageDescription = MyTexts.Languages
                .Where(x => x.Key == currentLanguage)
                .Select(x => x.Value)
                .FirstOrDefault();

            if (languageDescription == null)
                return;

            var cultureName = string.IsNullOrWhiteSpace(languageDescription.CultureName)
                ? null
                : languageDescription.CultureName;
            var subcultureName = string.IsNullOrWhiteSpace(languageDescription.SubcultureName)
                ? null
                : languageDescription.SubcultureName;

            MyTexts.LoadTexts(path, cultureName, subcultureName);
            LcdModSessionComponent.RaiseLanguageChanged();
        }
    }
}
