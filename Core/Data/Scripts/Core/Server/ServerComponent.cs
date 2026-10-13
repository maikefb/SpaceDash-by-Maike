using System;
using System.Collections.Generic;
using Generated;
using LcdMod.Common.Config;
using LcdMod.Common.Config.Components;
using LcdMod.Common.Config.Models;
using LcdMod.Common.Helpers;
using LcdMod.Common.Networking;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using IMyTextSurfaceProvider = Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider;

namespace LcdMod.Server
{
    /// <summary>
    /// Lado servidor: persiste a configuração sincronizada pelos clientes e mantém o remapeamento
    /// de blocos selecionados quando grids são colados, projetados, divididos ou juntados.
    /// </summary>
    public partial class LcdModServerComponent : ISingleton<LcdModServerComponent>
    {
        const int REMAP_FLUSH_DELAY_FRAMES = 60;

        readonly LcdModSessionComponent _session;
        readonly Dictionary<long, IMyCubeGrid> _trackedGrids = new Dictionary<long, IMyCubeGrid>();
        readonly Dictionary<long, Dictionary<long, long>> _gridRemaps = new Dictionary<long, Dictionary<long, long>>();
        readonly Dictionary<long, long> _dirtyGridDueFrame = new Dictionary<long, long>();
        readonly List<long> _dueGridIds = new List<long>();
        readonly HashSet<IMyEntity> _entities = new HashSet<IMyEntity>();

        public LcdModServerComponent(LcdModSessionComponent session)
        {
            RegisterSingleton();
            _session = session;
        }

        public void LoadData()
        {
            LoadTextureIndex();
            MyAPIGateway.Entities.OnEntityAdd += EntityAdded;
            LoadModData();
        }

        public void UnloadData()
        {
            ClearTextureRequests();
            MyAPIGateway.Entities.OnEntityAdd -= EntityAdded;
            UnloadModData();

            foreach (var grid in _trackedGrids.Values)
                UntrackGrid(grid);

            _trackedGrids.Clear();
            _gridRemaps.Clear();
            _dirtyGridDueFrame.Clear();
            _dueGridIds.Clear();
            _entities.Clear();
            UnregisterSingleton();
        }

        public void BeforeStart()
        {
            try
            {
                _entities.Clear();
                MyAPIGateway.Entities.GetEntities(_entities, entity => entity is IMyCubeGrid);

                foreach (var entity in _entities)
                    TrackGrid(entity as IMyCubeGrid);

                _entities.Clear();
                BeforeStartModData();
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, _session);
            }
        }

        public void SaveData()
        {
            SaveTextureIndex();
            SaveModData();
        }

        partial void LoadModData();
        partial void BeforeStartModData();
        partial void SaveModData();
        partial void UnloadModData();

        /// <summary>
        /// Blocos terminais adicionados marcam o grid como pendente; o remapeamento completo roda uma vez
        /// por grid a cada <see cref="REMAP_FLUSH_DELAY_FRAMES"/> frames, em vez de uma vez por bloco.
        /// </summary>
        public void Update()
        {
            CleanupTextureCooldowns();
            if (_dirtyGridDueFrame.Count == 0)
                return;

            var frame = MyAPIGateway.Session.GameplayFrameCounter;
            _dueGridIds.Clear();
            foreach (var entry in _dirtyGridDueFrame)
            {
                if (entry.Value <= frame)
                    _dueGridIds.Add(entry.Key);
            }

            for (int i = 0; i < _dueGridIds.Count; i++)
            {
                var gridId = _dueGridIds[i];
                _dirtyGridDueFrame.Remove(gridId);

                IMyCubeGrid grid;
                if (!_trackedGrids.TryGetValue(gridId, out grid) || grid.MarkedForClose || grid.Physics == null)
                    continue;

                RemapHelper.RemapGrid(grid, GetRemap(grid));
            }

            _dueGridIds.Clear();
        }

