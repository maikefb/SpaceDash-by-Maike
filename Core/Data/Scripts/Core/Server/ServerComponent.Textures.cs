using System;
using System.Collections.Generic;
using LcdMod.Common.Helpers;
using LcdMod.Common.Networking;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using static LcdMod.Common.Helpers.Constants;

namespace LcdMod.Server
{
    /// <summary>
    /// O servidor guarda cada imagem em "chave.dds" no próprio storage e um índice ordenado das chaves no world
    /// storage. Quem pede recebe do servidor, no máximo uma vez a cada 30 s por remetente e imagem; só se o
    /// servidor não tiver é que o dono (se online) é chamado a enviar. Upload idêntico ao arquivo já guardado é ignorado.
    /// </summary>
    public partial class LcdModServerComponent
    {
        const string TEXTURE_INDEX_FILE = "LcdMod.textures.xml";
        const long TEXTURE_COOLDOWN_FRAMES = 1500;
        const int MAX_UPLOADS_PER_WINDOW = MAX_TEXTURES_PER_OWNER;
        const long UPLOAD_WINDOW_FRAMES = 1800;
        const long TEXTURE_CLEANUP_FRAMES = 600;

        readonly List<string> _textureIndex = new List<string>();
        readonly Dictionary<string, long> _textureCooldowns = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<ulong, string> _ownerNames = new Dictionary<ulong, string>();
        readonly Dictionary<ulong, long> _uploadWindowStart = new Dictionary<ulong, long>();
        readonly Dictionary<ulong, int> _uploadWindowCount = new Dictionary<ulong, int>();
        readonly List<IMyPlayer> _texturePlayers = new List<IMyPlayer>();
        readonly List<string> _expiredCooldowns = new List<string>();
        long _nextTextureCleanupFrame;

        void LoadTextureIndex()
        {
            _textureIndex.Clear();
            try
            {
                if (!MyAPIGateway.Utilities.FileExistsInWorldStorage(TEXTURE_INDEX_FILE, typeof(LcdModServerComponent)))
                    return;

                string xml;
                using (var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(TEXTURE_INDEX_FILE, typeof(LcdModServerComponent)))
                    xml = reader.ReadToEnd();
                var keys = string.IsNullOrWhiteSpace(xml) ? null : MyAPIGateway.Utilities.SerializeFromXML<List<string>>(xml);
                if (keys == null)
                    return;

                foreach (var key in keys)
                    if (!string.IsNullOrWhiteSpace(key) && !ContainsKey(key))
                        _textureIndex.Add(key);
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
            }
        }

        void SaveTextureIndex()
        {
            try
            {
                using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(TEXTURE_INDEX_FILE, typeof(LcdModServerComponent)))
                    writer.Write(MyAPIGateway.Utilities.SerializeToXML(_textureIndex));
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
            }
        }

        void ClearTextureRequests()
        {
            _textureCooldowns.Clear();
            _ownerNames.Clear();
            _textureIndex.Clear();
            _uploadWindowStart.Clear();
            _uploadWindowCount.Clear();
        }

        /// <summary>No máximo 6 uploads por jogador a cada 30 s: cobre o uso normal e corta o reenvio em loop.</summary>
        bool TryConsumeUploadSlot(ulong senderSteamId)
        {
            long frame = MyAPIGateway.Session.GameplayFrameCounter;
            long start;
            if (!_uploadWindowStart.TryGetValue(senderSteamId, out start) || frame - start >= UPLOAD_WINDOW_FRAMES)
            {
                _uploadWindowStart[senderSteamId] = frame;
                _uploadWindowCount[senderSteamId] = 0;
            }

            int count = _uploadWindowCount[senderSteamId];
            if (count >= MAX_UPLOADS_PER_WINDOW)
                return false;

            _uploadWindowCount[senderSteamId] = count + 1;
            return true;
        }

