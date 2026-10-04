using System.Collections.Generic;
using Sandbox.ModAPI;

namespace LcdMod.Client.Terminal.Controls
{
    public interface IReferenceBlockSelection
    {
        bool IsReferenceBlockCandidate(IMyTerminalBlock block);
        bool TryGetReferenceBlockCandidates(List<IMyTerminalBlock> blocks);
    }
}
