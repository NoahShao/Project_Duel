using System.Collections.Generic;
using UnityEngine;

namespace JunzhenDuijue
{
    /// <summary>
    /// 从 Resources 加载技能框架表。路径：<c>Resources/Config/SkillFramework</c>（文件名可为 SkillFramework.json）。
    /// </summary>
    public static class SkillFrameworkRegistry
    {
        private const string ResourcePath = "Config/SkillFramework";
        private static Dictionary<string, SkillDefinition> _byKey;
        private static bool _loadAttempted;

        public static void EnsureLoaded()
        {
            if (_loadAttempted)
                return;
            _loadAttempted = true;
            _byKey = new Dictionary<string, SkillDefinition>();
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
                return;

            try
            {
                var table = JsonUtility.FromJson<SkillFrameworkTable>(WrapAsTableJson(asset.text));
                if (table?.Definitions == null)
                    return;
                for (int i = 0; i < table.Definitions.Count; i++)
                {
                    var def = table.Definitions[i];
                    if (def == null || string.IsNullOrWhiteSpace(def.SkillKey))
                        continue;
                    _byKey[def.SkillKey] = def;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[SkillFrameworkRegistry] Parse failed: " + e.Message);
            }
        }

        /// <summary>
        /// JsonUtility 要求 JSON 根对象与类型字段名一致；若文件根就是 {"Definitions":[...]} 可直接解析，
        /// 若只有数组则包一层。
        /// </summary>
        private static string WrapAsTableJson(string raw)
        {
            string t = raw.Trim();
            if (t.StartsWith("["))
                return "{\"Definitions\":" + t + "}";
            return t;
        }

        public static bool TryGet(string skillKey, out SkillDefinition def)
        {
            EnsureLoaded();
            if (_byKey == null)
            {
                def = null;
                return false;
            }

            return _byKey.TryGetValue(skillKey ?? string.Empty, out def);
        }

        public static void ReloadForTests()
        {
            _loadAttempted = false;
            _byKey = null;
            EnsureLoaded();
        }
    }
}
