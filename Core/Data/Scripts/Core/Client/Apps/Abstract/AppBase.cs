using System;
using System.Collections.Generic;
using LcdMod.Client.Animation;
using LcdMod.Client.Extensions;
using LcdMod.Client.Gui;
using LcdMod.Client.Gui.ControlsTemplates;
using LcdMod.Client.Gui.ControlsTemplates.Panels;
using LcdMod.Client.Gui.Styling;
using LcdMod.Client.Gui.Styling.DataTemplates;
using LcdMod.Client.Gui.Styling.Styles;
using LcdMod.Client.Helpers;
using LcdMod.Client.GridData;
using LcdMod.Common.Config.Components;
using VRage.Game.GUI.TextPanel;
using VRageMath;

using LcdMod.Common.Config.Generation;
using LcdMod.Common.Helpers;

namespace LcdMod.Client.Apps.Abstract
{
    
    
    
    public abstract partial class App : Control, IApp, ITextStyleProvider
    {
        readonly List<Control> _logicalChildren = new List<Control>();
        AnimationController _animationController;
        StyleTree _styles;
        ResourceTree _resources;
        Dictionary<string, Color> _theme;
        Color _rawHeaderColor;
        Color _rawBackgroundColor;
        Color _rawForegroundColor;
        Color _rawWarningColor;
        Color _rawErrorColor;
        bool _rawCustomizedColors;
        float _themeLayoutScale;
        float _themeFontScale;
        float _themeAutoScrollSecondsPerStep;
        string _themeTextFont;
        int _themeKind;
        bool _hasTheme;
        long _themeFrame = -1;
        GridLogic _neededGridLogic;
        GridCapability _gridCapabilities;

        protected App(IAppHost host)
        {
            Host = host;
            if (Host == null)
                throw new ArgumentNullException(nameof(host));

            _animationController = Host.Animations;
            _styles = BuildDefaultStyles();
            Bare = new BareLayout(host, this);
        }

        protected IAppHost Host { get; private set; }
        protected IComponentContainer Config => Host.Config;
        internal override AnimationController AnimationController => _animationController;
        public override IReadOnlyList<Control> LogicalChildren => _logicalChildren;
        public override StyleTree Styles => _styles;

        protected void NeedGridData(GridCapability need)
        {
            var logic = Host != null ? Host.GridLogic : null;
            if (!ReferenceEquals(_neededGridLogic, logic))
                ReleaseGridDataNeeds();
            if (logic == null)
                return;

            var additionalNeeds = need & ~_gridCapabilities;
            if (additionalNeeds == GridCapability.None)
                return;

            logic.RequestCapability(additionalNeeds);
            _neededGridLogic = logic;
            _gridCapabilities |= additionalNeeds;
        }

        void ReleaseGridDataNeeds()
        {
            if (_neededGridLogic != null && _gridCapabilities != GridCapability.None)
                _neededGridLogic.Release(_gridCapabilities);
            _neededGridLogic = null;
            _gridCapabilities = GridCapability.None;
        }

        public override bool IsDirty
        {
            get
            {
                if (_isDirty)
                    return true;

                for (int i = 0; i < _logicalChildren.Count; i++)
                {
                    if (IsVisibleTreeDirty(_logicalChildren[i]))
                        return true;
                }

                return false;
            }
        }

        protected static bool IsVisibleTreeDirty(Control control)
        {
            if (control == null || !control.Visible)
                return false;

            if (control._isDirty)
                return true;

            var children = control.LogicalChildren;
            if (children == null)
                return false;

            for (int i = 0; i < children.Count; i++)
            {
                if (IsVisibleTreeDirty(children[i]))
                    return true;
            }

            return false;
        }

        public override ResourceTree Resources
        {
            get
            {
                EnsureResources();
                return _resources;
            }
        }

        protected void ClearDirtyAfterRender()
        {
            _isDirty = false;
        }

        protected T AddLogicalChild<T>(T control)
            where T : ControlTemplate
        {
            if (control == null)
                return null;

            var previousOwner = control.StyleParent as App;
            if (previousOwner != null && !ReferenceEquals(previousOwner, this))
                previousOwner.RemoveLogicalChild(control);

            if (!_logicalChildren.Contains(control))
                _logicalChildren.Add(control);

            control.SetStyleParent(this);
            MarkDirty();
            return control;
        }

