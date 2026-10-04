using System;
using System.Collections.Generic;
using LcdMod.Common.Survivors;
using LcdMod.Server;
using Sandbox.ModAPI;

namespace LcdMod.Client.Modules.Survivors
{
    /// <summary>Cópia local da lista do servidor. No host vem direto do rastreador; em clientes remotos chega pelo pacote 2.</summary>
    public static class SurvivorsClient
    {
        const double REQUEST_RETRY_SECONDS = 15d;
        const int MAX_UNANSWERED_REQUESTS = 8;

        static DateTime _lastRequestUtc = DateTime.MinValue;
        static int _unansweredRequests;

        public static readonly List<SurvivorEntry> Entries = new List<SurvivorEntry>();
        public static int Version { get; private set; }
        public static bool HasData { get; private set; }

        public static void EnsureData()
        {
            if (HasData)
                return;

            var session = MyAPIGateway.Session;
            if (session == null)
                return;

            if (session.IsServer)
            {
                var server = LcdModServerComponent.Instance;
                if (server != null && server.Survivors != null)
                    Apply(server.Survivors.Snapshot());
                return;
            }

            var now = DateTime.UtcNow;
            if ((now - _lastRequestUtc).TotalSeconds < REQUEST_RETRY_SECONDS || _unansweredRequests >= MAX_UNANSWERED_REQUESTS)
                return;

            _lastRequestUtc = now;
            _unansweredRequests++;
            var manager = LcdModSessionComponent.NetworkManager;
            if (manager != null)
                manager.TransmitToServer(new NetworkPackageSurvivors(), false);
        }

        public static void Apply(List<SurvivorEntry> entries)
        {
            Entries.Clear();
            if (entries != null)
                Entries.AddRange(entries);
            HasData = true;
            _unansweredRequests = 0;
            Version++;
        }

        public static void Clear()
        {
            Entries.Clear();
            HasData = false;
            _unansweredRequests = 0;
            _lastRequestUtc = DateTime.MinValue;
            Version++;
        }
    }
}
