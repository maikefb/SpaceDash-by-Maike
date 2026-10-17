using System;
using System.Collections.Generic;
using LcdMod.Client.Apps.Abstract;
using LcdMod.Client.Extensions;
using LcdMod.Client.GridData;
using LcdMod.Client.Gui;
using LcdMod.Client.Gui.ControlsTemplates.Panels;
using LcdMod.Client.Gui.Styling;
using LcdMod.Client.Helpers;
using LcdMod.Common.Config.Components;
using LcdMod.Common.Helpers;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using static LcdMod.Common.Helpers.Constants;
using MyConveyorSorterMode = Sandbox.ModAPI.Ingame.MyConveyorSorterMode;
using MyInventoryItemFilter = Sandbox.ModAPI.Ingame.MyInventoryItemFilter;

namespace LcdMod.Client.Apps
{
    // ReSharper disable once PartialTypeWithSinglePart
    internal sealed partial class ConveyorMapApp : App
    {
        const int MAX_NODES = 160;
        const int REBUILD_TICKS = 600;
        const float LEGEND_HEIGHT_BASE = 56f;
        const float MAX_TILE_BASE = 64f;
        const float PITCH = 1.6f;
        const float ICON_FILL = 0.75f;

        static readonly float[] NetworkHueShifts = { 0f, 0.42f, 0.25f, 0.75f, 0.58f, 0.83f };

        struct PortTemplate
        {
            public Vector3 Position;
            public bool Small;
        }

        static readonly Dictionary<string, List<PortTemplate>> PortCache = new Dictionary<string, List<PortTemplate>>();

        readonly Dictionary<string, IMyModelDummy> _dummies = new Dictionary<string, IMyModelDummy>();
        readonly Dictionary<long, int> _portKeys = new Dictionary<long, int>();
        readonly List<MyInventoryItemFilter> _filters = new List<MyInventoryItemFilter>();
        readonly ConveyorLayout _layout = new ConveyorLayout();

        readonly List<IMyCubeBlock> _blocks = new List<IMyCubeBlock>();
        readonly List<int> _connected = new List<int>();
        readonly List<int> _empty = new List<int>();
        readonly List<bool> _hasLarge = new List<bool>();
        readonly List<bool> _removed = new List<bool>();
        readonly List<int> _nodeOfBlock = new List<int>();
        readonly List<List<int>> _incident = new List<List<int>>();

        readonly List<int> _portBlock = new List<int>();
        readonly List<Vector3I> _portCell = new List<Vector3I>();
        readonly List<Vector3I> _portDir = new List<Vector3I>();
        readonly List<bool> _portSmall = new List<bool>();
        readonly List<bool> _portLinked = new List<bool>();

        readonly List<int> _edgeU = new List<int>();
        readonly List<int> _edgeV = new List<int>();
        readonly List<bool> _edgeSmall = new List<bool>();
        readonly List<bool> _edgeAlive = new List<bool>();

        readonly List<IMyCubeBlock> _nodes = new List<IMyCubeBlock>();
        readonly List<string> _nodeIcons = new List<string>();
        readonly List<bool> _nodeAlert = new List<bool>();
        readonly List<bool> _nodeIsolated = new List<bool>();
        readonly List<int> _edgeNetwork = new List<int>();
        readonly List<bool> _edgeBottleneck = new List<bool>();
        readonly List<int> _layoutEdges = new List<int>();
        readonly List<int> _markerNode = new List<int>();
        readonly List<int> _markerFace = new List<int>();
        readonly List<IMyTerminalBlock> _networkAnchors = new List<IMyTerminalBlock>();
        readonly List<IMySlimBlock> _projected = new List<IMySlimBlock>();
        readonly Dictionary<string, string> _loc = new Dictionary<string, string>();
        int[] _anchorOfRoot = new int[0];
        readonly Color[] _networkColors = new Color[NetworkHueShifts.Length];
        int[] _unionFind = new int[0];
        int[] _networkOfRoot = new int[0];
        int[] _rootSize = new int[0];

        int _lastBlockCount = -1;
        long _lastBuildFrame;
        long _lastAnchorId;
        int _filteredOut;
        bool _tooLarge;
        int _networkCount;
        int _bottlenecks;
        int _alerts;

