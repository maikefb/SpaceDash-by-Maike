using System;
using ProtoBuf;

namespace LcdMod.Common.Survivors
{
    [ProtoContract]
    public sealed class SurvivorEntry
    {
        [ProtoMember(1)] public long IdentityId { get; set; }
        [ProtoMember(2)] public string Name { get; set; }
        [ProtoMember(3)] public bool Alive { get; set; }
        [ProtoMember(4)] public long AliveSinceTicks { get; set; }
        [ProtoMember(5)] public long RecordTicks { get; set; }

        public long CurrentLifeTicks(long nowTicks)
        {
            return Alive ? Math.Max(0L, nowTicks - AliveSinceTicks) : 0L;
        }

        public long BestTicks(long nowTicks)
        {
            return Math.Max(RecordTicks, CurrentLifeTicks(nowTicks));
        }

        public SurvivorEntry Clone()
        {
            return new SurvivorEntry
            {
                IdentityId = IdentityId,
                Name = Name,
                Alive = Alive,
                AliveSinceTicks = AliveSinceTicks,
                RecordTicks = RecordTicks
            };
        }
    }
}
