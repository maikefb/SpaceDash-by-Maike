using VRageMath;

namespace LcdMod.Client.Gui.ControlsTemplates
{
    public class RectangleControl : ControlTemplate
    {
        public RectangleControl(RectangleF bounds, object dataContext = null)
            : base(dataContext)
        {
            Rect = bounds;
        }

        public RectangleF Rect { get; private set; }

        public virtual void SetRect(RectangleF bounds)
        {
            if (Rect.Equals(bounds) && !IsLayoutDirty)
                return;

            Rect = bounds;
            ValidateLayout();
            MarkDirty();
        }

        public override RectangleF Bounds => GetRenderBounds(Rect);

        public override void Arrange(RectangleF bounds)
        {
            SetRect(bounds);
        }
    }
}
