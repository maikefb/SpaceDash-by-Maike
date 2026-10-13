using System;

namespace LcdMod.Client.Apps
{
    internal sealed class ConveyorLayout
    {
        const int SWEEPS = 6;

        public int Count;
        public int RealCount;
        public int[] Layer = new int[0];
        public int[] Row = new int[0];
        public int SegmentCount;
        public int[] SegA = new int[0];
        public int[] SegB = new int[0];
        public int[] SegEdge = new int[0];
        public bool[] SegSameLayer = new bool[0];
        public int Columns;
        public int Rows;

        int _edgeCount;
        int[] _edgeA = new int[0];
        int[] _edgeB = new int[0];
        int[] _start = new int[0];
        int[] _adj = new int[0];
        int[] _mark = new int[0];
        int[] _parent = new int[0];
        int[] _queue = new int[0];
        int[] _sequence = new int[0];
        int[] _order = new int[0];
        int[] _pos = new int[0];
        int[] _bestPos = new int[0];
        float[] _keys = new float[0];
        int[] _layerStart = new int[0];
        int[] _layerFill = new int[0];
        int[] _crossA = new int[0];
        int[] _crossB = new int[0];

        public void Reset(int nodeCount)
        {
            RealCount = nodeCount;
            _edgeCount = 0;
        }

        public void AddEdge(int a, int b)
        {
            Grow(ref _edgeA, _edgeCount + 1);
            Grow(ref _edgeB, _edgeCount + 1);
            _edgeA[_edgeCount] = a;
            _edgeB[_edgeCount] = b;
            _edgeCount++;
        }

        public void Run(int maxRows)
        {
            int n = RealCount;
            Count = n;
            SegmentCount = 0;
            Columns = 0;
            Rows = 0;
            if (n == 0)
                return;

            AssignLayers(n, maxRows);
            BuildSegments();
            OrderLayers();
            AssignRows(maxRows);
        }

        void AssignLayers(int n, int maxRows)
        {
            BuildAdjacency(n, _edgeA, _edgeB, _edgeCount);
            Grow(ref Layer, n);
            Grow(ref Row, n);
            Grow(ref _mark, n);
            Grow(ref _parent, n);
            Grow(ref _queue, n);
            Grow(ref _sequence, n);
            Array.Clear(_mark, 0, n);

            int sequenceLength = 0;
            int columnOffset = 0;
            for (int s = 0; s < n; s++)
            {
                if (_mark[s] != 0)
                    continue;

                int head = 0, tail = 0, hub = s;
                _queue[tail++] = s;
                _mark[s] = 1;
                while (head < tail)
                {
                    int v = _queue[head++];
                    if (Degree(v) > Degree(hub))
                        hub = v;
                    for (int k = _start[v]; k < _start[v + 1]; k++)
                        if (_mark[_adj[k]] == 0)
                        {
                            _mark[_adj[k]] = 1;
                            _queue[tail++] = _adj[k];
                        }
                }

                int size = tail;
                head = 0;
                tail = 0;
                _queue[tail++] = hub;
                _mark[hub] = 2;
                _parent[hub] = -1;
                while (head < tail)
                {
                    int v = _queue[head++];
                    for (int k = _start[v]; k < _start[v + 1]; k++)
                        if (_mark[_adj[k]] == 1)
                        {
                            _mark[_adj[k]] = 2;
                            _parent[_adj[k]] = v;
                            _queue[tail++] = _adj[k];
                        }
                }

                int cur = 0, used = 0;
                for (int k = 0; k < size; k++)
                {
                    int v = _queue[k];
                    int want = _parent[v] < 0 ? 0 : Layer[_parent[v]] + 1;
                    if (want > cur)
                    {
                        cur = want;
                        used = 0;
                    }
                    if (used == maxRows)
                    {
                        cur++;
                        used = 0;
                    }
                    Layer[v] = cur;
                    used++;
                }

                for (int k = 0; k < size; k++)
                {
                    Layer[_queue[k]] += columnOffset;
                    _sequence[sequenceLength++] = _queue[k];
                }
                columnOffset += cur + 2;
            }
            Columns = columnOffset - 1;
        }

        int Degree(int v)
        {
            return _start[v + 1] - _start[v];
        }

        void BuildSegments()
        {
            for (int e = 0; e < _edgeCount; e++)
            {
                int a = _edgeA[e], b = _edgeB[e];
                if (Layer[a] == Layer[b])
                {
                    AddSegment(a, b, e, true);
                    continue;
                }
                if (Layer[a] > Layer[b])
                {
                    int t = a;
                    a = b;
                    b = t;
                }
                int prev = a;
                for (int l = Layer[a] + 1; l < Layer[b]; l++)
                {
                    int dummy = AddDummy(l);
                    AddSegment(prev, dummy, e, false);
                    prev = dummy;
                }
                AddSegment(prev, b, e, false);
            }
        }

        int AddDummy(int layer)
        {
            Grow(ref Layer, Count + 1);
            Grow(ref Row, Count + 1);
            Layer[Count] = layer;
            return Count++;
        }

