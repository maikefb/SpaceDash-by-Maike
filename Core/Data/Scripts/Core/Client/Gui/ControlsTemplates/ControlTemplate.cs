using System;
using System.Collections.Generic;
using LcdMod.Client.Gui.ControlsTemplates.Panels;
using LcdMod.Client.Gui.Styling;
using LcdMod.Client.Helpers;
using LcdMod.Common.Mvvm;
using VRage.Game.GUI.TextPanel;
using VRageMath;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;

namespace LcdMod.Client.Gui.ControlsTemplates
{
    public delegate void InteractiveRenderHandler(ControlTemplate entry, List<MySprite> sprites);

    public interface ITextSurfaceProvider
    {
        IMyTextSurface TextSurface { get; }
    }

    public abstract partial class ControlTemplate : Control, ITextStyleProvider
    {
        public static readonly StyleProperty<Color> TextColorProperty =
            StyleProperty.Register<ControlTemplate, Color>("TextColor", null);

        public static readonly StyleProperty<string> TextFontProperty =
            StyleProperty.Register<ControlTemplate, string>("TextFont", "White");

        public static readonly StyleProperty<float> LayoutScaleProperty =
            StyleProperty.Register<ControlTemplate, float>("LayoutScale", 1f);

        public static readonly StyleProperty<float> FontScaleProperty =
            StyleProperty.Register<ControlTemplate, float>("FontScale", 1f);

        public static readonly StyleProperty<float> OpacityProperty =
            StyleProperty.Register<ControlTemplate, float>("Opacity", 1f);

        public static readonly StyleProperty<Color> BackgroundColorProperty =
            StyleProperty.Register<ControlTemplate, Color>("BackgroundColor", (Color?)Color.Gray);

        public static readonly StyleProperty<Color> BorderColorProperty =
            StyleProperty.Register<ControlTemplate, Color>("BorderColor", (Color?)Color.Transparent);

        public static readonly StyleProperty<float> BorderRadiusPixelsProperty =
            StyleProperty.Register<ControlTemplate, float>("BorderRadiusPixels", (float?)BorderRenderer.DEFAULT_RADIUS_PIXELS);

        public static readonly StyleProperty<float> BorderThicknessPixelsProperty =
            StyleProperty.Register<ControlTemplate, float>("BorderThicknessPixels", (float?)0f);

        public static readonly StyleProperty<Vector4> PaddingProperty =
            StyleProperty.Register<ControlTemplate, Vector4>("Padding", (Vector4?)Vector4.Zero);
        
        RectangleF? _renderBoundsOverride;
        float _renderBorderRadiusInsetPixels;
        bool _isLayoutDirty = true;
        ObservableObject _observableDataContext;

        readonly List<Control> _children = new List<Control>();
        public override IReadOnlyList<Control> LogicalChildren => _children;
        public override IReadOnlyList<Control> VisualChildren => _children;

        public bool HasChildren => _children.Count > 0;
        public ControlTemplate Parent { get; private set; }
        public string StyleId { get; private set; }

        public override IVisualStyleScope StyleParent => Parent ?? base.StyleParent;

        public bool IsLayoutDirty => _isLayoutDirty;

        public override bool IsDirty
        {
            get
            {
                if (_isDirty)
                    return true;

                for (int i = 0; i < _children.Count; i++)
                {
                    if (_children[i] != null && _children[i].IsDirty)
                        return true;
                }

                return false;
            }
        }
        
        public ControlTemplate SetStyleId(string styleId)
        {
            if (StyleId == styleId)
                return this;

            StyleId = styleId;
            MarkSubtreeDirty();
            return this;
        }

        public ControlTemplate SetStyles(StyleTree styles)
        {
            if (ReferenceEquals(Styles, styles))
                return this;

            Styles = styles;
            MarkSubtreeDirty();
            return this;
        }

        public ControlTemplate SetResources(ResourceTree resources)
        {
            if (ReferenceEquals(Resources, resources))
                return this;

            Resources = resources;
            MarkSubtreeDirty();
            return this;
        }

        public void InvalidateLayout()
        {
            _isLayoutDirty = true;
            MarkDirty();

            if (Parent != null)
                Parent.OnChildLayoutInvalidated(this);
        }

        protected void ValidateLayout()
        {
            _isLayoutDirty = false;
        }

        protected virtual void OnChildLayoutInvalidated(ControlTemplate child)
        {
            InvalidateLayout();
        }

