using System;

namespace LcdMod.Common.Exceptions
{
    /// <summary>Substituta local: System.NotImplementedException está fora da whitelist do ModAPI.</summary>
    public sealed class NotImplementedException : Exception
    {
        public NotImplementedException()
            : base("Not implemented")
        {
        }
    }
}
