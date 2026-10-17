using System;
using System.Collections.Generic;
using LcdMod.Client.Apps.Abstract;
using LcdMod.Client.GridData;
using LcdMod.Client.Gui;
using LcdMod.Client.Gui.ControlsTemplates.Custom;
using LcdMod.Client.Gui.ControlsTemplates.Panels;
using LcdMod.Client.Gui.ControlsTemplates.Progress;
using LcdMod.Client.Gui.Styling;
using LcdMod.Client.Helpers;
using LcdMod.Client.Modules.Defense;
using LcdMod.Common.Config.Components;
using LcdMod.Common.Config.Generation;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;
using IMyFunctionalBlock = Sandbox.ModAPI.IMyFunctionalBlock;
using IMyGunObject = VRage.Game.ModAPI.IMyGunObject<VRage.Game.ModAPI.MyDeviceBase>;
using IMyTerminalBlock = Sandbox.ModAPI.IMyTerminalBlock;
using IMyUserControllableGun = Sandbox.ModAPI.IMyUserControllableGun;
using static LcdMod.Common.Helpers.Constants;

namespace LcdMod.Client.Apps
{
    
    internal sealed partial class DefenseDashboardApp : App
    {
        const float MINIMAL_HERO_FONT_MUL = 3f;
        const float RING_TRACK_MIX = 0.15f;

        readonly Grid _rootGrid;
        readonly TilingPanel _shieldPanel;
        readonly TilingPanel _weaponPanel;
        readonly Dictionary<string, ShieldStatus> _shieldControls =
            new Dictionary<string, ShieldStatus>(StringComparer.Ordinal);
        readonly Dictionary<string, WeaponStatus> _weaponControls =
            new Dictionary<string, WeaponStatus>(StringComparer.OrdinalIgnoreCase);
        readonly HashSet<string> _desiredShieldKeys = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> _desiredWeaponKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, WeaponAggregate> _weaponAggregates =
            new Dictionary<string, WeaponAggregate>(StringComparer.OrdinalIgnoreCase);
        readonly List<WeaponAggregate> _sortedWeapons = new List<WeaponAggregate>();

        DefenseDataLease _lease;
        Color _cardColor;
        Color _warningColor;
        Color _errorColor;
        bool _hasShields;
        bool _hasWeapons;

        public DefenseDashboardApp(IAppHost host) : base(host)
        {
            NeedGridData(GridCapability.Blocks);
            _rootGrid = AddLogicalChild(new Grid(default(RectangleF), new[] { 1f }, new[] { 1f, 1f }));
            _shieldPanel = new TilingPanel
            {
                GapPixels = 6f,
                PaddingPixels = 3f,
                FillFromBottom = true
            };
            _weaponPanel = new TilingPanel { GapPixels = 6f, PaddingPixels = 3f };
            _rootGrid.Set(_shieldPanel, 0, 0);
            _rootGrid.Set(_weaponPanel, 0, 1);
            UpdatePresentationColors();
            CaptureLease();
        }

        public bool HasData => _hasShields || _hasWeapons;

        public override IReadOnlyList<Control> VisualChildren { get; } = new Control[] { };

        public override void Update()
        {
            CaptureLease();
            UpdatePresentationColors();

            UpdateWeaponStatuses(Host.GridLogic);
        }

        public override void LayoutChanged()
        {
            _rootGrid.InvalidateLayout();
            MarkDirty();
        }

        readonly List<MySprite> _sprites = new List<MySprite>();
        readonly List<BareLayout.KvRow> _rows = new List<BareLayout.KvRow>();
        readonly DonutSegment[] _ringSegments = new DonutSegment[2];

        public override List<MySprite> GetSprites()
        {
            var sprites = _sprites;
            sprites.Clear();
            if (IsMinimal)
            {
                DrawMinimal(sprites);
                return sprites;
            }
            if (IsCompact)
            {
                DrawCompact(sprites);
                return sprites;
            }
            if (Host.Frame != null)
            {
                DrawFramed(sprites);
                return sprites;
            }

            float scale = Math.Max(0.25f, GeneralComponent.GetScale());
            float fontScale = Math.Max(0.25f, Host.FontScale);
            float titleOffset = Host.TitleVisible ? 40f * scale * fontScale : 0f;
            RectangleF view = Host.ViewBox;
            var content = new RectangleF(
                view.X,
                view.Y + titleOffset,
                view.Width,
                Math.Max(0f, view.Height - titleOffset));

            ConfigureSections();
            _rootGrid.Arrange(content);
            _rootGrid.Render(sprites);
            ClearDirtyAfterRender();
            return sprites;
        }

