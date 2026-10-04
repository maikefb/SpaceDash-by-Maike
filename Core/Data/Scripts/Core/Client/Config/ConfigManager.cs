using System;
using System.Collections.Generic;
using System.Linq;
using Generated;
using LcdMod.Client.SurfaceScripts.Abstract;
using LcdMod.Client.Utility;
using LcdMod.Common.Config;
using LcdMod.Common.Config.Components;
using LcdMod.Common.Config.Models;
using LcdMod.Common.Helpers;
using LcdMod.Common.Networking;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using Constants = LcdMod.Common.Helpers.Constants;

namespace LcdMod.Client.Config
{
    /// <summary>
    /// Saves and synchronizes the component graph. Running apps read the exact components they
    /// consume directly from each SurfaceConfig.
    /// </summary>
    public static class ConfigManager
    {
        const int SYNC_DEBOUNCE_FRAMES = 15;
        const int SYNC_MAX_DELAY_FRAMES = 60;

        sealed class PendingSync
        {
            public IMyEntity Entity;
            public ScreenProviderConfig Config;
            public long DueFrame;
            public long DeadlineFrame;
        }

        static readonly Dictionary<long, PendingSync> PendingSyncs = new Dictionary<long, PendingSync>();
        static readonly Dictionary<long, ScreenProviderConfig> ReceivedBeforeInstance = new Dictionary<long, ScreenProviderConfig>();
        static readonly List<long> DueKeys = new List<long>();
        static readonly HashSet<long> UnsavedProviders = new HashSet<long>();

        public static void SaveAll()
        {
            try
            {
                foreach (var group in SurfaceScriptBase.Instances
                             .Where(screen => screen?.Block != null && screen.ProviderConfig != null)
                             .GroupBy(screen => screen.Block.EntityId))
                {
                    var screen = group.First();
                    Save(screen.Block, screen.ProviderConfig);
                }
            }
            catch (Exception exception)
            {
                ErrorHandlerHelper.LogError(exception, typeof(ConfigManager));
            }
        }

        public static void Save(IMyEntity storageEntity, ScreenProviderConfig providerConfig)
        {
            if (ScreenProviderConfigStorage.Save(storageEntity, providerConfig) && storageEntity != null)
                UnsavedProviders.Remove(storageEntity.EntityId);
        }

        /// <summary>Devolve true uma única vez se o bloco tem alterações de configuração ainda não gravadas.</summary>
        public static bool TakeUnsaved(long blockId)
        {
            return UnsavedProviders.Remove(blockId);
        }

        /// <summary>
        /// Aplica a configuração localmente na hora e agenda o envio ao servidor. Alterações em rajada
        /// (arrasto de slider) viram um único pacote por bloco.
        /// </summary>
        public static void Sync(IMyEntity storageEntity, ScreenProviderConfig providerConfig)
        {
            if (storageEntity == null || providerConfig == null || !providerConfig.CanWrite)
                return;
            if (!providerConfig.NormalizeComponentSchema())
                return;

            var block = storageEntity as IMyTerminalBlock;
            var apps = GetAppsForBlock(block).ToArray();
            foreach (var app in apps)
                app.UseProviderConfig(providerConfig);
            foreach (var app in apps)
                app.RequestRedraw();

            Save(storageEntity, providerConfig);
            QueueSync(storageEntity, providerConfig);
        }

        public static void Sync(IMyTerminalBlock storageEntity)
        {
            Sync(storageEntity, GetConfigForBlock(storageEntity));
        }

        static void QueueSync(IMyEntity entity, ScreenProviderConfig config)
        {
            var frame = CurrentFrame();
            PendingSync pending;
            if (!PendingSyncs.TryGetValue(entity.EntityId, out pending))
            {
                pending = new PendingSync { Entity = entity, DeadlineFrame = frame + SYNC_MAX_DELAY_FRAMES };
                PendingSyncs[entity.EntityId] = pending;
            }

            pending.Config = config;
            pending.DueFrame = Math.Min(frame + SYNC_DEBOUNCE_FRAMES, pending.DeadlineFrame);
        }

        public static void FlushPendingSyncs(bool force)
        {
            if (PendingSyncs.Count == 0)
                return;

            var frame = CurrentFrame();
            DueKeys.Clear();
            foreach (var entry in PendingSyncs)
            {
                if (force || entry.Value.DueFrame <= frame)
                    DueKeys.Add(entry.Key);
            }

            for (int i = 0; i < DueKeys.Count; i++)
            {
                PendingSync pending;
                if (!PendingSyncs.TryGetValue(DueKeys[i], out pending))
                    continue;

                PendingSyncs.Remove(DueKeys[i]);
                if (pending.Entity == null || pending.Entity.MarkedForClose || pending.Config == null)
                    continue;

                try
                {
                    Transmit(pending.Entity, pending.Config);
                }
                catch (Exception exception)
                {
                    ErrorHandlerHelper.LogError(exception, typeof(ConfigManager));
                }
            }

            DueKeys.Clear();
        }

