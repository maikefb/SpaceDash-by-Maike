using System;
using System.Collections.Generic;
using LcdMod.Common.Helpers;
using Sandbox.Definitions;
using VRage.Game;
using VRage.ObjectBuilders;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;

namespace LcdMod.Client.Helpers
{
    /// <summary>
    /// Resolve o sprite de um item: usa o sprite registrado com o próprio id do item quando existe
    /// (sprites de itens que o jogo ou outros mods expõem) e cai no ícone da definição caso contrário.
    /// </summary>
    public static partial class TextureHelper
    {
        public const string MISSING_ICON = "MissingIcon";

        static HashSet<string> _spriteCatalog;
        static readonly Dictionary<MyCubeBlockDefinition, string> RegisteredBlockTextures = new Dictionary<MyCubeBlockDefinition, string>();
        static readonly HashSet<MyCubeBlockDefinition> FailedBlockTextures = new HashSet<MyCubeBlockDefinition>();

        public static void Clear()
        {
            _spriteCatalog = null;
            RegisteredBlockTextures.Clear();
            FailedBlockTextures.Clear();
            ClearCustomState();
        }

        /// <summary>
        /// Registra o ícone da definição do bloco como sprite de LCD e devolve o nome do sprite. Definição nula ou sem ícone
        /// devolve <paramref name="fallback"/>; uma definição que o jogo recusa registrar é lembrada, vai uma vez para o log e
        /// devolve <paramref name="fallback"/>.
        /// </summary>
        public static string GetOrAddTextureForBlock(MyCubeBlockDefinition definition, string fallback = MISSING_ICON)
        {
            if (definition == null || definition.Icons == null || definition.Icons.Length == 0 ||
                string.IsNullOrEmpty(definition.Icons[0]) || FailedBlockTextures.Contains(definition))
                return fallback;

            string spriteName;
            if (RegisteredBlockTextures.TryGetValue(definition, out spriteName))
                return spriteName;

            try
            {
                var texture = new MyLCDTextureDefinition
                {
                    Id = new MyDefinitionId((MyObjectBuilderType)typeof(MyObjectBuilder_LCDTextureDefinition),
                        definition.Id.ToString()),
                    Public = false,
                    LocalizationId = definition.DisplayNameString,
                    SpritePath = definition.Icons[0],
                    Selectable = false
                };
                MyDefinitionManager.Static.Definitions.AddOrReplaceDefinition(texture);
                spriteName = texture.Id.SubtypeName;
                RegisteredBlockTextures[definition] = spriteName;
                return spriteName;
            }
            catch (Exception e)
            {
                FailedBlockTextures.Add(definition);
                LogHelper.LogOnce("TextureHelper.Block." + definition.Id, "Icon registration failed for " + definition.Id + ": " + e.Message);
                return fallback;
            }
        }

        public static string ResolveItemSprite(MyItemType itemType, IMyTextSurface surface)
        {
            var definition = MyDefinitionManager.Static != null
                ? MyDefinitionManager.Static.TryGetPhysicalItemDefinition(itemType)
                : null;
            if (definition != null)
                return ResolveItemSprite(definition, surface);

            return ResolveItemSprite(itemType.ToString(), BuildSpriteCatalog(surface), null);
        }

        public static string ResolveItemSprite(MyPhysicalItemDefinition definition, IMyTextSurface surface)
        {
            if (definition == null)
                return string.Empty;

            return ResolveItemSprite(definition.Id.ToString(), BuildSpriteCatalog(surface), definition);
        }

        static string ResolveItemSprite(string itemId, ISet<string> spriteCatalog, MyPhysicalItemDefinition definition)
        {
            if (spriteCatalog != null && spriteCatalog.Contains(itemId))
                return itemId;

            if (definition != null && definition.Icons != null && definition.Icons.Length > 0 &&
                !string.IsNullOrEmpty(definition.Icons[0]))
            {
                return definition.Icons[0];
            }

            return itemId;
        }

        static ISet<string> BuildSpriteCatalog(IMyTextSurface surface)
        {
            if (_spriteCatalog != null || surface == null)
                return _spriteCatalog;

            var sprites = new List<string>();
            surface.GetSprites(sprites);
            _spriteCatalog = new HashSet<string>(sprites);
            return _spriteCatalog;
        }
    }
}
