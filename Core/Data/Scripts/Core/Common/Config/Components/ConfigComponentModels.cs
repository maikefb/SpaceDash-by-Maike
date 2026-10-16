// ReSharper disable RedundantUsingDirective
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using ProtoBuf;
using Generated;
using VRage.Game.ModAPI;
using VRageMath;
using static LcdMod.Common.Helpers.Constants;

namespace LcdMod.Common.Config.Components
{
    [ProtoContract]
    [ProtoInclude(101, typeof(GeneralConfigComponent))]
    [ProtoInclude(102, typeof(ColorConfigComponent))]
    [ProtoInclude(103, typeof(InteractiveConfigComponent))]
    [ProtoInclude(104, typeof(FilterConfigComponent))]
    [ProtoInclude(105, typeof(BlockSelectionConfigComponent))]
    [ProtoInclude(106, typeof(ItemSelectionConfigComponent))]
    [ProtoInclude(107, typeof(BlockReferenceConfigComponent))]
    [ProtoInclude(108, typeof(PowerConfigComponent))]
    [ProtoInclude(109, typeof(RadarConfigComponent))]
    [ProtoInclude(113, typeof(RenderProxyConfigComponent))]
    [ProtoInclude(116, typeof(DigitalPictureFramesConfigComponent))]
    [ProtoInclude(119, typeof(ClockDashboardConfigComponent))]
    [ProtoInclude(124, typeof(ItemDisplayConfigComponent))]
    [ProtoInclude(125, typeof(DefenseDashboardConfigComponent))]
    // Tags 110-112, 114, 115, 117, 118 e 120-123 ficaram reservadas: componentes removidos que este mod nunca gravou.
    [XmlInclude(typeof(GeneralConfigComponent))]
    [XmlInclude(typeof(ColorConfigComponent))]
    [XmlInclude(typeof(InteractiveConfigComponent))]
    [XmlInclude(typeof(FilterConfigComponent))]
    [XmlInclude(typeof(BlockSelectionConfigComponent))]
    [XmlInclude(typeof(ItemSelectionConfigComponent))]
    [XmlInclude(typeof(BlockReferenceConfigComponent))]
    [XmlInclude(typeof(PowerConfigComponent))]
    [XmlInclude(typeof(RadarConfigComponent))]
    [XmlInclude(typeof(RenderProxyConfigComponent))]
    [XmlInclude(typeof(DigitalPictureFramesConfigComponent))]
    [XmlInclude(typeof(ClockDashboardConfigComponent))]
    [XmlInclude(typeof(DefenseDashboardConfigComponent))]
    [XmlInclude(typeof(ItemDisplayConfigComponent))]
    public abstract class ConfigComponent
    {
        public abstract ConfigComponent Clone();
    }

    [ProtoContract]
    public sealed class ConfigComponentEntry
    {
        public ConfigComponentEntry()
        {
        }

        public ConfigComponentEntry(string slot, ConfigComponent value)
        {
            Slot = slot;
            Value = value;
        }

        [ProtoMember(1)] public string Slot { get; set; }
        [ProtoMember(2)] public ConfigComponent Value { get; set; }

        public ConfigComponentEntry Clone()
        {
            return new ConfigComponentEntry(Slot, Value == null ? null : Value.Clone());
        }
    }

    /// <summary>Common component-bearing shape shared by top-level and deferred nested configs.</summary>
    public interface IComponentContainer
    {
        List<ConfigComponentEntry> Components { get; set; }
    }

    /// <summary>A top-level persisted app configuration with a concrete generated app identity.</summary>
    public interface IAppConfig : IComponentContainer
    {
        int AppTypeId { get; set; }
    }

    public static class ComponentConfigExtensions
    {
        public static T TryGet<T>(this IComponentContainer config, string slot) where T : ConfigComponent
        {
            if (config == null || config.Components == null)
                return null;

            var entry = config.Components.FirstOrDefault(component =>
                component != null && component.Slot == slot && component.Value is T);
            return entry == null ? null : entry.Value as T;
        }

        public static T Get<T>(this IComponentContainer config, string slot) where T : ConfigComponent
        {
            var component = config.TryGet<T>(slot);
            if (component == null)
                throw new InvalidOperationException(
                    $"Missing config component '{slot}' ({typeof(T).Name}) for app {GetAppIdentity(config)}.");
            return component;
        }

