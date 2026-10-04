using System.Collections.Generic;
using Generated;
using ProtoBuf;

namespace LcdMod.Common.Survivors
{
    /// <summary>Do cliente para o servidor vai vazio, como pedido. Do servidor para o cliente traz a lista completa.</summary>
    [ProtoContract]
    public sealed class NetworkPackageSurvivors : NetworkPackage
    {
        [ProtoMember(1)] public List<SurvivorEntry> Entries { get; set; }

        public override int Id { get { return 2; } }
    }
}