        /// <summary>Escudo principal e as seis primeiras armas (prontas / total) nos campos da moldura.</summary>
        void DrawFramed(List<MySprite> sprites)
        {
            var f = Host.Frame;
            Color fg = GetForegroundColor();
            Color accent = GetHeaderColor();
            f.Header(sprites, Host.Block, (int)GridLinkTypeEnum.Logical, accent);
            ShieldInfo shield = _lease != null && _lease.Service != null ? _lease.Service.Latest.Primary : null;
            if (shield != null && shield.HasCapacity)
            {
                float ratio = shield.ChargeRatio;
                f.Bar(sprites, "escudo.barra", ratio, accent);
                f.Text(sprites, "escudo.valor", FormatingHelper.PercentageToString(ratio), fg);
            }

            for (int i = 0; i < 6 && i < _sortedWeapons.Count; i++)
            {
                WeaponAggregate weapon = _sortedWeapons[i];
                string line = "linha_" + (i + 1);
                f.Text(sprites, line + ".nome_bloco", weapon.DisplayName, fg, false);
                f.Text(sprites, line + ".valor", CountText(weapon.Ready, weapon.Total), weapon.Warning + weapon.Unavailable > 0 ? GetWarningColor() : fg);
            }

            ClearDirtyAfterRender();
        }

        string CountText(int count, int total)
        {
            return DefenseComponent.ShowPercent
                ? FormatingHelper.PercentageToString(count / (float)Math.Max(1, total))
                : count + " / " + total;
        }

        static string Loc(string suffix)
        {
            return LocHelper.GetLoc(MOD_PREFIX + "DefenseDashboard_" + suffix);
        }

        void SumWeapons(out int total, out int ready, out int shooting, out int warning, out int unavailable)
        {
            total = ready = shooting = warning = unavailable = 0;
            for (int i = 0; i < _sortedWeapons.Count; i++)
            {
                WeaponAggregate weapon = _sortedWeapons[i];
                total += weapon.Total;
                ready += weapon.Ready;
                shooting += weapon.Shooting;
                warning += weapon.Warning;
                unavailable += weapon.Unavailable;
            }
        }

        string StateWord(int shooting, int warning, int unavailable, Color accent, out Color color)
        {
            if (shooting > 0)
            {
                color = accent;
                return Loc("Firing");
            }
            if (unavailable > 0 && unavailable >= warning)
            {
                color = GetErrorColor();
                return Loc("Disabled");
            }
            if (warning > 0)
            {
                color = GetWarningColor();
                return Loc("NotReady");
            }
            color = accent;
            return Loc("Ready");
        }

