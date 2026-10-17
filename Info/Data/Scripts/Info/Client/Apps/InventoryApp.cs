using System;
using System.Collections.Generic;
using LcdMod.Client.Apps.Abstract;
using LcdMod.Client.Apps.ViewModel;
using LcdMod.Client.Config;
using LcdMod.Client.GridData;
using LcdMod.Client.Gui.Styling;
using LcdMod.Client.Helpers;
using LcdMod.Common.Config.Components;
using LcdMod.Common.Helpers;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;

using LcdMod.Common.Config.Generation;
using static LcdMod.Common.Helpers.Constants;
namespace LcdMod.Client.Apps
{


    // ReSharper disable once PartialTypeWithSinglePart
    internal sealed partial class InventoryApp : ItemsApp
    {
        public const string NAME = "Inventory";
        const string SEPARATOR = " · ";
        const float BAR_HEIGHT_BASE = 6f;
        const float VOLUME_ATTENTION = 0.8f;
        const float VOLUME_CRITICAL = 0.95f;
        const double CUBIC_METERS_TO_LITERS = 1000d;
        const string TYPE_ORE = "MyObjectBuilder_Ore";
        const string TYPE_INGOT = "MyObjectBuilder_Ingot";
        const string TYPE_COMPONENT = "MyObjectBuilder_Component";
        const string TYPE_AMMO = "MyObjectBuilder_AmmoMagazine";
        const string TYPE_TOOL = "MyObjectBuilder_PhysicalGunObject";

        readonly List<BareLayout.KvRow> _kv = new List<BareLayout.KvRow>();
        const long GROUP_REFRESH_FRAMES = 300;
        readonly List<IMyTerminalBlock> _groupBlocks = new List<IMyTerminalBlock>();
        readonly HashSet<long> _seenBlocks = new HashSet<long>();
        string[] _lastGroups;
        long _groupsFrame = -GROUP_REFRESH_FRAMES;
        readonly LinkedTypedBlockSourceSet<IMyTerminalBlock> _blocks =
            new LinkedTypedBlockSourceSet<IMyTerminalBlock>(delegate(TypedBlockCollection blocks)
            {
                return blocks.TerminalBlocks;
            });

        double _volumeUsed, _volumeMax;
        string _volumeIcon;

        protected override string DefaultTitle => NAME;
        protected override ItemDisplayMode PresentationMode =>
            ItemDisplayComponent.ResolveDisplayMode(GeneralComponent);

        public InventoryApp(IAppHost host) : base(host)
        {
            if (!ItemDisplayComponent.MigrateLegacyDisplayMode(GeneralComponent))
                return;

            var block = Host.Block as IMyTerminalBlock;
            var provider = Host.ProviderConfig;
            if (block == null || provider == null || !provider.CanWrite)
                return;

            LcdModClientComponent.RunNextFrame.Add(delegate
            {
                ConfigManager.Sync(block, provider);
            });
        }

        public override void Update()
        {
            base.Update();
            if (!IsCompact)
                return;

            try
            {
                ReadVolume();
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
            }
        }

        public override void Close()
        {
            _blocks.Dispose();
            base.Close();
        }

        void ReadVolume()
        {
            double used = 0d, max = 0d;
            var selectedBlocks = BlockSelectionComponent.SelectedBlocks;
            var selectedGroups = BlockSelectionComponent.SelectedGroups;
            bool hasBlocks = selectedBlocks != null && selectedBlocks.Length > 0;
            bool hasGroups = selectedGroups != null && selectedGroups.Length > 0;
            _seenBlocks.Clear();

            if (hasBlocks || hasGroups)
            {
                _blocks.Unbind();
                if (hasBlocks)
                {
                    for (int i = 0; i < selectedBlocks.Length; i++)
                        AddVolume(MyAPIGateway.Entities.GetEntityById(selectedBlocks[i]) as IMyTerminalBlock, ref used, ref max);
                }
                if (hasGroups && GridLogic != null)
                {
                    long frame = MyAPIGateway.Session != null ? MyAPIGateway.Session.GameplayFrameCounter : 0L;
                    if (!ReferenceEquals(selectedGroups, _lastGroups) || frame - _groupsFrame >= GROUP_REFRESH_FRAMES)
                    {
                        _groupBlocks.Clear();
                        GridLogic.GetTerminalGroupBlocks(selectedGroups, _groupBlocks);
                        _lastGroups = selectedGroups;
                        _groupsFrame = frame;
                    }
                    for (int i = 0; i < _groupBlocks.Count; i++)
                        AddVolume(_groupBlocks[i], ref used, ref max);
                }
            }
            else
            {
                _groupBlocks.Clear();
                _lastGroups = null;
                _blocks.Bind(GridLogic, (GridLinkTypeEnum)BlockSelectionComponent.GridLinkTypeInternal);
                var sources = _blocks.Sources;
                for (int s = 0; s < sources.Count; s++)
                {
                    var list = sources[s];
                    for (int i = 0; i < list.Count; i++)
                        AddVolume(list[i], ref used, ref max);
                }
            }

            _seenBlocks.Clear();
            if (used == _volumeUsed && max == _volumeMax)
                return;

            _volumeUsed = used;
            _volumeMax = max;
            InvalidateContentSprites();
        }

