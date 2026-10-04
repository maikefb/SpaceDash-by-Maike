using System;
using System.Collections.Generic;
using System.Linq;
using LcdMod.Client.Apps;
using LcdMod.Client.Config;
using LcdMod.Client.Helpers;
using LcdMod.Common.Config.Components;
using Sandbox.ModAPI;
using VRage.ModAPI;
using static LcdMod.Common.Helpers.Constants;

namespace LcdMod.Client.Terminal.Controls.Filter.Listbox
{
    public sealed partial class ListboxSpriteCandidates : TerminalControlsListbox
    {
        public ListboxSpriteCandidates()
        {
            CreateListbox("SpriteSelectorCandidates", MOD_PREFIX + "SpriteCandidates");
        }

        protected override void Getter(IMyTerminalBlock b, List<MyTerminalControlListBoxItem> itemList,
            List<MyTerminalControlListBoxItem> selected)
        {
            var config = ConfigManager.GetComponentForTerminalApp<DigitalPictureFramesConfigComponent>(b);
            if (config == null)
                return;

            var chosen = DigitalPictureFramesApp.GetConfiguredSprites(config);
            var sprites = new List<string>();
            TextureHelper.GetRegisteredSpriteNames(sprites);

            var provider = b as IMyTextSurfaceProvider;
            var index = GetThisSurfaceIndex(b);
            if (provider != null && index >= 0 && index < provider.SurfaceCount)
                provider.GetSurface(index).GetSprites(sprites);

            itemList.AddRange(sprites
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(s => !chosen.Contains(s, StringComparer.OrdinalIgnoreCase))
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .Select(s => ListBoxItemHelper.GetOrComputeListBoxItem(SpriteDisplayName.Of(s), s, s)));
            base.Getter(b, itemList, selected);
        }
    }
}
