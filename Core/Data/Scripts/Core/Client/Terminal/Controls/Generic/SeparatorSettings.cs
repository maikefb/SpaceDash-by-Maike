using Sandbox.ModAPI.Interfaces.Terminal;

namespace LcdMod.Client.Terminal.Controls.Generic
{
    /// <summary>Separa as configurações comuns das específicas de cada tela. Visível só onde há controles específicos (partial do mod).</summary>
    public sealed partial class SeparatorSettings : TerminalControlsWrapper
    {
        public override IMyTerminalControl TerminalControl { get; }

        public SeparatorSettings()
        {
            var separator = CreateControl<IMyTerminalControlSeparator>("SettingsSeparator");
            separator.Visible = Visible;
            TerminalControl = separator;
        }
    }

    /// <summary>Separa copiar/colar do restante, no fim do terminal.</summary>
    public sealed class SeparatorClipboard : TerminalControlsWrapper
    {
        public override IMyTerminalControl TerminalControl { get; }

        public SeparatorClipboard()
        {
            var separator = CreateControl<IMyTerminalControlSeparator>("ClipboardSeparator");
            separator.Visible = Visible;
            TerminalControl = separator;
        }

        public override bool VisibleForScript(string script)
        {
            return true;
        }
    }
}
