using System;
using System.Collections.Generic;
using System.Linq;
using LcdMod.Client.Config;
using LcdMod.Common.Helpers;
using LcdMod.Common.Networking;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.ObjectBuilders;
using VRageMath;
using static LcdMod.Common.Helpers.Constants;

namespace LcdMod.Client.Helpers
{
    /// <summary>
    /// Imagens do PC para a Moldura digital. Um DDS no storage local vira uma MyLCDTextureDefinition com caminho
    /// absoluto. O servidor é o repositório: o dono envia a imagem quando a usa num LCD, o servidor distribui a
    /// todos e responde a quem pedir depois, com o dono online ou não.
    /// </summary>
    public static partial class TextureHelper
    {
        const long REQUEST_RETRY_FRAMES = 1800;

        static readonly Dictionary<string, Vector2I> CustomTextures = new Dictionary<string, Vector2I>(StringComparer.OrdinalIgnoreCase);
        static readonly HashSet<string> LocalTextures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        static readonly Dictionary<string, long> PendingRequests = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        static readonly HashSet<string> FailedParse = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        static readonly HashSet<string> UploadedThisSession = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        static ulong LocalSteamId => MyAPIGateway.Session?.Player?.SteamUserId ?? 0UL;
        static long Frame => MyAPIGateway.Session != null ? MyAPIGateway.Session.GameplayFrameCounter : 0L;

        public static bool IsKnownTexture(string name) => !string.IsNullOrWhiteSpace(name) && CustomTextures.ContainsKey(name);
        public static bool IsLocalTexture(string key) => !string.IsNullOrWhiteSpace(key) && LocalTextures.Contains(key);
        public static bool HasTextureParseFailed(string name) => FailedParse.Contains(name);
        public static void MarkTextureParseFailed(string name) { if (!string.IsNullOrWhiteSpace(name)) FailedParse.Add(name); }

        public static bool HasPendingTextureRequest(string key)
        {
            long requestedAt;
            return PendingRequests.TryGetValue(key, out requestedAt) && Frame - requestedAt < REQUEST_RETRY_FRAMES;
        }

        public static void ForgetTexture(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || IsLocalTexture(key))
                return;
            CustomTextures.Remove(key);
            PendingRequests.Remove(key);
        }

        public static bool TryGetTextureSize(string name, out Vector2I size)
        {
            return CustomTextures.TryGetValue(name ?? string.Empty, out size) && size.X > 0 && size.Y > 0;
        }

        public static void GetRegisteredSpriteNames(List<string> target)
        {
            foreach (var name in LocalTextures)
                target.Add(name);
        }

        static void ClearCustomState()
        {
            CustomTextures.Clear();
            LocalTextures.Clear();
            PendingRequests.Clear();
            FailedParse.Clear();
            UploadedThisSession.Clear();
        }

        /// <summary>Registra "nome.dds" do storage local como textura do jogador local.</summary>
        public static bool RegisterLocalTexture(string textureName, bool verbose)
        {
            textureName = TextureFileHelper.NormalizeTextureName(textureName);
            ulong steamId = LocalSteamId;
            if (string.IsNullOrEmpty(textureName) || steamId == 0UL)
            {
                if (verbose)
                    MyAPIGateway.Utilities.ShowNotification("LCD: nome de imagem inválido (use letras, números, espaço, ponto, hífen ou sublinhado, até 64 caracteres)", 5000, "Red");
                return false;
            }

            string key = TextureFileHelper.BuildTextureKey(steamId, textureName);
            if (!Register(key, textureName + ".dds", textureName, verbose))
                return false;

            LocalTextures.Add(key);
            UploadedThisSession.Remove(key);
            var config = LocalConfigManager.EnsureConfig();
            if (config.LocalTextures == null)
                config.LocalTextures = new List<string>();
            if (!config.LocalTextures.Contains(textureName, StringComparer.OrdinalIgnoreCase))
            {
                config.LocalTextures.Add(textureName);
                LocalConfigManager.Save();
            }
            return true;
        }