        RectangleF ViewBox => Host.ViewBox;
        float Scale => GeneralComponent.GetScale();
        float FontScale => Host.FontScale;
        float LayoutScale => Scale * FontScale;
        Color BackgroundColor => Host.BackgroundColor;
        float ContentTop => Host.TitleVisible ? ViewBox.Y + 40f * LayoutScale : ViewBox.Y;
        float FooterHeight => LEGEND_HEIGHT_BASE * LayoutScale;

        public bool HasData { get; private set; }
        public int Damaged { get; private set; }
        public int Destroyed { get; private set; }
        public int BlueprintTotal { get; private set; }
        public string WorstDamagedName { get; private set; }
        public float WorstDamagedRatio { get; private set; }
        public bool HasBlueprint { get; private set; }

        /// <summary>Um bloco de terminal por rede (o de menor EntityId): é o que o jogador escolhe no terminal.</summary>
        public IReadOnlyList<IMyTerminalBlock> NetworkAnchors => _networkAnchors;

        public bool IsConveyorBlock(IMyTerminalBlock block)
        {
            return _blocks.Contains(block);
        }

        public ConveyorMapApp(IAppHost host)
            : base(host)
        {
            NeedGridData(GridCapability.Blocks);
        }

        public override void Update()
        {
            HasData = false;
            try
            {
                var gridLogic = Host.GridLogic;
                if (gridLogic == null || gridLogic.Grid == null)
                    return;

                var all = gridLogic.Blocks.All;
                long frame = MyAPIGateway.Session != null ? MyAPIGateway.Session.GameplayFrameCounter : 0L;
                long anchorId = ConveyorReferenceComponent.EntityId;
                if (all.Count != _lastBlockCount || anchorId != _lastAnchorId || frame - _lastBuildFrame >= REBUILD_TICKS)
                {
                    _lastBlockCount = all.Count;
                    _lastAnchorId = anchorId;
                    _lastBuildFrame = frame;
                    Rebuild(gridLogic.Grid, all);
                }

                EvaluateAlerts();
                HasData = true;
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine($"[LcdMod] ConveyorMap error: {ex}");
            }
        }

        public override void LayoutChanged()
        {
            _lastBlockCount = -1;
            _footerSummary = null;
            _footerLegend = null;
        }

        void Rebuild(IMyCubeGrid grid, IList<IMyCubeBlock> all)
        {
            CollectPorts(grid, all);
            LinkPorts();
            ContractPassThroughs();
            bool largeGrid = grid.GridSizeEnum == MyCubeSize.Large;
            BuildNodes(largeGrid);
            CollectAnchors();
            if (FilterToNetwork(ConveyorReferenceComponent.EntityId))
                BuildNodes(largeGrid);

            CollectDamage(grid, all);
            _tooLarge = _nodes.Count > MAX_NODES || Destroyed >= MAX_NODES;
            if (_tooLarge || _nodes.Count == 0)
                return;

            _layout.Reset(_nodes.Count);
            _layoutEdges.Clear();
            for (int e = 0; e < _edgeU.Count; e++)
            {
                if (!_edgeAlive[e])
                    continue;
                _layoutEdges.Add(e);
                _layout.AddEdge(_nodeOfBlock[_edgeU[e]], _nodeOfBlock[_edgeV[e]]);
            }

            var area = GetMapArea();
            double aspect = area.Width > 0f ? area.Height / area.Width : 1d;
            _layout.Run(Math.Max(1, (int)Math.Ceiling(Math.Sqrt(_nodes.Count * aspect))), aspect);
        }

