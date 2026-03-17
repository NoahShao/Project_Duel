using UnityEngine;
using UnityEditor;

namespace JunzhenDuijue.Editor
{
    /// <summary>
    /// 解决 Source Image 被导入成 1024×1024 导致卡牌拉伸的问题：强制保持原图比例 1016×1488。
    /// 菜单：Tools -> 军阵对决 -> 设置卡牌图片导入（并重新导入，使 Sprite 尺寸恢复为原图）
    /// </summary>
    public static class CardTextureImportSettings
    {
        private const int CardMaxSize = 2048;
        private const string CardsPath = "Assets/Resources/Cards";

        [MenuItem("Tools/军阵对决/设置卡牌图片导入")]
        public static void ApplyToCardTextures()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources") || !AssetDatabase.IsValidFolder(CardsPath))
            {
                Debug.LogWarning("[军阵对决] 未找到 " + CardsPath + "，请先创建 Resources/Cards 并放入卡牌图。");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { CardsPath });
            int count = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                importer.maxTextureSize = Mathf.Max(importer.maxTextureSize, CardMaxSize);
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.mipmapEnabled = false;
                importer.isReadable = true;
                importer.alphaIsTransparency = true;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.SaveAndReimport();
                count++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[军阵对决] 已对 {count} 张卡牌图应用导入设置并重新导入。若原图是 1016×1488，Inspector 里 Source Image 的尺寸应变为 1016×1488，不再出现 1024×1024。");
        }
    }
}