        public static T TryGetComponent<T>(this IComponentContainer config) where T : ConfigComponent
        {
            if (config == null || config.Components == null)
                return null;

            T result = null;
            foreach (var entry in config.Components)
            {
                var component = entry == null ? null : entry.Value as T;
                if (component == null)
                    continue;
                if (result != null)
                    throw new InvalidOperationException(
                        $"Multiple {typeof(T).Name} components exist for app {GetAppIdentity(config)}; use the slot overload.");
                result = component;
            }
            return result;
        }

        public static T GetComponent<T>(this IComponentContainer config) where T : ConfigComponent
        {
            var component = config.TryGetComponent<T>();
            if (component == null)
                throw new InvalidOperationException(
                    $"Missing {typeof(T).Name} component for app {GetAppIdentity(config)}.");
            return component;
        }

        public static T TryGetComponent<T>(this IComponentContainer config, string slot) where T : ConfigComponent
        {
            return config.TryGet<T>(slot);
        }

        public static T GetComponent<T>(this IComponentContainer config, string slot) where T : ConfigComponent
        {
            return config.Get<T>(slot);
        }

        public static void Set(this IComponentContainer config, string slot, ConfigComponent component)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (config.Components == null)
                config.Components = new List<ConfigComponentEntry>();

            var existing = config.Components.FirstOrDefault(entry => entry != null && entry.Slot == slot);
            if (existing == null)
                config.Components.Add(new ConfigComponentEntry(slot, component));
            else
                existing.Value = component;
        }

        /// <summary>
        /// Copies only slots that exist in both schemas and have the same component data shape.
        /// Reference components therefore copy only when their semantic slot also matches.
        /// </summary>
        public static void CopyCompatibleFrom(this IComponentContainer targetConfig, IComponentContainer sourceConfig)
        {
            if (sourceConfig?.Components == null || targetConfig?.Components == null)
                return;

            foreach (var target in targetConfig.Components)
            {
                if (target?.Value == null)
                    continue;

                var sourceEntry = sourceConfig.Components.FirstOrDefault(candidate =>
                    candidate?.Value != null
                    && candidate.Slot == target.Slot
                    && candidate.Value.GetType() == target.Value.GetType());

                if (sourceEntry != null)
                    target.Value = sourceEntry.Value.Clone();
            }
        }

        public static List<ConfigComponentEntry> CloneComponents(this IComponentContainer config)
        {
            return config?.Components == null
                ? new List<ConfigComponentEntry>()
                : config.Components.Where(entry => entry != null).Select(entry => entry.Clone()).ToList();
        }