        protected bool RemoveLogicalChild(Control control)
        {
            if (control == null || !_logicalChildren.Remove(control))
                return false;

            var visualChildren = VisualChildren as IList<Control>;
            if (visualChildren != null &&
                !ReferenceEquals(visualChildren, _logicalChildren) &&
                !visualChildren.IsReadOnly)
            {
                visualChildren.Remove(control);
            }

            control.CancelAnimationTree(_animationController);
            if (ReferenceEquals(control.StyleParent, this))
                control.SetStyleParent(null);

            MarkDirty();
            return true;
        }

        public Sandbox.ModAPI.Ingame.IMyTextSurface TextSurface => Host.Surface;

        public string TextFont
        {
            get
            {
                string value;
                return ScopedResourceResolver.TryResolve(this, ThemeResources.TextFont, out value) &&
                       !string.IsNullOrEmpty(value)
                    ? value
                    : "White";
            }
        }

        string ITextStyleProvider.ResolvedTextFont => TextFont;

        protected Vector2 MeasureText(string text, float scale)
        {
            return FormatingHelper.GetSizeInPixel(text, this, scale, TextSurface);
        }

        protected float MeasureLineHeight(float scale, string probe = "Ag")
        {
            return FormatingHelper.LineHeight(scale, this, TextSurface, probe);
        }


        public abstract void Update();

        public virtual void LayoutChanged()
        {
        }

        public abstract List<MySprite> GetSprites();
        

        public virtual bool HasVisibleItems()
        {
            return true;
        }

        public virtual void Close()
        {
            ReleaseGridDataNeeds();
        }

        protected Color GetHeaderColor()
        {
            return Readable(ColorComponent.ResolveHeaderColor(Host.Block as Sandbox.ModAPI.IMyTerminalBlock));
        }

        /// <summary>Cor padrão (não escolhida pelo usuário) ganha contraste mínimo de 3:1 com o fundo real da tela.</summary>
        Color Readable(Color color)
        {
            return color.ReadableFor(ColorComponent, Host.BackgroundColor);
        }

        protected LcdTheme Theme => Host.ThemeKind;
        protected bool IsMinimal => Theme == LcdTheme.Minimal;
        protected bool IsCompact => Theme == LcdTheme.Compact;
        protected bool IsBare => ThemeChrome.IsBare(Theme);
        protected BareLayout Bare { get; }
        protected bool BlinkOn => ((Sandbox.ModAPI.MyAPIGateway.Session?.GameplayFrameCounter ?? 0L) / 10) % 2 == 0;
        protected static long LocalIdentityId => Sandbox.ModAPI.MyAPIGateway.Session?.Player?.IdentityId ?? 0L;

        protected Color GetForegroundColor()
        {
            return Host.ForegroundColor;
        }

        protected Color GetWarningColor()
        {
            return Readable(ColorComponent.ResolveWarningColor());
        }

        protected Color GetErrorColor()
        {
            return Readable(ColorComponent.ResolveErrorColor());
        }

        protected Color ResolveResource(ResourceKey<Color> key, Color fallback)
        {
            Color value;
            return ScopedResourceResolver.TryResolve(this, key, out value) ? value : fallback;
        }