        void DrawMinimal(List<MySprite> sprites)
        {
            int total, ready, shooting, warning, unavailable;
            SumWeapons(out total, out ready, out shooting, out warning, out unavailable);
            var view = Host.ViewBox;
            Color fg = GetForegroundColor();
            Color accent = GetHeaderColor();
            Color error = GetErrorColor();
            Color track = Color.Lerp(Host.BackgroundColor, fg, RING_TRACK_MIX);
            bool valid = total > 0;
            float fraction = valid ? ready / (float)total : 0f;
            Color readyColor = !valid ? fg : shooting > 0 && BlinkOn ? error : accent;
            _ringSegments[0] = new DonutSegment(fraction, readyColor);
            _ringSegments[1] = new DonutSegment(valid ? 1f - fraction : 0f, error);
            string center = !HasData ? null : valid ? FormatingHelper.PercentageToString(fraction) : LocHelper.GetLoc(MOD_PREFIX + "NotAvailable");
            Color line2Color = fg;
            string line1 = valid ? CountText(ready, total) + " " + Loc("Weapons") : null;
            string line2 = valid ? StateWord(shooting, warning, unavailable, accent, out line2Color) : null;
            float margin = Bare.Margin;
            float lineHeight = Bare.LineHeight(1f);

            if (BareLayout.IsWide(view))
            {
                var ringArea = new RectangleF(view.X, view.Y, view.Width * 0.5f, view.Height);
                Bare.Ring(sprites, ringArea, _ringSegments, track, center, readyColor, MINIMAL_HERO_FONT_MUL);
                float left = ringArea.Right + margin;
                float textTop = view.Center.Y - lineHeight;
                Bare.Text(sprites, line1, new Vector2(left, textTop), TextAlignment.LEFT, 1f, fg, view.Right - left, lineHeight);
                Bare.Text(sprites, line2, new Vector2(left, textTop + lineHeight), TextAlignment.LEFT, 1f, line2Color, view.Right - left, lineHeight);
                return;
            }

            float textBlock = lineHeight * 2f + margin;
            float ringSide = Math.Min(view.Width, view.Height - textBlock);
            if (ringSide < textBlock)
            {
                Bare.Ring(sprites, view, _ringSegments, track, center, readyColor, MINIMAL_HERO_FONT_MUL);
                return;
            }

            float top = view.Y + (view.Height - ringSide - textBlock) * 0.5f;
            var ring = new RectangleF(view.X, top, view.Width, ringSide);
            Bare.Ring(sprites, ring, _ringSegments, track, center, readyColor, MINIMAL_HERO_FONT_MUL);
            float linesTop = ring.Bottom + margin;
            Bare.Text(sprites, line1, new Vector2(view.Center.X, linesTop), TextAlignment.CENTER, 1f, fg, view.Width, lineHeight);
            Bare.Text(sprites, line2, new Vector2(view.Center.X, linesTop + lineHeight), TextAlignment.CENTER, 1f, line2Color, view.Width, lineHeight);
        }

        void DrawCompact(List<MySprite> sprites)
        {
            int total, ready, shooting, warning, unavailable;
            SumWeapons(out total, out ready, out shooting, out warning, out unavailable);
            ShieldInfo shield = _hasShields && _lease != null && _lease.Service != null ? _lease.Service.Latest.Primary : null;
            Color fg = GetForegroundColor();
            Color accent = GetHeaderColor();
            Color stateColor;

            _rows.Clear();
            if (total > 0)
            {
                string state = StateWord(shooting, warning, unavailable, accent, out stateColor);
                _rows.Add(new BareLayout.KvRow { Label = Loc("Weapons"), Value = state, ValueColor = stateColor, Bar = -1f });
                _rows.Add(new BareLayout.KvRow { Label = Loc("Ready"), Value = CountText(ready, total), ValueColor = ready == total ? accent : GetWarningColor(), Bar = -1f });
                for (int i = 0; i < _sortedWeapons.Count; i++)
                {
                    WeaponAggregate weapon = _sortedWeapons[i];
                    string word = StateWord(weapon.Shooting, weapon.Warning, weapon.Unavailable, accent, out stateColor);
                    _rows.Add(new BareLayout.KvRow { Label = weapon.DisplayName, Icon = weapon.SpriteName, Value = word, ValueColor = stateColor, Bar = -1f });
                }
            }
            if (shield != null && shield.HasCapacity)
            {
                _rows.Add(new BareLayout.KvRow
                {
                    Label = string.IsNullOrEmpty(shield.ProviderName) ? Loc("ShieldFallback") : shield.ProviderName,
                    Value = shield.IsWorking ? FormatingHelper.PercentageToString(shield.ChargeRatio) : Loc("Offline"),
                    ValueColor = shield.IsWorking ? accent : GetErrorColor(),
                    Bar = -1f
                });
            }

            if (_rows.Count == 0)
            {
                Host.DrawMessage(sprites, LocHelper.GetLoc(MOD_PREFIX + "NotAvailable"), "Warning", GetWarningColor());
                return;
            }

            Bare.Kv(sprites, Host.ViewBox, _rows, fg, Theme);
        }

        public override void Close()
        {
            ReleaseLease();
            base.Close();
        }

        void CaptureLease()
        {
            var defenseData = LcdModSessionComponent.Client?.DefenseData;
            var requester = Host.GridLogic;
            const GridLinkTypeEnum linkType = GridLinkTypeEnum.Logical;
            if (defenseData == null || requester == null)
                return;

            if (_lease != null && _lease.Service != null &&
                ReferenceEquals(_lease.Service.Requester, requester) && _lease.Service.LinkType == linkType)
                return;

            ReleaseLease();
            _lease = defenseData.Capture(requester, linkType);
            if (_lease.Service != null)
            {
                _lease.Service.ShieldsChanged += OnShieldsChanged;
                OnShieldsChanged(_lease.Service);
            }
        }

