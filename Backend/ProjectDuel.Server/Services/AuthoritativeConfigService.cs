using System.Text.Json;
using Microsoft.Extensions.Options;
using ProjectDuel.Server.Options;
using ProjectDuel.Shared.Config;
using ProjectDuel.Shared.SkillFramework;

namespace ProjectDuel.Server.Services;

/// <summary>
/// ??????????
/// ?????? SkillRules.json?????????????????????????
/// </summary>
public sealed class AuthoritativeConfigService : ISkillRuleLookup
{
    private readonly ILogger<AuthoritativeConfigService> _logger;
    private readonly ConfigOptions _options;
    private readonly Dictionary<string, SkillRuleDefinition> _skillRuleByKey = new();

    public AuthoritativeConfigService(ILogger<AuthoritativeConfigService> logger, IOptions<ConfigOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        SkillRules = new SkillRuleCollection();
    }

    public SkillRuleCollection SkillRules { get; private set; }

    public int SkillRuleCount => SkillRules.Entries.Count;

    /// <summary>
    /// 从源配置文件加载技能规则。
    /// 服务端权威判定始终使用源配置 JSON，而不是客户端导出的 bytes。
    /// </summary>
    public void Load()
    {
        _skillRuleByKey.Clear();
        string absolutePath = ResolvePath(_options.SkillRulesPath);
        if (!File.Exists(absolutePath))
        {
            _logger.LogWarning("Skill rules source not found: {Path}", absolutePath);
            SkillRules = new SkillRuleCollection();
            return;
        }

        try
        {
            string json = File.ReadAllText(absolutePath);
            SkillRules = JsonSerializer.Deserialize<SkillRuleCollection>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? new SkillRuleCollection();
            foreach (var entry in SkillRules.Entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.SkillKey))
                    continue;
                _skillRuleByKey[entry.SkillKey] = entry;
            }
            _logger.LogInformation("Loaded {Count} skill rules from {Path}", SkillRuleCount, absolutePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load skill rules from {Path}", absolutePath);
            SkillRules = new SkillRuleCollection();
            _skillRuleByKey.Clear();
        }

        string frameworkPath = ResolvePath(_options.SkillFrameworkPath);
        SkillFrameworkRegistry.Load(frameworkPath);
        if (SkillFrameworkRegistry.HasLoadedDefinitions)
            _logger.LogInformation("Loaded skill framework from {Path}", frameworkPath);
        else
            _logger.LogWarning("Skill framework file missing or empty: {Path}", frameworkPath);
    }

    /// <summary>
    /// 按卡牌 ID + 技能槽索引查询单条技能规则。
    /// 这是服务端处理出牌、士气、主动技时最常用的入口。
    /// </summary>
    public SkillRuleDefinition? GetRule(string cardId, int skillIndex)
    {
        _skillRuleByKey.TryGetValue(cardId + "_" + skillIndex, out SkillRuleDefinition? rule);
        return rule;
    }

    private static string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path))
            return path;
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }
}