        void EnsureResources()
        {
            long frame = Sandbox.ModAPI.MyAPIGateway.Session != null
                ? Sandbox.ModAPI.MyAPIGateway.Session.GameplayFrameCounter
                : 0L;
            if (_hasTheme && frame == _themeFrame)
                return;
            _themeFrame = frame;

            var colors = ColorComponent;
            var rawHeaderColor = colors.ResolveHeaderColor(Host.Block as Sandbox.ModAPI.IMyTerminalBlock);
            var rawWarningColor = colors.ResolveWarningColor();
            var rawErrorColor = colors.ResolveErrorColor();
            bool customizedColors = colors.CustomizedColors;
            var backgroundColor = Host.BackgroundColor;
            var foregroundColor = Host.ForegroundColor;
            float layoutScale = GetLayoutScaleResourceValue();
            float fontScale = GetFontScaleResourceValue();
            float autoScrollStep = InteractionComponent.AutoScrollStep;
            string textFont = GetTextFontResourceValue();
            int themeKind = (int)Host.ThemeKind;

            if (_hasTheme &&
                themeKind == _themeKind &&
                rawHeaderColor == _rawHeaderColor &&
                rawWarningColor == _rawWarningColor &&
                rawErrorColor == _rawErrorColor &&
                customizedColors == _rawCustomizedColors &&
                backgroundColor == _rawBackgroundColor &&
                foregroundColor == _rawForegroundColor &&
                layoutScale == _themeLayoutScale &&
                fontScale == _themeFontScale &&
                autoScrollStep == _themeAutoScrollSecondsPerStep &&
                string.Equals(textFont, _themeTextFont, StringComparison.Ordinal))
                return;

            var headerColor = Readable(rawHeaderColor);
            var warningColor = Readable(rawWarningColor);
            var errorColor = Readable(rawErrorColor);
            bool dark = ShouldUseDarkTheme();

            _theme = headerColor.ToTheme(dark);
            _resources = BuildThemeResources(_theme);
            // A cor da letra escolhida no terminal vale para todo texto, inclusive nos cartões; só é
            // ajustada quando não contrasta com o fundo onde é desenhada.
            _resources.Set(ThemeResources.FontColor, foregroundColor);
            _resources.Set(ThemeResources.OnSurfaceColor, foregroundColor.EnsureMinimalContrast(backgroundColor));
            _resources.Set(ThemeResources.OnAccentContainerColor,
                foregroundColor.EnsureMinimalContrast(_theme[Constants.PRIMARY_CONTAINER]));
            _resources.Set(ThemeResources.WarningColor, warningColor);
            _resources.Set(ThemeResources.ErrorColor, errorColor);
            _resources.Set(ThemeResources.LayoutScale, layoutScale);
            _resources.Set(ThemeResources.FontScale, fontScale);
            _resources.Set(ThemeResources.AutoScrollSecondsPerStep, autoScrollStep);
            _resources.Set(ThemeResources.TextFont, textFont);
            _resources.Set(ThemeResources.Theme, themeKind);
            _rawHeaderColor = rawHeaderColor;
            _rawWarningColor = rawWarningColor;
            _rawErrorColor = rawErrorColor;
            _rawCustomizedColors = customizedColors;
            _rawBackgroundColor = backgroundColor;
            _rawForegroundColor = foregroundColor;
            _themeLayoutScale = layoutScale;
            _themeFontScale = fontScale;
            _themeAutoScrollSecondsPerStep = autoScrollStep;
            _themeTextFont = textFont;
            _themeKind = themeKind;
            _hasTheme = true;
            MarkSubtreeDirty();
        }