        internal bool HandleSyncConfig(NetworkPackageSyncComponentConfig packet, ulong senderSteamId)
        {
            if (packet == null || packet.Config == null)
                return false;

            var block = MyEntities.GetEntityById(packet.BlockId) as IMyFunctionalBlock;
            if (block == null || block.MarkedForClose)
                return false;

            var identityId = MyAPIGateway.Players.TryGetIdentityId(senderSteamId);
            if (identityId == 0 || !block.HasPlayerAccess(identityId))
                return false;

            var provider = block as IMyTextSurfaceProvider;
            var surfaceCount = block is IMyTextPanel ? 1 : (provider != null ? provider.SurfaceCount : 0);
            if (surfaceCount <= 0 || (packet.Config.Surfaces != null && packet.Config.Surfaces.Count > surfaceCount))
                return false;

            PinAccessibleBlocks(packet.Config, identityId);
            return ScreenProviderConfigStorage.Save(block, packet.Config);
        }

        const int MAX_PINNED_BLOCKS = 256;

        /// <summary>Só fixa o id de blocos que o remetente pode acessar, e no máximo 256 por config.</summary>
        static void PinAccessibleBlocks(ScreenProviderConfig config, long identityId)
        {
            if (config == null || config.Surfaces == null)
                return;

            var entityIds = new List<long>();
            for (int i = 0; i < config.Surfaces.Count; i++)
                ComponentConfigEntityReferences.CollectPinnedEntityIds(config.Surfaces[i], entityIds);

            int pinned = 0;
            foreach (var entityId in entityIds)
            {
                if (pinned >= MAX_PINNED_BLOCKS)
                    return;
                var target = MyEntities.GetEntityById(entityId) as IMyTerminalBlock;
                if (target == null || !target.HasPlayerAccess(identityId))
                    continue;
                RemapHelper.PinBlock(target);
                pinned++;
            }
        }

        void EntityAdded(IMyEntity entity)
        {
            try
            {
                var grid = entity as IMyCubeGrid;
                if (grid == null)
                    return;

                TrackGrid(grid);
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, _session);
            }
        }

        void TrackGrid(IMyCubeGrid grid)
        {
            if (grid == null || grid.MarkedForClose || _trackedGrids.ContainsKey(grid.EntityId))
                return;

            _trackedGrids[grid.EntityId] = grid;
            grid.OnBlockAdded += BlockAdded;
            grid.OnMarkForClose += GridMarkedForClose;

            // Previews de projetor e da area de transferencia nao tem fisica e sao recriados o tempo todo.
            if (grid.Physics != null)
                RemapHelper.RemapGrid(grid, GetRemap(grid));
        }

        void UntrackGrid(IMyCubeGrid grid)
        {
            if (grid == null)
                return;

            grid.OnBlockAdded -= BlockAdded;
            grid.OnMarkForClose -= GridMarkedForClose;
        }

        void GridMarkedForClose(IMyEntity entity)
        {
            try
            {
                var grid = entity as IMyCubeGrid;
                if (grid != null)
                    UntrackGrid(grid);

                _trackedGrids.Remove(entity.EntityId);
                _gridRemaps.Remove(entity.EntityId);
                _dirtyGridDueFrame.Remove(entity.EntityId);
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, _session);
            }
        }

        void BlockAdded(IMySlimBlock block)
        {
            try
            {
                var terminalBlock = block?.FatBlock as IMyTerminalBlock;
                if (terminalBlock == null)
                    return;

                var grid = terminalBlock.CubeGrid;
                if (grid == null || grid.MarkedForClose || grid.Physics == null)
                    return;

                RemapHelper.TryRegisterRemappedBlock(terminalBlock, GetRemap(grid));

                if (!_dirtyGridDueFrame.ContainsKey(grid.EntityId))
                    _dirtyGridDueFrame[grid.EntityId] = MyAPIGateway.Session.GameplayFrameCounter + REMAP_FLUSH_DELAY_FRAMES;
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, _session);
            }
        }

        Dictionary<long, long> GetRemap(IMyCubeGrid grid)
        {
            Dictionary<long, long> remap;
            if (!_gridRemaps.TryGetValue(grid.EntityId, out remap))
            {
                remap = new Dictionary<long, long>();
                _gridRemaps[grid.EntityId] = remap;
            }

            return remap;
        }
    }
}