        public virtual void ClearChildren()
        {
            for (int i = 0; i < _children.Count; i++)
            {
                var child = _children[i] as ControlTemplate;
                if (child != null && ReferenceEquals(child.Parent, this))
                {
                    child.CancelAnimationTree(AnimationController);
                    child.Parent = null;
                    child.SetStyleParent(null);
                    child.DetachDataContextEventsTree();
                }
            }

            _children.Clear();
            OnChildrenChanged();
        }

        public virtual void AddChild(ControlTemplate child)
        {
            if (child == null)
                return;

            if (ReferenceEquals(child, this))
                throw new InvalidOperationException("A control cannot contain itself.");

            if (WouldCreateCycle(child))
                throw new InvalidOperationException("Adding the child would create a cycle.");

            if (ReferenceEquals(child.Parent, this))
            {
                if (!_children.Contains(child))
                {
                    _children.Add(child);
                    OnChildrenChanged();
                }

                return;
            }

            if (child.Parent != null)
                child.Parent.RemoveChild(child);

            if (!_children.Contains(child))
                _children.Add(child);

            child.Parent = this;
            child.MarkSubtreeDirty();
            child.AttachDataContextEventsTree();
            OnChildrenChanged();
        }

        public virtual void AddChildren(IEnumerable<ControlTemplate> children)
        {
            if (children == null)
                return;

            foreach (var child in children)
                AddChild(child);
        }

        public virtual void AddOverlayEntries(List<Control> entries)
        {
            if (!Visible || entries == null)
                return;

            for (int i = 0; i < _children.Count; i++)
            {
                var child = _children[i] as ControlTemplate;
                if (child != null)
                    child.AddOverlayEntries(entries);
            }
        }

        public virtual bool RemoveChild(Control child)
        {
            
            var childControl = child as ControlTemplate;
            
            if (childControl == null || !_children.Remove(child))
                return false;

            if (ReferenceEquals(childControl.Parent, this))
                childControl.Parent = null;

            childControl.CancelAnimationTree(AnimationController);
            childControl.SetStyleParent(null);
            childControl.DetachDataContextEventsTree();

            OnChildrenChanged();
            return true;
        }

        public virtual bool MoveChild(ControlTemplate child, int index)
        {
            if (child == null || !ReferenceEquals(child.Parent, this))
                return false;

            int currentIndex = _children.IndexOf(child);
            if (currentIndex < 0)
                return false;

            int targetIndex = Math.Max(0, Math.Min(index, _children.Count - 1));
            if (currentIndex == targetIndex)
                return false;

            _children.RemoveAt(currentIndex);
            _children.Insert(targetIndex, child);
            OnChildrenChanged();
            return true;
        }

        protected void AttachTo(ControlTemplate parent)
        {
            // Derived constructors call this after local initialization so parent invalidation observes a ready child.
            if (parent != null)
                parent.AddChild(this);
        }

        protected virtual void OnChildrenChanged()
        {
            InvalidateLayout();
        }

        bool WouldCreateCycle(ControlTemplate child)
        {
            for (var parent = this; parent != null; parent = parent.Parent)
            {
                if (ReferenceEquals(parent, child))
                    return true;
            }

            return false;
        }

        protected ControlTemplate(object dataContext = null)
        {
            ReplaceDataContext(dataContext);
        }

        public ControlModelBase Model => DataContext as ControlModelBase;

        public ControlTemplate SetDataContext(object dataContext)
        {
            if (ReplaceDataContext(dataContext))
                MarkDirty();
            return this;
        }

        bool ReplaceDataContext(object dataContext)
        {
            if (ReferenceEquals(DataContext, dataContext))
                return false;

            if (_observableDataContext != null)
                _observableDataContext.PropertyChanged -= OnDataContextPropertyChanged;

            DataContext = dataContext;
            _observableDataContext = dataContext as ObservableObject;
            if (_observableDataContext != null)
                _observableDataContext.PropertyChanged += OnDataContextPropertyChanged;
            return true;
        }

        /// <summary>Controles removidos da árvore param de ouvir o view model, que costuma viver mais que eles.</summary>
        internal void DetachDataContextEventsTree()
        {
            if (_observableDataContext != null)
                _observableDataContext.PropertyChanged -= OnDataContextPropertyChanged;

            for (int i = 0; i < _children.Count; i++)
            {
                var child = _children[i] as ControlTemplate;
                if (child != null)
                    child.DetachDataContextEventsTree();
            }
        }

