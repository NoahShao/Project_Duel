using UnityEngine;
using UnityEditor;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using TMPro;

namespace JunzhenDuijue.Editor
{
    /// <summary>
    /// 从 Resources/Fonts/KaiTi.ttf 生成 TextMeshPro 动态字体资源，保存为 Resources/Fonts/KaiTi SDF.asset。
    /// 生成后 TMPHelper 会优先使用该字体，中文即可正常显示。
    /// 菜单：Tools → 军阵对决 → 创建 TMP 中文字体（楷体）
    /// </summary>
    public static class CreateTMPChineseFont
    {
        private const string MenuPath = "Tools/军阵对决/创建 TMP 中文字体（楷体）";
        private const string FontPath = "Assets/Resources/Fonts/KaiTi.ttf";
        private const string OutputPath = "Assets/Resources/Fonts/KaiTi SDF.asset";

        [MenuItem(MenuPath)]
        public static void Create()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null)
            {
                Debug.LogError("[军阵对决] 未找到字体: " + FontPath + "，请确保楷体已放入 Assets/Resources/Fonts/ 并命名为 KaiTi.ttf。");
                return;
            }

            if (TMP_Settings.instance == null)
            {
                Debug.LogError("[军阵对决] 请先导入 TextMesh Pro 必要资源（Window → TextMeshPro → Import TMP Essential Resources）。");
                return;
            }

            FontEngine.InitializeFontEngine();
            if (FontEngine.LoadFontFace(font, 90) != FontEngineError.Success)
            {
                Debug.LogError("[军阵对决] 无法加载楷体字体。请在 Inspector 中选中 KaiTi.ttf，勾选 \"Include Font Data\"。");
                return;
            }

            // Dynamic 模式：运行时会按需将中文字符加入图集
            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024);
            if (fontAsset == null)
            {
                Debug.LogError("[军阵对决] 创建 TMP 字体资源失败。");
                return;
            }

            string dir = System.IO.Path.GetDirectoryName(OutputPath);
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Fonts")) AssetDatabase.CreateFolder("Assets/Resources", "Fonts");

            AssetDatabase.CreateAsset(fontAsset, OutputPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[军阵对决] 已创建 TMP 中文字体: " + OutputPath + "。运行游戏后文字应能正常显示。");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputPath));
        }
    }
}