        static int GetAppIdentity(IComponentContainer config)
        {
            var appConfig = config as IAppConfig;
            return appConfig == null ? 0 : appConfig.AppTypeId;
        }
    }

    [ProtoContract]
    public sealed class SurfaceConfig : IAppConfig
    {
        [ProtoMember(1)] public int SurfaceIndex { get; set; }

        // Public V0 migration hint. Migration-only; never write new AppType values here.
        [ProtoMember(2)]
        [XmlElement("AppKind")]
        public int LegacyAppKind { get; set; }

        [ProtoMember(3)]
        [XmlArrayItem("Component")]
        public List<ConfigComponentEntry> Components { get; set; } = new List<ConfigComponentEntry>();

        // Component-schema V1 concrete app identity generated from [LcdApp].
        [ProtoMember(4)] public int AppTypeId { get; set; }

        public SurfaceConfig Clone()
        {
            return new SurfaceConfig
            {
                SurfaceIndex = SurfaceIndex,
                LegacyAppKind = LegacyAppKind,
                AppTypeId = AppTypeId,
                Components = this.CloneComponents()
            };
        }
    }

    [ProtoContract]
    public sealed class GeneralConfigComponent : ConfigComponent
    {
        [ProtoMember(1)]
        
        public bool TitleVisible { get; set; } = true;
        [ProtoMember(2)] public float InternalScale { get; set; } = 1f;
        [ProtoMember(3)] public bool DrawLines { get; set; }
        [ProtoMember(4)] public int DisplayMode { get; set; }
        [ProtoMember(5)] public OptionalValue<byte> BackgroundAlpha { get; set; } = new OptionalValue<byte>();
        [ProtoMember(6)] public int ThemeInternal { get; set; }
        public override ConfigComponent Clone()
        {
            return new GeneralConfigComponent
            {
                TitleVisible = TitleVisible,
                InternalScale = InternalScale,
                DrawLines = DrawLines,
                DisplayMode = DisplayMode,
                BackgroundAlpha = ConfigComponentClone.Copy(BackgroundAlpha),
                ThemeInternal = ThemeInternal
            };
        }
    }

    [ProtoContract]
    public sealed class ColorConfigComponent : ConfigComponent
    {
        [ProtoMember(1)]
        
        public OptionalValue<Color> HeaderColor { get; set; } = new OptionalValue<Color>();
        [ProtoMember(2)]
        
        public OptionalValue<Color> ErrorColor { get; set; } = new OptionalValue<Color>();
        [ProtoMember(3)]
        
        public OptionalValue<Color> WarningColor { get; set; } = new OptionalValue<Color>();
        [ProtoMember(4)]
        
        public bool CustomizedColors { get; set; }
        // ProtoMember 5 era SyncColors (removido); não reutilizar.

        public override ConfigComponent Clone()
        {
            return new ColorConfigComponent
            {
                HeaderColor = ConfigComponentClone.Copy(HeaderColor),
                ErrorColor = ConfigComponentClone.Copy(ErrorColor),
                WarningColor = ConfigComponentClone.Copy(WarningColor),
                CustomizedColors = CustomizedColors
            };
        }
    }

    [ProtoContract]
    public sealed class InteractiveConfigComponent : ConfigComponent
    {
        [ProtoMember(3)] public int ReferenceMode { get; set; }
        [ProtoMember(4)] public float AutoScrollStep { get; set; } = 2f;

        public override ConfigComponent Clone()
        {
            return new InteractiveConfigComponent
            {
                ReferenceMode = ReferenceMode,
                AutoScrollStep = AutoScrollStep
            };
        }
    }

    [ProtoContract]
    public sealed class FilterConfigComponent : ConfigComponent
    {
        [ProtoMember(1)] public int SortMethod { get; set; }
        [ProtoMember(2)] public bool HideEmpty { get; set; } = true;
        // -1 preserves the historical defaults: amount descending, item ascending.
        [ProtoMember(3)] public int SortDirection { get; set; } = -1;

        public override ConfigComponent Clone()
        {
            return new FilterConfigComponent
            {
                SortMethod = SortMethod,
                HideEmpty = HideEmpty,
                SortDirection = SortDirection
            };
        }
    }

    [ProtoContract]
    public sealed class BlockSelectionConfigComponent : ConfigComponent
    {
        [ProtoMember(1)] public long[] SelectedBlocks { get; set; } = Array.Empty<long>();
        [ProtoMember(2)] public string[] SelectedGroups { get; set; } = Array.Empty<string>();
        [ProtoMember(3)] public int GridLinkTypeInternal { get; set; } = 1;
        [ProtoMember(4)] public string[] SortFilterKeys { get; set; } = Array.Empty<string>();
        [ProtoMember(5)] public string[] SortFilterCategories { get; set; } = Array.Empty<string>();

        public override ConfigComponent Clone()
        {
            return new BlockSelectionConfigComponent
            {
                SelectedBlocks = ConfigComponentClone.Copy(SelectedBlocks),
                SelectedGroups = ConfigComponentClone.Copy(SelectedGroups),
                GridLinkTypeInternal = GridLinkTypeInternal,
                SortFilterKeys = ConfigComponentClone.Copy(SortFilterKeys),
                SortFilterCategories = ConfigComponentClone.Copy(SortFilterCategories)
            };
        }
    }

    [ProtoContract]
    public sealed class ItemSelectionConfigComponent : ConfigComponent
    {
        [ProtoMember(1)] public string[] SelectedDefinition { get; set; } = Array.Empty<string>();
        [ProtoMember(2)] public string[] SelectedCategories { get; set; } = Array.Empty<string>();

        public override ConfigComponent Clone()
        {
            return new ItemSelectionConfigComponent
            {
                SelectedDefinition = ConfigComponentClone.Copy(SelectedDefinition),
                SelectedCategories = ConfigComponentClone.Copy(SelectedCategories)
            };
        }
    }

    [ProtoContract]
    public sealed class ItemDisplayConfigComponent : ConfigComponent
    {
        // -1 means this component was newly added and still needs to import the
        // former General.DisplayMode + General.DrawLines combination.
        [ProtoMember(1)] public int DisplayMode { get; set; } = -1;

        public override ConfigComponent Clone()
        {
            return new ItemDisplayConfigComponent
            {
                DisplayMode = DisplayMode
            };
        }
    }

    [ProtoContract]
    public sealed class BlockReferenceConfigComponent : ConfigComponent
    {
        [ProtoMember(1)] public long EntityId { get; set; }

        public override ConfigComponent Clone()
        {
            return new BlockReferenceConfigComponent { EntityId = EntityId };
        }
    }

    [ProtoContract]
    public sealed class PowerConfigComponent : ConfigComponent
    {
        [ProtoMember(1), DefaultValue(true)] public bool HideEmpty { get; set; } = true;
        [ProtoMember(4), DefaultValue((int)GridLinkTypeEnum.Mechanical)] public int GridLinkTypeInternal { get; set; } = (int)GridLinkTypeEnum.Mechanical;

        public override ConfigComponent Clone()
        {
            return new PowerConfigComponent
            {
                HideEmpty = HideEmpty,
                GridLinkTypeInternal = GridLinkTypeInternal
            };
        }
    }

    [ProtoContract]
    public sealed class RadarConfigComponent : ConfigComponent
    {
        [ProtoMember(1)] public float RangeScale { get; set; } = 1f;
        [ProtoMember(2)] public double CameraPanX { get; set; }
        [ProtoMember(3)] public double CameraPanY { get; set; }
        [ProtoMember(4)] public float CameraZoomScale { get; set; } = 1f;

        public override ConfigComponent Clone()
        {
            return new RadarConfigComponent
            {
                RangeScale = RangeScale,
                CameraPanX = CameraPanX,
                CameraPanY = CameraPanY,
                CameraZoomScale = CameraZoomScale
            };
        }
    }

    [ProtoContract]
    public sealed class RenderProxyConfigComponent : ConfigComponent
    {
        [ProtoMember(1)]
        public sbyte XAxisOffset { get; set; }
        [ProtoMember(2)]
        public sbyte YAxisOffset { get; set; }
        [ProtoMember(3)]
        public bool EnableAutoAdjust { get; set; } = true;

        public override ConfigComponent Clone()
        {
            return new RenderProxyConfigComponent
            {
                XAxisOffset = XAxisOffset,
                YAxisOffset = YAxisOffset,
                EnableAutoAdjust = EnableAutoAdjust
            };
        }
    }

    [ProtoContract]
    public sealed class DigitalPictureFramesConfigComponent : ConfigComponent
    {
        [ProtoMember(1)] public string BackgroundSprite { get; set; } = string.Empty;
        [ProtoMember(2)] public string[] SelectedSprites { get; set; } = Array.Empty<string>();
        [ProtoMember(3)]
        
        public float ImageChangeInterval { get; set; }

        public override ConfigComponent Clone()
        {
            return new DigitalPictureFramesConfigComponent
            {
                BackgroundSprite = BackgroundSprite,
                SelectedSprites = ConfigComponentClone.Copy(SelectedSprites),
                ImageChangeInterval = ImageChangeInterval
            };
        }
    }

    [ProtoContract]
    public sealed class ClockDashboardConfigComponent : ConfigComponent
    {
        [ProtoMember(1)]
        
        public bool Use24HourClock { get; set; } = true;
        [ProtoMember(2)] public int TemperatureModeInternal { get; set; }

        public override ConfigComponent Clone()
        {
            return new ClockDashboardConfigComponent
            {
                Use24HourClock = Use24HourClock,
                TemperatureModeInternal = TemperatureModeInternal
            };
        }
    }

    [ProtoContract]
    public sealed class DefenseDashboardConfigComponent : ConfigComponent
    {
        [ProtoMember(1)] public bool ShowPercent { get; set; }

        public override ConfigComponent Clone()
        {
            return new DefenseDashboardConfigComponent { ShowPercent = ShowPercent };
        }
    }

    static class ConfigComponentClone
    {
        public static OptionalValue<T> Copy<T>(OptionalValue<T> value)
        {
            return value == null
                ? new OptionalValue<T>()
                : new OptionalValue<T> { HasValue = value.HasValue, Value = value.Value };
        }

        public static T[] Copy<T>(T[] values)
        {
            if (values == null || values.Length == 0)
                return Array.Empty<T>();
            return (T[])values.Clone();
        }
    }
}