        internal void AttachDataContextEventsTree()
        {
            if (_observableDataContext != null)
            {
                _observableDataContext.PropertyChanged -= OnDataContextPropertyChanged;
                _observableDataContext.PropertyChanged += OnDataContextPropertyChanged;
            }

            for (int i = 0; i < _children.Count; i++)
            {
                var child = _children[i] as ControlTemplate;
                if (child != null)
                    child.AttachDataContextEventsTree();
            }
        }

        void OnDataContextPropertyChanged(ObservableObject sender, string propertyName)
        {
            if (ReferenceEquals(sender, _observableDataContext) &&
                ShouldInvalidateForDataContextProperty(propertyName))
                MarkDirty();
        }

        protected virtual bool ShouldInvalidateForDataContextProperty(string propertyName)
        {
            return true;
        }

        protected virtual bool ClipContent => false;

        protected virtual RectangleF ClipContentBounds => Bounds;

        public InteractiveRenderHandler CustomRender { get; set; }

        public abstract RectangleF Bounds { get; }

        public virtual Vector2 Measure(Vector2 availableSize)
        {
            return Bounds.Size;
        }

        public virtual void Arrange(RectangleF bounds)
        {
            ValidateLayout();
        }

        public void Render(List<MySprite> sprites)
        {
            if (!Visible || sprites == null)
                return;

            int spriteStart = sprites.Count;
            RectangleF renderBounds = Bounds;
            RectangleF? inheritedClip = GetInheritedClipBounds();
            bool rendered = false;

            try
            {
                var customRender = CustomRender ?? Model?.CustomRender;

                // Draw the interaction border at the control's normal bounds,
                // then render the button itself against an inset bounds. This
                // preserves the existing two-layer rounded rendering while
                // keeping the border inside the layout/hit-test rectangle.
                float borderInsetPixels = ShouldRenderStyleBorder()
                    ? RenderStyleBorder(sprites)
                    : 0f;

                if (borderInsetPixels > 0f)
                    BeginStyleBorderBackgroundInset(renderBounds, borderInsetPixels);

                if (customRender != null)
                    customRender(this, sprites);
                else
                    RenderDefault(sprites);

                rendered = true;
            }
            finally
            {
                EndStyleBorderBackgroundInset();

                if (rendered)
                    this.RestoreClipAfterRender(sprites, spriteStart, inheritedClip);

                CompleteRenderState();
            }
        }

        RectangleF? GetInheritedClipBounds()
        {
            ControlTemplate clipParent = FindClipContentParent();
            return clipParent != null
                ? (RectangleF?)clipParent.ClipContentBounds
                : null;
        }

        void CompleteRenderState()
        {
            _isDirty = IsDirtyAfterRender();
        }

        protected virtual bool IsDirtyAfterRender()
        {
            return false;
        }

        protected virtual StyleState GetStyleState()
        {
            StyleState state = StyleState.None;

            if (!Enabled)
                state |= StyleState.Disabled;

            return state;
        }

        internal StyleState GetStyleStateForResolver()
        {
            return GetStyleState();
        }

        protected TValue GetStyleValue<TValue>(
            StyleProperty<TValue> property,
            PropertyValue<TValue> value)
        {
            if (value.LocalOverride)
                return value.Local;

            if (_isDirty || !value.HasCache)
            {
                value.Cache = ResolveStyleValue(property);
                value.HasCache = true;
            }

            return value.Cache;
        }



        protected bool TryResolveStyleValue<TValue>(
            StyleProperty<TValue> property,
            out TValue value)
        {
            return TryResolveStyleValueForState(
                property,
                GetStyleStateForResolver(),
                out value);
        }

        internal bool TryResolveStyleValueForState<TValue>(
            StyleProperty<TValue> property,
            StyleState state,
            out TValue value)
        {
            int guard = 0;
            for (IVisualStyleScope scope = this; scope != null && guard++ < 128;)
            {
                StyleTree styles = scope.Styles;
                if (styles != null && styles.TryResolve(this, StyleId, state, property, out value))
                    return true;

                IVisualStyleScope next = scope.StyleParent;
                if (ReferenceEquals(next, scope))
                    break;

                scope = next;
            }

            value = default(TValue);
            return false;
        }

        protected TValue ResolveStyleValue<TValue>(StyleProperty<TValue> property)
        {
            return ResolveStyleValueForState(property, GetStyleStateForResolver());
        }

        internal TValue ResolveStyleValueForState<TValue>(
            StyleProperty<TValue> property,
            StyleState state)
        {
            TValue value;

            if (TryResolveStyleValueForState(property, state, out value))
                return value;

            if (property.Inherits && Parent != null)
                return Parent.ResolveStyleValue(property);

            if (property.HasDefaultValue)
                return property.DefaultValue;

            throw new KeyNotFoundException("Resource key not found: " + property.OwnerType.Name + "." + property.Name + " in " + typeof(TValue).Name);
        }

