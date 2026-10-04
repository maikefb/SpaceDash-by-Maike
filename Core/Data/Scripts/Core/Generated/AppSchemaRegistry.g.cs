// Mantido como fonte estática: tabela de componentes de configuração por app.
using System;
using System.Collections.Generic;
using LcdMod.Common.Config.Components;
using VRage.Game.ModAPI;

namespace Generated
{
    public static class AppSchemaRegistry
    {
        sealed class Slot
        {
            public readonly string Name;
            public readonly Type Type;
            public readonly Func<ConfigComponent> Create;

            public Slot(string name, Type type, Func<ConfigComponent> create)
            {
                Name = name;
                Type = type;
                Create = create;
            }
        }

        static Slot Define<T>(string name) where T : ConfigComponent, new()
        {
            return new Slot(name, typeof(T), () => new T());
        }

        static readonly Slot General = Define<GeneralConfigComponent>("core.general");
        static readonly Slot Colors = Define<ColorConfigComponent>("core.colors");
        static readonly Slot Interaction = Define<InteractiveConfigComponent>("core.interaction");
        static readonly Slot Filters = Define<FilterConfigComponent>("data.filters");
        static readonly Slot Blocks = Define<BlockSelectionConfigComponent>("data.blocks");
        static readonly Slot Items = Define<ItemSelectionConfigComponent>("data.items");
        static readonly Slot ItemDisplay = Define<ItemDisplayConfigComponent>("view.items");
        static readonly Slot PowerSettings = Define<PowerConfigComponent>("app.settings");
        // Telas de energia começam na rede elétrica (juntas mecânicas e conectores acoplados), por onde a energia flui.
        static readonly Slot EnergySettings = new Slot("app.settings", typeof(PowerConfigComponent),
            () => new PowerConfigComponent { GridLinkTypeInternal = (int)GridLinkTypeEnum.Electrical });
        static readonly Slot RadarSettings = Define<RadarConfigComponent>("app.settings");
        static readonly Slot ProjectorReference = Define<BlockReferenceConfigComponent>("reference.projector");
        static readonly Slot DockableReference = Define<BlockReferenceConfigComponent>("reference.dockable");
        static readonly Slot ClockSettings = Define<ClockDashboardConfigComponent>("app.settings");
        static readonly Slot PictureFramesSettings = Define<DigitalPictureFramesConfigComponent>("app.settings");

        static readonly Dictionary<AppType, Slot[]> Schemas = new Dictionary<AppType, Slot[]>
        {
            { AppType.Antenna, new[] { General, Colors, Interaction, Blocks } },
            { AppType.Gas, new[] { General, Colors, Interaction, Blocks } },
            { AppType.Inventory, new[] { General, Colors, Interaction, Filters, Blocks, Items, ItemDisplay } },
            { AppType.PowerFilled, new[] { General, Colors, Interaction, PowerSettings } },
            { AppType.Farm, new[] { General, Colors, Interaction } },
            { AppType.DockingAlignment, new[] { General, Colors, Interaction, DockableReference } },
            { AppType.Radar, new[] { General, Colors, Interaction, RadarSettings } },
            { AppType.Projector, new[] { General, Colors, Interaction, Filters, Blocks, Items, ProjectorReference, ItemDisplay } },
            { AppType.Thrust, new[] { General, Colors, Interaction } },
            { AppType.DefenseDashboard, new[] { General, Colors, Interaction } },
            { AppType.ClockDashboard, new[] { General, Colors, Interaction, ClockSettings } },
            { AppType.Survivors, new[] { General, Colors, Interaction } },
            { AppType.DigitalPictureFrames, new[] { General, Colors, Interaction, PictureFramesSettings } },
            { AppType.EnergyChart, new[] { General, Colors, Interaction, EnergySettings } },
            { AppType.EnergyGeneral, new[] { General, Colors, Interaction, EnergySettings } },
            { AppType.EnergyBatteries, new[] { General, Colors, Interaction, EnergySettings } },
            { AppType.EnergySolar, new[] { General, Colors, Interaction, EnergySettings } },
            { AppType.EnergyWind, new[] { General, Colors, Interaction, EnergySettings } },
            { AppType.EnergyHydrogen, new[] { General, Colors, Interaction, EnergySettings } },
            { AppType.EnergyReactors, new[] { General, Colors, Interaction, EnergySettings } },
        };

        static readonly HashSet<string> RegisteredSlots = new HashSet<string>
        {
            "app.settings", "core.colors", "core.general", "core.interaction", "data.blocks", "data.filters",
            "data.items", "reference.dockable", "reference.projector", "view.items"
        };

        public static readonly AppType[] AllAppTypes = new List<AppType>(Schemas.Keys).ToArray();

        public static bool IsKnownAppType(int appTypeId)
        {
            AppType appType;
            return TryNormalizeAppType(appTypeId, out appType);
        }

