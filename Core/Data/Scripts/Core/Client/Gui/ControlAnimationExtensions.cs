using System;
using System.Collections.Generic;
using LcdMod.Client.Animation;
using LcdMod.Client.Gui.ControlsTemplates;
using LcdMod.Client.Gui.Styling;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace LcdMod.Client.Gui
{
    internal static class ControlAnimationExtensions
    {
        public static AnimationHandle RunAnimation(this Control control, params IAnimationStep[] keyframes)
        {
            return control.RunAnimation(null, AnimationConflict.Allow, keyframes);
        }

        public static AnimationHandle RunAnimation(this Control control, string channel, params IAnimationStep[] keyframes)
        {
            return control.RunAnimation(channel, AnimationConflict.Replace, keyframes);
        }

        public static AnimationHandle RunAnimation(
            this Control control,
            string channel,
            AnimationConflict conflict,
            params IAnimationStep[] keyframes)
        {
            return control.RunAnimation(control.MarkDirty, channel, conflict, keyframes);
        }

        public static AnimationHandle RunAnimation(
            this Control control,
            Action invalidate,
            string channel,
            AnimationConflict conflict,
            params IAnimationStep[] keyframes)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            AnimationController animationController = control.AnimationController;
            if (animationController == null)
                throw new InvalidOperationException(
"The control must be attached to a visual tree before starting animations.");

            return animationController.Run(
                control,
                invalidate ?? control.MarkDirty,
                channel,
                conflict,
                keyframes);
        }

        public static int ResolveAnimationFrames(this Control control, string resourceName, int fallback)
        {
            if (control == null)
                return Math.Max(0, fallback);

            ResourceKey<int> key;
            int value;
            if (!string.IsNullOrEmpty(resourceName) &&
                ResourceKey.TryGet(resourceName, out key) &&
                ScopedResourceResolver.TryResolve(control, key, out value))
            {
                return Math.Max(0, value);
            }

            return Math.Max(0, fallback);
        }

        public static void CancelAnimations(this Control control)
        {
            if (control == null)
                return;

            AnimationController animationController = control.AnimationController;
            if (animationController != null)
                animationController.CancelOwner(control);
        }

        public static void CancelAnimation(this Control control, string channel)
        {
            control.CancelAnimation(channel, true);
        }

        public static void CancelAnimation(this Control control, string channel, bool requestRedraw)
        {
            if (control == null)
                return;

            AnimationController animationController = control.AnimationController;
            if (animationController != null)
                animationController.Cancel(control, channel, requestRedraw);
        }

        public static void CancelAnimationTree(this Control control)
        {
            if (control == null)
                return;

            control.CancelAnimationTree(control.AnimationController);
        }

        internal static void CancelAnimationTree(
            this Control control,
            AnimationController animationController)
        {
            if (control == null)
                return;

            if (animationController != null)
                animationController.CancelOwner(control, false);

            IReadOnlyList<Control> children = control.LogicalChildren;

            if (children == null)
                return;

            for (int i = 0; i < children.Count; i++)
            {
                Control child = children[i];
                if (child != null)
                    child.CancelAnimationTree(animationController);
            }
        }

        // Clip commands are stateful on the LCD sprite surface. A control
        // must not leak its private rounded-rectangle/content clip into the
        // next sibling. Restore the clip context that was active on entry.
        internal static void RestoreClipAfterRender(
            this ControlTemplate control,
            List<MySprite> sprites,
            int startIndex,
            RectangleF? inheritedClip)
        {
            if (control == null || sprites == null)
                return;

            for (int i = Math.Max(0, startIndex); i < sprites.Count; i++)
            {
                if (sprites[i].Type != SpriteType.CLIP_RECT)
                    continue;

                RestoreInheritedClip(sprites, inheritedClip);
                return;
            }
        }

        static void RestoreInheritedClip(List<MySprite> sprites, RectangleF? inheritedClip)
        {
            sprites.Add(MySprite.CreateClearClipRect());
            if (!inheritedClip.HasValue)
                return;

            RectangleF bounds = inheritedClip.Value;
            int x = (int)Math.Floor(bounds.X);
            int y = (int)Math.Floor(bounds.Y);
            int right = (int)Math.Ceiling(bounds.Right);
            int bottom = (int)Math.Ceiling(bounds.Bottom);
            sprites.Add(MySprite.CreateClipRect(new Rectangle(
                x,
                y,
                Math.Max(0, right - x),
                Math.Max(0, bottom - y))));
        }
    }
}
