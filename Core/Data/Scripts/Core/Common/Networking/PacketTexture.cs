using System;
using Generated;
using ProtoBuf;

namespace LcdMod.Common.Networking
{
    /// <summary>Pacote 4: cliente pede ao servidor uma imagem "steamId-nome" que ainda não tem.</summary>
    [ProtoContract]
    public sealed class NetworkPackageRequestTexture : NetworkPackage
    {
        [ProtoMember(1)] public ulong OwnerSteamId { get; set; }
        [ProtoMember(2)] public string TextureName { get; set; }

        public override int Id { get { return 4; } }

        public NetworkPackageRequestTexture()
        {
        }

        public NetworkPackageRequestTexture(ulong ownerSteamId, string textureName)
        {
            OwnerSteamId = ownerSteamId;
            TextureName = textureName;
        }
    }

    /// <summary>
    /// Pacote 5: o dono envia o DDS ao servidor, que guarda e distribui a todos; o servidor também responde
    /// com ele a quem pedir depois, mesmo com o dono offline.
    /// </summary>
    [ProtoContract]
    public sealed class NetworkPackageSyncTexture : NetworkPackage
    {
        [ProtoMember(1)] public ulong OwnerSteamId { get; set; }
        [ProtoMember(2)] public string TextureName { get; set; }
        [ProtoMember(3)] public byte[] Data { get; set; }
        [ProtoMember(4)] public string OwnerName { get; set; }

        public override int Id { get { return 5; } }

        public NetworkPackageSyncTexture()
        {
            Data = Array.Empty<byte>();
        }

        public NetworkPackageSyncTexture(ulong ownerSteamId, string textureName, byte[] data, string ownerName)
        {
            OwnerSteamId = ownerSteamId;
            TextureName = textureName;
            Data = data ?? Array.Empty<byte>();
            OwnerName = ownerName;
        }
    }
}