        public static bool TryNormalizeAppType(int appTypeId, out AppType appType)
        {
            foreach (var known in Schemas.Keys)
            {
                if ((int)known != appTypeId)
                    continue;
                appType = known;
                return true;
            }

            appType = default(AppType);
            return false;
        }

        public static AppType NormalizeAppType(int appTypeId)
        {
            AppType appType;
            if (!TryNormalizeAppType(appTypeId, out appType))
                throw new Exception("Unknown app type");
            return appType;
        }

        public static string GetName(AppType appType)
        {
            if (!Schemas.ContainsKey(appType))
                throw new Exception("Unknown app type");
            return appType.ToString();
        }

        public static SurfaceConfig CreateSurface(AppType appType, int surfaceIndex)
        {
            if (!IsKnownAppType((int)appType))
                throw new Exception("Unknown app type");
            var surface = new SurfaceConfig
            {
                SurfaceIndex = surfaceIndex,
                Components = new List<ConfigComponentEntry>()
            };
            EnsureSchema(surface, appType);
            return surface;
        }

        public static bool EnsureSchema(IAppConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            AppType appType;
            return TryNormalizeAppType(config.AppTypeId, out appType) && EnsureSchema(config, appType);
        }

        public static bool EnsureSchema(IAppConfig config, AppType appType)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            Slot[] slots;
            if (!Schemas.TryGetValue(appType, out slots)) return false;
            EnsureComponentList(config);
            RemoveRegisteredIncompatibleComponents(config, appType);
            foreach (var slot in slots)
                Ensure(config, slot);
            config.AppTypeId = (int)appType;
            return true;
        }

        public static bool IsEnergyApp(AppType appType)
        {
            return appType >= AppType.EnergyChart && appType <= AppType.EnergyReactors;
        }

        /// <summary>
        ///     Copia os componentes compatíveis da tela anterior. Entre o "Energia %" e as telas de energia o slot app.settings
        ///     tem o mesmo tipo mas padrões de vínculo diferentes; nesse caso fica o padrão da tela nova.
        /// </summary>
        public static void ChangeApp(SurfaceConfig surface, AppType targetAppType)
        {
            if (surface == null) throw new ArgumentNullException(nameof(surface));
            if (!IsKnownAppType((int)targetAppType))
                throw new Exception("Unknown app type");
            var target = CreateSurface(targetAppType, surface.SurfaceIndex);
            AppType sourceAppType;
            bool sameFamily = TryNormalizeAppType(surface.AppTypeId, out sourceAppType) && IsEnergyApp(sourceAppType) == IsEnergyApp(targetAppType);
            var freshSettings = sameFamily ? null : target.TryGet<PowerConfigComponent>("app.settings");
            target.CopyCompatibleFrom(surface);
            if (freshSettings != null)
                target.Set("app.settings", freshSettings);
            if (surface.Components != null)
            {
                foreach (var entry in surface.Components)
                    if (entry != null && !IsRegisteredSlot(entry.Slot))
                        target.Components.Add(entry.Clone());
            }
            surface.Components = target.Components;
            surface.AppTypeId = target.AppTypeId;
            surface.LegacyAppKind = 0;
        }

        public static bool IsRegisteredSlot(string slot)
        {
            return slot != null && RegisteredSlots.Contains(slot);
        }

        public static bool IsAllowedComponent(AppType appType, string slot, Type componentType)
        {
            Slot[] slots;
            if (!Schemas.TryGetValue(appType, out slots))
                return false;

            foreach (var candidate in slots)
                if (candidate.Name == slot && candidate.Type == componentType)
                    return true;

            return false;
        }

        static void EnsureComponentList(IComponentContainer config)
        {
            if (config.Components == null)
                config.Components = new List<ConfigComponentEntry>();
            for (var i = config.Components.Count - 1; i >= 0; i--)
                if (config.Components[i] == null || string.IsNullOrEmpty(config.Components[i].Slot))
                    config.Components.RemoveAt(i);
        }

        static void RemoveRegisteredIncompatibleComponents(IComponentContainer config, AppType appType)
        {
            for (var i = config.Components.Count - 1; i >= 0; i--)
            {
                var entry = config.Components[i];
                if (entry != null && IsRegisteredSlot(entry.Slot)
                    && !IsAllowedComponent(appType, entry.Slot, entry.Value == null ? null : entry.Value.GetType()))
                    config.Components.RemoveAt(i);
            }
        }

        static void Ensure(IComponentContainer config, Slot slot)
        {
            ConfigComponentEntry keeper = null;
            for (var i = config.Components.Count - 1; i >= 0; i--)
            {
                var entry = config.Components[i];
                if (entry == null || entry.Slot != slot.Name) continue;
                if (keeper == null && entry.Value != null && entry.Value.GetType() == slot.Type)
                    keeper = entry;
                else
                    config.Components.RemoveAt(i);
            }
            if (keeper == null)
                config.Components.Add(new ConfigComponentEntry(slot.Name, slot.Create()));
        }
    }
}
