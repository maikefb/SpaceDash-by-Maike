using LcdMod.Client.Config;
using LcdMod.Client.Terminal.Controls.Filter.Listbox;
using LcdMod.Common.Config.Components;
using LcdMod.Common.Config.Models;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Utils;

namespace LcdMod.Client.Terminal.Controls.Filter.Buttons
{
    public abstract class TerminalControlFilterButton : TerminalControlFilter
    {
        protected readonly TerminalControlsListbox SourceList;
        protected readonly TerminalControlsListbox TargetList;
        
        public override IMyTerminalControl TerminalControl => _terminalControl;
        IMyTerminalControl _terminalControl;
        
        protected TerminalControlFilterButton(TerminalControlsListbox sourceList, TerminalControlsListbox targetList)
        {
            SourceList = sourceList;
            TargetList = targetList;
        }

        protected void CreateButton(string id, string title)
        {
            var button = CreateControl<IMyTerminalControlButton>(id);
            button.Action = Action;
            button.Visible = Visible;
            button.Title = MyStringId.GetOrCompute(title);
            _terminalControl = button;
        }

        protected abstract void Action(IMyTerminalBlock block);

        protected bool TryBeginEdit(IMyTerminalBlock block, out ScreenProviderConfig settings, out SurfaceConfig surface)
        {
            settings = ConfigManager.GetConfigForBlock(block);
            surface = settings == null ? null : settings.GetSurfaceConfig(GetThisSurfaceIndex(block));
            return settings != null && settings.CanWriteConfig(surface);
        }

        protected void FinishEdit(IMyTerminalBlock block, ScreenProviderConfig settings)
        {
            SourceList.TerminalControl.UpdateVisual();
            TargetList.TerminalControl.UpdateVisual();
            ConfigManager.Sync(block, settings);
        }
    }
}
