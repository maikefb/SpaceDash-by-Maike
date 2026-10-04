namespace LcdMod.Client.Gui.ControlsTemplates.Basic
{
    public class ButtonModel : ControlModelBase
    {
        public string Text { get; set; }
        public bool Enabled { get; set; } = true;

        public override string ToString()
        {
            return Text ?? string.Empty;
        }
    }
}