        void AddSegment(int a, int b, int edge, bool sameLayer)
        {
            int i = SegmentCount++;
            Grow(ref SegA, SegmentCount);
            Grow(ref SegB, SegmentCount);
            Grow(ref SegEdge, SegmentCount);
            Grow(ref SegSameLayer, SegmentCount);
            SegA[i] = a;
            SegB[i] = b;
            SegEdge[i] = edge;
            SegSameLayer[i] = sameLayer;
        }

        void OrderLayers()
        {
            int c = Count;
            Grow(ref _order, c);
            Grow(ref _pos, c);
            Grow(ref _bestPos, c);
            Grow(ref _keys, c);
            Grow(ref _layerStart, Columns + 1);
            Grow(ref _crossA, SegmentCount);
            Grow(ref _crossB, SegmentCount);
            BuildAdjacency(c, SegA, SegB, SegmentCount);

            Array.Clear(_layerStart, 0, Columns + 1);
            for (int v = 0; v < c; v++)
                _layerStart[Layer[v] + 1]++;
            for (int l = 0; l < Columns; l++)
                _layerStart[l + 1] += _layerStart[l];
            Grow(ref _layerFill, Columns);
            Array.Clear(_layerFill, 0, Columns);
            for (int i = 0; i < c; i++)
            {
                int v = i < RealCount ? _sequence[i] : i;
                int l = Layer[v];
                _pos[v] = _layerFill[l]++;
                _order[_layerStart[l] + _pos[v]] = v;
            }

            int best = CountCrossings();
            Array.Copy(_pos, _bestPos, c);
            for (int sweep = 0; sweep < SWEEPS && best > 0; sweep++)
            {
                if (sweep % 2 == 0)
                    for (int l = 1; l < Columns; l++) Reorder(l, l - 1);
                else
                    for (int l = Columns - 2; l >= 0; l--) Reorder(l, l + 1);

                int crossings = CountCrossings();
                if (crossings < best)
                {
                    best = crossings;
                    Array.Copy(_pos, _bestPos, c);
                }
            }

            Array.Copy(_bestPos, _pos, c);
            for (int v = 0; v < c; v++)
                _order[_layerStart[Layer[v]] + _pos[v]] = v;
        }

        void Reorder(int layer, int fixedLayer)
        {
            int s = _layerStart[layer], len = _layerStart[layer + 1] - s;
            if (len < 2)
                return;

            for (int i = 0; i < len; i++)
            {
                int v = _order[s + i];
                float sum = 0f;
                int count = 0;
                for (int k = _start[v]; k < _start[v + 1]; k++)
                    if (Layer[_adj[k]] == fixedLayer)
                    {
                        sum += _pos[_adj[k]];
                        count++;
                    }
                _keys[s + i] = (count > 0 ? sum / count : _pos[v]) + _pos[v] * 1e-4f;
            }

            Array.Sort(_keys, _order, s, len);
            for (int i = 0; i < len; i++)
                _pos[_order[s + i]] = i;
        }

        int CountCrossings()
        {
            int total = 0;
            for (int l = 0; l + 1 < Columns; l++)
            {
                int count = 0;
                for (int i = _layerStart[l]; i < _layerStart[l + 1]; i++)
                {
                    int v = _order[i];
                    for (int k = _start[v]; k < _start[v + 1]; k++)
                        if (Layer[_adj[k]] == l + 1)
                        {
                            _crossA[count] = _pos[v];
                            _crossB[count] = _pos[_adj[k]];
                            count++;
                        }
                }

                for (int i = 0; i < count; i++)
                    for (int j = i + 1; j < count; j++)
                        if ((_crossA[i] - _crossA[j]) * (_crossB[i] - _crossB[j]) < 0)
                            total++;
            }
            return total;
        }

        void AssignRows(int maxRows)
        {
            Rows = maxRows;
            for (int l = 0; l < Columns; l++)
                Rows = Math.Max(Rows, _layerStart[l + 1] - _layerStart[l]);
            for (int v = 0; v < Count; v++)
            {
                int size = _layerStart[Layer[v] + 1] - _layerStart[Layer[v]];
                Row[v] = _pos[v] + (Rows - size) / 2;
            }
        }

        void BuildAdjacency(int nodes, int[] a, int[] b, int count)
        {
            Grow(ref _start, nodes + 1);
            Grow(ref _adj, count * 2);
            Array.Clear(_start, 0, nodes + 1);
            for (int e = 0; e < count; e++)
            {
                _start[a[e] + 1]++;
                _start[b[e] + 1]++;
            }
            for (int v = 0; v < nodes; v++)
                _start[v + 1] += _start[v];

            Grow(ref _mark, nodes);
            Array.Copy(_start, _mark, nodes);
            for (int e = 0; e < count; e++)
            {
                _adj[_mark[a[e]]++] = b[e];
                _adj[_mark[b[e]]++] = a[e];
            }
        }

        static void Grow<T>(ref T[] array, int size)
        {
            if (array.Length < size)
                Array.Resize(ref array, Math.Max(size, array.Length * 2));
        }
    }
}
