using VRageMath;

namespace LcdMod.Client.Gui.ControlsTemplates.Basic
{
    // ReSharper disable once PartialTypeWithSinglePart
    public partial class Button : RectangleControl
    {
        public Button(RectangleF bounds, ButtonModel model = null)
            : base(bounds, model ?? new ButtonModel())
        {
        }

        public Button(RectangleF bounds, string text)
            : this(bounds, new ButtonModel { Text = text })
        {
        }

        public Button(RectangleF bounds, object dataContext)
            : base(bounds, dataContext)
        {
        }

        protected override Color GetRenderBackgroundColor()
        {
            return BackgroundColor;
        }

        protected override bool ShouldRenderStyleBorder()
        {
            return true;
        }

    }
}
