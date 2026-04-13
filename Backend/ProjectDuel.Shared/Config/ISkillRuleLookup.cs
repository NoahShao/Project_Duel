namespace ProjectDuel.Shared.Config;

/// <summary>
/// 供权威引擎在结算时查询 <see cref="SkillRuleDefinition"/>（如刘备被动）。
/// </summary>
public interface ISkillRuleLookup
{
    SkillRuleDefinition? GetRule(string cardId, int skillIndex);
}