        static void Transmit(IMyEntity entity, ScreenProviderConfig config)
        {
            var packet = new NetworkPackageSyncComponentConfig(entity.EntityId, config);
            var server = LcdModSessionComponent.Server;
            if (server != null)
            {
                // Host ou single player: o servidor local valida e persiste; só os outros jogadores recebem.
                if (server.HandleSyncConfig(packet, MyAPIGateway.Multiplayer.MyId) && MyAPIGateway.Multiplayer.MultiplayerActive)
                    LcdModSessionComponent.NetworkManager.TransmitToAllPlayers(packet, MyAPIGateway.Multiplayer.MyId);
                return;
            }

            // Eco ao remetente: o servidor fica com a última config e todos, inclusive quem enviou, convergem.
            LcdModSessionComponent.NetworkManager.TransmitToServer(packet, true, true);
        }

        public static bool HasPendingSync(long blockId)
        {
            return PendingSyncs.ContainsKey(blockId);
        }

        /// <summary>
        /// Guarda a configuração recebida do servidor para um bloco que ainda não tem tela instanciada
        /// neste cliente, para que a instância nasça com ela em vez do storage antigo.
        /// </summary>
        internal static void RememberReceivedConfig(long blockId, ScreenProviderConfig config)
        {
            if (config != null)
                ReceivedBeforeInstance[blockId] = config;
        }

        public static void Clear()
        {
            PendingSyncs.Clear();
            ReceivedBeforeInstance.Clear();
            DueKeys.Clear();
            UnsavedProviders.Clear();
        }

        static long CurrentFrame()
        {
            return MyAPIGateway.Session != null ? MyAPIGateway.Session.GameplayFrameCounter : 0L;
        }

        public static void LoadSettings(
            IMyCubeBlock block,
            int index,
            AppType requestedAppType,
            ref ScreenProviderConfig provider,
            out SurfaceConfig screen)
        {
            try
            {
                provider = GetConfigForBlock((IMyTerminalBlock)block);
                if (provider != null)
                {
                    screen = ResolveSurfaceConfig(block, provider, index, requestedAppType);
                    return;
                }

                var storageEntity = (IMyEntity)block;
                if (storageEntity.Storage == null)
                    storageEntity.Storage = new MyModStorageComponent();

                ScreenProviderConfig received;
                if (ReceivedBeforeInstance.TryGetValue(block.EntityId, out received))
                {
                    ReceivedBeforeInstance.Remove(block.EntityId);
                    if (received != null && received.NormalizeComponentSchema())
                    {
                        provider = received;
                        Save(block, provider);
                        screen = ResolveSurfaceConfig(block, provider, index, requestedAppType);
                        return;
                    }
                }

                bool hasUnreadableValue;
                provider = TryLoad(block, out hasUnreadableValue);
                if (provider != null)
                {
                    screen = ResolveSurfaceConfig(block, provider, index, requestedAppType);
                    return;
                }

                CreateSettings(block, index, requestedAppType, out provider, out screen, hasUnreadableValue);
            }
            catch (Exception exception)
            {
                MyAPIGateway.Utilities.ShowNotification(
                    $"Fail to Load Settings for block {block.DisplayNameText}\n{exception.Message}");
                ErrorHandlerHelper.LogError(exception, typeof(ConfigManager));
                CreateSettings(block, index, requestedAppType, out provider, out screen, true);
            }
        }

        public static ScreenProviderConfig TryLoad(IMyCubeBlock block)
        {
            bool hasUnreadableValue;
            return TryLoad(block, out hasUnreadableValue);
        }

        public static ScreenProviderConfig TryLoad(IMyCubeBlock block, out bool hasUnreadableValue)
        {
            var provider = ScreenProviderConfigStorage.TryLoad(block, out hasUnreadableValue);
            if (provider != null && provider.CanWrite && provider.Parent != block.CubeGrid.EntityId)
                provider.SetParent(block);
            return provider;
        }

        public static void CreateSettings(
            IMyCubeBlock block,
            int index,
            AppType requestedAppType,
            out ScreenProviderConfig provider,
            out SurfaceConfig screen)
        {
            CreateSettings(block, index, requestedAppType, out provider, out screen, false);
        }

