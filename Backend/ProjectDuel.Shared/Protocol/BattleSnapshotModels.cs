namespace ProjectDuel.Shared.Protocol;

public sealed class BattleCardDto
{
    public string Suit { get; set; } = string.Empty;
    public int Rank { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

public sealed class BattleSideSnapshot
{
    public int SeatIndex { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public string DeckId { get; set; } = string.Empty;
    public int DeckCount { get; set; }
    public int HandCount { get; set; }
    public int DiscardCount { get; set; }
    public int CurrentHp { get; set; }
    public int MaxHp { get; set; }
    public int Morale { get; set; }
    public int MoraleCap { get; set; } = 2;
    public List<bool> MoraleUsedThisTurn { get; set; } = new();
    public List<string> GeneralCardIds { get; set; } = new();
    public List<bool> GeneralFaceUp { get; set; } = new();
    public List<BattleCardDto> DiscardTopPreview { get; set; } = new();
    public List<BattleCardDto> DiscardCards { get; set; } = new();
}

public sealed class BattleSnapshotResponse
{
    public string RoomId { get; set; } = string.Empty;
    public int LocalSeatIndex { get; set; }
    public int ActiveSeatIndex { get; set; }
    public int TurnNumber { get; set; }
    public DuelPhaseName Phase { get; set; }
    public int HandLimit { get; set; }
    public int TotalPlayPhasesThisTurn { get; set; }
    public int CurrentPlayPhaseIndex { get; set; }
    public string PendingAttackSkillName { get; set; } = string.Empty;
    public string PendingDefenseSkillName { get; set; } = string.Empty;
    public BattleSideSnapshot Self { get; set; } = new();
    public BattleSideSnapshot Opponent { get; set; } = new();
    public List<BattleCardDto> SelfHand { get; set; } = new();
    public List<BattleCardDto> PlayedCards { get; set; } = new();
}
