using ProjectDuel.Shared.Config;
using ProjectDuel.Shared.Protocol;
using ProjectDuel.Shared.SkillFramework;

namespace ProjectDuel.Shared.Rules;

/// <summary>
/// <summary>
/// <summary>????????????</summary>
/// </summary>
public static class AuthoritativeBattleEngine
{
    public static AuthoritativeBattleState StartMatch(DeckSelectionDto first, DeckSelectionDto second)
    {
        var state = new AuthoritativeBattleState();
        InitSide(state.Sides[0], first);
        InitSide(state.Sides[1], second);
        Draw(state.Sides[0], AuthoritativeBattleState.DefaultHandLimit);
        Draw(state.Sides[1], AuthoritativeBattleState.DefaultHandLimit);
        state.ActiveSeatIndex = 0;
        state.TurnNumber = 1;
        state.Phase = DuelPhaseName.Preparation;
        return state;
    }

    public static bool TryUseMorale(AuthoritativeBattleState state, int seatIndex, int effectIndex, int? generalIndex, ISkillRuleLookup? skillRules = null)
    {
        if (!IsActiveSeat(state, seatIndex) || state.Phase != DuelPhaseName.Primary)
            return false;
        var side = state.ActiveSide;
        if (effectIndex < 0 || effectIndex >= side.MoraleUsedThisTurn.Length || side.Morale <= 0 || side.MoraleUsedThisTurn[effectIndex])
            return false;

        if (effectIndex == 0)
            Draw(side, 2);
        else if (effectIndex == 1)
            state.TotalPlayPhasesThisTurn++;
        else if (effectIndex == 2)
        {
            if (!generalIndex.HasValue || !TryFlipGeneral(side, generalIndex.Value))
                return false;
        }

        side.MoraleUsedThisTurn[effectIndex] = true;
        side.Morale--;
        LiuBeiSkillHooks.AfterMoraleSpent(state, seatIndex, skillRules);
        return true;
    }

    public static bool TryPlayCards(AuthoritativeBattleState state, int seatIndex, IReadOnlyList<int> handIndices)
    {
        if (!IsActiveSeat(state, seatIndex) || state.Phase != DuelPhaseName.Main || handIndices == null || handIndices.Count == 0)
            return false;
        var side = state.ActiveSide;
        if (side.PlayedThisPhase.Count + handIndices.Count > AuthoritativeBattleState.MaxPlayPerPhase)
            return false;

        var sorted = handIndices.Distinct().OrderByDescending(index => index).ToList();
        foreach (int handIndex in sorted)
        {
            if (handIndex < 0 || handIndex >= side.Hand.Count)
                return false;
        }

        for (int i = 0; i < sorted.Count; i++)
        {
            int handIndex = sorted[i];
            var card = side.Hand[handIndex];
            side.Hand.RemoveAt(handIndex);
            side.PlayedThisPhase.Insert(0, card);
        }
        return true;
    }

    public static bool TryTakeBackPlayedCard(AuthoritativeBattleState state, int seatIndex, int playedIndex)
    {
        if (!IsActiveSeat(state, seatIndex) || state.Phase != DuelPhaseName.Main)
            return false;
        var side = state.ActiveSide;
        if (playedIndex < 0 || playedIndex >= side.PlayedThisPhase.Count)
            return false;
        var card = side.PlayedThisPhase[playedIndex];
        side.PlayedThisPhase.RemoveAt(playedIndex);
        side.Hand.Add(card);
        return true;
    }

    public static bool TryActivatePrimarySkill(AuthoritativeBattleState state, int seatIndex, string effectId, int value1, int value2, string stringValue1)
    {
        if (!IsActiveSeat(state, seatIndex) || state.Phase != DuelPhaseName.Primary)
            return false;

        var side = state.ActiveSide;
        switch (effectId)
        {
            case "manual_primary_effect":
                if (string.Equals(stringValue1, "draw", StringComparison.OrdinalIgnoreCase))
                {
                    Draw(side, Math.Max(1, value1));
                    return true;
                }
                if (string.Equals(stringValue1, "heal", StringComparison.OrdinalIgnoreCase))
                {
                    side.CurrentHp = Math.Min(side.MaxHp, side.CurrentHp + Math.Max(1, value1));
                    return true;
                }
                if (string.Equals(stringValue1, "extra_phase", StringComparison.OrdinalIgnoreCase))
                {
                    state.TotalPlayPhasesThisTurn += Math.Max(1, value1 == 0 ? 1 : value1);
                    return true;
                }
                return true;
            case "primary_discard_any_draw_same":
                Draw(side, Math.Max(1, value1));
                return true;
            case "primary_draw_to_eight_gain_extra_phases":
                while (side.Hand.Count < Math.Max(8, value1))
                {
                    if (Draw(side, 1) == 0)
                        break;
                    state.TotalPlayPhasesThisTurn++;
                }
                return true;
            case "primary_recover_discard_gain_extra_phase":
                side.CurrentHp = Math.Min(side.MaxHp, side.CurrentHp + Math.Max(1, value1));
                state.TotalPlayPhasesThisTurn += 1;
                return true;
            case "primary_raise_morale_cap_and_small3_no_flip":
                side.Morale = Math.Min(AuthoritativeBattleState.MaxMorale, side.Morale + Math.Max(1, value1));
                return true;
            case "primary_pay_morale_cap_gain_yofeng":
                if (side.Morale <= 0)
                    return false;
                side.Morale--;
                return true;
            case "effect_gain_choose_heal_or_damage":
                side.CurrentHp = Math.Min(side.MaxHp, side.CurrentHp + Math.Max(1, value1));
                return true;
            case "anytime_remove_effect_layers":
                side.Morale = Math.Min(AuthoritativeBattleState.MaxMorale, side.Morale + 1);
                return true;
            case "grant_resist_layer":
                side.Morale = Math.Min(AuthoritativeBattleState.MaxMorale, side.Morale + 1);
                return true;
            default:
                return false;
        }
    }