        static bool Register(string key, string fileName, string displayName, bool verbose)
        {
            byte[] bytes;
            int width, height;
            if (!TextureFileHelper.TryReadFile(fileName, out bytes) || !TextureFileHelper.TryGetDdsDimensions(bytes, out width, out height))
            {
                if (verbose)
                    MyAPIGateway.Utilities.ShowNotification("LCD: " + fileName + " não é um DDS válido no storage do mod", 5000, "Red");
                return false;
            }

            if (!TextureFileHelper.IsValidTexturePayload(bytes))
            {
                if (verbose)
                    MyAPIGateway.Utilities.ShowNotification("LCD: " + fileName + " precisa ter largura e altura múltiplas de 4, até 2048x2048 e até 5 MB", 5000, "Red");
                return false;
            }

            string path = TextureFileHelper.StoragePath(fileName);
            MyDefinitionManager.Static.Definitions.AddOrReplaceDefinition(new MyLCDTextureDefinition
            {
                Id = new MyDefinitionId((MyObjectBuilderType)typeof(MyObjectBuilder_LCDTextureDefinition), key),
                Public = true,
                LocalizationId = displayName,
                SpritePath = path,
                TexturePath = path,
                Selectable = true,
                AvailableInSurvival = true
            });
            CustomTextures[key] = new Vector2I(width, height);
            PendingRequests.Remove(key);
            LogHelper.LogInfo("Registered texture " + key + " at " + path);
            if (verbose)
                MyAPIGateway.Utilities.ShowNotification("LCD: imagem " + displayName + " registrada (" + width + "x" + height + ")", 5000);
            return true;
        }

        public static void LoadLocalTextures()
        {
            var config = LocalConfigManager.Config;
            if (config == null || config.LocalTextures == null)
                return;

            if (LocalSteamId == 0UL)
            {
                LcdModClientComponent.RunNextFrame.Add(LoadLocalTextures);
                return;
            }

            foreach (var name in config.LocalTextures.ToArray())
                RegisterLocalTexture(name, false);
        }

        /// <summary>Lê import.txt (um nome de arquivo por linha), registra cada DDS e apaga a lista.</summary>
        public static void ImportFromList(bool verbose)
        {
            if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(TEXTURE_IMPORT_FILE, typeof(TextureHelper)))
            {
                if (verbose)
                    MyAPIGateway.Utilities.ShowNotification("LCD: " + TEXTURE_IMPORT_FILE + " não existe no storage do mod", 5000, "Red");
                return;
            }

            if (LocalSteamId == 0UL)
            {
                LcdModClientComponent.RunNextFrame.Add(() => ImportFromList(verbose));
                return;
            }

            try
            {
                var names = new List<string>();
                using (var reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(TEXTURE_IMPORT_FILE, typeof(TextureHelper)))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                        if (!string.IsNullOrWhiteSpace(line))
                            names.Add(line.Trim());
                }

                foreach (var name in names)
                    RegisterLocalTexture(name, true);

