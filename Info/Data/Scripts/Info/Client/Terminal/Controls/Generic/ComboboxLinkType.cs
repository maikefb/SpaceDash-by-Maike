using System.Collections.Generic;
using Generated;
using LcdMod.Client.Config;
using LcdMod.Client.Helpers;
using LcdMod.Common.Config.Components;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using static LcdMod.Common.Helpers.Constants;

namespace LcdMod.Client.Terminal.Controls.Generic
{
    public partial class ComboboxLinkType : TerminalControlsWrapper
    {
        public override IMyTerminalControl TerminalControl { get; }

        public ComboboxLinkType()
        {
            var combo = CreateControl<IMyTerminalControlCombobox>("LinkTypeCombobox");
            combo.Getter = Getter;
            combo.Setter = Setter;
            combo.ComboBoxContent = Content;
            combo.Visible = Visible;
            combo.Title = MyStringId.GetOrCompute(MOD_PREFIX + "LinkType");
            combo.Tooltip = MyStringId.GetOrCompute(string.Format(
                LocHelper.GetLoc(MOD_PREFIX + "LinkTypeDescription"),
                LocHelper.GetLoc(MOD_PREFIX + "LocalGrid"),
                LocHelper.GetLoc(MOD_PREFIX + "MechanicalConnection"),
                LocHelper.GetLoc(MOD_PREFIX + "PhysicalConnection"),
                LocHelper.GetLoc(MOD_PREFIX + "ElectricalConnection")
            ));
            TerminalControl = combo;
        }

        static void Content(List<MyTerminalControlComboBoxItem> list)
        {
            list.Add(new MyTerminalControlComboBoxItem
            {
                Key = 0, Value = MyStringId.GetOrCompute(MOD_PREFIX + "LocalGrid")
            });
            list.Add(new MyTerminalControlComboBoxItem
            {
                Key = ToId(GridLinkTypeEnum.Mechanical),
                Value = MyStringId.GetOrCompute(MOD_PREFIX + "MechanicalConnection")
            });
            list.Add(new MyTerminalControlComboBoxItem
            {
                Key = ToId(GridLinkTypeEnum.Physical),
                Value = MyStringId.GetOrCompute(MOD_PREFIX + "PhysicalConnection")
            });
            list.Add(new MyTerminalControlComboBoxItem
            {
                Key = ToId(GridLinkTypeEnum.Electrical),
                Value = MyStringId.GetOrCompute(MOD_PREFIX + "ElectricalConnection")
            });
        }

        long Getter(IMyTerminalBlock block)
        {
            int value = GetGridLinkTypeInternal(block, 3);
            if (value == (int)GridLinkTypeEnum.Physical && IsEnergyApp(block))
                value = (int)GridLinkTypeEnum.Electrical;
            return ToId((GridLinkTypeEnum)value);
        }

        void Setter(IMyTerminalBlock block, long value)
        {
            var gridLinkType = FromId(value);
            if (gridLinkType == (int)GridLinkTypeEnum.Physical && IsEnergyApp(block))
                gridLinkType = (int)GridLinkTypeEnum.Electrical;
            if (ConfigManager.ModifyComponentForCurrentSurface<BlockSelectionConfigComponent>(
                    block,
                    BLOCKS,
                    config => config.GridLinkTypeInternal = gridLinkType))
                return;

            ConfigManager.ModifyComponentForTerminalApp<PowerConfigComponent>(
                block,
                config => config.GridLinkTypeInternal = gridLinkType);
        }

        static long ToId(GridLinkTypeEnum enumValue)
        {
            switch (enumValue)
            {
                case GridLinkTypeEnum.Mechanical:
                    return 1;
                case GridLinkTypeEnum.Physical:
                    return 2;
                case GridLinkTypeEnum.Electrical:
                    return 3;
            }

            return 0;
        }

        int FromId(long value)
        {
            switch (value)
            {
                case 1:
                    return (int)GridLinkTypeEnum.Mechanical;
                case 2:
                    return (int)GridLinkTypeEnum.Physical;
                case 3:
                    return (int)GridLinkTypeEnum.Electrical;
            }

            return -1;
        }

        /// <summary>Energia só flui por juntas mecânicas e conectores acoplados: nas telas de energia "Física" é gravada como "Elétrica".</summary>
        static bool IsEnergyApp(IMyTerminalBlock block)
        {
            var surface = ConfigManager.GetSurfaceConfigForCurrentScreen(block);
            AppType appType;
            return surface != null && AppSchemaRegistry.TryNormalizeAppType(surface.AppTypeId, out appType) && AppSchemaRegistry.IsEnergyApp(appType);
        }

        static int GetGridLinkTypeInternal(IMyTerminalBlock block, int defaultValue)
        {
            var blocks = ConfigManager.GetComponentForCurrentSurface<BlockSelectionConfigComponent>(
                block,
                BLOCKS);
            if (blocks != null)
                return blocks.GridLinkTypeInternal;

            var power = ConfigManager.GetComponentForTerminalApp<PowerConfigComponent>(block);
            return power != null ? power.GridLinkTypeInternal : defaultValue;
        }
    }
}
