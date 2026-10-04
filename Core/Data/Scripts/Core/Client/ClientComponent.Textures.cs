using System;
using LcdMod.Client.Helpers;
using LcdMod.Common.Helpers;
using LcdMod.Common.Networking;
using Sandbox.ModAPI;

namespace LcdMod.Client
{
    public partial class LcdModClientComponent
    {
        const string CHAT_PREFIX = "/lcd";

        void LoadTextures()
        {
            TextureHelper.LoadLocalTextures();
            TextureHelper.ImportFromList(false);
            TextureHelper.ExportConverter();
            MyAPIGateway.Utilities.MessageEnteredSender += OnMessageEntered;
        }

        void UnloadTextures()
        {
            MyAPIGateway.Utilities.MessageEnteredSender -= OnMessageEntered;
        }

        void OnMessageEntered(ulong sender, string text, ref bool sendToOthers)
        {
            if (string.IsNullOrWhiteSpace(text) || !text.StartsWith(CHAT_PREFIX, StringComparison.OrdinalIgnoreCase))
                return;

            var parts = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || !string.Equals(parts[1], "import", StringComparison.OrdinalIgnoreCase))
                return;

            sendToOthers = false;
            try
            {
                if (parts.Length >= 3)
                    TextureHelper.RegisterLocalTexture(parts[2], true);
                else
                    TextureHelper.ImportFromList(true);
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
            }
        }

        /// <summary>O servidor ainda não tem uma imagem minha que alguém pediu: envio agora.</summary>
        internal void HandleRequestTexture(NetworkPackageRequestTexture packet)
        {
            ulong localId = MyAPIGateway.Session?.Player?.SteamUserId ?? 0UL;
            if (packet == null || localId == 0UL || packet.OwnerSteamId != localId)
                return;

            TextureHelper.UploadLocalTexture(TextureFileHelper.BuildTextureKey(localId, packet.TextureName), true);
        }

        internal void HandleSyncTexture(NetworkPackageSyncTexture packet)
        {
            TextureHelper.SaveRemoteTexture(packet);
        }
    }
}
