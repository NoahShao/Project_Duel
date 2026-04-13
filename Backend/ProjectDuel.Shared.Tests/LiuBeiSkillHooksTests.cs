using ProjectDuel.Shared.Config;
using ProjectDuel.Shared.Protocol;
using ProjectDuel.Shared.Rules;
using Xunit;

namespace ProjectDuel.Shared.Tests;

public class LiuBeiSkillHooksTests
{
    private sealed class LiuBeiRulesStub : ISkillRuleLookup
    {
        public SkillRuleDefinition? GetRule(string cardId, int skillIndex)
        {
            if (cardId == "NO001" && skillIndex == 0)
                return new SkillRuleDefinition { CardId = "NO001", SkillIndex = 0, EffectId = "start_game_gain_morale_and_max" };
            if (cardId == "NO001" && skillIndex == 1)
                return new SkillRuleDefinition { CardId = "NO001", SkillIndex = 1, EffectId = "discard_end_draw_reveal_red_heal_black_damage", Value1 = 1, Value2 = 2 };
            return null;
        }
    }

    private static DeckSelectionDto Deck(string id, params string[] cards) => new()
    {
        DeckId = id,
        DisplayName = id,
        RemovedSuit = string.Empty,
        CardIds = cards.ToList(),
    };

    [Fact]
    public void AfterMoraleSpent_RenDe_HealsOneWhenLiuBeiFaceUp()
    {
        var rules = new LiuBeiRulesStub();
        var state = AuthoritativeBattleEngine.StartMatch(Deck("A", "NO001", "NO002", "NO003"), Deck("B", "NO004", "NO005", "NO006"));
        state.Phase = DuelPhaseName.Primary;
        state.ActiveSeatIndex = 0;
        state.Sides[0].GeneralCardIds[0] = "NO001";
        state.Sides[0].CurrentHp = 20;
        state.Sides[0].Morale = 1;
        state.Sides[0].MoraleUsedThisTurn = new bool[3];

        bool ok = AuthoritativeBattleEngine.TryUseMorale(state, 0, 0, null, rules);

        Assert.True(ok);
        Assert.Equal(21, state.Sides[0].CurrentHp);
    }

    [Fact]
    public void OnDiscardEnd_RenZheWuDi_RedTop_HealsSelf()
    {
        var rules = new LiuBeiRulesStub();
        var state = AuthoritativeBattleEngine.StartMatch(Deck("A", "NO001", "NO002", "NO003"), Deck("B", "NO004", "NO005", "NO006"));
        state.ActiveSeatIndex = 0;
        state.Sides[0].GeneralCardIds[0] = "NO001";
        state.Sides[0].CurrentHp = 25;
        state.Sides[0].Deck.Add(new AuthoritativePokerCard { Suit = "\u7ea2\u6843", Rank = 3 });
        state.Sides[1].CurrentHp = 30;

        LiuBeiSkillHooks.OnDiscardPhaseEndRenZheWuDi(state, rules);

        Assert.Equal(27, state.Sides[0].CurrentHp);
        Assert.Equal(30, state.Sides[1].CurrentHp);
        Assert.Equal(7, state.Sides[0].Hand.Count);
    }

    [Fact]
    public void OnDiscardEnd_RenZheWuDi_BlackTop_DamagesOpponent()
    {
        var rules = new LiuBeiRulesStub();
        var state = AuthoritativeBattleEngine.StartMatch(Deck("A", "NO001", "NO002", "NO003"), Deck("B", "NO004", "NO005", "NO006"));
        state.ActiveSeatIndex = 0;
        state.Sides[0].GeneralCardIds[0] = "NO001";
        state.Sides[0].CurrentHp = 30;
        state.Sides[0].Deck.Add(new AuthoritativePokerCard { Suit = "\u9ed1\u6843", Rank = 3 });
        state.Sides[1].CurrentHp = 30;

        LiuBeiSkillHooks.OnDiscardPhaseEndRenZheWuDi(state, rules);

        Assert.Equal(30, state.Sides[0].CurrentHp);
        Assert.Equal(28, state.Sides[1].CurrentHp);
    }
}