        void CollectDamage(IMyCubeGrid grid, IList<IMyCubeBlock> all)
        {
            Damaged = 0;
            WorstDamagedName = null;
            WorstDamagedRatio = 1f;
            for (int b = 0; b < _blocks.Count; b++)
            {
                var slim = _blocks[b].SlimBlock;
                if (slim == null || slim.IsFullIntegrity)
                    continue;
                Damaged++;
                float ratio = slim.BuildLevelRatio;
                if (ratio >= WorstDamagedRatio)
                    continue;
                WorstDamagedRatio = ratio;
                var terminal = _blocks[b] as IMyTerminalBlock;
                WorstDamagedName = terminal != null ? terminal.CustomName : slim.BlockDefinition.Id.SubtypeName;
            }

            Destroyed = 0;
            var projector = FindRepairProjector(grid, all);
            HasBlueprint = projector != null;
            if (HasBlueprint)
            {
                var projected = projector.ProjectedGrid;
                for (int i = 0; i < _projected.Count && Destroyed < MAX_NODES; i++)
                {
                    var fat = _projected[i].FatBlock;
                    if (fat == null || fat.Model == null || GetPorts(fat.Model).Count == 0)
                        continue;
                    if (!grid.CubeExists(grid.WorldToGridInteger(projected.GridIntegerToWorld(_projected[i].Position))))
                        Destroyed++;
                }
            }
            _projected.Clear();
            BlueprintTotal = _blocks.Count + Destroyed;
        }

        IMyProjector FindRepairProjector(IMyCubeGrid grid, IList<IMyCubeBlock> all)
        {
            for (int i = 0; i < all.Count; i++)
            {
                var projector = all[i] as IMyProjector;
                if (projector == null || projector.Closed || !projector.IsProjecting)
                    continue;
                var projected = projector.ProjectedGrid;
                if (projected == null || projected.GridSizeEnum != grid.GridSizeEnum)
                    continue;

                _projected.Clear();
                projected.GetBlocks(_projected, null);
                if (_projected.Count == 0)
                    continue;

                int matches = 0;
                for (int j = 0; j < _projected.Count; j++)
                    if (grid.CubeExists(grid.WorldToGridInteger(projected.GridIntegerToWorld(_projected[j].Position))))
                        matches++;
                if ((float)matches / _projected.Count >= 0.5f)
                    return projector;
            }
            _projected.Clear();
            return null;
        }

        void CollectPorts(IMyCubeGrid grid, IList<IMyCubeBlock> all)
        {
            _blocks.Clear();
            _connected.Clear();
            _empty.Clear();
            _hasLarge.Clear();
            _removed.Clear();
            _portBlock.Clear();
            _portCell.Clear();
            _portDir.Clear();
            _portSmall.Clear();
            _portLinked.Clear();
            _portKeys.Clear();

            for (int i = 0; i < all.Count; i++)
            {
                var block = all[i];
                if (block == null || block.Closed || block.Model == null)
                    continue;

                var templates = GetPorts(block.Model);
                int b = _blocks.Count;
                bool hasLarge = false;
                for (int t = 0; t < templates.Count; t++)
                {
                    Vector3I cell, dir;
                    if (!TryResolvePort(block, templates[t].Position, grid.GridSize, out cell, out dir))
                        continue;

                    long key = PortKey(cell, DirIndex(dir));
                    if (_portKeys.ContainsKey(key))
                        continue;

                    _portKeys[key] = _portBlock.Count;
                    _portBlock.Add(b);
                    _portCell.Add(cell);
                    _portDir.Add(dir);
                    _portSmall.Add(templates[t].Small);
                    _portLinked.Add(false);
                    hasLarge |= !templates[t].Small;
                }

                if (_portBlock.Count == 0 || _portBlock[_portBlock.Count - 1] != b)
                    continue;

                _blocks.Add(block);
                _connected.Add(0);
                _empty.Add(0);
                _hasLarge.Add(hasLarge);
                _removed.Add(false);
            }
        }

        List<PortTemplate> GetPorts(IMyModel model)
        {
            string asset = model.AssetName ?? string.Empty;
            List<PortTemplate> ports;
            if (PortCache.TryGetValue(asset, out ports))
                return ports;

            ports = new List<PortTemplate>();
            _dummies.Clear();
            model.GetDummies(_dummies);

            foreach (var pair in _dummies)
            {
                string name = pair.Key.ToLowerInvariant();
                if (!name.StartsWith("detector_conveyor") || name.Contains("section") || name.Contains("cut") ||
                    name.Contains("bars") || name.Contains("parts") || name.Contains("past") || name.Contains("lod"))
                    continue;

                ports.Add(new PortTemplate { Position = pair.Value.Matrix.Translation, Small = name.Contains("small") });
            }

            PortCache[asset] = ports;
            return ports;
        }

