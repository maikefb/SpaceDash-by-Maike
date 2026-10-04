using System;
using System.Collections.Generic;
using LcdMod.Common.Config.Components;
using System.Globalization;
using LcdMod.Client.Apps.Abstract;
using LcdMod.Client.ClockDashboard;
using LcdMod.Client.Gui;
using LcdMod.Client.Gui.ControlsTemplates.Custom;
using LcdMod.Client.Gui.ControlsTemplates.Panels;
using LcdMod.Client.Helpers;
using LcdMod.Client.Modules.Survivors;
using LcdMod.Common.Helpers;
using LcdMod.Common.Survivors;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRageMath;
using static LcdMod.Common.Helpers.Constants;

namespace LcdMod.Client.Apps
{
    internal static class SurvivorsLocalization
    {
        public const string TITLE_KEY = MOD_PREFIX + "Survivors";

        public static string Player => LocHelper.GetLoc(TITLE_KEY + "_Player");
        public static string Alive => LocHelper.GetLoc(TITLE_KEY + "_Alive");
        public static string Record => LocHelper.GetLoc(TITLE_KEY + "_Record");
        public static string Dead => LocHelper.GetLoc(TITLE_KEY + "_Dead");
    }

    /// <summary>
    /// Ranking único ordenado por tempo vivo, com o recorde pessoal ao lado e medalha para os três maiores recordes.
    /// Quando há mais jogadores do que linhas, pagina no ritmo do passo da rolagem do menu K.
    /// </summary>
    internal sealed partial class SurvivorsApp : App, IApp
    {
        const float MIN_CONTENT_HEIGHT = 40f;
        const float ROW_HEIGHT_BASE = 30f;
        const int MEDALS = 3;

        static readonly Color[] MedalColors =
        {
            new Color(242, 193, 78),
            new Color(201, 210, 218),
            new Color(208, 138, 90)
        };


        readonly List<Control> _interactiveChildren = new List<Control>();
        readonly Grid _rootGrid;
        readonly SurvivorRowControl _header;
        readonly List<SurvivorRowControl> _rows = new List<SurvivorRowControl>();
        readonly List<SurvivorEntry> _sorted = new List<SurvivorEntry>();
        readonly long[] _medalIdentity = new long[MEDALS];
        readonly long[] _medalTicks = new long[MEDALS];
        int _configuredRows = -1;
        int _dataVersion = -1;

        public SurvivorsApp(IAppHost host)
            : base(host)
        {
            _rootGrid = AddLogicalChild(new Grid());
            _header = new SurvivorRowControl();
        }

        public override IReadOnlyList<Control> VisualChildren => _interactiveChildren;

        public override bool HasVisibleItems()
        {
            return SurvivorsClient.HasData && SurvivorsClient.Entries.Count > 0;
        }

        public override void Update()
        {
            SurvivorsClient.EnsureData();
            if (_dataVersion == SurvivorsClient.Version)
                return;

            _sorted.Clear();
            _sorted.AddRange(SurvivorsClient.Entries);
            SortByRanking();
            _dataVersion = SurvivorsClient.Version;
        }

        public override void LayoutChanged()
        {
            _configuredRows = -1;
            _rootGrid.InvalidateLayout();
        }

        public override List<MySprite> GetSprites()
        {
            var sprites = new List<MySprite>();
            var bounds = GetContentBounds();
            if (bounds.Width <= 5f || bounds.Height <= 5f)
                return sprites;

            float scale = MathHelper.Clamp(
                GeneralComponent.GetScale() * Math.Max(0.55f, Math.Min(bounds.Width, bounds.Height) / 512f),
                0.45f, 2.5f);
            int visibleRows = Math.Max(1, (int)Math.Floor(bounds.Height / (ROW_HEIGHT_BASE * scale)) - 1);

            ConfigureRows(visibleRows);
            BindRows(visibleRows);
            _rootGrid.Arrange(bounds);
            _rootGrid.Render(sprites);
            ClearDirtyAfterRender();
            return sprites;
        }

        RectangleF GetContentBounds()
        {
            RectangleF view = Host.ViewBox;
            float topInset = Host.TitleVisible ? 40f * GeneralComponent.GetScale() * Host.Surface.FontSize : 0f;
            float top = view.Y + topInset;
            float height = Math.Max(MIN_CONTENT_HEIGHT, view.Bottom - top);
            return new RectangleF(view.X, top, view.Width, height);
        }

