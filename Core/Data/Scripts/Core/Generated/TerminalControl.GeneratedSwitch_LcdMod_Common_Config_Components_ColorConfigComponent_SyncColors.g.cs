// Mantido como fonte estática: switch "Sincronizar cores" por facção.
namespace LcdMod.Client.Terminal.Controls.GeneratedSettings
{
    internal sealed class GeneratedSwitch_LcdMod_Common_Config_Components_ColorConfigComponent_SyncColors : global::LcdMod.Client.Terminal.Controls.TerminalControlsWrapper
    {
        static readonly global::LcdMod.Common.Config.Components.ColorConfigComponent DefaultComponent = new global::LcdMod.Common.Config.Components.ColorConfigComponent();

        public override global::Sandbox.ModAPI.Interfaces.Terminal.IMyTerminalControl TerminalControl { get; }

        public GeneratedSwitch_LcdMod_Common_Config_Components_ColorConfigComponent_SyncColors()
        {
            var control = CreateControl<global::Sandbox.ModAPI.Interfaces.Terminal.IMyTerminalControlOnOffSwitch>("SwitchSyncColors");
            control.Getter = Getter;
            control.Setter = Setter;
            control.Visible = Visible;
            control.Title = global::VRage.Utils.MyStringId.GetOrCompute("LcdMod_SyncColors");
            control.Tooltip = global::VRage.Utils.MyStringId.GetOrCompute("LcdMod_SyncColors_Tooltip");
            control.OnText = global::VRage.Utils.MyStringId.GetOrCompute("HudInfoOn");
            control.OffText = global::VRage.Utils.MyStringId.GetOrCompute("HudInfoOff");
            TerminalControl = control;
        }

        public override bool VisibleForScript(string script)
        {
            return true;
        }

        protected override bool IsAvailableForCurrentConfig(global::Sandbox.ModAPI.IMyTerminalBlock block)
        {
            var component = global::LcdMod.Client.Config.ConfigManager.GetComponentForCurrentSurface<global::LcdMod.Common.Config.Components.ColorConfigComponent>(
                block,
"core.colors");
            return component != null
                   && global::LcdMod.Common.FactionColors.FactionColorsStore.FactionIdOf(block) != 0L;
        }

        void Setter(global::Sandbox.ModAPI.IMyTerminalBlock block, bool value)
        {
            var surface = GetThisSurface(block);
            if (global::LcdMod.Client.Config.ConfigManager.ModifyComponentForCurrentSurface<global::LcdMod.Common.Config.Components.ColorConfigComponent>(
                    block,
"core.colors",
                    component =>
                    {
                        // Ligar: adota a paleta da facção (ou publica as cores atuais se ainda não há paleta).
                        // Desligar: congela as cores da paleta nesta tela, como cores personalizadas.
                        bool copied = global::LcdMod.Client.FactionColors.FactionColorsSync.TryCopyToComponent(block, component, surface);
                        component.SyncColors = value;
                        if (value && !copied)
                            global::LcdMod.Client.FactionColors.FactionColorsSync.PublishModColors(block, component, surface);
                        if (!value && copied)
                            component.CustomizedColors = true;
                    }))
                global::LcdMod.Client.Extensions.IMyTerminalBlockExtensions.RefreshTerminal(block);
        }

        bool Getter(global::Sandbox.ModAPI.IMyTerminalBlock block)
        {
            var component = global::LcdMod.Client.Config.ConfigManager.GetComponentForCurrentSurface<global::LcdMod.Common.Config.Components.ColorConfigComponent>(
                block,
"core.colors");
            return component == null
                ? DefaultComponent.SyncColors
                : component.SyncColors;
        }
    }
}
