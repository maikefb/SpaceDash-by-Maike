using System;
using System.Collections.Generic;
using System.Linq;
using LcdMod.Client.Animation;
using LcdMod.Client.Apps.Abstract;
using LcdMod.Client.Gui;
using LcdMod.Client.Helpers;
using LcdMod.Common.Config.Components;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace LcdMod.Client.Apps
{
    /// <summary>
    /// Moldura digital: mostra as imagens escolhidas no terminal, uma por vez, trocando no intervalo configurado
    /// com deslize horizontal. Sem a seleção pela própria tela do mod original, que dependia da interação no LCD.
    /// </summary>
    internal sealed partial class DigitalPictureFramesApp : App, IApp
    {
        const int MAX_TILE_SPRITES = 2048;
        const string TRANSITION_FRAMES_RESOURCE = "pictureTransitionFrames";

        static readonly List<Control> NoChildren = new List<Control>();

        readonly List<MySprite> _sprites = new List<MySprite>();
        string _currentSprite = string.Empty;
        string _previousSprite = string.Empty;
        float _transitionProgress = 1f;
        AnimationHandle _transitionAnimation;

        public DigitalPictureFramesApp(IAppHost host) : base(host)
        {
        }

        public override IReadOnlyList<Control> VisualChildren => NoChildren;

        public override void Update()
        {
        }

        public override bool HasVisibleItems()
        {
            return GetConfiguredSprites(DigitalPictureFramesComponent).Length > 0;
        }

        public override List<MySprite> GetSprites()
        {
            _sprites.Clear();
            var config = DigitalPictureFramesComponent;
            if (config != null)
                DrawBackgroundImage(config, (PictureFrameDisplayMode)GeneralComponent.DisplayMode);
            ClearDirtyAfterRender();
            return _sprites;
        }

        void DrawBackgroundImage(DigitalPictureFramesConfigComponent config, PictureFrameDisplayMode displayMode)
        {
            var spriteName = GetCurrentSprite(config);
            UpdateTransition(spriteName);

            if (string.IsNullOrWhiteSpace(spriteName))
                return;

            var viewBox = Host.ViewBox;
            if (viewBox.Width <= 0f || viewBox.Height <= 0f)
                return;

            _sprites.Add(MySprite.CreateClipRect(ToRectangle(viewBox)));

            if (_transitionAnimation != null && _transitionAnimation.IsRunning && !string.IsNullOrWhiteSpace(_previousSprite))
            {
                var previousTravel = GetTransitionTravel(_previousSprite, displayMode, viewBox);
                var currentTravel = GetTransitionTravel(spriteName, displayMode, viewBox);
                var previousOffset = new Vector2(-previousTravel * _transitionProgress, 0f);
                var currentOffset = new Vector2(currentTravel * (1f - _transitionProgress), 0f);

                if (displayMode == PictureFrameDisplayMode.Tile)
                {
                    DrawTransitionTile(_previousSprite, viewBox, previousOffset);
                    DrawTransitionTile(spriteName, viewBox, currentOffset);
                }
                else
                {
                    DrawImage(_previousSprite, displayMode, viewBox, previousOffset);
                    DrawImage(spriteName, displayMode, viewBox, currentOffset);
                }
            }
            else
            {
                DrawImage(spriteName, displayMode, viewBox, Vector2.Zero);
            }

            _sprites.Add(MySprite.CreateClearClipRect());
        }

        static string GetCurrentSprite(DigitalPictureFramesConfigComponent config)
        {
            var sprites = GetConfiguredSprites(config);
            if (sprites.Length == 0)
                return string.Empty;

            var interval = Math.Max(0f, config.ImageChangeInterval);
            if (interval <= 0f || sprites.Length == 1)
                return sprites[0];

            var session = MyAPIGateway.Session;
            var totalSeconds = session != null ? session.ElapsedPlayTime.TotalSeconds : 0d;
            return sprites[(int)(totalSeconds / interval) % sprites.Length];
        }

        public static string[] GetConfiguredSprites(DigitalPictureFramesConfigComponent config)
        {
            if (config == null)
                return Array.Empty<string>();

            if (config.SelectedSprites != null && config.SelectedSprites.Length > 0)
                return config.SelectedSprites.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

            return string.IsNullOrWhiteSpace(config.BackgroundSprite)
                ? Array.Empty<string>()
                : new[] { config.BackgroundSprite };
        }

        void UpdateTransition(string spriteName)
        {
            spriteName = spriteName ?? string.Empty;
            if (string.Equals(_currentSprite, spriteName, StringComparison.OrdinalIgnoreCase))
                return;

            _previousSprite = _currentSprite;
            _currentSprite = spriteName;

            if (string.IsNullOrWhiteSpace(_previousSprite) || string.IsNullOrWhiteSpace(_currentSprite))
            {
                this.CancelAnimation("PictureTransition", false);
                _transitionAnimation = null;
                _previousSprite = string.Empty;
                _transitionProgress = 1f;
                MarkDirty();
                return;
            }

            _transitionProgress = 0f;
            _transitionAnimation = this.RunAnimation(
                "PictureTransition",
                AnimationConflict.Replace,
                new Keyframe(value => _transitionProgress = value, 0f, 1f,
                    this.ResolveAnimationFrames(TRANSITION_FRAMES_RESOURCE, 0), EasingMode.EaseInOutCubic),
                new ActionKeyframe(CompletePictureTransition, false));
        }

        void CompletePictureTransition()
        {
            _transitionProgress = 1f;
            _previousSprite = string.Empty;
            _transitionAnimation = null;
        }

        void DrawTransitionTile(string spriteName, RectangleF viewBox, Vector2 offset)
        {
            RectangleF visibleBounds;
            if (!TryGetTranslatedViewClip(viewBox, offset, out visibleBounds))
                return;

            _sprites.Add(MySprite.CreateClipRect(ToRectangle(visibleBounds)));
            DrawTiledImage(spriteName, viewBox, GeneralComponent.GetScale(), offset);
        }

        static bool TryGetTranslatedViewClip(RectangleF viewBox, Vector2 offset, out RectangleF clip)
        {
            var translatedLeft = viewBox.X + offset.X;
            var translatedTop = viewBox.Y + offset.Y;
            var left = Math.Max(viewBox.X, translatedLeft);
            var top = Math.Max(viewBox.Y, translatedTop);
            var right = Math.Min(viewBox.X + viewBox.Width, translatedLeft + viewBox.Width);
            var bottom = Math.Min(viewBox.Y + viewBox.Height, translatedTop + viewBox.Height);

            var width = right - left;
            var height = bottom - top;
            if (width <= 0f || height <= 0f)
            {
                clip = default(RectangleF);
                return false;
            }

            clip = new RectangleF(left, top, width, height);
            return true;
        }

        void DrawImage(string spriteName, PictureFrameDisplayMode displayMode, RectangleF viewBox, Vector2 offset)
        {
            if (displayMode == PictureFrameDisplayMode.Tile)
            {
                DrawTiledImage(spriteName, viewBox, GeneralComponent.GetScale(), offset);
                return;
            }

            Vector2 sourceSize;
            var hasSourceSize = TryGetSourceSize(spriteName, out sourceSize);
            _sprites.Add(new MySprite
            {
                Type = SpriteType.TEXTURE,
                Data = spriteName,
                Position = viewBox.Center + offset,
                Size = GetImageDrawSize(viewBox.Size, sourceSize, hasSourceSize, displayMode, GeneralComponent.GetScale()),
                Color = Color.White,
                Alignment = TextAlignment.CENTER
            });
        }

        float GetTransitionTravel(string spriteName, PictureFrameDisplayMode displayMode, RectangleF viewBox)
        {
            if (displayMode == PictureFrameDisplayMode.Tile)
                return viewBox.Width;

            Vector2 sourceSize;
            var hasSourceSize = TryGetSourceSize(spriteName, out sourceSize);
            var drawSize = GetImageDrawSize(viewBox.Size, sourceSize, hasSourceSize, displayMode, GeneralComponent.GetScale());
            return Math.Max(0f, viewBox.Width * 0.5f + drawSize.X * 0.5f);
        }

        void DrawTiledImage(string spriteName, RectangleF viewBox, float imageScale, Vector2 offset)
        {
            Vector2 sourceSize;
            if (!TryGetSourceSize(spriteName, out sourceSize))
                sourceSize = viewBox.Size;

            sourceSize *= imageScale;
            sourceSize.X = Math.Max(1f, sourceSize.X);
            sourceSize.Y = Math.Max(1f, sourceSize.Y);

            var startX = GetFirstTileCenter(viewBox.X, viewBox.Center.X + offset.X, sourceSize.X);
            var startY = GetFirstTileCenter(viewBox.Y, viewBox.Center.Y + offset.Y, sourceSize.Y);
            var right = viewBox.X + viewBox.Width;
            var bottom = viewBox.Y + viewBox.Height;
            var spriteCount = 0;

            for (var y = startY; y - sourceSize.Y * 0.5f < bottom; y += sourceSize.Y)
            {
                for (var x = startX; x - sourceSize.X * 0.5f < right; x += sourceSize.X)
                {
                    if (spriteCount >= MAX_TILE_SPRITES)
                        return;

                    _sprites.Add(new MySprite
                    {
                        Type = SpriteType.TEXTURE,
                        Data = spriteName,
                        Position = new Vector2(x, y),
                        Size = sourceSize,
                        Color = Color.White,
                        Alignment = TextAlignment.CENTER
                    });
                    spriteCount++;
                }
            }
        }

        static Vector2 GetImageDrawSize(Vector2 viewSize, Vector2 sourceSize, bool hasSourceSize,
            PictureFrameDisplayMode displayMode, float imageScale)
        {
            if (!hasSourceSize || sourceSize.X <= 0f || sourceSize.Y <= 0f)
                return viewSize;

            switch (displayMode)
            {
                case PictureFrameDisplayMode.Center:
                    return sourceSize * imageScale;
                case PictureFrameDisplayMode.Fit:
                    return sourceSize * Math.Max(0f, Math.Min(viewSize.X / sourceSize.X, viewSize.Y / sourceSize.Y));
                case PictureFrameDisplayMode.Fill:
                    return sourceSize * Math.Max(0f, Math.Max(viewSize.X / sourceSize.X, viewSize.Y / sourceSize.Y));
                default:
                    return viewSize;
            }
        }

        static bool TryGetSourceSize(string spriteName, out Vector2 size)
        {
            size = Vector2.Zero;
            Vector2I textureSize;
            if (!TextureHelper.TryGetTextureSize(spriteName, out textureSize))
                return false;

            size = new Vector2(textureSize.X, textureSize.Y);
            return true;
        }

        static float GetFirstTileCenter(float boundsStart, float baseCenter, float tileSize)
        {
            var halfSize = tileSize * 0.5f;
            var start = baseCenter;
            while (start - halfSize > boundsStart)
                start -= tileSize;
            return start;
        }

        static Rectangle ToRectangle(RectangleF rect)
        {
            var x = (int)Math.Floor(rect.X);
            var y = (int)Math.Floor(rect.Y);
            var right = (int)Math.Ceiling(rect.X + rect.Width);
            var bottom = (int)Math.Ceiling(rect.Y + rect.Height);
            return new Rectangle(x, y, Math.Max(0, right - x), Math.Max(0, bottom - y));
        }

        public enum PictureFrameDisplayMode
        {
            Stretch = 0,
            Center = 1,
            Fit = 2,
            Fill = 3,
            Tile = 4
        }
    }
}
