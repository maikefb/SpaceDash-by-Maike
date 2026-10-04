using System.Collections.Generic;
using System.Linq;
using LcdMod.Client.Apps;
using LcdMod.Client.Config;
using LcdMod.Client.Helpers;
using LcdMod.Common.Config.Components;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.ModAPI;
using static LcdMod.Common.Helpers.Constants;

namespace LcdMod.Client.Terminal.Controls.Filter.Listbox
{
    public sealed partial class ListboxSpriteSelected : TerminalControlsListbox
    {
        public ListboxSpriteSelected()
        {
            CreateListbox("SpriteSelectorSelected", MOD_PREFIX + "SpriteSelected");
        }

        protected override void Getter(IMyTerminalBlock b, List<MyTerminalControlListBoxItem> itemList,
            List<MyTerminalControlListBoxItem> selected)
        {
            var config = ConfigManager.GetComponentForTerminalApp<DigitalPictureFramesConfigComponent>(b);
            if (config == null)
                return;

            itemList.AddRange(DigitalPictureFramesApp.GetConfiguredSprites(config)
                .Select(s => ListBoxItemHelper.GetOrComputeListBoxItem(SpriteDisplayName.Of(s), s, s)));
            base.Getter(b, itemList, selected);
        }
    }

    /// <summary>Nome legível de um sprite: o LocalizationId da definição de textura quando existe, senão o próprio id.</summary>
    static class SpriteDisplayName
    {
        public static string Of(string spriteName)
        {
            MyLCDTextureDefinition definition;
            var id = new MyDefinitionId(typeof(MyObjectBuilder_LCDTextureDefinition), spriteName);
            if (MyDefinitionManager.Static.TryGetDefinition(id, out definition) && definition != null
                && !string.IsNullOrWhiteSpace(definition.LocalizationId))
                return MyTexts.GetString(definition.LocalizationId);
            return spriteName;
        }
    }
}
