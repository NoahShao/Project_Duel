using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace JunzhenDuijue
{
    /// <summary>
    /// 安全获取 TMP 默认字体。优先使用支持中文的字体（KaiTi），确保中文不乱码、不显示方框。
    /// </summary>
    public static class TMPHelper
    {
        static TMP_FontAsset _cachedFont;

        public static TMP_FontAsset GetDefaultFont()
        {
            if (_cachedFont != null) return _cachedFont;
            // 1) 预生成的 TMP 中文字体（菜单 Tools→军阵对决→创建 TMP 中文字体 生成）
            var prebuilt = Resources.Load<TMP_FontAsset>("Fonts/KaiTi SDF");
            if (prebuilt != null) { _cachedFont = prebuilt; return prebuilt; }
            // 2) 运行时用楷体创建 TMP 动态字体（楷体放在 Assets/Resources/Fonts/ 下，如 KaiTi.ttf）
            var kaiTi = Resources.Load<Font>("Fonts/KaiTi");
            if (kaiTi != null)
            {
                var runtimeFont = TMP_FontAsset.CreateFontAsset(kaiTi, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024);
                if (runtimeFont != null) { _cachedFont = runtimeFont; return runtimeFont; }
            }
            try
            {
                var f = TMP_Settings.defaultFontAsset;
                if (f != null) { _cachedFont = f; return f; }
            }
            catch { /* ignore */ }
            var fallback = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (fallback != null) { _cachedFont = fallback; return fallback; }
            var all = Resources.LoadAll<TMP_FontAsset>("Fonts & Materials");
            var any = all != null && all.Length > 0 ? all[0] : null;
            if (any != null) { _cachedFont = any; return any; }
            return null;
        }
    }
}