        void ConfigureRows(int visibleRows)
        {
            if (_configuredRows == visibleRows)
                return;

            while (_rows.Count < visibleRows)
                _rows.Add(new SurvivorRowControl());

            var weights = new float[visibleRows + 1];
            for (int i = 0; i < weights.Length; i++)
                weights[i] = 1f;
            weights[0] = 0.85f;

            _rootGrid.ClearChildren();
            _rootGrid.SetColumns(1f);
            _rootGrid.SetRows(weights);
            _rootGrid.Set(_header, 0, 0);
            for (int i = 0; i < visibleRows; i++)
                _rootGrid.Set(_rows[i], 0, i + 1);

            _configuredRows = visibleRows;
        }

        void BindRows(int visibleRows)
        {
            long now = WorldClock.ElapsedTicks();
            Color foreground = Host.ForegroundColor;
            Color dim = new Color(foreground.ToVector3() * 0.6f, foreground.A / 255f);
            Color header = GetHeaderColor();

            _header.Bind(SurvivorsLocalization.Player, SurvivorsLocalization.Alive, SurvivorsLocalization.Record, header, header, header, null);
            ComputeMedals(now);

            int total = _sorted.Count;
            int start = ResolveFirstRow(total, visibleRows);
            for (int i = 0; i < visibleRows; i++)
            {
                var row = _rows[i];
                int index = start + i;
                if (index >= total)
                {
                    row.Bind(string.Empty, string.Empty, string.Empty, foreground, foreground, dim, null);
                    continue;
                }

                var entry = _sorted[index];
                Color nameColor = entry.Alive ? foreground : dim;
                string rank = (index + 1).ToString(CultureInfo.InvariantCulture) + ". " + (entry.Name ?? "?");
                string alive = entry.Alive
                    ? ClockDashboardFormatter.FormatDuration(WorldClock.ToSeconds(entry.CurrentLifeTicks(now)))
                    : SurvivorsLocalization.Dead;
                string record = ClockDashboardFormatter.FormatDuration(WorldClock.ToSeconds(entry.BestTicks(now)));
                row.Bind(rank, alive, record, nameColor, nameColor, dim, ResolveMedal(entry.IdentityId));
            }
        }

        /// <summary>Janela que desce uma linha por passo da rolagem e volta ao topo no fim, como nas outras listas.</summary>
        int ResolveFirstRow(int total, int visibleRows)
        {
            if (total <= visibleRows)
                return 0;

            float step = InteractionComponent.AutoScrollStep;
            if (step <= 0f)
                return 0;

            int positions = total - visibleRows + 1;
            long framesPerStep = Math.Max(12L, (long)(step * 60f));
            long frame = MyAPIGateway.Session != null ? MyAPIGateway.Session.GameplayFrameCounter : 0L;
            return (int)(frame / framesPerStep % positions);
        }

        void ComputeMedals(long now)
        {
            for (int i = 0; i < MEDALS; i++)
            {
                _medalIdentity[i] = 0L;
                _medalTicks[i] = 0L;
            }

            for (int e = 0; e < _sorted.Count; e++)
            {
                var entry = _sorted[e];
                long best = entry.BestTicks(now);
                if (best <= 0L)
                    continue;

                for (int i = 0; i < MEDALS; i++)
                {
                    if (best <= _medalTicks[i])
                        continue;

                    for (int j = MEDALS - 1; j > i; j--)
                    {
                        _medalIdentity[j] = _medalIdentity[j - 1];
                        _medalTicks[j] = _medalTicks[j - 1];
                    }

                    _medalIdentity[i] = entry.IdentityId;
                    _medalTicks[i] = best;
                    break;
                }
            }
        }

        Color? ResolveMedal(long identityId)
        {
            for (int i = 0; i < MEDALS; i++)
            {
                if (_medalTicks[i] > 0L && _medalIdentity[i] == identityId)
                    return MedalColors[i];
            }

            return null;
        }

        void SortByRanking()
        {
            _sorted.Sort(CompareEntries);
        }

        static int CompareEntries(SurvivorEntry a, SurvivorEntry b)
        {
            if (a.Alive != b.Alive)
                return a.Alive ? -1 : 1;

            int byTime = a.Alive
                ? a.AliveSinceTicks.CompareTo(b.AliveSinceTicks)
                : b.RecordTicks.CompareTo(a.RecordTicks);
            if (byTime != 0)
                return byTime;

            return string.CompareOrdinal(a.Name ?? string.Empty, b.Name ?? string.Empty);
        }
    }
}
