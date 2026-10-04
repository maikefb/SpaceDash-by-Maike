using System;
using System.Linq;
using LcdMod.Client.Apps;
using LcdMod.Client.Config;
using LcdMod.Client.Helpers;
using LcdMod.Client.Terminal.Controls.Filter.Listbox;
using LcdMod.Common.Config.Components;
using LcdMod.Common.Config.Models;
using Sandbox.ModAPI;

namespace LcdMod.Client.Terminal.Controls.Filter.Buttons
{
    public sealed partial class ButtonSpriteAddToSelection : TerminalControlFilterButton
    {
        public ButtonSpriteAddToSelection(TerminalControlsListbox sourceList, TerminalControlsListbox targetList)
            : base(sourceList, targetList)
        {
            CreateButton("SpriteSelectorAddToSelection", "BlockPropertyTitle_ConveyorSorterAdd");
        }

        protected override void Action(IMyTerminalBlock block)
        {
            var picked = SourceList.Selection;
            if (picked == null || picked.Count == 0)
                return;

            ScreenProviderConfig settings;
            SurfaceConfig surface;
            if (!TryBeginEdit(block, out settings, out surface))
                return;
            var config = ConfigManager.GetComponentForTerminalApp<DigitalPictureFramesConfigComponent>(block);
            if (config == null)
                return;

            var names = picked.Select(i => i.UserData as string).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            if (names.Length == 0)
                return;

            var selected = DigitalPictureFramesApp.GetConfiguredSprites(config).ToList();
            foreach (var sprite in names)
                if (!selected.Contains(sprite, StringComparer.OrdinalIgnoreCase))
                    selected.Add(sprite);
            config.SelectedSprites = selected.ToArray();
            config.BackgroundSprite = config.SelectedSprites.Length > 0 ? config.SelectedSprites[0] : string.Empty;

            FinishEdit(block, settings);

            foreach (var sprite in names)
                if (TextureHelper.IsLocalTexture(sprite))
                    TextureHelper.UploadLocalTexture(sprite);
        }
    }
}
