// Mantido como fonte estática: controles de terminal derivados dos componentes de configuração comuns.
using System.Collections.Generic;
using LcdMod.Client.Terminal;

namespace Generated
{
    public static class GeneratedTerminalControlRegistry
    {
        public static void AddTo(List<TerminalControlRegistration> registrations)
        {
            registrations.Add(new TerminalControlRegistration(
                2150,
                new global::LcdMod.Client.Terminal.Controls.GeneratedSettings.GeneratedSwitch_LcdMod_Common_Config_Components_ColorConfigComponent_SyncColors()));
            registrations.Add(new TerminalControlRegistration(
                2200,
                new global::LcdMod.Client.Terminal.Controls.GeneratedSettings.GeneratedSwitch_LcdMod_Common_Config_Components_ColorConfigComponent_CustomizedColors()));
            registrations.Add(new TerminalControlRegistration(
                2300,
                new global::LcdMod.Client.Terminal.Controls.GeneratedSettings.GeneratedColor_LcdMod_Common_Config_Components_ColorConfigComponent_HeaderColor()));
            registrations.Add(new TerminalControlRegistration(
                2400,
                new global::LcdMod.Client.Terminal.Controls.GeneratedSettings.GeneratedColor_LcdMod_Common_Config_Components_ColorConfigComponent_WarningColor()));
            registrations.Add(new TerminalControlRegistration(
                2500,
                new global::LcdMod.Client.Terminal.Controls.GeneratedSettings.GeneratedColor_LcdMod_Common_Config_Components_ColorConfigComponent_ErrorColor()));
            registrations.Add(new TerminalControlRegistration(
                2600,
                new global::LcdMod.Client.Terminal.Controls.GeneratedSettings.GeneratedSwitch_LcdMod_Common_Config_Components_GeneralConfigComponent_TitleVisible()));
        }
    }
}
