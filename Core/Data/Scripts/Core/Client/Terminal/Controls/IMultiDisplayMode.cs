using System.Collections.Generic;
using VRage.ModAPI;

namespace LcdMod.Client.Terminal.Controls
{
    public interface IMultiDisplayMode
    {
        List<MyTerminalControlComboBoxItem> GetDisplayModes();
    }
}
