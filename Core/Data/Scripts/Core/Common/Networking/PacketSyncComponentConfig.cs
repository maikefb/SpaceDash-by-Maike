using Generated;
using LcdMod.Common.Config.Models;
using ProtoBuf;

namespace LcdMod.Common.Networking
{
    [ProtoContract]
    internal sealed class NetworkPackageSyncComponentConfig : NetworkPackage
    {
        public override int Id => 1;
        [ProtoMember(1)] public long BlockId { get; set; }
        [ProtoMember(2)] public ScreenProviderConfig Config { get; set; }

        // ReSharper disable once UnusedMember.Global
        public NetworkPackageSyncComponentConfig()// Needed for Protobuf
        {
        }

        public NetworkPackageSyncComponentConfig(long blockId, ScreenProviderConfig config)
        {
            BlockId = blockId;
            Config = config;
        }
    }
}