        void ReleaseLease()
        {
            if (_lease == null)
                return;

            if (_lease.Service != null)
                _lease.Service.ShieldsChanged -= OnShieldsChanged;
            _lease.Dispose();
            _lease = null;
        }

        void OnShieldsChanged(DefenseDataService service)
        {
            if (_lease == null || !ReferenceEquals(_lease.Service, service))
                return;

            List<ShieldInfo> shields = service.Latest.Shields;
            _desiredShieldKeys.Clear();
            int desiredIndex = 0;
            int count = shields?.Count ?? 0;
            for (int i = 0; i < count; i++)
            {
                ShieldInfo shield = shields?[i];
                if (shield == null)
                    continue;

                string baseKey = string.IsNullOrEmpty(shield.ProviderName) ? "Shield": shield.ProviderName;
                string key = baseKey;
                int suffix = 2;
                while (!_desiredShieldKeys.Add(key))
                    key = baseKey + "#"+ suffix++;

                ShieldStatus control;
                if (!_shieldControls.TryGetValue(key, out control))
                {
                    control = new ShieldStatus(key, shield);
                    _shieldControls[key] = control;
                }
                else
                {
                    control.SetViewModel(shield);
                }

                control.SetPresentationColors(_cardColor, _warningColor, _errorColor);
                _shieldPanel.AddChild(control);
                _shieldPanel.MoveChild(control, desiredIndex++);
            }

            RemoveStaleShieldTiles();
            _hasShields = desiredIndex > 0;
            MarkDirty();
        }

        void UpdatePresentationColors()
        {
            Color cardColor = GetHeaderColor();
            Color warningColor = GetWarningColor();
            Color errorColor = GetErrorColor();
            bool changed = !_cardColor.Equals(cardColor) ||
                           !_warningColor.Equals(warningColor) ||
                           !_errorColor.Equals(errorColor);

            _cardColor = cardColor;
            _warningColor = warningColor;
            _errorColor = errorColor;
            if (!changed)
                return;

            foreach (var pair in _shieldControls)
                pair.Value.SetPresentationColors(_cardColor, _warningColor, _errorColor);
        }

        void RemoveStaleShieldTiles()
        {
            for (int i = _shieldPanel.VisualChildren.Count - 1; i >= 0; i--)
            {
                var control = _shieldPanel.VisualChildren[i] as ShieldStatus;
                if (control != null && !_desiredShieldKeys.Contains(control.ProviderKey))
                    _shieldPanel.RemoveChild(control);
            }
        }

        void UpdateWeaponStatuses(GridLogic gridLogic)
        {
            _weaponAggregates.Clear();
            var weaponBlocks = gridLogic != null ? gridLogic.Blocks.UserControllableGuns : null;
            int blockCount = weaponBlocks != null ? weaponBlocks.Count : 0;
            for (int i = 0; i < blockCount; i++)
            {
                IMyTerminalBlock block = weaponBlocks[i];
                if (block == null || block.Closed || block.MarkedForClose)
                    continue;

                string subtype = block.BlockDefinition.SubtypeName;
                if (string.IsNullOrEmpty(subtype))
                    subtype = block.BlockDefinition.ToString();

                WeaponAggregate aggregate;
                if (!_weaponAggregates.TryGetValue(subtype, out aggregate))
                {
                    string displayName = block.DefinitionDisplayNameText;
                    aggregate = new WeaponAggregate
                    {
                        SubtypeId = subtype,
                        DisplayName = string.IsNullOrEmpty(displayName) ? subtype : displayName,
                        SpriteName = ResolveWeaponSprite(block)
                    };
                    _weaponAggregates[subtype] = aggregate;
                }

                aggregate.Total++;
                switch (GetWeaponState(block))
                {
                    case WeaponState.Shooting:
                        aggregate.Shooting++;
                        break;
                    case WeaponState.Warning:
                        aggregate.Warning++;
                        break;
                    case WeaponState.Unavailable:
                        aggregate.Unavailable++;
                        break;
                    default:
                        aggregate.Ready++;
                        break;
                }
            }

            SyncWeaponControls();
        }

