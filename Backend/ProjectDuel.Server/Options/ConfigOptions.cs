namespace ProjectDuel.Server.Options;

public sealed class ConfigOptions
{
    public const string SectionName = "Config";
    public string SkillRulesPath { get; set; } = @"..\..\..\..\..\Project_Duel\Assets\StreamingAssets\SkillRules.json";
    /// <summary>与 Unity <c>Assets/Resources/Config/SkillFramework.json</c> 同源，联机权威服攻击牌型等逻辑读此文件。</summary>
    public string SkillFrameworkPath { get; set; } = @"..\..\..\..\..\Project_Duel\Assets\Resources\Config\SkillFramework.json";
}
