using ProjectDuel.Shared.Config;

namespace ProjectDuel.Shared.Rules;

/// <summary>
/// 刘备 NO001：【仁德】消耗士气回血；【仁者无敌】弃牌阶段结束时摸牌展示，红回血/黑对敌方通用伤害。
/// 数值与 <c>SkillRules.json</c> 中 NO001 条目一致。
/// </summary>
public static class LiuBeiSkillHooks
{
    private const string LiuBeiCardId = "NO001";
    private const string RenDeEffectId = "start_game_gain_morale_and_max";
    private const string RenZheWuDiEffectId = "discard_end_draw_reveal_red_heal_black_damage";

    public static void AfterMoraleSpent(AuthoritativeBattleState state, int seatIndex, ISkillRuleLookup? rules)
    {
        if (rules == null || seatIndex < 0 || seatIndex >= state.Sides.Length)
            return;

        if (!TryFaceUpLiuBei(state.Sides[seatIndex], out _))
            return;

        SkillRuleDefinition? r0 = rules.GetRule(LiuBeiCardId, 0);
        if (r0 == null || !string.Equals(r0.EffectId, RenDeEffectId, StringComparison.Ordinal))
            return;

        AuthoritativeSideState side = state.Sides[seatIndex];
        side.CurrentHp = Math.Min(side.MaxHp, side.CurrentHp + 1);
    }

    /// <summary>在弃牌堆调整完毕后、回合结束前调用（对应「弃牌阶段结束时」）。</summary>
    public static void OnDiscardPhaseEndRenZheWuDi(AuthoritativeBattleState state, ISkillRuleLookup? rules)
    {
        if (rules == null)
            return;

        AuthoritativeSideState side = state.ActiveSide;
        if (!TryFaceUpLiuBei(side, out _))
            return;

        SkillRuleDefinition? r1 = rules.GetRule(LiuBeiCardId, 1);
        if (r1 == null || !string.Equals(r1.EffectId, RenZheWuDiEffectId, StringComparison.Ordinal))
            return;

        int drawCount = Math.Max(1, r1.Value1);
        int amount = Math.Max(1, r1.Value2);
        if (side.Deck.Count <= 0)
            return;

        for (int i = 0; i < drawCount; i++)
        {
            if (side.Deck.Count <= 0)
                break;
            AuthoritativePokerCard card = side.Deck[^1];
            side.Deck.RemoveAt(side.Deck.Count - 1);
            side.Hand.Add(card);
        }

        AuthoritativePokerCard shown = side.Hand[^1];
        if (IsRedSuit(shown.Suit))
            side.CurrentHp = Math.Min(side.MaxHp, side.CurrentHp + amount);
        else
        {
            AuthoritativeSideState opp = state.InactiveSide;
            opp.CurrentHp = Math.Max(0, opp.CurrentHp - amount);
        }
    }

    private static bool TryFaceUpLiuBei(AuthoritativeSideState side, out int generalIndex)
    {
        for (int i = 0; i < side.GeneralCardIds.Count; i++)
        {
            if (i >= side.GeneralFaceUp.Count || !side.GeneralFaceUp[i])
                continue;

            if (string.Equals(side.GeneralCardIds[i], LiuBeiCardId, StringComparison.Ordinal))
            {
                generalIndex = i;
                return true;
            }
        }

        generalIndex = -1;
        return false;
    }

    private static bool IsRedSuit(string? suit) =>
        suit == "\u7ea2\u6843" || suit == "\u65b9\u7247";
}