        void AddVolume(IMyTerminalBlock block, ref double used, ref double max)
        {
            if (block == null || block.Closed || block.MarkedForClose || !block.HasInventory || !_seenBlocks.Add(block.EntityId))
                return;

            if (_volumeIcon == null)
                _volumeIcon = block.BlockDefinition.ToString();

            for (int i = 0; i < block.InventoryCount; i++)
            {
                var inventory = block.GetInventory(i);
                if (inventory == null)
                    continue;
                used += (double)inventory.CurrentVolume * CUBIC_METERS_TO_LITERS;
                max += (double)inventory.MaxVolume * CUBIC_METERS_TO_LITERS;
            }
        }

        protected override void DrawCompact(List<MySprite> s)
        {
            var view = ViewBox;
            Color fg = ForegroundColor;
            float scale = Bare.Scale;
            float margin = Bare.Margin;
            float barHeight = BAR_HEIGHT_BASE * scale;
            float fraction = _volumeMax > 0d ? MathHelper.Clamp((float)(_volumeUsed / _volumeMax), 0f, 1f) : 0f;
            Color fill = fraction >= VOLUME_CRITICAL ? GetErrorColor() : fraction >= VOLUME_ATTENTION ? GetWarningColor() : GetHeaderColor();
            ThemeChrome.DrawBar(s, Theme, new RectangleF(view.X, view.Y, view.Width, barHeight), fraction, fill, fg, scale);

            _kv.Clear();
            _kv.Add(new BareLayout.KvRow
            {
                Label = LocHelper.GetLoc(MOD_PREFIX + "Inventory_Volume"),
                Icon = _volumeIcon ?? BareIcons.Volume,
                Value = FormatingHelper.VolumeToString(_volumeUsed) + " / " + FormatingHelper.VolumeToString(_volumeMax) + SEPARATOR + FormatingHelper.PercentageToString(fraction),
                ValueColor = fraction >= VOLUME_ATTENTION ? fill : fg,
                Bar = -1f
            });

            var items = DisplayItems;
            if (items.Count > 0)
            {
                double ores = 0d, ingots = 0d, components = 0d, ammo = 0d, tools = 0d;
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    if (item == null)
                        continue;
                    double amount = (double)item.Amount;
                    switch (item.TypeId)
                    {
                        case TYPE_ORE: ores += amount; break;
                        case TYPE_INGOT: ingots += amount; break;
                        case TYPE_COMPONENT: components += amount; break;
                        case TYPE_AMMO: ammo += amount; break;
                        case TYPE_TOOL: tools += amount; break;
                    }
                }

                _kv.Add(CategoryRow("Ore", BareIcons.Ore, FormatingHelper.FormatItemQty(ores), fg));
                _kv.Add(CategoryRow("Ingot", BareIcons.Ingot, FormatingHelper.FormatItemQty(ingots), fg));
                _kv.Add(CategoryRow("Component", BareIcons.Component, FormatingHelper.FormatItemQty(components), fg));
                _kv.Add(CategoryRow("AmmoMagazine", BareIcons.Ammo,
                    FormatingHelper.FormatItemQty(ammo) + SEPARATOR + FormatingHelper.FormatItemQty(tools), fg,
                    ItemCategoryHelper.GetGroupDisplayName("AmmoMagazine") + SEPARATOR + ItemCategoryHelper.GetGroupDisplayName("PhysicalGun")));
            }

            float top = view.Y + barHeight + margin;
            Bare.Kv(s, new RectangleF(view.X, top, view.Width, view.Bottom - top), _kv, fg, Theme);
        }

        static BareLayout.KvRow CategoryRow(string group, string icon, string value, Color fg, string label = null)
        {
            return new BareLayout.KvRow
            {
                Label = label ?? ItemCategoryHelper.GetGroupDisplayName(group),
                Icon = icon,
                Value = value,
                ValueColor = fg,
                Bar = -1f
            };
        }
    }
}
