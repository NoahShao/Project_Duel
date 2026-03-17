using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JunzhenDuijue.Editor
{
    /// <summary>
    /// 检查项目中是否仍有使用旧版 UnityEngine.UI.Text / InputField，未替换为 TextMeshPro。
    /// 菜单：Tools -> 军阵对决 -> 检查是否全部使用 TextMeshPro
    /// </summary>
    public static class CheckTextMeshProUsage
    {
        private const string MenuPath = "Tools/军阵对决/检查是否全部使用 TextMeshPro";

        [MenuItem(MenuPath)]
        public static void RunCheck()
        {
            var legacyTextList = new List<string>();
            var legacyInputFieldList = new List<string>();

            // 1) 所有 Prefab
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = null;
                try
                {
                    root = PrefabUtility.LoadPrefabContents(path);
                    if (root != null)
                        CollectLegacyTextInTransform(root.transform, path, "", legacyTextList, legacyInputFieldList);
                }
                finally
                {
                    if (root != null)
                        PrefabUtility.UnloadPrefabContents(root);
                }
            }

            // 2) 当前已打开的 Scene（不自动打开未加载场景，避免触发场景内的光照等资源类型错误）
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene sc = SceneManager.GetSceneAt(i);
                if (!sc.isLoaded) continue;
                string scenePath = sc.path;
                if (string.IsNullOrEmpty(scenePath)) scenePath = "(Untitled)";
                foreach (GameObject go in sc.GetRootGameObjects())
                {
                    CollectLegacyTextInTransform(go.transform, scenePath, "", legacyTextList, legacyInputFieldList);
                }
            }

            // 报告
            var sb = new StringBuilder();
            sb.AppendLine("========== 检查是否全部使用 TextMeshPro ==========");
            if (legacyTextList.Count == 0 && legacyInputFieldList.Count == 0)
            {
                sb.AppendLine("通过：未发现 UnityEngine.UI.Text 或 InputField，已全部为 TextMeshPro。");
            }
            else
            {
                sb.AppendLine("发现仍在使用旧版 UI 文本的物体：");
                if (legacyTextList.Count > 0)
                {
                    sb.AppendLine("  [Text] " + legacyTextList.Count + " 处");
                    foreach (var s in legacyTextList)
                        sb.AppendLine("    - " + s);
                }
                if (legacyInputFieldList.Count > 0)
                {
                    sb.AppendLine("  [InputField] " + legacyInputFieldList.Count + " 处");
                    foreach (var s in legacyInputFieldList)
                        sb.AppendLine("    - " + s);
                }
            }
            sb.AppendLine("==================================================");

            Debug.Log(sb.ToString());

            if (legacyTextList.Count > 0 || legacyInputFieldList.Count > 0)
            {
                EditorUtility.DisplayDialog(
                    "检查 TextMeshPro",
                    "发现 " + legacyTextList.Count + " 处 Text、" + legacyInputFieldList.Count + " 处 InputField 仍为旧版。\n详情见 Console。",
                    "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("检查 TextMeshPro", "通过：所有文本均已使用 TextMeshPro。", "确定");
            }
        }

        private static void CollectLegacyTextInTransform(
            Transform t,
            string assetPath,
            string hierarchyPath,
            List<string> legacyTextList,
            List<string> legacyInputFieldList)
        {
            string nodePath = string.IsNullOrEmpty(hierarchyPath) ? t.name : hierarchyPath + "/" + t.name;

            var text = t.GetComponent<Text>();
            if (text != null)
                legacyTextList.Add(assetPath + " -> " + nodePath);

            var inputField = t.GetComponent<InputField>();
            if (inputField != null)
                legacyInputFieldList.Add(assetPath + " -> " + nodePath);

            for (int i = 0; i < t.childCount; i++)
                CollectLegacyTextInTransform(t.GetChild(i), assetPath, nodePath, legacyTextList, legacyInputFieldList);
        }
    }
}
