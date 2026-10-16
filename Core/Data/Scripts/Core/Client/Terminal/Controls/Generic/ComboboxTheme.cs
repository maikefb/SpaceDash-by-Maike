using System.Collections.Generic;
using LcdMod.Client.Config;
using LcdMod.Common.Config.Components;
using LcdMod.Common.Helpers;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.ModAPI;
using VRage.Utils;

namespace LcdMod.Client.Terminal.Controls.Generic
{
    public sealed partial class ComboboxTheme : TerminalControlsWrapper
    {
        public override IMyTerminalControl TerminalControl { get; }

        public ComboboxTheme()
        {
            var combobox = CreateControl<IMyTerminalControlCombobox>("ComboboxTheme");
            combobox.Getter = Getter;
            combobox.ComboBoxContent = Content;
            combobox.Setter = Setter;
            combobox.Visible = Visible;
            combobox.Title = MyStringId.GetOrCompute("LcdMod_Theme");
            combobox.Tooltip = MyStringId.GetOrCompute("LcdMod_Theme_Tooltip");

            TerminalControl = combobox;
        }

        static void Content(List<MyTerminalControlComboBoxItem> items)
        {
            items.Add(new MyTerminalControlComboBoxItem { Key = (long)LcdTheme.Default, Value = MyStringId.GetOrCompute("LcdMod_Theme_Default") });
            items.Add(new MyTerminalControlComboBoxItem { Key = (long)LcdTheme.Futuristic, Value = MyStringId.GetOrCompute("LcdMod_Theme_Futuristic") });
            items.Add(new MyTerminalControlComboBoxItem { Key = (long)LcdTheme.Clean, Value = MyStringId.GetOrCompute("LcdMod_Theme_Clean") });
            items.Add(new MyTerminalControlComboBoxItem { Key = (long)LcdTheme.Cafe, Value = MyStringId.GetOrCompute("LcdMod_Theme_Cafe") });
            items.Add(new MyTerminalControlComboBoxItem { Key = (long)LcdTheme.Small, Value = MyStringId.GetOrCompute("LcdMod_Theme_Small") });
        }

        void Setter(IMyTerminalBlock block, long value)
        {
            ConfigManager.ModifyComponentForCurrentSurface<GeneralConfigComponent>(
                block,
                Constants.GENERAL,
                config => config.ThemeInternal = (int)value);
        }

        long Getter(IMyTerminalBlock block)
        {
            var config = ConfigManager.GetComponentForCurrentSurface<GeneralConfigComponent>(
                block,
                Constants.GENERAL);
            return (long)config.GetTheme();
        }
    }
}
