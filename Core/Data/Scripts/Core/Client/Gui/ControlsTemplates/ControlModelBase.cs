using LcdMod.Common.Mvvm;

namespace LcdMod.Client.Gui.ControlsTemplates
{
    public abstract class ControlModelBase : ObservableObject
    {
        public InteractiveRenderHandler CustomRender { get; set; }
    }
}