    public static bool TrySelectAttackSkill(AuthoritativeBattleState state, int seatIndex, int generalIndex, int skillIndex, string skillName)
    {
        if (!IsActiveSeat(state, seatIndex))
            return false;
        if (state.ActiveSide.PlayedThisPhase.Count <= 0)
            return false;
        if (state.Phase != DuelPhaseName.Main && state.Phase != DuelPhaseName.Defense)
            return false;

        state.PendingAttackGeneralIndex = generalIndex;
        state.PendingAttackSkillIndex = skillIndex;
        state.PendingAttackSkillName = skillName ?? string.Empty;

        state.PendingIgnoreDefenseReduction = false;
        state.PendingPostResolveDrawToAttacker = 0;
        state.PendingPostResolveHealToAttacker = 0;
        state.PendingPostResolveMoraleToAttacker = 0;

        int playedCount = state.ActiveSide.PlayedThisPhase.Count;
        state.PendingBaseDamage = Math.Max(1, playedCount);
        state.PendingAttackBonus = generalIndex >= 0 ? 1 : 0;

        if (generalIndex >= 0 && generalIndex < state.ActiveSide.GeneralCardIds.Count)
        {
            string cardId = state.ActiveSide.GeneralCardIds[generalIndex] ?? string.Empty;
            string skillKey = cardId + "_" + skillIndex;
            if (SkillFrameworkExecutor.TryApplyAttackSkillFromRegistry(state, skillKey, state.ActiveSide.PlayedThisPhase))
                state.PendingAttackBonus = 0;
        }

        return true;
    }

    public static bool TrySelectDefenseSkill(AuthoritativeBattleState state, int seatIndex, int generalIndex, int skillIndex, string skillName)
    {
        if (IsActiveSeat(state, seatIndex) || state.Phase != DuelPhaseName.Defense)
            return false;
        state.PendingDefenseGeneralIndex = generalIndex;
        state.PendingDefenseSkillIndex = skillIndex;
        state.PendingDefenseSkillName = skillName ?? string.Empty;
        state.PendingDefenseReduction = 1;
        return true;
    }

    public static void AdvancePhase(AuthoritativeBattleState state, ISkillRuleLookup? skillRules = null)
    {
        switch (state.Phase)
        {
            case DuelPhaseName.Preparation:
                state.Phase = DuelPhaseName.Income;
                break;
            case DuelPhaseName.Income:
                RunIncome(state);
                state.Phase = DuelPhaseName.Primary;
                break;
            case DuelPhaseName.Primary:
                state.Phase = DuelPhaseName.Main;
                break;
            case DuelPhaseName.Main:
                if (state.ActiveSide.PlayedThisPhase.Count > 0)
                {
                    state.PendingBaseDamage = Math.Max(1, state.ActiveSide.PlayedThisPhase.Count);
                    if (string.IsNullOrWhiteSpace(state.PendingAttackSkillName))
                        state.PendingAttackSkillName = "通用攻击";
                    state.Phase = DuelPhaseName.Defense;
                }
                else
                {
                    FinishPlayPhase(state);
                }
                break;
            case DuelPhaseName.Defense:
                state.Phase = DuelPhaseName.Resolve;
                break;
            case DuelPhaseName.Resolve:
                ResolveDamage(state);
                ApplyPostResolveEffects(state);
                FinishPlayPhase(state);
                break;
            case DuelPhaseName.Discard:
                RunDiscard(state);
                LiuBeiSkillHooks.OnDiscardPhaseEndRenZheWuDi(state, skillRules);
                EndTurn(state);
                break;
            case DuelPhaseName.TurnEnd:
                EndTurn(state);
                break;
        }
    }

