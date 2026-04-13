namespace ProjectDuel.Shared.Config;

public sealed class SkillRuleDefinition
{
    public string SkillKey { get; set; } = string.Empty;
    public string CardId { get; set; } = string.Empty;
    public int SkillIndex { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string TriggerHint { get; set; } = string.Empty;
    public bool AllowOnOpponentTurn { get; set; }
    public string EffectId { get; set; } = string.Empty;
    public int Value1 { get; set; }
    public int Value2 { get; set; }
    public string StringValue1 { get; set; } = string.Empty;
}

public sealed class SkillRuleCollection
{
    public List<SkillRuleDefinition> Entries { get; set; } = new();
}