        public Color GetResourceColor(ResourceKey<Color> key, Color fallback)
        {
            Color value;
            return ScopedResourceResolver.TryResolve(this, key, out value) ? value : fallback;
        }

        public Color ResolveColor(ResourceKey<Color> key)
        {
            Color value;
            if (ScopedResourceResolver.TryResolve(this, key, out value))
                return value;

            throw new KeyNotFoundException("Resource key not found: " + key.Name + " in ResourceTree");
        }

        protected virtual void RenderDefault(List<MySprite> sprites)
        {
            var rect = GetViewBox();
            var fillColor = GetRenderBackgroundColor();

            BorderRenderer.CreateSpritesFromRect(rect, sprites, fillColor,
                radiusPixels: GetRenderBorderRadiusPixels(),
                radiusScale: LayoutScale);
            RenderDefaultText(rect, sprites);
        }

        protected virtual Color GetRenderBackgroundColor()
        {
            return BackgroundColor;
        }

        protected virtual bool ShouldRenderStyleBorder()
        {
            return false;
        }

        float RenderStyleBorder(List<MySprite> sprites)
        {
            RectangleF rect = GetViewBox();
            if (rect.Width <= 0f || rect.Height <= 0f)
                return 0f;

            float thicknessPixels = Math.Max(0f, BorderThicknessPixels);
            Color borderColor = ApplyOpacity(BorderColor);
            if (thicknessPixels <= 0f || borderColor.A == 0)
                return 0f;

            BorderRenderer.CreateBorderSpritesFromRect(
                rect,
                sprites,
                GetRenderBackgroundColor(),
                borderColor,
                GetRenderBorderRadiusPixels(),
                LayoutScale,
                thicknessPixels);

            return thicknessPixels;
        }

        void BeginStyleBorderBackgroundInset(RectangleF bounds, float thicknessPixels)
        {
            float scaledThickness = Math.Max(0f, thicknessPixels * Math.Max(0f, LayoutScale));
            if (scaledThickness <= 0f)
                return;

            _renderBoundsOverride = new RectangleF(
                bounds.X + scaledThickness,
                bounds.Y + scaledThickness,
                Math.Max(0f, bounds.Width - scaledThickness * 2f),
                Math.Max(0f, bounds.Height - scaledThickness * 2f));
            _renderBorderRadiusInsetPixels = thicknessPixels;
        }

        void EndStyleBorderBackgroundInset()
        {
            _renderBoundsOverride = null;
            _renderBorderRadiusInsetPixels = 0f;
        }

        internal RectangleF GetRenderBounds(RectangleF bounds)
        {
            return _renderBoundsOverride ?? bounds;
        }

        internal float GetEffectiveRenderBorderRadiusPixels()
        {
            return Math.Max(0f, BorderRadiusPixels - _renderBorderRadiusInsetPixels);
        }

        protected Color ApplyOpacity(Color color)
        {
            float opacity = MathHelper.Clamp(Opacity, 0f, 1f);
            byte alpha = (byte)Math.Round(color.A * opacity);
            return new Color(color.R, color.G, color.B, alpha);
        }

        protected virtual Color GetRenderTextColor()
        {
            return TextColor;
        }

        protected virtual string GetRenderTextFont()
        {
            return TextFont;
        }

        string ITextStyleProvider.ResolvedTextFont => GetRenderTextFont();

        protected virtual float GetRenderBorderRadiusPixels()
        {
            return GetEffectiveRenderBorderRadiusPixels();
        }

        public RectangleF GetViewBox()
        {
            return ApplyPadding(Bounds, GetLocalPadding());
        }

        protected Vector4 GetLocalPadding()
        {
            return Padding;
        }

        static RectangleF ApplyPadding(RectangleF bounds, Vector4 padding)
        {
            if (bounds.Width <= 0f || bounds.Height <= 0f ||
                padding.X == 0f && padding.Y == 0f && padding.Z == 0f && padding.W == 0f)
                return bounds;

            float left = ClampPadding(padding.X);
            float top = ClampPadding(padding.Y);
            float right = ClampPadding(padding.Z);
            float bottom = ClampPadding(padding.W);

            var x = bounds.X + bounds.Width * left;
            var y = bounds.Y + bounds.Height * top;
            var width = Math.Max(0f, bounds.Width * (1f - left - right));
            var height = Math.Max(0f, bounds.Height * (1f - top - bottom));

            return new RectangleF(x, y, width, height);
        }