                MyAPIGateway.Utilities.DeleteFileInLocalStorage(TEXTURE_IMPORT_FILE, typeof(TextureHelper));
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, typeof(TextureHelper));
            }
        }

        /// <summary>Imagem de outro jogador: tenta o cache local e, se não houver, pede ao servidor (repete a cada 30 s).</summary>
        public static void TryQueueTextureRequest(ulong ownerSteamId, string textureName, string key)
        {
            ulong localId = LocalSteamId;
            if (localId == 0UL || ownerSteamId == 0UL || IsLocalTexture(key) || IsKnownTexture(key))
                return;

            if (TextureFileHelper.FileExists(key + ".dds") && Register(key, key + ".dds", textureName, false))
                return;

            if (HasPendingTextureRequest(key))
                return;

            PendingRequests[key] = Frame;
            LogHelper.LogInfo("Requesting texture " + key + " from server");
            LcdModClientComponent.RunNextFrame.Add(delegate
            {
                var packet = new NetworkPackageRequestTexture(ownerSteamId, textureName);
                var server = LcdModSessionComponent.Server;
                if (server != null)
                    server.HandleRequestTexture(packet, localId);
                else
                    LcdModSessionComponent.NetworkManager.TransmitToServer(packet, false);
            });
        }

        /// <summary>
        /// Manda uma imagem minha ao servidor, que guarda e distribui. Ao usar a imagem num LCD envia uma vez por
        /// sessão; quando o servidor pede (não tem o arquivo), envia de novo. Em single player não há para quem mandar.
        /// </summary>
        public static void UploadLocalTexture(string key, bool force = false)
        {
            ulong localId = LocalSteamId;
            ulong owner;
            string name;
            if (localId == 0UL || !MyAPIGateway.Multiplayer.MultiplayerActive || !IsLocalTexture(key)
                || !TextureFileHelper.TryParseTextureKey(key, out owner, out name) || owner != localId)
                return;
            if (!force && UploadedThisSession.Contains(key))
                return;

            byte[] bytes;
            if (!TextureFileHelper.TryReadFile(name + ".dds", out bytes) || !TextureFileHelper.IsValidTexturePayload(bytes))
            {
                LogHelper.Log(VRage.Utils.MyLogSeverity.Warning, "Texture " + key + " is missing or invalid, not uploaded");
                return;
            }
            UploadedThisSession.Add(key);

            var packet = new NetworkPackageSyncTexture(localId, name, bytes, MyAPIGateway.Session.Player.DisplayName);
            var server = LcdModSessionComponent.Server;
            if (server != null)
                server.HandleSyncTexture(packet, localId);
            else
                LcdModSessionComponent.NetworkManager.TransmitToServer(packet, false);
        }

        public static void SaveRemoteTexture(NetworkPackageSyncTexture packet)
        {
            if (packet == null || !TextureFileHelper.IsValidTexturePayload(packet.Data))
                return;

            string key = TextureFileHelper.BuildTextureKey(packet.OwnerSteamId, packet.TextureName);
            if (string.IsNullOrEmpty(key) || IsLocalTexture(key))
                return;

            // No host o servidor já gravou o mesmo arquivo nesta pasta; só registra.
            bool alreadyOnDisk = LcdModSessionComponent.Server != null && TextureFileHelper.FileExists(key + ".dds");
            if (!alreadyOnDisk && !TextureFileHelper.TryWriteFile(key + ".dds", packet.Data))
                return;

            string displayName = string.IsNullOrWhiteSpace(packet.OwnerName)
                ? TextureFileHelper.NormalizeTextureName(packet.TextureName)
                : packet.OwnerName + " - " + TextureFileHelper.NormalizeTextureName(packet.TextureName);
            Register(key, key + ".dds", displayName, false);
        }

        /// <summary>Grava no storage o .bat que converte PNG em DDS com o texconv do jogo e preenche import.txt.</summary>
        public static void ExportConverter()
        {
            try
            {
                if (!MyAPIGateway.Utilities.FileExistsInLocalStorage("tools_path.txt", typeof(TextureHelper)))
                {
                    using (var file = MyAPIGateway.Utilities.WriteFileInLocalStorage("tools_path.txt", typeof(TextureHelper)))
                        file.WriteLine(MyAPIGateway.Utilities.GamePaths.ContentPath.Replace("Content", "Tools\\TexturePacking\\Tools"));
                }

                if (!MyAPIGateway.Utilities.FileExistsInLocalStorage("png-to-dds.bat", typeof(TextureHelper)))
                {
                    using (var file = MyAPIGateway.Utilities.WriteFileInLocalStorage("png-to-dds.bat", typeof(TextureHelper)))
                        file.Write(PNG_TO_DDS_BAT);
                }

                if (!MyAPIGateway.Utilities.FileExistsInLocalStorage("leia-me-imagens.txt", typeof(TextureHelper)))
                {
                    using (var file = MyAPIGateway.Utilities.WriteFileInLocalStorage("leia-me-imagens.txt", typeof(TextureHelper)))
                        file.Write(README);
                }
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, typeof(TextureHelper));
            }
        }

        const string PNG_TO_DDS_BAT = @"@echo off
setlocal enabledelayedexpansion
set ""PATHVAR=""
for /f ""usebackq delims="" %%A in (""tools_path.txt"") do (
    if not defined PATHVAR set ""PATHVAR=%%~A""
)
if not defined PATHVAR (
    echo tools_path.txt vazio ou ausente.
    exit /b 1
)
if not ""%PATHVAR:~-1%""==""\"" set ""PATHVAR=%PATHVAR%\""
""%PATHVAR%texconv.exe"" .\*.png -nologo -y -f BC7_UNORM -pmalpha
if not ""%ERRORLEVEL%""==""0"" (
    echo texconv.exe falhou com codigo %ERRORLEVEL%.
    exit /b %ERRORLEVEL%
)
> import.txt (
    for %%F in (*.DDS) do (
        if exist ""%%F"" (
            ren ""%%F"" ""%%~nF.__rename_tmp__""
            ren ""%%~nF.__rename_tmp__"" ""%%~nF.dds""
            echo %%~nF.dds
        )
    )
)
exit /b 0
";

        const string README = @"Imagens do PC na Moldura digital (LCD Info)

1. Copie seus PNG para esta pasta (largura e altura multiplas de 4, no maximo 2048x2048 e 5 MB em DDS).
   Nome do arquivo: letras, numeros, espaco, ponto, hifen ou sublinhado, ate 64 caracteres.
2. Rode png-to-dds.bat: ele converte com o texconv do jogo e escreve import.txt.
   Se ja tiver DDS prontos, escreva um nome por linha em import.txt.
3. Abra o jogo (ou digite /lcd import no chat). As imagens aparecem na lista da Moldura digital.
   Para importar um unico arquivo: /lcd import nome.dds

Ao colocar a imagem num LCD, ela e enviada ao servidor, que a guarda e distribui a todos os jogadores.
Quem entrar depois recebe do servidor, mesmo com voce offline. Limite de 32 imagens por jogador no servidor.
";
    }
}
