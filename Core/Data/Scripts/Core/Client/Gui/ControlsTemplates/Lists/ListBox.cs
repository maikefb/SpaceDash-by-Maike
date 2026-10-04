using System;
using System.Collections.Generic;
using LcdMod.Client.Gui.ControlsTemplates.Panels;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace LcdMod.Client.Gui.ControlsTemplates.Lists
{
    public sealed class ListBox<T> : RectangleControl
    {
        readonly ScrollPanel _scrollPanel;
        readonly Dictionary<int, ListBoxItemModel<T>> _rowModelsByIndex =
            new Dictionary<int, ListBoxItemModel<T>>();
        readonly Dictionary<int, ListBoxItem<T>> _rowControlsByIndex =
            new Dictionary<int, ListBoxItem<T>>();
        readonly List<int> _rowIndexesToRemove = new List<int>();
        ListBoxModel<T> _cachedListModel;
        bool _rowsBuilt;
        int _rowsStart;
        int _rowsRender;
        int _rowsCount;
        float _rowsRowHeight;
        RectangleF _rowsViewport;

        public ListBox(RectangleF bounds, ListBoxModel<T> model = null)
            : base(bounds, model ?? new ListBoxModel<T>())
        {
            _scrollPanel = new ScrollPanel();
            AddChild(_scrollPanel);
            ConfigureScrollPanel();
        }

        public ListBoxModel<T> ListModel => DataContext as ListBoxModel<T>;

        public ScrollPanel ScrollPanel => _scrollPanel;

        public override void SetRect(RectangleF bounds)
        {
            base.SetRect(bounds);
            ConfigureScrollPanel();
        }

        protected override void RenderDefault(List<MySprite> sprites)
        {
            ConfigureScrollPanel();

            var viewBox = GetViewBox();
            var backgroundColor = GetRenderBackgroundColor();
            BorderRenderer.CreateSpritesFromRect(viewBox, sprites, backgroundColor,
                BorderRenderer.ScaleRadius(GetRenderBorderRadiusPixels(), LayoutScale));

            BeginContentClip(sprites, _scrollPanel.ContentViewportBounds);
            RenderRows(sprites);
            EndContentClip(sprites);

            _scrollPanel.Render(sprites);
        }

        void ConfigureScrollPanel()
        {
            var model = ListModel;
            float rowHeight = model != null && model.RowHeight > 0f ? model.RowHeight : 32f;
            float scrollerWidth = model != null && model.ScrollerWidthPixels > 0f ? model.ScrollerWidthPixels : 6f;
            int count = model?.Count ?? 0;

            var viewBox = GetViewBox();
            _scrollPanel.Configure(viewBox, viewBox.Y, 0f, rowHeight, count, scrollerWidth, 0f);
            RebuildVisibleRows();
        }


        void RebuildVisibleRows()
        {
            var model = ListModel;
            if (!ReferenceEquals(_cachedListModel, model))
            {
                ClearRowCache();
                _cachedListModel = model;
            }

            if (model == null || model.Count <= 0)
            {
                ClearRowCache();
                return;
            }

            int start = _scrollPanel.StartRow;
            int renderRows = _scrollPanel.RenderRows;
            var viewport = _scrollPanel.ContentViewportBounds;
            if (_rowsBuilt && !_isDirty &&
                _rowsStart == start && _rowsRender == renderRows && _rowsCount == model.Count &&
                _rowsRowHeight == _scrollPanel.RowHeight &&
                _rowsViewport.X == viewport.X && _rowsViewport.Y == viewport.Y &&
                _rowsViewport.Width == viewport.Width && _rowsViewport.Height == viewport.Height)
                return;

            _rowsBuilt = true;
            _rowsStart = start;
            _rowsRender = renderRows;
            _rowsCount = model.Count;
            _rowsRowHeight = _scrollPanel.RowHeight;
            _rowsViewport = viewport;

            int end = Math.Min(model.Count, start + renderRows);
            PruneRowCache(start, end);

            for (int itemIndex = start; itemIndex < end; itemIndex++)
            {
                int visibleIndex = itemIndex - start;
                var rowBounds = new RectangleF(
                    _scrollPanel.ContentViewportBounds.X,
                    _scrollPanel.ContentBounds.Y + visibleIndex * _scrollPanel.RowHeight,
                    _scrollPanel.ContentViewportBounds.Width,
                    _scrollPanel.RowHeight);

                ListBoxItemModel<T> itemModel;
                if (!_rowModelsByIndex.TryGetValue(itemIndex, out itemModel))
                {
                    itemModel = new ListBoxItemModel<T>(model, model.GetItem(itemIndex), itemIndex);
                    _rowModelsByIndex[itemIndex] = itemModel;
                }
                else
                {
                    itemModel.Update(model, model.GetItem(itemIndex), itemIndex);
                }

                ListBoxItem<T> item;
                if (!_rowControlsByIndex.TryGetValue(itemIndex, out item))
                {
                    item = new ListBoxItem<T>(rowBounds, itemModel);
                    _rowControlsByIndex[itemIndex] = item;
                }
                else
                {
                    item.SetRect(rowBounds);
                    item.SetDataContext(itemModel);
                }

                item.BorderRadiusPixels = BorderRadiusPixels;
                item.Padding = Padding;
                item.SetStyleId(model.ItemStyleIdSelector?.Invoke(itemModel.Item));
                var itemClass = "ControlBase Button Row";
                var additionalClass = model.ItemClassSelector?.Invoke(itemModel.Item, itemIndex);
                if (!string.IsNullOrEmpty(additionalClass))
                    itemClass += " "+ additionalClass;
                item.SetClass(itemClass);

                if (!ReferenceEquals(item.Parent, _scrollPanel))
                    _scrollPanel.AddChild(item);
            }
        }

        void PruneRowCache(int start, int end)
        {
            _rowIndexesToRemove.Clear();
            foreach (var pair in _rowModelsByIndex)
            {
                if (pair.Key < start || pair.Key >= end)
                    _rowIndexesToRemove.Add(pair.Key);
            }

            for (int i = 0; i < _rowIndexesToRemove.Count; i++)
            {
                int key = _rowIndexesToRemove[i];
                ListBoxItem<T> item;
                if (_rowControlsByIndex.TryGetValue(key, out item) && item != null)
                    _scrollPanel.RemoveChild(item);

                ListBoxItemModel<T> itemModel;
                if (_rowModelsByIndex.TryGetValue(key, out itemModel))
                    itemModel.UnbindItem();

                _rowModelsByIndex.Remove(key);
                _rowControlsByIndex.Remove(key);
            }

            _rowIndexesToRemove.Clear();
        }

        void ClearRowCache()
        {
            _rowsBuilt = false;
            foreach (var model in _rowModelsByIndex.Values)
                model?.UnbindItem();

            foreach (var item in _rowControlsByIndex.Values)
            {
                if (item != null)
                    _scrollPanel.RemoveChild(item);
            }

            _rowModelsByIndex.Clear();
            _rowControlsByIndex.Clear();
            _rowIndexesToRemove.Clear();
        }

        void RenderRows(List<MySprite> sprites)
        {
            var children = _scrollPanel.VisualChildren;
            if (children == null)
                return;

            for (int i = 0; i < children.Count; i++)
            {
                var item = children[i] as ListBoxItem<T>;
                item?.Render(sprites);
            }
        }
    }
}
