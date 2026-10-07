using System;
using UnityEditor;
using UnityEngine;

namespace FS.EditorTools
{
    /// <summary>
    /// Import das folhas pre-renderizadas (Assets/_FS/Resources/Sprites/**.png): textura crua (Default) que a SpriteSheet
    /// fatia em runtime pelo meta.json. Sem mipmap (ortografica 2D), Clamp (celula vizinha nao vaza no Bilinear), sem
    /// compressao no PC; ASTC 6x6 no Android. Mudou alguma regra aqui: suba GetVersion para reimportar as folhas.
    /// </summary>
    public sealed class SpriteImport : AssetPostprocessor
    {
        const string Root = "Assets/_FS/Resources/Sprites/";
        const string Tex = "Assets/_FS/Resources/Textures/";   // chao/rua/parede/madeira: repetidas em Tiled (Art.Ground)

        public override uint GetVersion() => 2;

        void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            bool tiled = path.StartsWith(Tex, StringComparison.OrdinalIgnoreCase);
            if (!(tiled || path.StartsWith(Root, StringComparison.OrdinalIgnoreCase)) || !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Default;
            ti.maxTextureSize = 4096;
            ti.mipmapEnabled = false;
            ti.filterMode = FilterMode.Bilinear;
            ti.wrapMode = tiled ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;   // textura de chao repete; folha de sprite nao vaza celula
            ti.alphaIsTransparency = !tiled;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                maxTextureSize = 4096,
                format = TextureImporterFormat.ASTC_6x6,
            });
        }
    }
}