        static ResourceTree BuildThemeResources(IReadOnlyDictionary<string, Color> theme)
        {
            ResourceTree resources = new ResourceTree();
            ItemDataTemplate.AddResources(resources);

            resources.Set(ThemeResources.AccentColor, theme[Constants.PRIMARY]);
            resources.Set(ThemeResources.AccentContainerColor, theme[Constants.PRIMARY_CONTAINER]);
            resources.Set(ThemeResources.OnAccentContainerColor, theme[Constants.ON_PRIMARY_CONTAINER]);
            resources.Set(ThemeResources.WarningColor, theme[Constants.TERTIARY]);
            resources.Set(ThemeResources.ErrorColor, theme[Constants.ERROR]);
            resources.Set(ThemeResources.SuccessColor, theme[Constants.SUCCESS]);
            resources.Set(ThemeResources.MutedTextColor, theme[Constants.ON_SURFACE_VARIANT]);
            resources.Set(ThemeResources.DividerColor, theme[Constants.OUTLINE_VARIANT]);
            resources.Set(ThemeResources.DisabledColor, theme[Constants.DISABLED_FOREGROUND]);
            resources.Set(ThemeResources.BackgroundColor, theme[Constants.BACKGROUND]);
            resources.Set(ThemeResources.FontColor, theme[Constants.ON_BACKGROUND]);
            resources.Set(ThemeResources.SurfaceColor, theme[Constants.SURFACE]);
            resources.Set(ThemeResources.OnSurfaceColor, theme[Constants.ON_SURFACE]);
            resources.Set(ThemeResources.OnSurfaceVariantColor, theme[Constants.ON_SURFACE_VARIANT]);
            resources.Set(ThemeResources.SurfaceContainerLowColor, theme[Constants.SURFACE_CONTAINER_LOW]);
            resources.Set(ThemeResources.SurfaceContainerColor, theme[Constants.SURFACE_CONTAINER]);
            resources.Set(ThemeResources.SurfaceContainerHighestColor, theme[Constants.SURFACE_CONTAINER_HIGHEST]);
            resources.Set(ThemeResources.SecondaryContainerColor, theme[Constants.SECONDARY_CONTAINER]);
            resources.Set(ThemeResources.OnSecondaryContainerColor, theme[Constants.ON_SECONDARY_CONTAINER]);
            resources.Set(ThemeResources.BorderColor, theme[Constants.OUTLINE]);
            resources.Set(ThemeResources.BorderVariantColor, theme[Constants.OUTLINE_VARIANT]);
            resources.Set(ThemeResources.ScrollBarTrackColor, new Color(theme[Constants.OUTLINE_VARIANT], 127));
            resources.Set(ThemeResources.ScrollBarThumbColor, new Color(theme[Constants.PRIMARY], 250));

            resources.Set(ThemeResources.TextFont, "White");
            resources.Set(ThemeResources.PictureTransitionFrames, 24);

            return resources;
        }

        static StyleTree BuildDefaultStyles()
        {
            StyleTree styles = new StyleTree();

            Style control = styles.For<ControlTemplate>()
                .Set(ControlTemplate.TextColorProperty, ThemeResources.FontColor)
                .Set(ControlTemplate.TextFontProperty, ThemeResources.TextFont)
                .Set(ControlTemplate.LayoutScaleProperty, ThemeResources.LayoutScale)
                .Set(ControlTemplate.FontScaleProperty, ThemeResources.FontScale)
                .Set(ControlTemplate.OpacityProperty, 1f)
                .Set(ControlTemplate.BackgroundColorProperty, ThemeResources.SurfaceColor)
                .Set(ControlTemplate.BorderColorProperty, ThemeResources.BorderVariantColor)
                .Set(ControlTemplate.BorderRadiusPixelsProperty, BorderRenderer.DEFAULT_RADIUS_PIXELS)
                .Set(ControlTemplate.BorderThicknessPixelsProperty, 0f)
                .Set(ControlTemplate.PaddingProperty, Vector4.Zero);

            styles.For<ScrollPanel>()
                .Set(ScrollPanel.ScrollBarTrackColorProperty, ThemeResources.ScrollBarTrackColor)
                .Set(ScrollPanel.ScrollBarThumbColorProperty, ThemeResources.ScrollBarThumbColor);

            control.State(StyleState.Disabled)
                .Set(ControlTemplate.BackgroundColorProperty, ThemeResources.SurfaceContainerLowColor)
                .Set(ControlTemplate.TextColorProperty, ThemeResources.DisabledColor);

            styles.For<Border>()
                .Set(ControlTemplate.BackgroundColorProperty, ThemeResources.AccentContainerColor)
                .Set(ControlTemplate.BorderThicknessPixelsProperty, 0);

            ButtonStyle.Build(styles);

            return styles;
        }

        float GetLayoutScaleResourceValue()
        {
            float scale = GeneralComponent.GetScale();
            return scale > 0f ? scale : 1f;
        }

        float GetFontScaleResourceValue()
        {
            float fontScale = Host.FontScale;
            return fontScale > 0f ? fontScale : 1f;
        }

        string GetTextFontResourceValue()
        {
            string font = Host.Surface.Font;
            return string.IsNullOrEmpty(font) ? "White": font;
        }

        bool ShouldUseDarkTheme()
        {
            var backgroundColor = Host.BackgroundColor;
            return backgroundColor.ContrastRatio(Color.White) >=
                   backgroundColor.ContrastRatio(Color.Black);
        }

    }
}
