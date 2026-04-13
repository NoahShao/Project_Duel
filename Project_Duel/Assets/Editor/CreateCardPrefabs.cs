using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.IO;

namespace JunzhenDuijue.Editor
{
    /// <summary>
    /// 菜单：Tools -> 军阵对决 -> 创建 150 张卡牌预制体 (NO001-NO150)
    /// 卡牌比例 63mm×88mm，一行 4 张、一页 2 行，与图鉴区域匹配。
    /// </summary>
    public static class CreateCardPrefabs
    {
        private const string PrefabFolder = "Assets/Resources/CardPrefabs";
        private const int CardCount = 150;

        // 与当前图鉴 GridLayoutGroup 保持一致：单张卡牌固定 240×360
        private static readonly float CardWidth = 240f;
        private static readonly float CardHeight = 360f;

        [MenuItem("Tools/军阵对决/测试加载 Cards.xlsx")]
        public static void TestLoadCardsXlsx()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "Cards.xlsx");
            Debug.Log("尝试路径: " + path);
            Debug.Log("File.Exists: " + File.Exists(path));
            if (!File.Exists(path))
            {
                Debug.LogError("未找到 Cards.xlsx，请放到 Assets/StreamingAssets/Cards.xlsx");
                return;
            }
            CardTableLoader.Load();
            Debug.Log("MaxId=" + CardTableLoader.MaxId + ", AllCards.Count=" + (CardTableLoader.AllCards?.Count ?? 0));
            if (CardTableLoader.AllCards != null && CardTableLoader.AllCards.Count > 0)
            {
                var first = CardTableLoader.AllCards[0];
                Debug.Log("首条: id=" + first.Id + ", 角色名称=" + first.RoleName);
            }
        }

        public static void CreateAll()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/CardPrefabs"))
                AssetDatabase.CreateFolder("Assets/Resources", "CardPrefabs");

            for (int i = 1; i <= CardCount; i++)
            {
                string cardId = "NO" + i.ToString("D3");
                string path = $"{PrefabFolder}/{cardId}.prefab";
                GameObject prefab = CreateSingleCardPrefab(cardId);
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
                Object.DestroyImmediate(prefab);
            }

            EnsureCompendiumConfig();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"已创建 {CardCount} 个卡牌预制体：{PrefabFolder}");
        }

        public static void EnsureCompendiumConfig()
        {
            const string path = "Assets/Resources/CompendiumConfig.asset";
            var config = AssetDatabase.LoadAssetAtPath<CompendiumConfig>(path);
            if (config != null) return;
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            config = ScriptableObject.CreateInstance<CompendiumConfig>();
            for (int i = 1; i <= 8; i++)
                config.CardIds.Add("NO" + i.ToString("D3"));
            AssetDatabase.CreateAsset(config, path);
            Debug.Log("已创建图鉴配置：Assets/Resources/CompendiumConfig.asset，默认包含 NO001-NO008");
        }

        private static GameObject CreateSingleCardPrefab(string cardId)
        {
            var root = new GameObject(cardId);
            var rect = root.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(CardWidth, CardHeight);

            var display = root.AddComponent<CardDisplay>();
            display.CardId = cardId;

            var face = new GameObject("FaceImage");
            face.transform.SetParent(root.transform, false);
            var faceRect = face.AddComponent<RectTransform>();
            faceRect.anchorMin = Vector2.zero;
            faceRect.anchorMax = Vector2.one;
            faceRect.offsetMin = Vector2.zero;
            faceRect.offsetMax = Vector2.zero;
            var img = face.AddComponent<Image>();
            img.color = Color.white;
            img.raycastTarget = true;
            display.FaceImage = img;

            return root;
        }

        [MenuItem("Tools/军阵对决/创建单张卡牌预制体（图鉴与局内共用）")]
        public static void CreateCardSlotPrefab()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/CardPrefabs"))
                AssetDatabase.CreateFolder("Assets/Resources", "CardPrefabs");

            GameObject go = CreateCardSlotPrefabRoot();
            string path = $"{PrefabFolder}/CardSlot.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("已创建单张卡牌预制体：Assets/Resources/CardPrefabs/CardSlot.prefab。图鉴与局内均可 Resources.Load 后实例化，设 CardView.Data 并 LoadFaceSprite() 即可显示。");
        }

        /// <summary>
        /// 生成与图鉴 CreateCardView 一致的结构：Root(Image+CardView) + 子节点 Face(Image)，CardView.FaceImage 指向 Face。
        /// </summary>
        internal static GameObject CreateCardSlotPrefabRoot()
        {
            const float CardPixelW = 1016f;
            const float CardPixelH = 1488f;

            var root = new GameObject("CardSlot");
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(CardWidth, CardHeight);

            var bg = root.AddComponent<Image>();
            bg.color = new Color(0.22f, 0.24f, 0.3f, 1f);
            bg.raycastTarget = true;

            var face = new GameObject("Face");
            face.transform.SetParent(root.transform, false);
            var faceRect = face.AddComponent<RectTransform>();
            faceRect.anchorMin = Vector2.zero;
            faceRect.anchorMax = Vector2.one;
            faceRect.offsetMin = Vector2.zero;
            faceRect.offsetMax = Vector2.zero;

            var faceImg = face.AddComponent<Image>();
            faceImg.color = new Color(0.3f, 0.3f, 0.35f, 1f);
            faceImg.raycastTarget = true;
            faceImg.preserveAspect = true;

            var view = root.AddComponent<CardView>();
            view.FaceImage = faceImg;
            view.Data = null;

            return root;
        }
    }
}