        static bool TryResolvePort(IMyCubeBlock block, Vector3 local, float gridSize, out Vector3I cell, out Vector3I dir)
        {
            var p = Vector3.Transform(local, block.LocalMatrix) / gridSize;
            cell = new Vector3I((int)Math.Round(p.X), (int)Math.Round(p.Y), (int)Math.Round(p.Z));
            dir = Vector3I.Zero;

            int axis = -1;
            float best = 0.25f;
            for (int a = 0; a < 3; a++)
            {
                float v = p.GetDim(a);
                float frac = Math.Abs(v - (float)Math.Round(v));
                if (frac > best)
                {
                    best = frac;
                    axis = a;
                }
            }
            if (axis < 0)
                return false;

            int floor = (int)Math.Floor(p.GetDim(axis));
            var unit = axis == 0 ? Vector3I.UnitX : axis == 1 ? Vector3I.UnitY : Vector3I.UnitZ;
            var lower = new Vector3I(axis == 0 ? floor : cell.X, axis == 1 ? floor : cell.Y, axis == 2 ? floor : cell.Z);
            var upper = lower + unit;
            if (Inside(block, lower))
            {
                cell = lower;
                dir = unit;
                return true;
            }
            if (Inside(block, upper))
            {
                cell = upper;
                dir = -unit;
                return true;
            }
            return false;
        }

        static bool Inside(IMyCubeBlock block, Vector3I c)
        {
            var min = block.Min;
            var max = block.Max;
            return c.X >= min.X && c.X <= max.X && c.Y >= min.Y && c.Y <= max.Y && c.Z >= min.Z && c.Z <= max.Z;
        }

        void LinkPorts()
        {
            _edgeU.Clear();
            _edgeV.Clear();
            _edgeSmall.Clear();
            _edgeAlive.Clear();
            while (_incident.Count < _blocks.Count)
                _incident.Add(new List<int>());
            for (int b = 0; b < _blocks.Count; b++)
                _incident[b].Clear();

            for (int p = 0; p < _portBlock.Count; p++)
            {
                int q;
                var dir = _portDir[p];
                if (!_portKeys.TryGetValue(PortKey(_portCell[p] + dir, DirIndex(-dir)), out q) || q <= p ||
                    _portBlock[q] == _portBlock[p])
                    continue;

                _portLinked[p] = true;
                _portLinked[q] = true;
                AddEdge(_portBlock[p], _portBlock[q], _portSmall[p] || _portSmall[q]);
            }

            for (int p = 0; p < _portBlock.Count; p++)
            {
                if (_portLinked[p])
                    _connected[_portBlock[p]]++;
                else
                    _empty[_portBlock[p]]++;
            }
        }

        void AddEdge(int u, int v, bool small)
        {
            _incident[u].Add(_edgeU.Count);
            _incident[v].Add(_edgeU.Count);
            _edgeU.Add(u);
            _edgeV.Add(v);
            _edgeSmall.Add(small);
            _edgeAlive.Add(true);
        }

        void ContractPassThroughs()
        {
            for (int b = 0; b < _blocks.Count; b++)
            {
                if (_blocks[b] is IMyTerminalBlock || _connected[b] != 2 || _empty[b] != 0)
                    continue;

                int first = -1, second = -1;
                var incident = _incident[b];
                for (int i = 0; i < incident.Count; i++)
                {
                    if (!_edgeAlive[incident[i]])
                        continue;
                    if (first < 0) first = incident[i];
                    else second = incident[i];
                }
                if (second < 0)
                    continue;

                int x = _edgeU[first] == b ? _edgeV[first] : _edgeU[first];
                int y = _edgeU[second] == b ? _edgeV[second] : _edgeU[second];
                if (x == b || y == b || x == y)
                    continue;

                _edgeAlive[first] = false;
                _edgeAlive[second] = false;
                _removed[b] = true;
                AddEdge(x, y, _edgeSmall[first] || _edgeSmall[second]);
            }
        }

