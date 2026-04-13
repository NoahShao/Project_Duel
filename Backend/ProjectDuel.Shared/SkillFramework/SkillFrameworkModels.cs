namespace ProjectDuel.Shared.SkillFramework;

/// <summary>
/// 与 Unity 客户端 <c>Resources/Config/SkillFramework.json</c> 字段一致，供 System.Text.Json 反序列化（枚举用整型，与 JsonUtility 导出一致）。
/// </summary>
public enum SkillFrameworkEventKind
{
    None = 0,
    GameStart = 1,
    PreparationStart = 2,
    IncomeEnd = 3,
    DiscardStart = 4,
    DiscardEnd = 5,
    TurnEnd = 6,
    AfterMoraleSpent = 7,
    ManualPrimary = 8,
}

public enum SkillFrameworkEffectOp
{
    None = 0,
    GainMorale = 1,
    AddMoraleCap = 2,
    HealSelf = 3,
    DrawSelf = 4,
    AddEffectLayerSelf = 5,
    RemoveEffectLayersAnySelf = 6,
    AddExtraPlayPhases = 7,
    SetPendingBaseDamage = 8,
    AddPendingBonusDamage = 9,
    SetIgnoreDefense = 10,
    AddPostResolveDraw = 11,
    AddPostResolveHeal = 12,
    AddPostResolveMorale = 13,
    AppendCombatNote = 14,
    MarkTriggeredThisTurn = 15,
}

public enum AttackPatternKind
{
    None = 0,
    SingleCard = 1,
    Pair = 2,
    TwoPair = 3,
    Triple = 4,
    Straight = 5,
    StraightFlush = 6,
    FullHouse = 7,
    FourOfAKind = 8,
}

public sealed class SkillEffectStep
{
    public SkillFrameworkEffectOp Op { get; set; }
    public int I0 { get; set; }
    public int I1 { get; set; }
    public int I2 { get; set; }
    public string S0 { get; set; } = string.Empty;
}

public sealed class SkillTriggerBlock
{
    public SkillFrameworkEventKind Event { get; set; }
    public int LimitPerTurn { get; set; }
    public List<SkillEffectStep> Steps { get; set; } = new();
}

public sealed class AttackPatternRow
{
    public AttackPatternKind Kind { get; set; }
    public int MinStraightLength { get; set; } = 3;
    public bool RequireAllRed { get; set; }
    public bool RequireAllBlack { get; set; }
    public bool RequireNotFlush { get; set; }
    public int MinEffectiveRankExclusive { get; set; }
    public int BaseDamage { get; set; }
    public bool Unblockable { get; set; }
    public int ExtraPlayPhases { get; set; }
    public int PostDraw { get; set; }
    public int PostHeal { get; set; }
    public int PostMorale { get; set; }
    public string Note { get; set; } = string.Empty;
}

public sealed class SkillDefinition
{
    public string SkillKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<SkillTriggerBlock> Triggers { get; set; } = new();
    public List<AttackPatternRow> AttackPatterns { get; set; } = new();
}

public sealed class SkillFrameworkTable
{
    public List<SkillDefinition> Definitions { get; set; } = new();
}
