using System;
using System.Text;
using LcdMod.Client.Config;
using LcdMod.Client.Helpers;
using LcdMod.Common.Config.Components;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Utils;
using static LcdMod.Common.Helpers.Constants;

namespace LcdMod.Client.Terminal.Controls.Generic
{
    public sealed partial class SliderImageChangeInterval : TerminalControlsWrapper
    {
        const float MAX_SECONDS = 60f;

        public override IMyTerminalControl TerminalControl { get; }

        public SliderImageChangeInterval()
        {
            var slider = CreateControl<IMyTerminalControlSlider>("ImageChangeIntervalSlider");
            slider.Getter = Getter;
            slider.Setter = Setter;
            slider.Visible = Visible;
            slider.SetLimits(0f, MAX_SECONDS);
            slider.Writer = Writer;
            slider.Title = MyStringId.GetOrCompute(MOD_PREFIX + "ImageChangeInterval");
            slider.Tooltip = MyStringId.GetOrCompute(MOD_PREFIX + "ImageChangeInterval_Tooltip");
            TerminalControl = slider;
        }

        void Writer(IMyTerminalBlock block, StringBuilder text)
        {
            float seconds = Getter(block);
            if (seconds <= 0f)
                text.Append(LocHelper.Disabled);
            else
                text.Append(seconds.ToString("0.#")).Append(" s");
        }

        void Setter(IMyTerminalBlock block, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                value = 0f;
            float clamped = Math.Max(0f, Math.Min(MAX_SECONDS, (float)Math.Round(value, 1)));
            ConfigManager.ModifyComponentForCurrentSurface<DigitalPictureFramesConfigComponent>(
                block, APP, config => config.ImageChangeInterval = clamped);
        }

        float Getter(IMyTerminalBlock block)
        {
            var config = ConfigManager.GetComponentForCurrentSurface<DigitalPictureFramesConfigComponent>(block, APP);
            return config == null ? 0f : config.ImageChangeInterval;
        }
    }
}