        void BuildNodes(bool largeGrid)
        {
            _nodes.Clear();
            _nodeIcons.Clear();
            _nodeAlert.Clear();
            _nodeIsolated.Clear();
            _nodeOfBlock.Clear();
            _edgeNetwork.Clear();
            _edgeBottleneck.Clear();
            _markerNode.Clear();
            _markerFace.Clear();

            for (int b = 0; b < _blocks.Count; b++)
            {
                _nodeOfBlock.Add(_removed[b] ? -1 : _nodes.Count);
                if (_removed[b])
                    continue;

                _nodes.Add(_blocks[b]);
                _nodeAlert.Add(false);
                var slim = _blocks[b].SlimBlock;
                _nodeIcons.Add(TextureHelper.GetOrAddTextureForBlock(slim != null ? slim.BlockDefinition as MyCubeBlockDefinition : null));
            }

            int n = _nodes.Count;
            if (_unionFind.Length < n) _unionFind = new int[n * 2];
            if (_networkOfRoot.Length < n) _networkOfRoot = new int[n * 2];
            if (_rootSize.Length < n) _rootSize = new int[n * 2];
            for (int i = 0; i < n; i++)
                _unionFind[i] = i;

            _bottlenecks = 0;
            for (int e = 0; e < _edgeU.Count; e++)
            {
                bool bottleneck = _edgeAlive[e] && largeGrid && _edgeSmall[e] && _hasLarge[_edgeU[e]] && _hasLarge[_edgeV[e]];
                _edgeBottleneck.Add(bottleneck);
                if (bottleneck) _bottlenecks++;
                if (_edgeAlive[e])
                    _unionFind[Find(_nodeOfBlock[_edgeU[e]])] = Find(_nodeOfBlock[_edgeV[e]]);
            }

            Array.Clear(_rootSize, 0, n);
            for (int i = 0; i < n; i++)
                _rootSize[Find(i)]++;
            _networkCount = 0;
            for (int i = 0; i < n; i++)
                if (Find(i) == i)
                    _networkOfRoot[i] = _rootSize[i] >= 2 ? _networkCount++ : -1;
            for (int i = 0; i < n; i++)
                _nodeIsolated.Add(_rootSize[Find(i)] < 2);
            for (int e = 0; e < _edgeU.Count; e++)
                _edgeNetwork.Add(_edgeAlive[e] ? _networkOfRoot[Find(_nodeOfBlock[_edgeU[e]])] : -1);

            for (int p = 0; p < _portBlock.Count; p++)
            {
                if (_portLinked[p] || _removed[_portBlock[p]] || _blocks[_portBlock[p]] is IMyTerminalBlock)
                    continue;
                _markerNode.Add(_nodeOfBlock[_portBlock[p]]);
                _markerFace.Add(DirIndex(_portDir[p]) % 4);
            }
        }

        void CollectAnchors()
        {
            _networkAnchors.Clear();
            int n = _nodes.Count;
            if (_anchorOfRoot.Length < n) _anchorOfRoot = new int[n * 2];
            for (int i = 0; i < n; i++)
                _anchorOfRoot[i] = -1;

            for (int b = 0; b < _blocks.Count; b++)
            {
                if (_removed[b] || !(_blocks[b] is IMyTerminalBlock))
                    continue;
                int root = Find(_nodeOfBlock[b]);
                if (_networkOfRoot[root] < 0)
                    continue;
                int current = _anchorOfRoot[root];
                if (current < 0 || _blocks[b].EntityId < _blocks[current].EntityId)
                    _anchorOfRoot[root] = b;
            }

            for (int i = 0; i < n; i++)
                if (_anchorOfRoot[i] >= 0)
                    _networkAnchors.Add((IMyTerminalBlock)_blocks[_anchorOfRoot[i]]);
        }

        /// <summary>Marca como removidos os blocos fora da rede do bloco âncora; âncora ausente mantém todas as redes.</summary>
        bool FilterToNetwork(long anchorId)
        {
            _filteredOut = 0;
            if (anchorId == 0L)
                return false;

            int anchor = -1;
            for (int b = 0; b < _blocks.Count && anchor < 0; b++)
                if (!_removed[b] && _blocks[b].EntityId == anchorId)
                    anchor = b;
            if (anchor < 0)
                return false;

            int root = Find(_nodeOfBlock[anchor]);
            for (int b = 0; b < _blocks.Count; b++)
            {
                if (_removed[b] || Find(_nodeOfBlock[b]) == root)
                    continue;
                _removed[b] = true;
                _filteredOut++;
            }

            for (int e = 0; e < _edgeU.Count; e++)
                if (_edgeAlive[e] && (_removed[_edgeU[e]] || _removed[_edgeV[e]]))
                    _edgeAlive[e] = false;
            return true;
        }

