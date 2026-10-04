using System;
using System.Collections.Generic;
using LcdMod.Client.Gui.Styling;
using LcdMod.Client.Gui.ControlsTemplates.Panels.Virtualized;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace LcdMod.Client.Gui.ControlsTemplates.Panels
{
    public sealed partial class ScrollPanel : ControlTemplate
    {
        public const float DEFAULT_SCROLLER_WIDTH_PIXELS = 5f;
        const float SCROLLBAR_CONTENT_MARGIN_RATIO = 0.5f;

        public static readonly StyleProperty<Color> ScrollBarTrackColorProperty =
            StyleProperty.Register<ScrollPanel, Color>("ScrollBarTrackColor", null);

        public static readonly StyleProperty<Color> ScrollBarThumbColorProperty =
            StyleProperty.Register<ScrollPanel, Color>("ScrollBarThumbColor", null);

        Panel _content;
        bool _automaticContentMode;
        bool _manualConfigured;
        float _contentExtentHeightPixels;
        float _configuredAutomaticScrollerWidthPixels = DEFAULT_SCROLLER_WIDTH_PIXELS;
        float? _localAutoScrollSecondsPerStep;
        float _resolvedAutoScrollSecondsPerStep;

        public ScrollPanel(object dataContext = null)
            : base(dataContext)
        {
        }

        public override void ClearChildren()
        {
            _content = null;
            _automaticContentMode = false;
            _manualConfigured = false;
            base.ClearChildren();
        }

        public RectangleF PanelBounds { get; private set; }
        public RectangleF ContentViewportBounds { get; private set; }
        public RectangleF ContentBounds { get; private set; }
        public float RowHeight { get; private set; }
        public float ScrollerWidthPixels { get; private set; }
        public float ScrollOffsetPixels { get; private set; }
        public float RowOffsetPixels { get; private set; }
        public Action<ScrollPanel> ScrollChanged { get; set; }
        public int TotalRows { get; private set; }
        public int MaxVisibleRows { get; private set; }
        public int RenderRows { get; private set; }
        public int StartRow { get; private set; }
        public bool IsScrollable { get; private set; }

        protected override bool ClipContent => true;

        protected override RectangleF ClipContentBounds => ContentViewportBounds;

        public override RectangleF Bounds => PanelBounds;

        public override void AddChild(ControlTemplate child)
        {
            var panel = child as Panel;
            if (!_automaticContentMode && !_manualConfigured && panel != null)
            {
                SetContent(panel);
                return;
            }

            base.AddChild(child);
        }

        public bool RemoveChild(ControlTemplate child)
        {
            if (ReferenceEquals(child, _content))
            {
                _content = null;
                _automaticContentMode = false;
            }

            return base.RemoveChild(child);
        }

        public void SetContent(Panel content)
        {
            if (ReferenceEquals(_content, content))
                return;

            if (_content != null)
                base.RemoveChild(_content);

            _content = content;
            _automaticContentMode = _content != null;
            if (_automaticContentMode)
                _manualConfigured = false;

            if (_content != null)
                base.AddChild(_content);

            InvalidateLayout();
        }

        public override void Arrange(RectangleF bounds)
        {
            if (!PanelBounds.Equals(bounds) || IsLayoutDirty)
            {
                PanelBounds = bounds;
                InvalidateLayout();
            }

            EnsureAutomaticLayout();
        }

        public void ConfigureAutomatic(RectangleF bounds, float scrollerWidthPixels, float scrollStepPixels)
        {
            var normalizedScrollerWidth = Math.Max(0f, scrollerWidthPixels);
            var normalizedRowHeight = Math.Max(1f, scrollStepPixels);
            bool unchanged =
                PanelBounds.Equals(bounds) &&
                Math.Abs(_configuredAutomaticScrollerWidthPixels - normalizedScrollerWidth) <= 0.0001f &&
                Math.Abs(RowHeight - normalizedRowHeight) <= 0.0001f &&
                _automaticContentMode == (_content != null) &&
                !_manualConfigured &&
                !IsLayoutDirty;

            if (unchanged)
                return;

            PanelBounds = bounds;
            _configuredAutomaticScrollerWidthPixels = normalizedScrollerWidth;
            RowHeight = normalizedRowHeight;
            UpdateResolvedAutoScrollSecondsPerStep();
            _automaticContentMode = _content != null;
            _manualConfigured = false;
            InvalidateLayout();
            EnsureAutomaticLayout();
        }

        internal void Configure(RectangleF viewBox, float contentTop, float footerHeight, float rowHeight, int totalRows, float scrollerWidthPixels, float autoScrollSecondsPerStep)
        {
            SetAutoScrollSecondsPerStep(autoScrollSecondsPerStep);

            _automaticContentMode = false;
            _manualConfigured = true;
            RowHeight = Math.Max(1f, rowHeight);
            ScrollerWidthPixels = Math.Max(0f, scrollerWidthPixels);
            TotalRows = Math.Max(0, totalRows);

            float viewportHeight = Math.Max(0f, viewBox.Bottom - contentTop - Math.Max(0f, footerHeight));
            MaxVisibleRows = Math.Max(1, (int)Math.Floor(viewportHeight / RowHeight));

            _contentExtentHeightPixels = TotalRows * RowHeight;
            IsScrollable = _contentExtentHeightPixels > viewportHeight + 0.001f;
            PanelBounds = new RectangleF(viewBox.X, contentTop, viewBox.Width, viewportHeight);

            float contentGutterWidth = IsScrollable ? ScrollerWidthPixels + GetScrollbarContentMarginPixels() : 0f;
            float contentWidth = Math.Max(1f, viewBox.Width - contentGutterWidth);
            ContentViewportBounds = new RectangleF(viewBox.X, contentTop, contentWidth, viewportHeight);

            ScrollOffsetPixels = GetCurrentScrollOffset();
            UpdateRowState();

            int renderRowsForViewport = Math.Max(1, (int)Math.Ceiling((viewportHeight + RowOffsetPixels) / RowHeight) + 1);
            RenderRows = TotalRows == 0 ? 0 : Math.Min(TotalRows - StartRow, renderRowsForViewport);

            ContentBounds = new RectangleF(viewBox.X, contentTop - RowOffsetPixels, contentWidth, RenderRows * RowHeight);
        }

        void SetAutoScrollSecondsPerStep(float secondsPerStep)
        {
            var normalized = NormalizeAutoScrollSecondsPerStep(secondsPerStep);
            if (_localAutoScrollSecondsPerStep.HasValue &&
                Math.Abs(_localAutoScrollSecondsPerStep.Value - normalized) <= 0.0001f)
                return;

            _localAutoScrollSecondsPerStep = normalized;
            UpdateResolvedAutoScrollSecondsPerStep();
        }

        public bool UpdateAutoScroll()
        {
            UpdateResolvedAutoScrollSecondsPerStep();
            if (!IsAutoScrolling())
                return false;

            float nextOffset = GetAutoScrollOffset(GetMaxScrollOffsetPixels());
            if (Math.Abs(ScrollOffsetPixels - nextOffset) <= 0.001f)
                return false;

            InvalidateScrollState();
            return true;
        }

        void UpdateResolvedAutoScrollSecondsPerStep()
        {
            float secondsPerStep = _localAutoScrollSecondsPerStep ?? GetResourceAutoScrollSecondsPerStep();
            if (Math.Abs(_resolvedAutoScrollSecondsPerStep - secondsPerStep) <= 0.0001f)
                return;

            _resolvedAutoScrollSecondsPerStep = secondsPerStep;
            InvalidateScrollState();
        }

        void InvalidateScrollState()
        {
            if (_automaticContentMode)
                InvalidateLayout();
            else
                MarkDirty();
        }

        float GetResourceAutoScrollSecondsPerStep()
        {
            float value;
            return ScopedResourceResolver.TryResolve(this, ThemeResources.AutoScrollSecondsPerStep, out value)
                ? NormalizeAutoScrollSecondsPerStep(value)
                : 0f;
        }

        static float NormalizeAutoScrollSecondsPerStep(float secondsPerStep)
        {
            if (float.IsNaN(secondsPerStep) || float.IsInfinity(secondsPerStep))
                return 0f;

            return secondsPerStep > 0f ? Math.Max(0.2f, secondsPerStep) : 0f;
        }

        void EnsureAutomaticLayout()
        {
            if (_automaticContentMode && IsLayoutDirty)
                ArrangeAutomaticContent();
        }

        void ArrangeAutomaticContent()
        {
            UpdateResolvedAutoScrollSecondsPerStep();
            ScrollerWidthPixels = Math.Max(0f, _configuredAutomaticScrollerWidthPixels);

            if (_content == null)
            {
                ContentViewportBounds = PanelBounds;
                ContentBounds = PanelBounds;
                TotalRows = 0;
                MaxVisibleRows = 0;
                RenderRows = 0;
                StartRow = 0;
                RowOffsetPixels = 0f;
                ScrollOffsetPixels = 0f;
                _contentExtentHeightPixels = 0f;
                IsScrollable = false;
                ValidateLayout();
                return;
            }

            var scrollContent = _content as IScrollContent;
            var showScrollBar = false;
            var desired = Vector2.Zero;
            var viewport = default(RectangleF);

            for (var pass = 0; pass < 4; pass++)
            {
                viewport = CalculateAutomaticViewport(showScrollBar);
                desired = scrollContent != null ? scrollContent.MeasureContent(viewport.Size) : _content.Measure(viewport.Size);
                var nextShowScrollBar = desired.Y > viewport.Height + 0.001f;
                if (nextShowScrollBar == showScrollBar)
                    break;

                showScrollBar = nextShowScrollBar;
            }

            ContentViewportBounds = viewport;
            _contentExtentHeightPixels = Math.Max(0f, desired.Y);
            IsScrollable = showScrollBar;
            RowHeight = Math.Max(1f, RowHeight);
            ScrollOffsetPixels = GetCurrentScrollOffset();
            UpdateRowState();
            TotalRows = _content.HasChildren ? _content.VisualChildren.Count : 0;
            MaxVisibleRows = Math.Max(1, (int)Math.Floor(viewport.Height / RowHeight));
            RenderRows = TotalRows;

            ContentBounds = new RectangleF(viewport.X, viewport.Y - ScrollOffsetPixels, viewport.Width, _contentExtentHeightPixels);

            if (scrollContent != null)
                scrollContent.ArrangeViewport(viewport, ScrollOffsetPixels);
            else
                _content.Arrange(ContentBounds);

            ValidateLayout();
        }

        RectangleF CalculateAutomaticViewport(bool showScrollBar)
        {
            var gutter = showScrollBar ? ScrollerWidthPixels + GetScrollbarContentMarginPixels() : 0f;
            return new RectangleF(
                PanelBounds.X,
                PanelBounds.Y,
                Math.Max(0f, PanelBounds.Width - gutter),
                PanelBounds.Height);
        }

        float GetScrollbarContentMarginPixels()
        {
            return Math.Max(0f, ScrollerWidthPixels * SCROLLBAR_CONTENT_MARGIN_RATIO);
        }

        protected override void RenderDefault(List<MySprite> sprites)
        {
            if (_automaticContentMode)
            {
                EnsureAutomaticLayout();
                BeginClip(sprites, ContentViewportBounds);
                if (_content != null)
                    _content.Render(sprites);
                EndClip(sprites);
            }

            RenderScrollBar(sprites);
        }

        void RenderScrollBar(List<MySprite> sprites)
        {
            if (sprites == null || !IsScrollable || ScrollerWidthPixels <= 0f || _contentExtentHeightPixels <= 0f)
                return;

            float gutter = Math.Max(1f, ScrollerWidthPixels);
            float visual = gutter <= 1f ? 1f : (float)Math.Round(gutter, MidpointRounding.AwayFromZero);
            float padding = Math.Max(0f, (gutter - visual) * 0.5f);
            float trackHeight = Math.Max(1f, ContentViewportBounds.Height - ScrollerWidthPixels * 2f);
            float thumbHeight = Math.Max(1f, Math.Min(trackHeight, ContentViewportBounds.Height / _contentExtentHeightPixels * trackHeight));
            float maxOffset = GetMaxScrollOffsetPixels();
            float fraction = maxOffset > 0f ? MathHelper.Clamp(ScrollOffsetPixels / maxOffset, 0f, 1f) : 0f;
            float trackY = ContentViewportBounds.Y + ScrollerWidthPixels;
            float thumbY = trackY + Math.Max(0f, trackHeight - thumbHeight) * fraction;
            float barXCenter = PanelBounds.Right - gutter + padding + visual / 2f;
            int barWidth = Math.Max(1, (int)Math.Round(visual, MidpointRounding.AwayFromZero));

            DrawCapsule(sprites, new Vector2(barXCenter, (float)Math.Round(trackY + trackHeight / 2f, MidpointRounding.ToEven)), barWidth, trackHeight, ScrollBarTrackColor);
            DrawCapsule(sprites, new Vector2(barXCenter, (float)Math.Round(thumbY + thumbHeight / 2f, MidpointRounding.ToEven)), barWidth, thumbHeight, ScrollBarThumbColor);
        }

        float GetCurrentScrollOffset()
        {
            return IsAutoScrolling() ? GetAutoScrollOffset(GetMaxScrollOffsetPixels()) : 0f;
        }

        int GetMaxStartRow()
        {
            return Math.Max(0, (int)Math.Floor(GetMaxScrollOffsetPixels() / RowHeight));
        }

        float GetMaxScrollOffsetPixels()
        {
            return Math.Max(0f, _contentExtentHeightPixels - ContentViewportBounds.Height);
        }

        void UpdateRowState()
        {
            StartRow = MathHelper.Clamp((int)Math.Floor(ScrollOffsetPixels / RowHeight), 0, GetMaxStartRow());
            RowOffsetPixels = Math.Max(0f, ScrollOffsetPixels - StartRow * RowHeight);
        }

        bool IsAutoScrolling()
        {
            return IsScrollable && _resolvedAutoScrollSecondsPerStep > 0f;
        }

        float GetAutoScrollOffset(float maxScrollOffset)
        {
            if (_resolvedAutoScrollSecondsPerStep <= 0f || maxScrollOffset <= 0f)
                return 0f;

            var framesPerStep = Math.Max(1, (int)Math.Round(_resolvedAutoScrollSecondsPerStep * 60f));
            var step = (int)(GetFrameCounter() / framesPerStep);
            int maxStartRow = (int)Math.Floor(maxScrollOffset / RowHeight);
            return (maxStartRow <= 0 ? 0 : step % (maxStartRow + 1)) * RowHeight;
        }

        static long GetFrameCounter()
        {
            var session = MyAPIGateway.Session;
            return session != null ? session.GameplayFrameCounter : 0L;
        }

        static void BeginClip(List<MySprite> sprites, RectangleF bounds)
        {
            if (sprites == null)
                return;

            int x = (int)Math.Floor(bounds.X);
            int y = (int)Math.Floor(bounds.Y);
            int right = (int)Math.Ceiling(bounds.Right);
            int bottom = (int)Math.Ceiling(bounds.Bottom);
            sprites.Add(MySprite.CreateClipRect(new Rectangle(x, y, Math.Max(0, right - x), Math.Max(0, bottom - y))));
        }

        static void EndClip(List<MySprite> sprites)
        {
            if (sprites != null)
                sprites.Add(MySprite.CreateClearClipRect());
        }

        static void DrawCapsule(List<MySprite> sprites, Vector2 center, int thickness, float length, Color color)
        {
            sprites.Add(new MySprite { Type = SpriteType.TEXTURE, Data = "SquareSimple", Position = center, Size = new Vector2(thickness, length + .5f), Color = color, Alignment = TextAlignment.CENTER });
            var capsSize = new Vector2(thickness);
            sprites.Add(new MySprite { Type = SpriteType.TEXTURE, Data = "SemiCircle", Position = new Vector2(center.X, center.Y - length / 2f), Size = capsSize, RotationOrScale = 0f, Color = color, Alignment = TextAlignment.CENTER });
            sprites.Add(new MySprite { Type = SpriteType.TEXTURE, Data = "SemiCircle", Position = new Vector2(center.X, center.Y + length / 2f), Size = capsSize, RotationOrScale = (float)Math.PI, Color = color, Alignment = TextAlignment.CENTER });
        }
    }
}
