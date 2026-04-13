using ProjectDuel.Shared.Protocol;
using ProjectDuel.Shared.Rules;
using Xunit;

namespace ProjectDuel.Shared.Tests;

public class AuthoritativeBattleEngineTests
{
    private static DeckSelectionDto MakeDeck(string id, params string[] cards)
    {
        return new DeckSelectionDto
        {
            DeckId = id,
            DisplayName = id,
            RemovedSuit = string.Empty,
            CardIds = cards.ToList(),
        };
    }

    [Fact]
    public void StartMatch_DrawsOpeningHands()
    {
        var state = AuthoritativeBattleEngine.StartMatch(
            MakeDeck("A", "NO001", "NO002", "NO003"),
            MakeDeck("B", "NO004", "NO005", "NO006"));

        Assert.Equal(6, state.Sides[0].Hand.Count);
        Assert.Equal(6, state.Sides[1].Hand.Count);
        Assert.Equal(DuelPhaseName.Preparation, state.Phase);
        Assert.Equal(1, state.TurnNumber);
    }

    [Fact]
    public void TryUseMorale_DrawEffect_AddsTwoCards()
    {
        var state = AuthoritativeBattleEngine.StartMatch(
            MakeDeck("A", "NO001", "NO002", "NO003"),
            MakeDeck("B", "NO004", "NO005", "NO006"));
        state.Phase = DuelPhaseName.Primary;
        state.ActiveSeatIndex = 0;
        state.Sides[0].Morale = 1;
        int before = state.Sides[0].Hand.Count;

        bool ok = AuthoritativeBattleEngine.TryUseMorale(state, 0, 0, null);

        Assert.True(ok);
        Assert.Equal(before + 2, state.Sides[0].Hand.Count);
        Assert.Equal(0, state.Sides[0].Morale);
        Assert.True(state.Sides[0].MoraleUsedThisTurn[0]);
    }

    [Fact]
    public void TryPlayCards_MovesCardsIntoPlayedArea()
    {
        var state = AuthoritativeBattleEngine.StartMatch(
            MakeDeck("A", "NO001", "NO002", "NO003"),
            MakeDeck("B", "NO004", "NO005", "NO006"));
        state.Phase = DuelPhaseName.Main;
        state.ActiveSeatIndex = 0;
        int before = state.Sides[0].Hand.Count;

        bool ok = AuthoritativeBattleEngine.TryPlayCards(state, 0, new[] { 0, 1 });

        Assert.True(ok);
        Assert.Equal(before - 2, state.Sides[0].Hand.Count);
        Assert.Equal(2, state.Sides[0].PlayedThisPhase.Count);
    }

    [Fact]
    public void AdvancePhase_FromMainWithPlayedCards_EntersDefense()
    {
        var state = AuthoritativeBattleEngine.StartMatch(
            MakeDeck("A", "NO001", "NO002", "NO003"),
            MakeDeck("B", "NO004", "NO005", "NO006"));
        state.Phase = DuelPhaseName.Main;
        state.ActiveSeatIndex = 0;
        state.Sides[0].PlayedThisPhase.Add(new AuthoritativePokerCard { Suit = "\u7ea2\u6843", Rank = 1 });

        AuthoritativeBattleEngine.AdvancePhase(state);

        Assert.Equal(DuelPhaseName.Defense, state.Phase);
        Assert.Equal(1, state.PendingBaseDamage);
    }

    [Fact]
    public void AdvancePhase_FromMainWithoutSelectedSkill_UsesGenericAttackName()
    {
        var state = AuthoritativeBattleEngine.StartMatch(
            MakeDeck("A", "NO001", "NO002", "NO003"),
            MakeDeck("B", "NO004", "NO005", "NO006"));
        state.Phase = DuelPhaseName.Main;
        state.ActiveSeatIndex = 0;
        state.Sides[0].PlayedThisPhase.Add(new AuthoritativePokerCard { Suit = "红桃", Rank = 1 });

        AuthoritativeBattleEngine.AdvancePhase(state);

        Assert.Equal(DuelPhaseName.Defense, state.Phase);
        Assert.Equal("通用攻击", state.PendingAttackSkillName);
    }

    [Fact]
    public void TryActivatePrimarySkill_ExtraPhase_IncreasesPlayPhaseCount()
    {
        var state = AuthoritativeBattleEngine.StartMatch(
            MakeDeck("A", "NO001", "NO002", "NO003"),
            MakeDeck("B", "NO004", "NO005", "NO006"));
        state.Phase = DuelPhaseName.Primary;
        state.ActiveSeatIndex = 0;

        bool ok = AuthoritativeBattleEngine.TryActivatePrimarySkill(state, 0, "manual_primary_effect", 1, 0, "extra_phase");

        Assert.True(ok);
        Assert.Equal(2, state.TotalPlayPhasesThisTurn);
    }

    [Fact]
    public void TryActivatePrimarySkill_DrawToEight_AddsCardsAndPhases()
    {
        var state = AuthoritativeBattleEngine.StartMatch(
            MakeDeck("A", "NO001", "NO002", "NO003"),
            MakeDeck("B", "NO004", "NO005", "NO006"));
        state.Phase = DuelPhaseName.Primary;
        state.ActiveSeatIndex = 0;
        while (state.Sides[0].Hand.Count > 3)
            state.Sides[0].Hand.RemoveAt(state.Sides[0].Hand.Count - 1);

        bool ok = AuthoritativeBattleEngine.TryActivatePrimarySkill(state, 0, "primary_draw_to_eight_gain_extra_phases", 8, 1, "");

        Assert.True(ok);
        Assert.True(state.Sides[0].Hand.Count >= 8 || state.Sides[0].Deck.Count == 0);
        Assert.True(state.TotalPlayPhasesThisTurn > 1);
    }
}
