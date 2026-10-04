namespace LcdMod.Client.Terminal.Controls.Generic
{
    public partial class SliderBrightness
    {
        public override bool VisibleForScript(string script) => true;
    }

    public partial class TextSurfaceControlsVisibility
    {
        public override bool VisibleForScript(string script) =>
            script != null && script.StartsWith(LcdMod.Common.Helpers.Constants.ID_PREFIX, System.StringComparison.Ordinal);
    }
}

namespace LcdMod.Client.Terminal.Controls.Scale
{
    public partial class SliderScale
    {
        public override bool VisibleForScript(string script) => true;
    }
}
