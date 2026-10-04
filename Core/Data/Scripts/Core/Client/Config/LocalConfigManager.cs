using System;
using Sandbox.ModAPI;
using Constants = LcdMod.Common.Helpers.Constants;

namespace LcdMod.Client.Config
{
    /// <summary>
    /// Configuração local do jogador (arquivo XML no storage local do mod), independente do mundo.
    /// </summary>
    public static class LocalConfigManager
    {
        public static LcdModLocalConfig Config { get; private set; } = new LcdModLocalConfig();

        public static void Load()
        {
            try
            {
                if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(Constants.CONFIG_FILE, typeof(LocalConfigManager)))
                {
                    Config = new LcdModLocalConfig();
                    return;
                }

                using (var reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(Constants.CONFIG_FILE, typeof(LocalConfigManager)))
                    Config = DeserializeConfig(reader.ReadToEnd());
            }
            catch (Exception e)
            {
                LcdMod.Common.Helpers.LogHelper.Log(VRage.Utils.MyLogSeverity.Warning, "Local config unreadable, using defaults: " + e.Message);
                Config = new LcdModLocalConfig();
            }
        }

        public static void Save()
        {
            using (var writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(Constants.CONFIG_FILE, typeof(LocalConfigManager)))
                writer.Write(MyAPIGateway.Utilities.SerializeToXML(Config ?? new LcdModLocalConfig()));
        }

        internal static LcdModLocalConfig EnsureConfig()
        {
            if (Config == null)
                Config = new LcdModLocalConfig();

            return Config;
        }

        static LcdModLocalConfig DeserializeConfig(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
                return new LcdModLocalConfig();

            return MyAPIGateway.Utilities.SerializeFromXML<LcdModLocalConfig>(xml) ?? new LcdModLocalConfig();
        }
    }
}