        static float ClampPadding(float value)
        {
            if (value < 0f)
                return 0f;

            if (value > 1f)
                return 1f;

            return value;
        }

        protected void RenderDefaultText(RectangleF rect, List<MySprite> sprites)
        {
            RenderDefaultText(rect, sprites, GetRenderTextColor());
        }

        protected void RenderDefaultText(RectangleF rect, List<MySprite> sprites, Color color)
        {
            string text = DataContext != null ? DataContext.ToString() : string.Empty;

            if (string.IsNullOrEmpty(text))
                return;

            string fontId = GetRenderTextFont();
            float textScale = 0.58f * LayoutScale * FontScale;
            var textSize = MeasureText(text, textScale);

            sprites.Add(new MySprite
            {
                Type = SpriteType.TEXT,
                Data = text,
                Position = new Vector2(rect.Center.X, rect.Center.Y - textSize.Y * 0.5f),
                Color = color,
                FontId = fontId,
                Alignment = TextAlignment.CENTER,
                RotationOrScale = textScale
            });
        }

        public IMyTextSurface TextSurface => ResolveTextSurface();

        public Vector2 MeasureText(string text, float scale)
        {
            var surface = TextSurface;
            return surface != null ? FormatingHelper.GetSizeInPixel(text, this, scale, surface) : Vector2.Zero;
        }

        public Vector2 MeasureText(string text, string fontId, float scale)
        {
            var surface = TextSurface;
            if (surface == null || string.IsNullOrEmpty(text))
                return Vector2.Zero;

            return FormatingHelper.GetSizeInPixel(text, fontId, scale, surface);
        }

        public float GetLineHeight(float scale)
        {
            var surface = TextSurface;
            return surface != null ? FormatingHelper.LineHeight(scale, this, surface) : 0f;
        }

        public float GetLineHeight(float scale, string fontId)
        {
            var surface = TextSurface;
            return surface != null ? FormatingHelper.LineHeight(scale, surface, fontId) : 0f;
        }

        IMyTextSurface ResolveTextSurface()
        {
            int guard = 0;
            for (IVisualStyleScope scope = this; scope != null && guard++ < 128;)
            {
                var provider = scope as ITextSurfaceProvider;
                if (provider != null && provider.TextSurface != null)
                    return provider.TextSurface;

                IVisualStyleScope next = scope.StyleParent;
                if (ReferenceEquals(next, scope))
                    break;

                scope = next;
            }

            return null;
        }

        ControlTemplate FindClipContentParent()
        {
            for (var parent = Parent; parent != null; parent = parent.Parent)
            {
                if (parent.ClipContent)
                    return parent;
            }

            return null;
        }

        protected bool BeginContentClip(List<MySprite> sprites, RectangleF bounds)
        {
            RectangleF clip;
            if (!TryResolveClip(bounds, out clip))
                return false;

            AddClip(sprites, clip);
            return true;
        }

        protected void EndContentClip(List<MySprite> sprites)
        {
            if (sprites == null)
                return;

            sprites.Add(MySprite.CreateClearClipRect());

            var clipParent = FindClipContentParent();
            if (clipParent != null)
                AddClip(sprites, clipParent.ClipContentBounds);
        }

        bool TryResolveClip(RectangleF bounds, out RectangleF clip)
        {
            var clipParent = FindClipContentParent();
            clip = clipParent != null ? Intersect(bounds, clipParent.ClipContentBounds) : bounds;
            return clip.Width > 0f && clip.Height > 0f;
        }

        static RectangleF Intersect(RectangleF a, RectangleF b)
        {
            float x = Math.Max(a.X, b.X);
            float y = Math.Max(a.Y, b.Y);
            float right = Math.Min(a.Right, b.Right);
            float bottom = Math.Min(a.Bottom, b.Bottom);
            return new RectangleF(x, y, Math.Max(0f, right - x), Math.Max(0f, bottom - y));
        }

        static void AddClip(List<MySprite> sprites, RectangleF bounds)
        {
            if (sprites == null)
                return;

            int x = (int)Math.Floor(bounds.X);
            int y = (int)Math.Floor(bounds.Y);
            int right = (int)Math.Ceiling(bounds.Right);
            int bottom = (int)Math.Ceiling(bounds.Bottom);
            sprites.Add(MySprite.CreateClipRect(new Rectangle(x, y, Math.Max(0, right - x), Math.Max(0, bottom - y))));
        }
    }
}
