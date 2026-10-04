using System.Collections.Generic;
using LcdMod.Client.SurfaceScripts.Abstract;
using Sandbox.ModAPI;
using IMyTextSurfaceProvider = Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider;

namespace LcdMod.Client.Utility
{
    public sealed class SurfaceTssInstances
    {
        readonly Dictionary<int, SurfaceScriptBase> _instancesByIndex =
            new Dictionary<int, SurfaceScriptBase>();

        public SurfaceScriptBase GetInstance(int index)
        {
            SurfaceScriptBase instance;
            return _instancesByIndex.TryGetValue(index, out instance) ? instance : null;
        }

        public IEnumerable<SurfaceScriptBase> GetInstances()
        {
            foreach (var entry in _instancesByIndex)
                yield return entry.Value;
        }

        internal void Add(SurfaceScriptBase instance)
        {
            if (instance == null)
                return;

            Remove(instance);
            _instancesByIndex[instance.RotationOrSurfaceIndex] = instance;
        }

        internal void Remove(SurfaceScriptBase instance)
        {
            if (instance == null)
                return;

            var emptyIndexes = new List<int>();
            foreach (var entry in _instancesByIndex)
            {
                if (entry.Value == instance)
                    emptyIndexes.Add(entry.Key);
            }

            for (int i = 0; i < emptyIndexes.Count; i++)
                _instancesByIndex.Remove(emptyIndexes[i]);
        }

        internal bool IsEmpty => _instancesByIndex.Count == 0;
    }

    public sealed class SurfaceCollection : ICollection<SurfaceScriptBase>
    {
        readonly List<SurfaceScriptBase> _items = new List<SurfaceScriptBase>();
        readonly Dictionary<long, SurfaceTssInstances> _instancesByBlock =
            new Dictionary<long, SurfaceTssInstances>();

        public int Count => _items.Count;
        public bool IsReadOnly => false;

        public void Add(SurfaceScriptBase item)
        {
            _items.Add(item);
            AddToSlots(item);
        }

        public bool Remove(SurfaceScriptBase item)
        {
            var removed = _items.Remove(item);
            if (removed)
                RemoveFromSlots(item);

            return removed;
        }

        public void Clear()
        {
            _items.Clear();
            _instancesByBlock.Clear();
        }

        public SurfaceTssInstances GetInstances(IMyTerminalBlock block)
        {
            if (block == null)
                return null;

            SurfaceTssInstances instances;
            return _instancesByBlock.TryGetValue(block.EntityId, out instances) ? instances : null;
        }

        public SurfaceScriptBase GetInstance(IMyTerminalBlock block, int index)
        {
            var instances = GetInstances(block);
            return instances?.GetInstance(index);
        }

        void AddToSlots(SurfaceScriptBase item)
        {
            var block = item?.Block as IMyTerminalBlock;
            if (block == null || !(block is IMyTextPanel || block is IMyTextSurfaceProvider))
                return;

            SurfaceTssInstances instances;
            if (!_instancesByBlock.TryGetValue(block.EntityId, out instances))
            {
                instances = new SurfaceTssInstances();
                _instancesByBlock[block.EntityId] = instances;
            }

            instances.Add(item);
        }

        void RemoveFromSlots(SurfaceScriptBase item)
        {
            var block = item?.Block as IMyTerminalBlock;
            if (block == null)
                return;

            SurfaceTssInstances instances;
            if (!_instancesByBlock.TryGetValue(block.EntityId, out instances))
                return;

            instances.Remove(item);
            if (instances.IsEmpty)
                _instancesByBlock.Remove(block.EntityId);
        }

        public bool Contains(SurfaceScriptBase item)
        {
            return _items.Contains(item);
        }

        public void CopyTo(SurfaceScriptBase[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        public IEnumerator<SurfaceScriptBase> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }
    }
}
