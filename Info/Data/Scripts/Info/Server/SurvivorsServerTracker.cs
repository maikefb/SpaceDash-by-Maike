using System;
using System.Collections.Generic;
using LcdMod.Client.Modules.Survivors;
using LcdMod.Common.Helpers;
using LcdMod.Common.Survivors;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace LcdMod.Server
{
    /// <summary>
    /// Lado servidor: marca spawn e morte de cada jogador no tempo do mundo, guarda o recorde de vida no
    /// storage do mundo e manda a lista aos clientes quando algo muda.
    /// </summary>
    public sealed class SurvivorsServerTracker
    {
        const string FILE = "LcdMod.survivors.xml";

        readonly object _owner;
        readonly Dictionary<long, SurvivorEntry> _entries = new Dictionary<long, SurvivorEntry>();
        readonly List<IMyIdentity> _identities = new List<IMyIdentity>();

        public SurvivorsServerTracker(object owner)
        {
            _owner = owner;
        }

        public void Load()
        {
            MyVisualScriptLogicProvider.PlayerSpawned += PlayerSpawned;
            MyVisualScriptLogicProvider.PlayerDied += PlayerDied;
            MyVisualScriptLogicProvider.PlayerConnected += PlayerConnected;
        }

        public void Unload()
        {
            MyVisualScriptLogicProvider.PlayerSpawned -= PlayerSpawned;
            MyVisualScriptLogicProvider.PlayerDied -= PlayerDied;
            MyVisualScriptLogicProvider.PlayerConnected -= PlayerConnected;
            _entries.Clear();
            _lastSendFrame.Clear();
        }

        public void BeforeStart()
        {
            try
            {
                Read();
                Reconcile();
                Publish();
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, _owner);
            }
        }

        public void Save()
        {
            try
            {
                using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(FILE, typeof(SurvivorsServerTracker)))
                    writer.Write(MyAPIGateway.Utilities.SerializeToXML(Snapshot()));
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, _owner);
            }
        }

        public List<SurvivorEntry> Snapshot()
        {
            var list = new List<SurvivorEntry>(_entries.Count);
            foreach (var entry in _entries.Values)
                list.Add(entry.Clone());
            return list;
        }

        const long SEND_COOLDOWN_FRAMES = 600;
        readonly Dictionary<ulong, long> _lastSendFrame = new Dictionary<ulong, long>();

        /// <summary>Lista completa a quem pediu, no máximo uma vez a cada 10 s por jogador (o cliente tenta de novo a cada 15 s).</summary>
        public void SendTo(ulong steamId)
        {
            long frame = MyAPIGateway.Session.GameplayFrameCounter;
            long last;
            if (_lastSendFrame.TryGetValue(steamId, out last) && frame - last < SEND_COOLDOWN_FRAMES)
                return;
            _lastSendFrame[steamId] = frame;

            var manager = LcdModSessionComponent.NetworkManager;
            if (manager != null)
                manager.TransmitToPlayer(new NetworkPackageSurvivors { Entries = Snapshot() }, steamId);
        }

        void Read()
        {
            if (!MyAPIGateway.Utilities.FileExistsInWorldStorage(FILE, typeof(SurvivorsServerTracker)))
                return;

            string xml;
            using (var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(FILE, typeof(SurvivorsServerTracker)))
                xml = reader.ReadToEnd();
            if (string.IsNullOrWhiteSpace(xml))
                return;

            var stored = MyAPIGateway.Utilities.SerializeFromXML<List<SurvivorEntry>>(xml);
            if (stored == null)
                return;

            foreach (var entry in stored)
            {
                if (entry != null && IsRealPlayer(entry.IdentityId))
                    _entries[entry.IdentityId] = entry;
            }
        }

        /// <summary>Jogadores vivos que o mod ainda não viu nascer, como em mundo anterior ao mod, entram contando a partir de agora.</summary>
        bool Reconcile()
        {
            bool changed = false;
            long now = WorldClock.ElapsedTicks();
            _identities.Clear();
            MyAPIGateway.Players.GetAllIdentites(_identities, null);
            foreach (var identity in _identities)
            {
                if (identity == null || !IsRealPlayer(identity.IdentityId))
                    continue;

                SurvivorEntry entry;
                if (!_entries.TryGetValue(identity.IdentityId, out entry))
                {
                    if (identity.IsDead)
                        continue;

                    entry = new SurvivorEntry { IdentityId = identity.IdentityId, Alive = true, AliveSinceTicks = now };
                    _entries[identity.IdentityId] = entry;
                    changed = true;
                }

                if (!string.IsNullOrEmpty(identity.DisplayName) && identity.DisplayName != entry.Name)
                {
                    entry.Name = identity.DisplayName;
                    changed = true;
                }
            }

            _identities.Clear();
            return changed;
        }

        /// <summary>Identidade do slot humano (serialId 0) de um Steam ID. Bots do AiEnabled e similares usam serialId maior que zero.</summary>
        bool IsRealPlayer(long identityId)
        {
            if (identityId == 0L)
                return false;

            ulong steamId = MyAPIGateway.Players.TryGetSteamId(identityId);
            return steamId != 0UL && MyAPIGateway.Players.TryGetIdentityId(steamId) == identityId;
        }

        string ResolveName(long identityId)
        {
            _identities.Clear();
            MyAPIGateway.Players.GetAllIdentites(_identities, identity => identity != null && identity.IdentityId == identityId);
            string name = _identities.Count > 0 ? _identities[0].DisplayName : null;
            _identities.Clear();
            return name;
        }

        void PlayerSpawned(long identityId)
        {
            try
            {
                if (!IsRealPlayer(identityId))
                    return;

                SurvivorEntry entry;
                if (!_entries.TryGetValue(identityId, out entry))
                {
                    entry = new SurvivorEntry { IdentityId = identityId };
                    _entries[identityId] = entry;
                }

                if (!entry.Alive)
                {
                    entry.Alive = true;
                    entry.AliveSinceTicks = WorldClock.ElapsedTicks();
                }

                entry.Name = ResolveName(identityId) ?? entry.Name;
                Publish();
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, _owner);
            }
        }

        void PlayerDied(long identityId)
        {
            try
            {
                SurvivorEntry entry;
                if (!_entries.TryGetValue(identityId, out entry) || !entry.Alive)
                    return;

                long life = Math.Max(0L, WorldClock.ElapsedTicks() - entry.AliveSinceTicks);
                if (life > entry.RecordTicks)
                    entry.RecordTicks = life;

                entry.Alive = false;
                entry.AliveSinceTicks = 0L;
                Publish();
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, _owner);
            }
        }

        void PlayerConnected(long identityId)
        {
            try
            {
                if (Reconcile())
                    Publish();
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, _owner);
            }
        }

        void Publish()
        {
            var entries = Snapshot();
            var manager = LcdModSessionComponent.NetworkManager;
            if (manager != null)
                manager.TransmitToAllPlayers(new NetworkPackageSurvivors { Entries = entries }, 0UL);
            if (!MyAPIGateway.Utilities.IsDedicated)
                SurvivorsClient.Apply(entries);
        }
    }
}
