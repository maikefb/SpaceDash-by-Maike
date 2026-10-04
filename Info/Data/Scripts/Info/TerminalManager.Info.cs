using System.Collections.Generic;
using System.Text;
using LcdMod.Client.Helpers;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using static LcdMod.Common.Helpers.Constants;
using LcdMod.Client.Terminal.Controls.Blueprint;
using LcdMod.Client.Terminal.Controls.Filter;
using LcdMod.Client.Terminal.Controls.Filter.Buttons;
using LcdMod.Client.Terminal.Controls.Filter.Listbox;
using LcdMod.Client.Terminal.Controls.Generic;
using LcdMod.Client.Terminal.Controls.GeneratedSettings;

namespace LcdMod.Client.Terminal
{
    public partial class TerminalManager
    {
        readonly ButtonProjectorCraftMissing _craftMissing = new ButtonProjectorCraftMissing();

        /// <summary>Arrastar o LCD do Projetor para a barra oferece "Fabricar componentes faltantes" para todas as telas de projeção do bloco.</summary>
        partial void AddModActions(List<ModAction> actions)
        {
            var action = MyAPIGateway.TerminalControls.CreateAction<IMyTerminalBlock>(ID_PREFIX + "ProjectorCraftMissing");
            action.Name = new StringBuilder(LocHelper.GetLoc(MOD_PREFIX + "Projector_CraftMissing"));
            action.Icon = @"Textures\GUI\Icons\Actions\Start.dds";
            action.ValidForGroups = false;
            action.Enabled = block => _craftMissing.MissingOnAnyScreen(block) > 0;
            action.Action = block => _craftMissing.ApplyToAllScreens(block);
            action.Writer = (block, text) => text.Append(_craftMissing.MissingOnAnyScreen(block));
            actions.Add(new ModAction(action, block => _craftMissing.HasProjectorScreen(block)));
        }

        partial void AddModRegistrations(List<TerminalControlRegistration> registrations)
        {
            AddRegistration(registrations, 2950, new SeparatorSettings());
            AddRegistration(registrations, 3010, new SliderRadarRange());
            AddRegistration(registrations, 3020, new GeneratedSwitch_LcdMod_Common_Config_Components_ClockDashboardConfigComponent_Use24HourClock());
            AddRegistration(registrations, 3030, new ComboboxClockDashboardTemperatureMode());
            AddRegistration(registrations, 3040, new SliderAutoScrollStep());
            AddRegistration(registrations, 3300, new ComboboxDisplayMode());
            AddRegistration(registrations, 3325, new ComboboxItemDisplayMode());
            AddRegistration(registrations, 3400, new ComboboxReferenceMode());
            AddRegistration(registrations, 3600, new ListboxReferenceBlockSelection());
            AddRegistration(registrations, 3700, new SwitchToggleLines());
            AddRegistration(registrations, 3800, new ComboboxLinkType());
            AddRegistration(registrations, 3900, new ListboxProjectorSelection());
            AddRegistration(registrations, 3950, _craftMissing);
            AddRegistration(registrations, 4000, new CheckboxHideEmpty());
            AddRegistration(registrations, 4100, new SeparatorFilter());
            AddRegistration(registrations, 4200, new LabelSeparator());

            TerminalControlsListbox source = new ListboxBlockCandidates();
            TerminalControlsListbox target = new ListboxBlockSelected();
            AddRegistration(registrations, 4300, source);
            AddRegistration(registrations, 4400, new ButtonBlockAddToSelection(source, target));
            AddRegistration(registrations, 4500, target);
            AddRegistration(registrations, 4600, new ButtonBlockRemoveFromSelection(source, target));

            source = new ListboxItemsCandidates();
            target = new ListboxItemsSelected();
            AddRegistration(registrations, 4700, source);
            AddRegistration(registrations, 4800, new ButtonItemAddToSelection(source, target));
            AddRegistration(registrations, 4900, target);
            AddRegistration(registrations, 5000, new ButtonItemRemoveFromSelection(source, target));

            source = new ListboxSpriteCandidates();
            target = new ListboxSpriteSelected();
            AddRegistration(registrations, 5100, source);
            AddRegistration(registrations, 5200, new ButtonSpriteAddToSelection(source, target));
            AddRegistration(registrations, 5300, target);
            AddRegistration(registrations, 5400, new ButtonSpriteRemoveFromSelection(source, target));
            AddRegistration(registrations, 5500, new SliderImageChangeInterval());

            AddRegistration(registrations, 3330, new ComboboxSorting());
        }
    }
}