        /// <summary>Chave servida ou gravada vai para o fim: a cota substitui a menos usada, não a mais antiga.</summary>
        void TouchKey(string key)
        {
            for (int i = 0; i < _textureIndex.Count; i++)
            {
                if (!string.Equals(_textureIndex[i], key, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (i == _textureIndex.Count - 1)
                    return;
                _textureIndex.RemoveAt(i);
                _textureIndex.Add(key);
                return;
            }
        }

        bool ContainsKey(string key)
        {
            foreach (var existing in _textureIndex)
                if (string.Equals(existing, key, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        /// <summary>Um envio por remetente e imagem a cada 30 s, seja resposta do disco ou repasse ao dono.</summary>
        bool TryStartCooldown(string cooldownKey)
        {
            long frame = MyAPIGateway.Session.GameplayFrameCounter;
            long last;
            if (_textureCooldowns.TryGetValue(cooldownKey, out last) && frame - last < TEXTURE_COOLDOWN_FRAMES)
                return false;

            _textureCooldowns[cooldownKey] = frame;
            return true;
        }

        void CleanupTextureCooldowns()
        {
            long frame = MyAPIGateway.Session.GameplayFrameCounter;
            if (frame < _nextTextureCleanupFrame)
                return;

            _nextTextureCleanupFrame = frame + TEXTURE_CLEANUP_FRAMES;
            _expiredCooldowns.Clear();
            foreach (var entry in _textureCooldowns)
                if (frame - entry.Value >= TEXTURE_COOLDOWN_FRAMES)
                    _expiredCooldowns.Add(entry.Key);
            foreach (var key in _expiredCooldowns)
                _textureCooldowns.Remove(key);
            _expiredCooldowns.Clear();
        }

        internal void HandleRequestTexture(NetworkPackageRequestTexture packet, ulong senderSteamId)
        {
            if (packet == null || senderSteamId == 0UL || packet.OwnerSteamId == 0UL)
                return;

            string name = TextureFileHelper.NormalizeTextureName(packet.TextureName);
            string key = TextureFileHelper.BuildTextureKey(packet.OwnerSteamId, name);
            if (string.IsNullOrEmpty(key) || !TryStartCooldown(senderSteamId + "|" + key))
                return;

            byte[] cached;
            if (TextureFileHelper.TryReadFile(key + ".dds", out cached) && TextureFileHelper.IsValidTexturePayload(cached))
            {
                TouchKey(key);
                SendTexture(new NetworkPackageSyncTexture(packet.OwnerSteamId, name, cached, ResolveOwnerName(packet.OwnerSteamId)), senderSteamId);
                return;
            }

            if (!TryStartCooldown("forward|" + key))
                return;

            var owner = FindPlayer(packet.OwnerSteamId);
            if (owner == null)
            {
                LogHelper.LogOnce("texture-missing-" + packet.OwnerSteamId, "Texture " + key + " requested but the server does not have it and the owner is offline");
                return;
            }

            var forward = new NetworkPackageRequestTexture(packet.OwnerSteamId, name);
            ulong localId = MyAPIGateway.Session?.Player?.SteamUserId ?? 0UL;
            if (localId != 0UL && owner.SteamUserId == localId && LcdModSessionComponent.Client != null)
                LcdModSessionComponent.Client.HandleRequestTexture(forward);
            else
                LcdModSessionComponent.NetworkManager.TransmitToPlayer(forward, owner.SteamUserId);
        }

        /// <summary>
        /// Só o dono manda a própria imagem, dentro do teto. Conteúdo igual ao já guardado é ignorado. Com a cota
        /// cheia, a imagem mais antiga do dono é substituída. Imagem nova ou alterada vai para todos os jogadores.
        /// </summary>
        internal void HandleSyncTexture(NetworkPackageSyncTexture packet, ulong senderSteamId)
        {
            if (packet == null || senderSteamId == 0UL || packet.OwnerSteamId != senderSteamId)
                return;

            string name = TextureFileHelper.NormalizeTextureName(packet.TextureName);
            string key = TextureFileHelper.BuildTextureKey(packet.OwnerSteamId, name);
            if (string.IsNullOrEmpty(key) || !TextureFileHelper.IsValidTexturePayload(packet.Data))
                return;

            if (!TryConsumeUploadSlot(senderSteamId))
                return;

            byte[] existing;
            if (TextureFileHelper.FileLength(key + ".dds") == packet.Data.Length
                && TextureFileHelper.TryReadFile(key + ".dds", out existing) && SameBytes(existing, packet.Data))
            {
                _textureCooldowns.Remove("forward|" + key);
                IndexKey(key, packet.OwnerSteamId);
                return;
            }

            if (!TextureFileHelper.TryWriteFile(key + ".dds", packet.Data))
                return;

            IndexKey(key, packet.OwnerSteamId);
            _textureCooldowns.Remove("forward|" + key);

            var broadcast = new NetworkPackageSyncTexture(packet.OwnerSteamId, name, packet.Data, ResolveOwnerName(packet.OwnerSteamId));
            LcdModSessionComponent.NetworkManager.TransmitToAllPlayers(broadcast, packet.OwnerSteamId);

            ulong localId = MyAPIGateway.Session?.Player?.SteamUserId ?? 0UL;
            if (localId != 0UL && localId != packet.OwnerSteamId && LcdModSessionComponent.Client != null)
                LcdModSessionComponent.Client.HandleSyncTexture(broadcast);
        }

        /// <summary>Chave usada vai para o fim (menos usada recentemente sai primeiro); chave nova respeita o teto do dono.</summary>
        void IndexKey(string key, ulong ownerSteamId)
        {
            if (ContainsKey(key))
            {
                TouchKey(key);
            }
            else
            {
                while (CountTextures(ownerSteamId) >= MAX_TEXTURES_PER_OWNER)
                    RemoveOldestTexture(ownerSteamId);
                _textureIndex.Add(key);
            }
            SaveTextureIndex();
        }

        static bool SameBytes(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i])
                    return false;
            return true;
        }

        int CountTextures(ulong ownerSteamId)
        {
            string prefix = ownerSteamId + "-";
            int count = 0;
            foreach (var key in _textureIndex)
                if (key.StartsWith(prefix, StringComparison.Ordinal))
                    count++;
            return count;
        }

        void RemoveOldestTexture(ulong ownerSteamId)
        {
            string prefix = ownerSteamId + "-";
            for (int i = 0; i < _textureIndex.Count; i++)
            {
                if (!_textureIndex[i].StartsWith(prefix, StringComparison.Ordinal))
                    continue;

                string key = _textureIndex[i];
                _textureIndex.RemoveAt(i);
                try
                {
                    if (MyAPIGateway.Utilities.FileExistsInLocalStorage(key + ".dds", typeof(TextureFileHelper)))
                        MyAPIGateway.Utilities.DeleteFileInLocalStorage(key + ".dds", typeof(TextureFileHelper));
                }
                catch (Exception e)
                {
                    ErrorHandlerHelper.LogError(e, this);
                }
                // No host o cliente usa o mesmo arquivo: esquece a chave para pedir de novo quando precisar.
                // ponytail: um dono com mais de 32 imagens em uso no host entra num ciclo despejo/pedido limitado
                // pela cota e pelo cooldown; subir MAX_TEXTURES_PER_OWNER se isso acontecer na prática.
                if (LcdModSessionComponent.Client != null)
                    LcdMod.Client.Helpers.TextureHelper.ForgetTexture(key);
                return;
            }
        }

        static void SendTexture(NetworkPackageSyncTexture packet, ulong requester)
        {
            ulong localId = MyAPIGateway.Session?.Player?.SteamUserId ?? 0UL;
            if (localId != 0UL && requester == localId && LcdModSessionComponent.Client != null)
                LcdModSessionComponent.Client.HandleSyncTexture(packet);
            else
                LcdModSessionComponent.NetworkManager.TransmitToPlayer(packet, requester);
        }

        string ResolveOwnerName(ulong steamId)
        {
            var player = FindPlayer(steamId);
            if (player != null)
            {
                _ownerNames[steamId] = player.DisplayName;
                return player.DisplayName;
            }

            string cached;
            if (_ownerNames.TryGetValue(steamId, out cached))
                return cached;

            long identityId = MyAPIGateway.Players.TryGetIdentityId(steamId);
            var identities = new List<IMyIdentity>();
            MyAPIGateway.Players.GetAllIdentites(identities, identity => identity != null && identity.IdentityId == identityId);
            cached = identities.Count > 0 ? identities[0].DisplayName : null;
            _ownerNames[steamId] = cached;
            return cached;
        }

        IMyPlayer FindPlayer(ulong steamId)
        {
            if (steamId == 0UL)
                return null;

            _texturePlayers.Clear();
            MyAPIGateway.Players.GetPlayers(_texturePlayers, p => p != null && !p.IsBot && p.SteamUserId == steamId);
            var player = _texturePlayers.Count > 0 ? _texturePlayers[0] : null;
            _texturePlayers.Clear();
            return player;
        }
    }
}