        int Find(int i)
        {
            while (_unionFind[i] != i)
            {
                _unionFind[i] = _unionFind[_unionFind[i]];
                i = _unionFind[i];
            }
            return i;
        }

        void EvaluateAlerts()
        {
            _alerts = 0;
            for (int i = 0; i < _nodes.Count; i++)
            {
                bool alert = IsAlert(_nodes[i]);
                _nodeAlert[i] = alert;
                if (alert) _alerts++;
            }
        }

        bool IsAlert(IMyCubeBlock block)
        {
            if (block == null || block.Closed)
                return false;

            var sorter = block as IMyConveyorSorter;
            if (sorter != null && sorter.Mode == MyConveyorSorterMode.Whitelist)
            {
                _filters.Clear();
                sorter.GetFilterList(_filters);
                if (_filters.Count == 0)
                    return true;
            }

            var functional = block as IMyFunctionalBlock;
            return functional != null && (!functional.Enabled || !functional.IsWorking);
        }

        readonly List<MySprite> _sprites = new List<MySprite>();
        string[] _footerSummary;
        string[] _footerLegend;

        public override List<MySprite> GetSprites()
        {
            var sprites = _sprites;
            sprites.Clear();
            var area = GetMapArea();
            UpdateNetworkColors();
            float messageScale = 0.6f * LayoutScale;
            var messagePosition = new Vector2(area.Center.X, area.Center.Y - MeasureLineHeight(messageScale) * 0.5f);
            if (_nodes.Count == 0)
                AddText(sprites, LocHelper.GetLoc(MOD_PREFIX + "ConveyorMap_NoConveyors"), messagePosition, messageScale, TextAlignment.CENTER, Host.ForegroundColor);
            else if (_tooLarge)
                AddText(sprites, string.Format(LocHelper.GetLoc(MOD_PREFIX + "ConveyorMap_TooLarge"), _nodes.Count, MAX_NODES),
                    messagePosition, messageScale, TextAlignment.CENTER, Host.ForegroundColor);
            else
                DrawMap(sprites, area);

            DrawFooter(sprites);
            return sprites;
        }

        RectangleF GetMapArea()
        {
            float margin = 4f * Scale;
            float top = ContentTop + margin;
            float bottom = ViewBox.Bottom - FooterHeight - margin;
            return new RectangleF(ViewBox.X + margin, top, Math.Max(1f, ViewBox.Width - 2f * margin), Math.Max(1f, bottom - top));
        }

