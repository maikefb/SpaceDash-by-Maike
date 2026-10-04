using LcdMod.Client.SurfaceScripts;

namespace LcdMod.Client.Terminal.Controls.Generic
{
    public partial class ComboboxDisplayMode
    {
        public override bool VisibleForScript(string script)
        {
            switch (script)
            {
                case GasSurfaceScript.ID:
                case AntennaSurfaceScript.ID:
                case DockingAlignment.ID:
                case DigitalPictureFramesSurfaceScript.ID:
                    return true;
                default:
                    return false;
            }
        }
    }

    public partial class ComboboxReferenceMode
    {
        public override bool VisibleForScript(string script)
        {
            switch (script)
            {
                case RadarSurfaceScript.ID:
                    return true;
                default:
                    return false;
            }
        }
    }

    public partial class SliderRadarRange
    {
        public override bool VisibleForScript(string script)
        {
            switch (script)
            {
                case RadarSurfaceScript.ID:
                    return true;
                default:
                    return false;
            }
        }
    }

    public partial class ListboxReferenceBlockSelection
    {
        public override bool VisibleForScript(string script)
        {
            switch (script)
            {
                case DockingAlignment.ID:
                    return true;
                default:
                    return false;
            }
        }
    }

    public partial class ComboboxLinkType
    {
        public override bool VisibleForScript(string script)
        {
            switch (script)
            {
                case GasSurfaceScript.ID:
                case PowerFilledSurfaceScript.ID:
                case AntennaSurfaceScript.ID:
                case InventoryLcdSurfaceScript.ID:
                case ProjectorLcdSurfaceScript.ID:
                case EnergyChartSurfaceScript.ID:
                case EnergyGeneralSurfaceScript.ID:
                case EnergyBatteriesSurfaceScript.ID:
                case EnergySolarSurfaceScript.ID:
                case EnergyWindSurfaceScript.ID:
                case EnergyHydrogenSurfaceScript.ID:
                case EnergyReactorsSurfaceScript.ID:
                    return true;
                default:
                    return false;
            }
        }
    }

    public partial class SliderAutoScrollStep
    {
        public override bool VisibleForScript(string script)
        {
            switch (script)
            {
                case GasSurfaceScript.ID:
                case PowerFilledSurfaceScript.ID:
                case FarmSurfaceScript.ID:
                case AntennaSurfaceScript.ID:
                case InventoryLcdSurfaceScript.ID:
                case SurvivorsSurfaceScript.ID:
                    return true;
                default:
                    return false;
            }
        }
    }

    public partial class ComboboxItemDisplayMode
    {
        public override bool VisibleForScript(string script)
        {
            switch (script)
            {
                case InventoryLcdSurfaceScript.ID:
                case ProjectorLcdSurfaceScript.ID:
                    return true;
                default:
                    return false;
            }
        }
    }

    public partial class CheckboxHideEmpty
    {
        public override bool VisibleForScript(string script)
        {
            switch (script)
            {
                case InventoryLcdSurfaceScript.ID:
                    return true;
                default:
                    return false;
            }
        }
    }

    public partial class ComboboxSorting
    {
        public override bool VisibleForScript(string script)
        {
            switch (script)
            {
                case InventoryLcdSurfaceScript.ID:
                    return true;
                default:
                    return false;
            }
        }
    }

    public partial class ComboboxClockDashboardTemperatureMode
    {
        public override bool VisibleForScript(string script)
        {
            switch (script)
            {
                case InGameClockDashboardSurfaceScript.ID:
                    return true;
                default:
                    return false;
            }
        }
    }

    public partial class SwitchToggleLines
    {
        public override bool VisibleForScript(string script)
        {
            switch (script)
            {
                case AntennaSurfaceScript.ID:
                    return true;
                default:
                    return false;
            }
        }
    }
}

namespace LcdMod.Client.Terminal.Controls.Filter
{
    public partial class SeparatorFilter
    {
        public override bool VisibleForScript(string script)
        {
            switch (script)
            {
                case AntennaSurfaceScript.ID:
                case InventoryLcdSurfaceScript.ID:
                case ProjectorLcdSurfaceScript.ID:
                    return true;
                default:
                    return false;
            }
        }
    }

    public partial class LabelSeparator
    {
        public override bool VisibleForScript(string script)
        {
            switch (script)
            {
                case AntennaSurfaceScript.ID:
                case InventoryLcdSurfaceScript.ID:
                case ProjectorLcdSurfaceScript.ID:
                    return true;
                default:
                    return false;
            }
        }
    }
}

namespace LcdMod.Client.Terminal.Controls.Filter.Listbox
{
    public partial class ListboxBlockCandidates
    {
        public override bool VisibleForScript(string script)
        {
            switch (script)
            {
                case AntennaSurfaceScript.ID:
                case InventoryLcdSurfaceScript.ID:
                case ProjectorLcdSurfaceScript.ID:
                    return true;
                default:
                    return false;
            }
        }
    }

