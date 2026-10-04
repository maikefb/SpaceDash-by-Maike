using System;
using System.Collections.Generic;
using System.Linq;
using LcdMod.Client.Apps;
using LcdMod.Client.Config;
using LcdMod.Client.Helpers;
using LcdMod.Client.SurfaceScripts;
using LcdMod.Common.Helpers;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
using static LcdMod.Common.Helpers.Constants;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;

namespace LcdMod.Client.Terminal.Controls.Blueprint
{
    /// <summary>
    /// Enfileira nas montadoras do grid os componentes que ainda faltam para a projeção mostrada na tela.
    /// Substitui o botão "Craft all" que era desenhado no próprio LCD.
    /// </summary>
    public sealed partial class ButtonProjectorCraftMissing : TerminalControlsWrapper
    {
        readonly List<IMyCubeGrid> _grids = new List<IMyCubeGrid>();
        readonly List<IMySlimBlock> _blocks = new List<IMySlimBlock>();
        readonly List<IMyAssembler> _assemblers = new List<IMyAssembler>();
        readonly List<IMyAssembler> _capable = new List<IMyAssembler>();

        public override IMyTerminalControl TerminalControl { get; }

        public ButtonProjectorCraftMissing()
        {
            var button = CreateControl<IMyTerminalControlButton>("ProjectorCraftMissing");
            button.Action = Apply;
            button.Enabled = Enabled;
            button.Visible = Visible;
            button.Title = MyStringId.GetOrCompute(MOD_PREFIX + "Projector_CraftMissing");
            button.Tooltip = MyStringId.GetOrCompute(MOD_PREFIX + "Projector_CraftMissing_Tooltip");
            TerminalControl = button;
        }

        bool Enabled(IMyTerminalBlock block)
        {
            var app = GetProjectorApp(block);
            return app != null && app.MissingComponents > 0;
        }

        void Apply(IMyTerminalBlock block)
        {
            Apply(block, GetProjectorApp(block));
        }

        void Apply(IMyTerminalBlock block, ProjectorApp app)
        {
            if (app == null || app.MissingComponents <= 0)
                return;

            try
            {
                CollectAssemblers(block.CubeGrid);
                foreach (var missing in app.ComponentMissing)
                {
                    if (missing.Value > 0d)
                        Queue(missing.Key, (int)Math.Ceiling(missing.Value));
                }
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, typeof(ButtonProjectorCraftMissing));
            }
        }

        public bool HasProjectorScreen(IMyTerminalBlock block)
        {
            if (block == null)
                return false;
            foreach (var screen in ConfigManager.GetAppsForBlock(block))
                if (screen is ProjectorLcdSurfaceScript)
                    return true;
            return false;
        }

        public int MissingOnAnyScreen(IMyTerminalBlock block)
        {
            int missing = 0;
            if (block == null)
                return 0;
            foreach (var screen in ConfigManager.GetAppsForBlock(block))
            {
                var app = (screen as ProjectorLcdSurfaceScript)?.App as ProjectorApp;
                if (app != null)
                    missing += Math.Max(0, app.MissingComponents);
            }
            return missing;
        }

        /// <summary>Ação de barra: o bloco pode ter mais de uma tela de projeção; enfileira o que falta em todas.</summary>
        public void ApplyToAllScreens(IMyTerminalBlock block)
        {
            if (block == null)
                return;
            foreach (var screen in ConfigManager.GetAppsForBlock(block))
            {
                var app = (screen as ProjectorLcdSurfaceScript)?.App as ProjectorApp;
                if (app != null)
                    Apply(block, app);
            }
        }

        ProjectorApp GetProjectorApp(IMyTerminalBlock block)
        {
            if (block == null)
                return null;

            var surfaceIndex = GetThisSurfaceIndex(block);
            foreach (var screen in ConfigManager.GetAppsForBlock(block))
            {
                if (screen.RotationOrSurfaceIndex != surfaceIndex)
                    continue;

                var projectorScreen = screen as ProjectorLcdSurfaceScript;
                return projectorScreen != null ? projectorScreen.App as ProjectorApp : null;
            }

            return null;
        }

        void CollectAssemblers(IMyCubeGrid rootGrid)
        {
            _assemblers.Clear();
            _grids.Clear();
            if (rootGrid == null)
                return;

            MyAPIGateway.GridGroups.GetGroup(rootGrid, GridLinkTypeEnum.Physical, _grids);
            if (!_grids.Contains(rootGrid))
                _grids.Add(rootGrid);

            for (int i = 0; i < _grids.Count; i++)
            {
                _blocks.Clear();
                _grids[i].GetBlocks(_blocks, slim => slim.FatBlock is IMyAssembler);
                for (int j = 0; j < _blocks.Count; j++)
                {
                    var assembler = (IMyAssembler)_blocks[j].FatBlock;
                    if (assembler.IsFunctional && assembler.Enabled &&
                        assembler.Mode == Sandbox.ModAPI.Ingame.MyAssemblerMode.Assembly)
                        _assemblers.Add(assembler);
                }
            }
        }

        void Queue(MyItemType itemType, int requestedItems)
        {
            _capable.Clear();
            MyBlueprintDefinitionBase blueprint = null;
            bool hasFullSpeed = false;
            for (int i = 0; i < _assemblers.Count; i++)
            {
                MyBlueprintDefinitionBase candidate;
                if (!AssemblerBlueprintCatalog.TryGetBlueprint(_assemblers[i], itemType, out candidate))
                    continue;

                blueprint = blueprint ?? candidate;
                _capable.Add(_assemblers[i]);
                hasFullSpeed |= GetAssemblySpeed(_assemblers[i]) >= 1f;
            }

            if (blueprint == null || _capable.Count == 0)
                return;

            // Mesmo critério do diálogo original: montadoras básicas só entram quando não há outra.
            if (hasFullSpeed)
                _capable.RemoveAll(a => GetAssemblySpeed(a) < 1f);

            int count = _capable.Count;
            int baseShare = requestedItems / count;
            int remainder = requestedItems % count;
            for (int i = 0; i < count; i++)
            {
                int items = baseShare + (i < remainder ? 1 : 0);
                if (items > 0)
                    _capable[i].AddQueueItem(blueprint.Id, ToQueueAmount(blueprint, itemType, items));
            }
        }

        static double ToQueueAmount(MyBlueprintDefinitionBase blueprint, MyDefinitionId itemId, double requestedItems)
        {
            var results = blueprint.Results;
            for (int i = 0; results != null && i < results.Length; i++)
            {
                if (!results[i].Id.Equals(itemId))
                    continue;

                var resultAmount = (double)results[i].Amount;
                return resultAmount <= 0d ? requestedItems : Math.Ceiling(requestedItems / resultAmount);
            }

            return requestedItems;
        }

        static float GetAssemblySpeed(IMyAssembler assembler)
        {
            var definition = MyDefinitionManager.Static.GetCubeBlockDefinition(assembler.BlockDefinition) as MyAssemblerDefinition;
            return definition != null ? definition.AssemblySpeed : 1f;
        }
    }
}