        void SyncWeaponControls()
        {
            _sortedWeapons.Clear();
            foreach (var pair in _weaponAggregates)
                _sortedWeapons.Add(pair.Value);
            _sortedWeapons.Sort(delegate(WeaponAggregate left, WeaponAggregate right)
            {
                int byName = string.Compare(left.DisplayName, right.DisplayName,
                    StringComparison.OrdinalIgnoreCase);
                return byName != 0
                    ? byName
                    : string.Compare(left.SubtypeId, right.SubtypeId, StringComparison.OrdinalIgnoreCase);
            });

            _desiredWeaponKeys.Clear();
            for (int i = 0; i < _sortedWeapons.Count; i++)
            {
                WeaponAggregate weapon = _sortedWeapons[i];
                _desiredWeaponKeys.Add(weapon.SubtypeId);

                WeaponStatus control;
                if (!_weaponControls.TryGetValue(weapon.SubtypeId, out control))
                {
                    control = new WeaponStatus(weapon.SubtypeId);
                    _weaponControls[weapon.SubtypeId] = control;
                }

                control.Bind(weapon.DisplayName, weapon.SpriteName, weapon.Total, weapon.Ready, weapon.Shooting,
                    weapon.Warning, weapon.Unavailable, DefenseComponent.ShowPercent, _cardColor, _warningColor, _errorColor);
                _weaponPanel.AddChild(control);
                _weaponPanel.MoveChild(control, i);
            }

            RemoveStaleWeaponTiles();
            _hasWeapons = _sortedWeapons.Count > 0;
        }

        void RemoveStaleWeaponTiles()
        {
            for (int i = _weaponPanel.VisualChildren.Count - 1; i >= 0; i--)
            {
                var control = _weaponPanel.VisualChildren[i] as WeaponStatus;
                if (control != null && !_desiredWeaponKeys.Contains(control.SubtypeId))
                    _weaponPanel.RemoveChild(control);
            }
        }

        void ConfigureSections()
        {
            _shieldPanel.SetVisible(_hasShields);
            _weaponPanel.SetVisible(_hasWeapons);

            if (_hasShields && _hasWeapons)
            {
                _rootGrid.SetRows(1f, 1f);
                return;
            }

            if (_hasShields)
            {
                _rootGrid.SetRows(1f, 0f);
                return;
            }

            _rootGrid.SetRows(0f, 1f);
        }

        static WeaponState GetWeaponState(IMyTerminalBlock block)
        {
            var functional = block as IMyFunctionalBlock;
            if (functional != null && (!functional.IsFunctional || !functional.Enabled))
                return WeaponState.Unavailable;

            var gun = block as IMyUserControllableGun;
            if (gun != null && gun.IsShooting)
                return WeaponState.Shooting;

            var weapon = block as IMyGunObject;
            if (weapon != null)
            {
                
                try
                {
                    if (weapon.GetAmmunitionAmount() == 0 && gun != null && gun.HasInventory && (float)gun.GetInventory(0).CurrentVolume == 0f)
                        return WeaponState.Warning;

                    MyGunStatusEnum ignoredStatus;
                    return weapon.CanShoot(MyShootActionEnum.PrimaryAction, block.OwnerId, out ignoredStatus)
                        ? WeaponState.Ready
                        : WeaponState.Warning;
                }
                catch
                {
                    // A modded gun can reject the shared API path. Fall through to the
                    // public functional state rather than depending on an internal type.
                }
            }

            return functional == null || functional.IsWorking
                ? WeaponState.Ready
                : WeaponState.Warning;
        }

        static string ResolveWeaponSprite(IMyTerminalBlock block)
        {
            try
            {
                var cubeBlock = block as MyCubeBlock;
                if (cubeBlock?.BlockDefinition != null)
                    return TextureHelper.GetOrAddTextureForBlock(cubeBlock.BlockDefinition);
            }
            catch
            {
                // Use the explicit missing-icon asset when a modded definition cannot be registered.
            }

            return "MissingIcon";
        }

        sealed class WeaponAggregate
        {
            public string SubtypeId;
            public string DisplayName;
            public string SpriteName;
            public int Total;
            public int Ready;
            public int Shooting;
            public int Warning;
            public int Unavailable;
        }

        enum WeaponState
        {
            Ready,
            Shooting,
            Warning,
            Unavailable
        }
    }
}
