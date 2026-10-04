using System;

namespace LcdMod.Common.Exceptions
{
    /// <summary>Substituta local: System.ArgumentOutOfRangeException está fora da whitelist do ModAPI.</summary>
    public sealed class ArgumentOutOfRangeException : Exception
    {
        public ArgumentOutOfRangeException(string paramName)
            : base("Argument out of range: " + paramName)
        {
        }
    }
}
