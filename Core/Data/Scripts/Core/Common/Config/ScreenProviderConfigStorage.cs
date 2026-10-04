using System;
using LcdMod.Common.Config.Models;
using LcdMod.Common.Helpers;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace LcdMod.Common.Config
{
    public static class ScreenProviderConfigStorage
    {
        public static bool Save(IMyEntity storageEntity, ScreenProviderConfig providerConfig)
        {
            try
            {
                if (providerConfig == null || storageEntity == null)
                {
                    LogHelper.Log(MyLogSeverity.Warning, "Save call with invalid block");
                    return false;
                }

                if (providerConfig.IsRecoveryPlaceholder)
                    return false;

                if (!providerConfig.NormalizeComponentSchema())
                {
                    MyLog.Default.WriteLine(
                        $"[LcdMod] Refusing to overwrite unsupported component schema {providerConfig.SchemaVersion}; current schema is {ScreenProviderConfig.COMPONENT_SCHEMA_VERSION}.");
                    return false;
                }

                if (storageEntity.Storage == null)
                    storageEntity.Storage = new MyModStorageComponent();

                var base64 = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(providerConfig));
                if (string.IsNullOrEmpty(base64))
                    throw new Exception("Invalid component config");

                storageEntity.Storage[Constants.StorageGuid] = base64;
                return true;
            }
            catch (Exception exception)
            {
                ErrorHandlerHelper.LogError(exception, typeof(ScreenProviderConfigStorage));
                return false;
            }
        }

        public static ScreenProviderConfig TryLoad(IMyEntity storageEntity)
        {
            bool hasUnreadableValue;
            return TryLoad(storageEntity, out hasUnreadableValue);
        }

        /// <param name="hasUnreadableValue">true quando existe valor salvo mas ele não pôde ser desserializado.</param>
        public static ScreenProviderConfig TryLoad(IMyEntity storageEntity, out bool hasUnreadableValue)
        {
            hasUnreadableValue = false;
            if (storageEntity.Storage == null)
                return null;

            string value;
            if (storageEntity.Storage.TryGetValue(Constants.StorageGuid, out value) && !string.IsNullOrEmpty(value))
            {
                hasUnreadableValue = true;
                try
                {
                    var current = Deserialize<ScreenProviderConfig>(value);
                    if (current == null)
                        return null;

                    hasUnreadableValue = false;

                    var supported = current.NormalizeComponentSchema();
                    if(!supported) MyLog.Default.WriteLine(
                    $"[LcdMod] Loaded newer component config schema {current.SchemaVersion} read-only; current schema is {ScreenProviderConfig.COMPONENT_SCHEMA_VERSION}.");
                    return current;
                }
                catch (Exception exception)
                {
                    // The component key is authoritative once present. Never restore the original
                    // legacy snapshot and erase edits made after migration.
                    ErrorHandlerHelper.LogError(exception, typeof(ScreenProviderConfigStorage));
                    return null;
                }
            }

            return null;
        }

        static T Deserialize<T>(string base64)
        {
            var data = Convert.FromBase64String(base64);
            return MyAPIGateway.Utilities.SerializeFromBinary<T>(data);
        }
    }
}
