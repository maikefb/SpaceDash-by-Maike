using System;
using System.Collections.Generic;
using LcdMod.Client.GridData;
using VRage.Game.ModAPI;

namespace LcdMod.Client.Modules.Energy
{
    public struct EnergyScopeKey : IEquatable<EnergyScopeKey>
    {
        public readonly long GridEntityId;
        public readonly GridLinkTypeEnum LinkType;

        public EnergyScopeKey(long gridEntityId, GridLinkTypeEnum linkType)
        {
            GridEntityId = gridEntityId;
            LinkType = linkType;
        }

        public bool Equals(EnergyScopeKey other)
        {
            return GridEntityId == other.GridEntityId && LinkType == other.LinkType;
        }

        public override bool Equals(object obj)
        {
            return obj is EnergyScopeKey && Equals((EnergyScopeKey)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (GridEntityId.GetHashCode() * 397) ^ (int)LinkType;
            }
        }
    }

    /// <summary>
    ///     Um serviço por grid do LCD e tipo de vínculo, compartilhado por todas as telas de energia desse grid.
    ///     Uma tela recriada dentro do período de carência reencontra o mesmo histórico.
    /// </summary>
    public sealed class EnergyDataModule
    {
        const long RELEASE_GRACE_FRAMES = 600L;

        readonly Dictionary<EnergyScopeKey, EnergyDataService> _services =
            new Dictionary<EnergyScopeKey, EnergyDataService>();
        readonly List<EnergyScopeKey> _expired = new List<EnergyScopeKey>();

        public EnergyDataLease Capture(GridLogic requester, GridLinkTypeEnum linkType)
        {
            var key = new EnergyScopeKey(requester != null ? requester.TargetGrid : 0L, linkType);
            EnergyDataService service;
            if (!_services.TryGetValue(key, out service))
            {
                service = new EnergyDataService(requester, linkType, key.GetHashCode() & 0x7fffffff);
                _services[key] = service;
            }

            service.AddCapture(requester);
            return new EnergyDataLease(service);
        }

        public void Update(long gameplayFrame)
        {
            foreach (var pair in _services)
            {
                pair.Value.Update(gameplayFrame);
                if (!pair.Value.HasCaptures && gameplayFrame - pair.Value.ReleasedFrame > RELEASE_GRACE_FRAMES)
                    _expired.Add(pair.Key);
            }

            for (int i = 0; i < _expired.Count; i++)
            {
                _services[_expired[i]].Dispose();
                _services.Remove(_expired[i]);
            }

            _expired.Clear();
        }

        public void Unload()
        {
            foreach (var service in _services.Values)
                service.Dispose();
            _services.Clear();
        }
    }
}