    public partial class ListboxBlockSelected
    {
        public override bool VisibleForScript(string script)
        {
            switch (script)
            {
                case AntennaSurfaceScript.ID:
                case InventoryLcdSurfaceScript.ID:
                case ProjectorLcdSurfaceScript.ID:
                    return true;
                default:
                    return false;
            }
        }
    }

    public partial class ListboxItemsCandidates
    {
        public override bool VisibleForScript(string script)
        {
            switch (script)
            {
                case InventoryLcdSurfaceScript.ID:
                    return true;
                default:
                    return false;
            }
        }
    }

    public partial class ListboxItemsSelected
    {
        public override bool VisibleForScript(string script)
        {
            switch (script)
            {
                case InventoryLcdSurfaceScript.ID:
                    return true;
                default:
                    return false;
            }
        }
    }
}

namespace LcdMod.Client.Terminal.Controls.Filter.Buttons
{
    public partial class ButtonBlockAddToSelection
    {
        public override bool VisibleForScript(string script)
        {
            switch (script)
            {
                case AntennaSurfaceScript.ID:
                case InventoryLcdSurfaceScript.ID:
                case ProjectorLcdSurfaceScript.ID:
                    return true;
                default:
                    return false;
            }
        }
    }

    public partial class ButtonBlockRemoveFromSelection
    {
        public override bool VisibleForScript(string script)
        {
            switch (script)
            {
                case AntennaSurfaceScript.ID:
                case InventoryLcdSurfaceScript.ID:
                case ProjectorLcdSurfaceScript.ID:
                    return true;
                default:
                    return false;
            }
        }
    }

    public partial class ButtonItemAddToSelection
    {
        public override bool VisibleForScript(string script)
        {
            switch (script)
            {
                case InventoryLcdSurfaceScript.ID:
                    return true;
                default:
                    return false;
            }
        }
    }

    public partial class ButtonItemRemoveFromSelection
    {
        public override bool VisibleForScript(string script)
        {
            switch (script)
            {
                case InventoryLcdSurfaceScript.ID:
                    return true;
                default:
                    return false;
            }
        }
    }
}

namespace LcdMod.Client.Terminal.Controls.Blueprint
{
    public partial class ButtonProjectorCraftMissing
    {
        public override bool VisibleForScript(string script)
        {
            switch (script)
            {
                case ProjectorLcdSurfaceScript.ID:
                    return true;
                default:
                    return false;
            }
        }
    }

    public partial class ListboxProjectorSelection
    {
        public override bool VisibleForScript(string script)
        {
            switch (script)
            {
                case ProjectorLcdSurfaceScript.ID:
                    return true;
                default:
                    return false;
            }
        }
    }
}

namespace LcdMod.Client.Terminal.Controls.Filter.Listbox
{
    public partial class ListboxSpriteCandidates
    {
        public override bool VisibleForScript(string script)
        {
            return script == LcdMod.Client.SurfaceScripts.DigitalPictureFramesSurfaceScript.ID;
        }
    }
}

namespace LcdMod.Client.Terminal.Controls.Filter.Listbox
{
    public partial class ListboxSpriteSelected
    {
        public override bool VisibleForScript(string script)
        {
            return script == LcdMod.Client.SurfaceScripts.DigitalPictureFramesSurfaceScript.ID;
        }
    }
}

namespace LcdMod.Client.Terminal.Controls.Filter.Buttons
{
    public partial class ButtonSpriteAddToSelection
    {
        public override bool VisibleForScript(string script)
        {
            return script == LcdMod.Client.SurfaceScripts.DigitalPictureFramesSurfaceScript.ID;
        }
    }
}

namespace LcdMod.Client.Terminal.Controls.Filter.Buttons
{
    public partial class ButtonSpriteRemoveFromSelection
    {
        public override bool VisibleForScript(string script)
        {
            return script == LcdMod.Client.SurfaceScripts.DigitalPictureFramesSurfaceScript.ID;
        }
    }
}

namespace LcdMod.Client.Terminal.Controls.Generic
{
    public partial class SliderImageChangeInterval
    {
        public override bool VisibleForScript(string script)
        {
            return script == LcdMod.Client.SurfaceScripts.DigitalPictureFramesSurfaceScript.ID;
        }
    }
}

namespace LcdMod.Client.Terminal.Controls.Generic
{
    public partial class SeparatorSettings
    {
        public override bool VisibleForScript(string script)
        {
            switch (script)
            {
                case LcdMod.Client.SurfaceScripts.DefenseDashboardSurfaceScript.ID:
                case LcdMod.Client.SurfaceScripts.ThrustSurfaceScript.ID:
                    return false;
                default:
                    return true;
            }
        }
    }
}
