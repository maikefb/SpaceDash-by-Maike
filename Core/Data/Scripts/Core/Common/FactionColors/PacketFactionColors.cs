using Generated;
using ProtoBuf;
using VRageMath;

namespace LcdMod.Common.FactionColors
{
    /// <summary>Pacote 3: cliente manda a paleta nova da facção; servidor valida, guarda e repassa a todos.</summary>
    [ProtoContract]
    public sealed class NetworkPackageFactionColors : NetworkPackage
    {
        [ProtoMember(1)] public long FactionId { get; set; }
        [ProtoMember(2)] public uint Header { get; set; }
        [ProtoMember(3)] public uint Warning { get; set; }
        [ProtoMember(4)] public uint Error { get; set; }
        [ProtoMember(5)] public uint Foreground { get; set; }
        [ProtoMember(6)] public uint Background { get; set; }

        public override int Id { get { return 3; } }

        public static NetworkPackageFactionColors From(long factionId, FactionPalette palette)
        {
            return new NetworkPackageFactionColors
            {
                FactionId = factionId,
                Header = palette.Header.PackedValue,
                Warning = palette.Warning.PackedValue,
                Error = palette.Error.PackedValue,
                Foreground = palette.Foreground.PackedValue,
                Background = palette.Background.PackedValue
            };
        }

        public FactionPalette ToPalette()
        {
            return new FactionPalette
            {
                Header = new Color { PackedValue = Header },
                Warning = new Color { PackedValue = Warning },
                Error = new Color { PackedValue = Error },
                Foreground = new Color { PackedValue = Foreground },
                Background = new Color { PackedValue = Background }
            };
        }
    }
}
