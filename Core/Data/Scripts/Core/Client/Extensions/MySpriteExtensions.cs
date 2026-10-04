using VRage.Game.GUI.TextPanel;
using Color = VRageMath.Color;

namespace LcdMod.Client.Extensions
{
    public static class MySpriteExtensions
    {
        public static MySprite Shadow(this MySprite sprite, float offset, Color? color = null)
        {
            color = color ?? (sprite.Color ?? Color.White).MulValue(0.2f);
            return new MySprite(sprite.Type,
                sprite.Data,
                sprite.Position + offset,
                sprite.Size,
                color,
                sprite.FontId,
                sprite.Alignment,
                sprite.RotationOrScale);
        }
    }
}