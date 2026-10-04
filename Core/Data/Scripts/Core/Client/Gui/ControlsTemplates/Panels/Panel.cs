using System.Collections.Generic;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace LcdMod.Client.Gui.ControlsTemplates.Panels
{
    /// <summary>
    /// Non-visual rectangular container for child controls.
    /// </summary>
    public class Panel : ControlTemplate
    {
        public Panel()
            : this(default(RectangleF))
        {
        }

        public Panel(RectangleF bounds, object dataContext = null)
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
            ArrangeChildren();
            ValidateLayout();
            MarkDirty();
        }

        public override RectangleF Bounds => Rect;

        public override void Arrange(RectangleF bounds)
        {
            SetRect(bounds);
        }

        protected virtual void ArrangeChildren()
        {
        }

        protected void EnsureLayout()
        {
            if (!IsLayoutDirty)
                return;

            ArrangeChildren();
            ValidateLayout();
        }

        protected override void RenderDefault(List<MySprite> sprites)
        {
            EnsureLayout();
            RenderChildren(sprites);
        }

        protected void RenderChildren(List<MySprite> sprites)
        {
            var children = VisualChildren;
            if (children == null)
                return;

            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i] as ControlTemplate;
                if (child != null)
                    child.Render(sprites);
            }
        }
    }
}
