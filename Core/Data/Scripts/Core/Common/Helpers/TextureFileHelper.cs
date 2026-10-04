using System;
using System.IO;
using Sandbox.ModAPI;
using static LcdMod.Common.Helpers.Constants;

namespace LcdMod.Common.Helpers
{
    /// <summary>
    /// Arquivos DDS no storage local do mod (%AppData%\SpaceEngineers\Storage\&lt;mod&gt;). A chave de uma textura
    /// é "&lt;steamId&gt;-&lt;nome&gt;"; o arquivo do dono é "&lt;nome&gt;.dds" e o cache de outro jogador é "&lt;chave&gt;.dds".
    /// </summary>
    public static class TextureFileHelper
    {
        const uint DDS_HEADER_SIZE = 124;

        /// <summary>Só o nome do arquivo, sem extensão. Devolve vazio se tiver caractere fora de [A-Za-z0-9_ .-] ou passar do limite.</summary>
        public static string NormalizeTextureName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            name = name.Trim();
            int slash = Math.Max(name.LastIndexOf('/'), name.LastIndexOf('\\'));
            if (slash >= 0)
                name = name.Substring(slash + 1);
            if (name.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - 4);
            return IsValidTextureName(name) ? name : string.Empty;
        }

        public static bool IsValidTextureName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length > MAX_TEXTURE_NAME_LENGTH || name.StartsWith("."))
                return false;

            foreach (char c in name)
            {
                bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')
                          || c == '_' || c == '-' || c == '.' || c == ' ';
                if (!ok)
                    return false;
            }
            return true;
        }

        public static string BuildTextureKey(ulong ownerSteamId, string textureName)
        {
            textureName = NormalizeTextureName(textureName);
            return string.IsNullOrEmpty(textureName) ? string.Empty : ownerSteamId + "-" + textureName;
        }

        public static bool TryParseTextureKey(string key, out ulong ownerSteamId, out string textureName)
        {
            ownerSteamId = 0UL;
            textureName = string.Empty;
            if (string.IsNullOrWhiteSpace(key))
                return false;

            int separator = key.IndexOf('-');
            if (separator <= 0 || separator >= key.Length - 1)
                return false;

            if (!ulong.TryParse(key.Substring(0, separator), out ownerSteamId) || ownerSteamId == 0UL)
                return false;

            textureName = key.Substring(separator + 1);
            return true;
        }

        public static bool TryGetDdsDimensions(byte[] bytes, out int width, out int height)
        {
            width = 0;
            height = 0;
            if (bytes == null || bytes.Length < 20)
                return false;
            if (bytes[0] != (byte)'D' || bytes[1] != (byte)'D' || bytes[2] != (byte)'S' || bytes[3] != (byte)' ')
                return false;
            if (BitConverter.ToUInt32(bytes, 4) != DDS_HEADER_SIZE)
                return false;

            height = (int)BitConverter.ToUInt32(bytes, 12);
            width = (int)BitConverter.ToUInt32(bytes, 16);
            return width > 0 && height > 0;
        }

        /// <summary>
        /// Cabeçalho DDS válido, dimensões dentro do teto e múltiplas de 4, e bytes suficientes para pelo menos o
        /// primeiro mip no formato mais compacto (BC1, meio byte por pixel). Não garante que o conteúdo seja válido.
        /// </summary>
        public static bool IsValidTexturePayload(byte[] data)
        {
            int width, height;
            if (data == null || data.Length <= 0 || data.Length > MAX_TEXTURE_BYTES
                || !TryGetDdsDimensions(data, out width, out height))
                return false;

            return width <= MAX_TEXTURE_DIMENSION && height <= MAX_TEXTURE_DIMENSION
                   && width % 4 == 0 && height % 4 == 0
                   && data.Length >= 128 + (width * height) / 2;
        }

        public static bool FileExists(string fileName)
        {
            return !string.IsNullOrWhiteSpace(fileName)
                   && MyAPIGateway.Utilities.FileExistsInLocalStorage(fileName, typeof(TextureFileHelper));
        }

        public static long FileLength(string fileName)
        {
            if (!FileExists(fileName))
                return -1L;

            try
            {
                using (var reader = MyAPIGateway.Utilities.ReadBinaryFileInLocalStorage(fileName, typeof(TextureFileHelper)))
                    return reader.BaseStream.Length;
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, typeof(TextureFileHelper));
                return -1L;
            }
        }

        public static bool TryReadFile(string fileName, out byte[] bytes)
        {
            bytes = null;
            if (!FileExists(fileName))
                return false;

            try
            {
                using (var reader = MyAPIGateway.Utilities.ReadBinaryFileInLocalStorage(fileName, typeof(TextureFileHelper)))
                    bytes = reader.ReadBytes((int)reader.BaseStream.Length);
                return bytes != null && bytes.Length > 0;
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, typeof(TextureFileHelper));
                return false;
            }
        }

        public static bool TryWriteFile(string fileName, byte[] bytes)
        {
            if (string.IsNullOrWhiteSpace(fileName) || bytes == null)
                return false;

            try
            {
                using (var writer = MyAPIGateway.Utilities.WriteBinaryFileInLocalStorage(fileName, typeof(TextureFileHelper)))
                    writer.Write(bytes);
                return true;
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, typeof(TextureFileHelper));
                return false;
            }
        }

        public static string StorageFolder()
        {
            var paths = MyAPIGateway.Utilities.GamePaths;
            return Path.Combine(paths.UserDataPath, "Storage", paths.ModScopeName);
        }

        public static string StoragePath(string fileName)
        {
            return Path.Combine(StorageFolder(), fileName).Replace("/", "\\");
        }
    }
}
