using System;
using System.Linq;
using LcdMod.Client.Apps;
using LcdMod.Client.Config;
using LcdMod.Client.Terminal.Controls.Filter.Listbox;
using LcdMod.Common.Config.Components;
using LcdMod.Common.Config.Models;
using Sandbox.ModAPI;

namespace LcdMod.Client.Terminal.Controls.Filter.Buttons
{
    public sealed partial class ButtonSpriteRemoveFromSelection : TerminalControlFilterButton
    {
        public ButtonSpriteRemoveFromSelection(TerminalControlsListbox sourceList, TerminalControlsListbox targetList)
            : base(sourceList, targetList)
        {
            CreateButton("SpriteSelectorRemoveFromSelection", "BlockPropertyTitle_ConveyorSorterRemove");
        }

        protected override void Action(IMyTerminalBlock block)
        {
            var picked = TargetList.Selection;
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

            config.SelectedSprites = DigitalPictureFramesApp.GetConfiguredSprites(config)
                .Where(s => !names.Contains(s, StringComparer.OrdinalIgnoreCase))
                .ToArray();
            config.BackgroundSprite = config.SelectedSprites.Length > 0 ? config.SelectedSprites[0] : string.Empty;

            FinishEdit(block, settings);
        }
    }
}