        /// <summary>
        /// Cria a configuração padrão. Quando o storage tem um valor que não pôde ser lido, a configuração
        /// fica em memória como placeholder e nunca é gravada, para não apagar o que já estava salvo.
        /// </summary>
        static void CreateSettings(
            IMyCubeBlock block,
            int index,
            AppType requestedAppType,
            out ScreenProviderConfig provider,
            out SurfaceConfig screen,
            bool preserveExistingStorage)
        {
            provider = CreateSettings(block);
            screen = ResolveSurfaceConfig(block, provider, index, requestedAppType);
            var colors = screen?.TryGetComponent<ColorConfigComponent>();
            if (colors != null)
            {
                colors.HeaderColor.Clear();
                colors.ErrorColor.Clear();
                colors.WarningColor.Clear();
            }

            if (preserveExistingStorage)
            {
                provider.IsRecoveryPlaceholder = true;
                LogHelper.Log(MyLogSeverity.Warning,
                    "Stored screen config for block {0} could not be read; using defaults without overwriting it.",
                    block.EntityId);
                return;
            }

            Save(block, provider);
        }

        public static ScreenProviderConfig CreateSettings(IMyCubeBlock block)
        {
            return new ScreenProviderConfig(
                block is IMyTextPanel ? 1 : ((IMyTextSurfaceProvider)block).SurfaceCount,
                block as IMyTerminalBlock);
        }

        public static IEnumerable<SurfaceScriptBase> GetAppsForBlock(IMyTerminalBlock block)
        {
            var instances = block == null ? null : SurfaceScriptBase.Instances.GetInstances(block);
            return instances == null ? Enumerable.Empty<SurfaceScriptBase>() : instances.GetInstances();
        }

        public static ScreenProviderConfig GetConfigForBlock(IMyTerminalBlock block)
        {
            foreach (var app in GetAppsForBlock(block))
                return app.ProviderConfig;

            return null;
        }

        static SurfaceConfig ResolveSurfaceConfig(
            IMyCubeBlock block,
            ScreenProviderConfig provider,
            int index,
            AppType requestedAppType)
        {
            if (provider == null || index < 0)
                return null;

            var surface = EnsureSurfaceApp(block, provider, index, requestedAppType);
            if (surface == null)
                return null;
            var runtimeSurface = provider.CanWriteConfig(surface) ? surface : surface.Clone();
            return runtimeSurface;
        }

        public static SurfaceConfig GetSurfaceConfigForCurrentScreen(IMyTerminalBlock block)
        {
            var settings = GetConfigForBlock(block);
            return settings == null ? null : settings.GetSurfaceConfig(GetThisSurfaceIndex(block));
        }

        public static T GetComponentForTerminalApp<T>(IMyTerminalBlock block) where T : ConfigComponent
        {
            return GetComponentForCurrentSurface<T>(block, Constants.APP);
        }

        public static T GetComponentForCurrentSurface<T>(IMyTerminalBlock block, string slot) where T : ConfigComponent
        {
            var surface = GetSurfaceConfigForCurrentScreen(block);
            return surface?.TryGet<T>(slot);
        }

        public static bool ModifyComponentForTerminalApp<T>(IMyTerminalBlock block, Action<T> action)
            where T : ConfigComponent
        {
            return ModifyComponentForCurrentSurface(block, Constants.APP, action);
        }

        public static bool ModifyComponentForCurrentSurface<T>(IMyTerminalBlock block, string slot, Action<T> action)
            where T : ConfigComponent
        {
            if (block == null || action == null)
                return false;

            var settings = GetConfigForBlock(block);
            if (settings == null || !settings.CanWrite)
                return false;

            var surface = settings.GetSurfaceConfig(GetThisSurfaceIndex(block));
            if (!settings.CanWriteConfig(surface))
                return false;

            var component = surface?.TryGet<T>(slot);
            if (component == null)
                return false;

            action(component);
            Sync(block, settings);
            return true;
        }

        /// <summary>
        /// Garante o app da superfície e marca o bloco como não salvo quando o bind alterou a configuração
        /// (troca de script no terminal ou componentes novos), para o Dispose persistir só nesses casos.
        /// </summary>
        public static SurfaceConfig EnsureSurfaceApp(
            IMyCubeBlock block,
            ScreenProviderConfig provider,
            int index,
            AppType requestedAppType)
        {
            if (provider == null)
                return null;

            var before = provider.GetSurfaceConfig(index);
            var appTypeBefore = before != null ? before.AppTypeId : -1;
            var componentsBefore = before != null && before.Components != null ? before.Components.Count : -1;

            provider.EnsureSurfaceApp(index, requestedAppType);

            var surface = provider.GetSurfaceConfig(index);
            var appTypeAfter = surface != null ? surface.AppTypeId : -1;
            var componentsAfter = surface != null && surface.Components != null ? surface.Components.Count : -1;
            if (block != null && (appTypeBefore != appTypeAfter || componentsBefore != componentsAfter))
                UnsavedProviders.Add(block.EntityId);

            return surface;
        }

        public static int GetThisSurfaceIndex(IMyTerminalBlock block)
        {
            var multiTextPanel = block.Components.Get<MyMultiTextPanelComponent>();
            return multiTextPanel?.SelectedPanelIndex ?? 0;
        }
    }
}