    public static BattleSnapshotResponse BuildSnapshot(AuthoritativeBattleState state, int localSeatIndex, string roomId, string selfName, string opponentName, string selfDeckId, string opponentDeckId)
    {
        int opponentSeatIndex = localSeatIndex == 0 ? 1 : 0;
        var self = state.Sides[localSeatIndex];
        var opp = state.Sides[opponentSeatIndex];
        return new BattleSnapshotResponse
        {
            RoomId = roomId,
            LocalSeatIndex = localSeatIndex,
            ActiveSeatIndex = state.ActiveSeatIndex,
            TurnNumber = state.TurnNumber,
            Phase = state.Phase,
            HandLimit = state.HandLimit,
            TotalPlayPhasesThisTurn = state.TotalPlayPhasesThisTurn,
            CurrentPlayPhaseIndex = state.CurrentPlayPhaseIndex,
            PendingAttackSkillName = state.PendingAttackSkillName,
            PendingDefenseSkillName = state.PendingDefenseSkillName,
            Self = BuildSideSnapshot(localSeatIndex, selfName, selfDeckId, self),
            Opponent = BuildSideSnapshot(opponentSeatIndex, opponentName, opponentDeckId, opp),
            SelfHand = self.Hand.Select(ToCardDto).ToList(),
            PlayedCards = state.ActiveSide.PlayedThisPhase.Select(ToCardDto).ToList(),
        };
    }

    private static BattleSideSnapshot BuildSideSnapshot(int seatIndex, string playerName, string deckId, AuthoritativeSideState side)
    {
        return new BattleSideSnapshot
        {
            SeatIndex = seatIndex,
            PlayerName = playerName,
            DeckId = deckId,
            DeckCount = side.Deck.Count,
            HandCount = side.Hand.Count,
            DiscardCount = side.DiscardPile.Count,
            CurrentHp = side.CurrentHp,
            MaxHp = side.MaxHp,
            Morale = side.Morale,
            MoraleCap = side.MoraleCap,
            MoraleUsedThisTurn = side.MoraleUsedThisTurn.ToList(),
            GeneralCardIds = side.GeneralCardIds.ToList(),
            GeneralFaceUp = side.GeneralFaceUp.ToList(),
            DiscardTopPreview = side.DiscardPile.TakeLast(5).Select(ToCardDto).ToList(),
            DiscardCards = side.DiscardPile.Select(ToCardDto).ToList(),
        };
    }

    private static BattleCardDto ToCardDto(AuthoritativePokerCard card)
    {
        return new BattleCardDto
        {
            Suit = card.Suit,
            Rank = card.Rank,
            DisplayName = card.DisplayName,
        };
    }

    private static void InitSide(AuthoritativeSideState side, DeckSelectionDto deck)
    {
        side.Deck = CreateShuffledDeck(deck.RemovedSuit);
        side.Hand.Clear();
        side.DiscardPile.Clear();
        side.PlayedThisPhase.Clear();
        side.GeneralCardIds = deck.CardIds?.Take(3).ToList() ?? new List<string>();
        side.GeneralFaceUp = side.GeneralCardIds.Select(_ => true).ToList();
        side.FaceDownRecoverAfterOwnTurnEnds = side.GeneralCardIds.Select(_ => 0).ToList();
        side.Morale = 0;
        side.MoraleCap = AuthoritativeBattleState.DefaultMoraleCap;
        side.CurrentHp = 30;
        side.MaxHp = 30;
        side.MoraleUsedThisTurn = new bool[3];
        side.EffectLayers.Clear();
        side.TriggeredSkillKeysThisTurn.Clear();
    }

