using System.Linq;
using LcdMod.Client.Config;
using LcdMod.Client.SurfaceScripts;
using LcdMod.Client.SurfaceScripts.Abstract;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Utils;
using static LcdMod.Common.Helpers.Constants;

namespace LcdMod.Client.Terminal.Controls.Proxy
{
    public sealed partial class ButtonProxyAuto : TerminalControlsWrapper
    {
        public override IMyTerminalControl TerminalControl { get; }

        public ButtonProxyAuto()
        {
            var button = CreateControl<IMyTerminalControlButton>("ProxyAuto");
            button.Action = Apply;
            button.Enabled = Enabled;
            button.Visible = Visible;
            button.Title = MyStringId.GetOrCompute(MOD_PREFIX + "AutoOffset");
            button.Tooltip = MyStringId.GetOrCompute(MOD_PREFIX + "AutoOffset_Tooltip");

            TerminalControl = button;
        }

        void Apply(IMyTerminalBlock block)
        {
            var proxy = GetProxy(block);
            if (proxy == null)
                return;

            proxy.ApplyProxyAutoOffset();
        }

        bool Enabled(IMyTerminalBlock block)
        {
            var proxy = GetProxy(block);
            return proxy != null && proxy.CanApplyProxyAutoOffset();
        }

        RenderProxySurfaceScript GetProxy(IMyTerminalBlock block)
        {
            if (block == null)
                return null;

            var active = SurfaceScriptBase.Instances.GetActiveInstance(block);
            if (active != null)
                return active as RenderProxySurfaceScript;

            var surfaceIndex = GetThisSurfaceIndex(block);
            return ConfigManager.GetAppsForBlock(block)
                .FirstOrDefault(app => app.RotationOrSurfaceIndex == surfaceIndex) as RenderProxySurfaceScript;
        }
    }
}
