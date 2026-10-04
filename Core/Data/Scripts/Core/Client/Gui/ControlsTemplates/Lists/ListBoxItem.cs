using System.Collections.Generic;
using LcdMod.Client.Gui.ControlsTemplates.Panels;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace LcdMod.Client.Gui.ControlsTemplates.Lists
{
    public sealed class ListBoxItem<T> : RectangleControl
    {
        public ListBoxItem(RectangleF bounds, ListBoxItemModel<T> model)
            : base(bounds, model)
        {
        }

        public ListBoxItemModel<T> ItemModel => DataContext as ListBoxItemModel<T>;

        protected override void RenderDefault(List<MySprite> sprites)
        {
            var model = ItemModel;
            var owner = model != null ? model.Owner : null;
            var rect = GetViewBox();

            BorderRenderer.CreateSpritesFromRect(
                rect,
                sprites,
                GetRenderBackgroundColor(),
                GetRenderBorderRadiusPixels(),
                LayoutScale);

            if (model != null && owner != null && owner.ItemRenderer != null)
            {
                owner.ItemRenderer(this, model.Item, sprites);
                return;
            }

            RenderDefaultText(rect, sprites, GetRenderTextColor());
        }
    }
}