        void DrawMap(List<MySprite> sprites, RectangleF area)
        {
            int columns = Math.Max(1, _layout.Columns), rows = Math.Max(1, _layout.Rows);
            float tile = Math.Min(Math.Min(area.Width / columns, area.Height / rows) / PITCH, MAX_TILE_BASE * Scale);
            float pitch = tile * PITCH;
            float half = tile * 0.5f;
            var origin = new Vector2(area.X + (area.Width - columns * pitch + pitch) * 0.5f,
                area.Y + (area.Height - rows * pitch + pitch) * 0.5f);
            float line = Math.Max(1f, tile * 0.08f);
            float lane = (pitch - tile) / 5f;
            var warning = GetWarningColor();

            for (int s = 0; s < _layout.SegmentCount; s++)
            {
                int a = _layout.SegA[s], b = _layout.SegB[s], e = _layoutEdges[_layout.SegEdge[s]];
                var pa = NodeCenter(origin, pitch, a);
                var pb = NodeCenter(origin, pitch, b);
                bool bottleneck = _edgeBottleneck[e];
                var color = bottleneck ? warning : _networkColors[_edgeNetwork[e] % _networkColors.Length];
                float width = bottleneck ? line * 2f : line;
                float laneOffset = (s % 4 - 1.5f) * lane;

                if (_layout.SegSameLayer[s])
                {
                    DrawRoute(sprites, new Vector2(pa.X + half, pa.Y), pa.X + pitch * 0.5f + laneOffset,
                        new Vector2(pb.X + half, pb.Y), width, color);
                    continue;
                }

                float startX = a >= _layout.RealCount ? pa.X : pa.X + half;
                float endX = b >= _layout.RealCount ? pb.X : pb.X - half;
                DrawRoute(sprites, new Vector2(startX, pa.Y), (pa.X + pb.X) * 0.5f + laneOffset, new Vector2(endX, pb.Y), width, color);
            }

            var tileColor = BackgroundColor.MulValue(0.6f);
            var error = GetErrorColor();
            for (int i = 0; i < _nodes.Count; i++)
            {
                var center = NodeCenter(origin, pitch, i);
                float alpha = _nodeIsolated[i] ? 0.5f : 1f;
                sprites.Add(LineRenderer.Rect(center, new Vector2(tile), tileColor * alpha));
                sprites.Add(new MySprite(SpriteType.TEXTURE, _nodeIcons[i], center, new Vector2(tile * ICON_FILL), Color.White * alpha));
                if (_nodeAlert[i])
                    LineRenderer.DrawRectOutline(sprites, new RectangleF(center.X - half, center.Y - half, tile, tile), line * 2f, error);
            }

            // ponytail: face do marcador vem do índice da direção no grid (Z cai em direita/esquerda), sem girar pela orientação do bloco.
            var marker = new Vector2(tile * 0.2f);
            var markerBorder = marker + new Vector2(line * 2f);
            for (int m = 0; m < _markerNode.Count; m++)
            {
                var center = NodeCenter(origin, pitch, _markerNode[m]);
                int face = _markerFace[m];
                var offset = face == 0 ? new Vector2(half, 0f) : face == 1 ? new Vector2(-half, 0f) : face == 2 ? new Vector2(0f, -half) : new Vector2(0f, half);
                sprites.Add(LineRenderer.Rect(center + offset, markerBorder, BackgroundColor));
                sprites.Add(LineRenderer.Rect(center + offset, marker, Color.Yellow));
            }
        }

        Vector2 NodeCenter(Vector2 origin, float pitch, int node)
        {
            return new Vector2(origin.X + _layout.Layer[node] * pitch, origin.Y + _layout.Row[node] * pitch);
        }

        void UpdateNetworkColors()
        {
            var accent = ResolveResource(ThemeResources.AccentColor, GetHeaderColor());
            var hsv = accent.ColorToHSV();
            _networkColors[0] = accent;
            for (int i = 1; i < _networkColors.Length; i++)
                _networkColors[i] = new Vector3((hsv.X + NetworkHueShifts[i]) % 1f, Math.Max(hsv.Y, 0.55f), hsv.Z).HSVtoColor()
                    .EnsureMinimalContrast(BackgroundColor);
        }

        static void DrawRoute(List<MySprite> sprites, Vector2 start, float channelX, Vector2 end, float width, Color color)
        {
            var bendA = new Vector2(channelX, start.Y);
            var bendB = new Vector2(channelX, end.Y);
            float extend = width * 0.5f;
            LineRenderer.DrawLine(sprites, start, bendA, width, color, extend);
            LineRenderer.DrawLine(sprites, bendA, bendB, width, color, extend);
            LineRenderer.DrawLine(sprites, bendB, end, width, color, extend);
        }

