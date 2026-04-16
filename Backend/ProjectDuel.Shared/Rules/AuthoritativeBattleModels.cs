using ProjectDuel.Shared.Protocol;

namespace ProjectDuel.Shared.Rules;

/// <summary>
/// ??????????
/// </summary>
public sealed class AuthoritativePokerCard
{
    public string Suit { get; set; } = string.Empty;
    public int Rank { get; set; }
    /// <summary>【察势】由玩家声明：非角色 J/Q/K 是否按 10 点参与牌型（默认 false 为原有 0/11–13 规则）。</summary>
    public bool ChaShiCourtPlayedAsTen { get; set; }

    public string DisplayName => Suit + (Rank switch { 1 => "A", 11 => "J", 12 => "Q", 13 => "K", _ => Rank.ToString() });
}

public sealed class AuthoritativeSideState
{
    public List<AuthoritativePokerCard> Deck { get; set; } = new();
    public List<AuthoritativePokerCard> Hand { get; set; } = new();
    public List<AuthoritativePokerCard> DiscardPile { get; set; } = new();
    public List<AuthoritativePokerCard> PlayedThisPhase { get; set; } = new();
    public List<string> GeneralCardIds { get; set; } = new();
    public List<bool> GeneralFaceUp { get; set; } = new();
    public List<int> FaceDownRecoverAfterOwnTurnEnds { get; set; } = new();
    public bool[] MoraleUsedThisTurn { get; set; } = new bool[3];
    public int Morale { get; set; }
    /// <summary>士气存储上限（可与框架/技能提高的 MoraleCap 一致）。</summary>
    public int MoraleCap { get; set; } = AuthoritativeBattleState.DefaultMoraleCap;
    public int CurrentHp { get; set; } = 30;
    public int MaxHp { get; set; } = 30;
    public Dictionary<string, int> EffectLayers { get; set; } = new(StringComparer.Ordinal);
    public HashSet<string> TriggeredSkillKeysThisTurn { get; set; } = new(StringComparer.Ordinal);
}

/// <summary>
/// ??????????
/// ????????????????????????????
/// </summary>
public sealed class AuthoritativeBattleState
{
    public const int DefaultHandLimit = 6;
    public const int MaxMorale = 2;
    public const int DefaultMoraleCap = 2;
    public const int MaxPlayPerPhase = 5;

    public static readonly string[] Suits = { "\u7ea2\u6843", "\u65b9\u7247", "\u9ed1\u6843", "\u6885\u82b1" };

    public AuthoritativeSideState[] Sides { get; set; } = { new(), new() };
    public int ActiveSeatIndex { get; set; }
    public bool PlayerZeroGoesFirst { get; set; } = true;
    public int TurnNumber { get; set; } = 1;
    public DuelPhaseName Phase { get; set; } = DuelPhaseName.Preparation;
    public int HandLimit { get; set; } = DefaultHandLimit;
    public int TotalPlayPhasesThisTurn { get; set; } = 1;
    public int CurrentPlayPhaseIndex { get; set; }
    public int PendingBaseDamage { get; set; }
    public int PendingAttackBonus { get; set; }
    public int PendingDefenseReduction { get; set; }
    public int PendingAttackGeneralIndex { get; set; } = -1;
    public int PendingAttackSkillIndex { get; set; } = -1;
    public int PendingDefenseGeneralIndex { get; set; } = -1;
    public int PendingDefenseSkillIndex { get; set; } = -1;
    public string PendingAttackSkillName { get; set; } = string.Empty;
    public string PendingDefenseSkillName { get; set; } = string.Empty;
    public bool PendingIgnoreDefenseReduction { get; set; }
    public int PendingPostResolveDrawToAttacker { get; set; }
    public int PendingPostResolveHealToAttacker { get; set; }
    public int PendingPostResolveMoraleToAttacker { get; set; }

    public AuthoritativeSideState ActiveSide => Sides[ActiveSeatIndex];
    public AuthoritativeSideState InactiveSide => Sides[ActiveSeatIndex == 0 ? 1 : 0];
}
