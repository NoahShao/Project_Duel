using ProjectDuel.Shared.Config;

namespace ProjectDuel.Shared.Rules;

/// <summary>
/// 「手牌数变为 0」同节点多条强制技：在快照时刻收集全部满足条件的技能，按稳定顺序（场上位次）依次执行，
/// 避免先执行 A 摸牌后手牌非空导致 B 无法结算的问题。
/// 须在行动方将打出区牌移入弃牌堆、且本段结算效果已应用后再调用（与 <see cref="AuthoritativeBattleEngine.FinishPlayPhase"/> 对齐），
/// 不得在牌仍停留在打出区时因手牌暂时为 0 而调用。
/// </summary>
public static class HandEmptyTriggerHooks
{
    public const string EmptyHandDrawTwoOncePerTurnEffectId = "empty_hand_draw_two_once_per_turn";

    public static void TryResolveAfterHandBecameZero(AuthoritativeBattleState state, int seatIndex, ISkillRuleLookup? rules)
    {
        if (state == null || rules == null || seatIndex < 0 || seatIndex >= state.Sides.Length)
            return;

        AuthoritativeSideState side = state.Sides[seatIndex];
        if (side.Hand.Count != 0)
            return;

        var entries = new List<(int generalIndex, int skillIndex, string cardId, int drawCount)>();
        for (int gi = 0; gi < side.GeneralCardIds.Count; gi++)
        {
            if (gi >= side.GeneralFaceUp.Count || !side.GeneralFaceUp[gi])
                continue;

            string cid = side.GeneralCardIds[gi] ?? string.Empty;
            for (int sk = 0; sk < 3; sk++)
            {
                SkillRuleDefinition? rule = rules.GetRule(cid, sk);
                if (rule == null || !string.Equals(rule.EffectId, EmptyHandDrawTwoOncePerTurnEffectId, StringComparison.Ordinal))
                    continue;

                string skillKey = $"{cid}_{sk}";
                if (side.TriggeredSkillKeysThisTurn.Contains(skillKey))
                    continue;

                int draw = Math.Max(1, rule.Value1);
                entries.Add((gi, sk, cid, draw));
            }
        }

        if (entries.Count == 0)
            return;

        entries.Sort((a, b) =>
        {
            int c = a.generalIndex.CompareTo(b.generalIndex);
            return c != 0 ? c : a.skillIndex.CompareTo(b.skillIndex);
        });

        for (int i = 0; i < entries.Count; i++)
        {
            (int gi, int sk, string cid, int draw) = entries[i];
            string skillKey = $"{cid}_{sk}";
            if (side.TriggeredSkillKeysThisTurn.Contains(skillKey))
                continue;

            side.TriggeredSkillKeysThisTurn.Add(skillKey);
            Draw(side, draw);
        }
    }

    private static void Draw(AuthoritativeSideState side, int count)
    {
        while (count > 0 && side.Deck.Count > 0)
        {
            var card = side.Deck[^1];
            side.Deck.RemoveAt(side.Deck.Count - 1);
            side.Hand.Add(card);
            count--;
        }
    }
}