        void DrawFooter(List<MySprite> sprites)
        {
            float height = FooterHeight;
            float top = ViewBox.Bottom - height;
            float rowHeight = height * 0.5f;
            float textScale = 0.46f * LayoutScale;
            float lineHeight = MeasureLineHeight(textScale);
            float textY = (rowHeight - lineHeight) * 0.5f;
            float left = ViewBox.X + 6f * Scale;
            var foreground = Host.ForegroundColor;

            if (!ThemeChrome.DrawFooterBand(sprites, Theme, new RectangleF(ViewBox.X, top, ViewBox.Width, height), BackgroundColor, GetHeaderColor(), foreground, LayoutScale))
            {
                sprites.Add(LineRenderer.Rect(new Vector2(ViewBox.Center.X, top + height * 0.5f), new Vector2(ViewBox.Width, height),
                    new Color(BackgroundColor.MulValue(0.8f), 0.5f)));
            }

            var summary = _footerSummary ?? (_footerSummary = LocHelper.GetLoc(MOD_PREFIX + "ConveyorMap_Footer").Split('|'));
            var position = new Vector2(left, top + textY);
            for (int i = 0; i < summary.Length; i++)
            {
                if (summary[i].Contains("{4}"))
                    continue;
                if (i > 0)
                    position.X += AddText(sprites, "|", position, textScale, TextAlignment.LEFT, foreground);
                string part = string.Format(summary[i], _networkCount, _blocks.Count - _filteredOut, _markerNode.Count, _bottlenecks);
                position.X += AddText(sprites, part, position, textScale, TextAlignment.LEFT, SummaryColor(summary[i], foreground));
            }
            position.X += AddText(sprites, "|", position, textScale, TextAlignment.LEFT, foreground);
            position.X += AddText(sprites, " " + _alerts + " " + Loc("Alerts") + " ", position, textScale, TextAlignment.LEFT, _alerts > 0 ? GetErrorColor() : foreground);

            string damage = Damaged > 0 && Destroyed > 0 ? "| " + Damaged + " " + Loc("Damaged") + " | " + Destroyed + " " + Loc("Destroyed")
                : Damaged > 0 ? "| " + Damaged + " " + Loc("Damaged")
                : Destroyed > 0 ? "| " + Destroyed + " " + Loc("Destroyed") : null;
            if (damage != null)
            {
                position.X += MeasureText(" ", textScale).X;
                Bare.Text(sprites, damage, position, TextAlignment.LEFT, 0.46f * Scale, Destroyed > 0 ? GetErrorColor() : GetWarningColor(),
                    ViewBox.Right - position.X, rowHeight);
            }

            var legend = _footerLegend ?? (_footerLegend = LocHelper.GetLoc(MOD_PREFIX + "ConveyorMap_Legend").Split('|'));
            float swatch = lineHeight * 0.5f;
            position = new Vector2(left, top + rowHeight + textY);
            for (int i = 0; i < legend.Length; i++)
            {
                if (i > 0)
                    position.X += AddText(sprites, "|", position, textScale, TextAlignment.LEFT, foreground);
                for (int c = 0; i == 0 && c < Math.Min(Math.Max(_networkCount, 1), 3); c++)
                {
                    sprites.Add(LineRenderer.Rect(new Vector2(position.X + swatch * 0.5f, position.Y + lineHeight * 0.5f), new Vector2(swatch), _networkColors[c]));
                    position.X += swatch * 1.5f;
                }
                position.X += AddText(sprites, legend[i], position, textScale, TextAlignment.LEFT, foreground);
            }
        }

        Color SummaryColor(string template, Color foreground)
        {
            if (template.Contains("{2}") && _markerNode.Count > 0)
                return Color.Yellow.EnsureMinimalContrast(BackgroundColor);
            if (template.Contains("{3}") && _bottlenecks > 0)
                return GetWarningColor();
            return foreground;
        }

        string Loc(string key)
        {
            string text;
            if (!_loc.TryGetValue(key, out text))
                _loc[key] = text = LocHelper.GetLoc(MOD_PREFIX + "ConveyorMap_" + key).ToUpperInvariant();
            return text;
        }

        float AddText(List<MySprite> sprites, string text, Vector2 position, float scale, TextAlignment alignment, Color color)
        {
            sprites.Add(new MySprite
            {
                Type = SpriteType.TEXT,
                Data = text,
                Position = position,
                Color = color,
                Alignment = alignment,
                FontId = TextFont,
                RotationOrScale = scale
            });
            return MeasureText(text, scale).X;
        }

        static int DirIndex(Vector3I d)
        {
            return d.X > 0 ? 0 : d.X < 0 ? 1 : d.Y > 0 ? 2 : d.Y < 0 ? 3 : d.Z > 0 ? 4 : 5;
        }

        static long PortKey(Vector3I cell, int dir)
        {
            long packed = ((long)(cell.X & 0xFFFFF) << 40) | ((long)(cell.Y & 0xFFFFF) << 20) | (long)(cell.Z & 0xFFFFF);
            return (packed << 3) | (long)dir;
        }

        public override IReadOnlyList<Control> VisualChildren { get; } = new Control[] { };
    }
}
