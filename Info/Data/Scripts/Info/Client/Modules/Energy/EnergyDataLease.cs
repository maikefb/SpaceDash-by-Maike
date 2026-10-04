using System;

namespace LcdMod.Client.Modules.Energy
{
    public sealed class EnergyDataLease : IDisposable
    {
        internal EnergyDataLease(EnergyDataService service)
        {
            Service = service;
        }

        public EnergyDataService Service { get; private set; }
        public EnergySnapshot Latest => Service?.Latest ?? EnergySnapshot.Empty;

        public void Dispose()
        {
            if (Service == null)
                return;

            Service.Release();
            Service = null;
        }
    }
}