    private static List<AuthoritativePokerCard> CreateShuffledDeck(string removedSuit)
    {
        var cards = new List<AuthoritativePokerCard>();
        foreach (string suit in AuthoritativeBattleState.Suits)
        {
            if (!string.IsNullOrWhiteSpace(removedSuit) && string.Equals(removedSuit.Trim(), suit, StringComparison.Ordinal))
                continue;
            for (int rank = 1; rank <= 13; rank++)
                cards.Add(new AuthoritativePokerCard { Suit = suit, Rank = rank });
        }
        var random = Random.Shared;
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int swap = random.Next(i + 1);
            (cards[i], cards[swap]) = (cards[swap], cards[i]);
        }
        return cards;
    }

    private static int Draw(AuthoritativeSideState side, int count)
    {
        int actual = 0;
        while (count > 0 && side.Deck.Count > 0)
        {
            var card = side.Deck[^1];
            side.Deck.RemoveAt(side.Deck.Count - 1);
            side.Hand.Add(card);
            count--;
            actual++;
        }
        return actual;
    }

    private static bool TryFlipGeneral(AuthoritativeSideState side, int generalIndex)
    {
        if (generalIndex < 0 || generalIndex >= side.GeneralFaceUp.Count || !side.GeneralFaceUp[generalIndex])
            return false;
        side.GeneralFaceUp[generalIndex] = false;
        side.FaceDownRecoverAfterOwnTurnEnds[generalIndex] = 2;
        return true;
    }

    private static void RunIncome(AuthoritativeBattleState state)
    {
        var side = state.ActiveSide;
        int drawCount = Math.Max(1, side.GeneralFaceUp.Count(faceUp => faceUp));
        Draw(side, drawCount);
        bool skipFirstMorale = state.TurnNumber == 1 && state.ActiveSeatIndex == 0 && state.PlayerZeroGoesFirst;
        if (!skipFirstMorale)
            side.Morale = Math.Min(side.MoraleCap, side.Morale + 1);
    }

    private static void FinishPlayPhase(AuthoritativeBattleState state)
    {
        foreach (var card in state.ActiveSide.PlayedThisPhase)
            state.ActiveSide.DiscardPile.Add(card);
        state.ActiveSide.PlayedThisPhase.Clear();
        state.PendingBaseDamage = 0;
        state.PendingAttackBonus = 0;
        state.PendingDefenseReduction = 0;
        state.PendingAttackGeneralIndex = -1;
        state.PendingAttackSkillIndex = -1;
        state.PendingDefenseGeneralIndex = -1;
        state.PendingDefenseSkillIndex = -1;
        state.PendingAttackSkillName = string.Empty;
        state.PendingDefenseSkillName = string.Empty;
        state.PendingIgnoreDefenseReduction = false;
        state.PendingPostResolveDrawToAttacker = 0;
        state.PendingPostResolveHealToAttacker = 0;
        state.PendingPostResolveMoraleToAttacker = 0;
        state.CurrentPlayPhaseIndex++;
        if (state.CurrentPlayPhaseIndex < state.TotalPlayPhasesThisTurn)
            state.Phase = DuelPhaseName.Main;
        else
            state.Phase = DuelPhaseName.Discard;
    }

    private static void ResolveDamage(AuthoritativeBattleState state)
    {
        int defense = state.PendingIgnoreDefenseReduction ? 0 : state.PendingDefenseReduction;
        int damage = Math.Max(0, state.PendingBaseDamage + state.PendingAttackBonus - defense);
        state.InactiveSide.CurrentHp = Math.Max(0, state.InactiveSide.CurrentHp - damage);
    }

    private static void ApplyPostResolveEffects(AuthoritativeBattleState state)
    {
        var side = state.ActiveSide;
        if (state.PendingPostResolveDrawToAttacker > 0)
            Draw(side, state.PendingPostResolveDrawToAttacker);
        if (state.PendingPostResolveHealToAttacker > 0)
            side.CurrentHp = Math.Min(side.MaxHp, side.CurrentHp + state.PendingPostResolveHealToAttacker);
        if (state.PendingPostResolveMoraleToAttacker > 0)
            side.Morale = Math.Min(side.MoraleCap, side.Morale + state.PendingPostResolveMoraleToAttacker);
    }

    private static void RunDiscard(AuthoritativeBattleState state)
    {
        var side = state.ActiveSide;
        while (side.Hand.Count > state.HandLimit)
        {
            var card = side.Hand[^1];
            side.Hand.RemoveAt(side.Hand.Count - 1);
            side.DiscardPile.Add(card);
        }
    }

    private static void EndTurn(AuthoritativeBattleState state)
    {
        var side = state.ActiveSide;
        for (int i = 0; i < side.GeneralFaceUp.Count; i++)
        {
            if (side.GeneralFaceUp[i] || side.FaceDownRecoverAfterOwnTurnEnds[i] <= 0)
                continue;
            side.FaceDownRecoverAfterOwnTurnEnds[i]--;
            if (side.FaceDownRecoverAfterOwnTurnEnds[i] <= 0)
                side.GeneralFaceUp[i] = true;
        }
        side.MoraleUsedThisTurn = new bool[3];
        state.Sides[0].TriggeredSkillKeysThisTurn.Clear();
        state.Sides[1].TriggeredSkillKeysThisTurn.Clear();
        state.ActiveSeatIndex = state.ActiveSeatIndex == 0 ? 1 : 0;
        state.TotalPlayPhasesThisTurn = 1;
        state.CurrentPlayPhaseIndex = 0;
        state.TurnNumber++;
        state.Phase = DuelPhaseName.Preparation;
    }

    private static bool IsActiveSeat(AuthoritativeBattleState state, int seatIndex)
    {
        return state.ActiveSeatIndex == seatIndex;
    }
}
