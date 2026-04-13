using System.Text.Json;

namespace ProjectDuel.Shared.SkillFramework;

/// <summary>
/// 服务端从磁盘加载与 Unity 同结构的 <c>SkillFramework.json</c>。
/// </summary>
public static class SkillFrameworkRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static Dictionary<string, SkillDefinition> _byKey = new(StringComparer.Ordinal);
    private static bool _loadedOnce;

    public static void Load(string? absolutePath)
    {
        _byKey = new Dictionary<string, SkillDefinition>(StringComparer.Ordinal);
        _loadedOnce = true;
        if (string.IsNullOrWhiteSpace(absolutePath) || !File.Exists(absolutePath))
            return;

        try
        {
            string raw = File.ReadAllText(absolutePath);
            string json = WrapAsTableJson(raw.Trim());
            SkillFrameworkTable? table = JsonSerializer.Deserialize<SkillFrameworkTable>(json, JsonOptions);
            if (table?.Definitions == null)
                return;

            foreach (SkillDefinition? def in table.Definitions)
            {
                if (def == null || string.IsNullOrWhiteSpace(def.SkillKey))
                    continue;
                _byKey[def.SkillKey] = def;
            }
        }
        catch
        {
            _byKey.Clear();
        }
    }

    private static string WrapAsTableJson(string raw)
    {
        if (raw.StartsWith('['))
            return "{\"Definitions\":" + raw + "}";
        return raw;
    }

    public static bool TryGet(string skillKey, out SkillDefinition? def) =>
        _byKey.TryGetValue(skillKey ?? string.Empty, out def);

    /// <summary>单元测试用：重置加载状态。</summary>
    public static void ResetForTests()
    {
        _byKey = new Dictionary<string, SkillDefinition>(StringComparer.Ordinal);
        _loadedOnce = false;
    }

    public static bool HasLoadedDefinitions => _loadedOnce && _byKey.Count > 0;
}
