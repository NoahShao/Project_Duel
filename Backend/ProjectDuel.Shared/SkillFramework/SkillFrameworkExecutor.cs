using ProjectDuel.Shared.Rules;

namespace ProjectDuel.Shared.SkillFramework;

/// <summary>
/// 权威服解释执行技能框架数据（当前联机流程主要使用攻击牌型表）。
/// </summary>
public static class SkillFrameworkExecutor
{
    /// <summary>
    /// 按 SkillKey 匹配攻击牌型表，写入 <see cref="AuthoritativeBattleState"/> 的待结算字段。匹配成功返回 true。
    /// </summary>
    public static bool TryApplyAttackSkillFromRegistry(AuthoritativeBattleState state, string skillKey, IReadOnlyList<AuthoritativePokerCard> playedCards)
    {
        if (state == null || string.IsNullOrWhiteSpace(skillKey) || playedCards == null || playedCards.Count == 0)
            return false;

        if (!SkillFrameworkRegistry.TryGet(skillKey, out SkillDefinition? def) || def?.AttackPatterns == null || def.AttackPatterns.Count == 0)
            return false;

        int attackerSeat = state.ActiveSeatIndex;
        var shape = SkillFrameworkCardAnalysis.Analyze(state, attackerSeat, playedCards);
        foreach (AttackPatternRow row in def.AttackPatterns)
        {
            if (row.Kind == AttackPatternKind.None)
                continue;

            if (!RowMatches(row, shape, state, attackerSeat, playedCards))
                continue;

            ApplyAttackPatternRow(state, row);
            return true;
        }

        return false;
    }

    public static void DispatchFrameworkEvent(AuthoritativeBattleState state, int sideSeatIndex, SkillFrameworkEventKind evt)
    {
        if (state == null || evt == SkillFrameworkEventKind.None || sideSeatIndex < 0 || sideSeatIndex >= state.Sides.Length)
            return;

        AuthoritativeSideState side = state.Sides[sideSeatIndex];
        for (int gi = 0; gi < side.GeneralCardIds.Count; gi++)
        {
            if (gi >= side.GeneralFaceUp.Count || !side.GeneralFaceUp[gi])
                continue;

            string cardId = side.GeneralCardIds[gi] ?? string.Empty;
            for (int si = 0; si < 3; si++)
            {
                string key = $"{cardId}_{si}";
                if (!SkillFrameworkRegistry.TryGet(key, out SkillDefinition? def) || def?.Triggers == null)
                    continue;

                foreach (SkillTriggerBlock block in def.Triggers)
                {
                    if (block.Event != evt)
                        continue;

                    TryRunTriggerBlock(state, sideSeatIndex, key, block);
                }
            }
        }
    }

    private static bool TryRunTriggerBlock(AuthoritativeBattleState state, int sideSeatIndex, string skillKey, SkillTriggerBlock block)
    {
        AuthoritativeSideState side = state.Sides[sideSeatIndex];
        if (block.LimitPerTurn > 0 && side.TriggeredSkillKeysThisTurn.Contains(skillKey))
            return false;

        if (block.Steps == null || block.Steps.Count == 0)
            return false;

        foreach (SkillEffectStep step in block.Steps)
            ApplyEffectStep(state, sideSeatIndex, skillKey, step);

        if (block.LimitPerTurn > 0)
            side.TriggeredSkillKeysThisTurn.Add(skillKey);

        return true;
    }

    private static void ApplyEffectStep(AuthoritativeBattleState state, int sideSeatIndex, string skillKey, SkillEffectStep step)
    {
        if (step.Op == SkillFrameworkEffectOp.None)
            return;

        AuthoritativeSideState side = state.Sides[sideSeatIndex];
        switch (step.Op)
        {
            case SkillFrameworkEffectOp.GainMorale:
                side.Morale = Math.Min(side.MoraleCap, side.Morale + Math.Max(1, step.I0));
                break;

            case SkillFrameworkEffectOp.AddMoraleCap:
                side.MoraleCap = Math.Max(AuthoritativeBattleState.DefaultMoraleCap, side.MoraleCap + Math.Max(1, step.I0));
                if (side.Morale > side.MoraleCap)
                    side.Morale = side.MoraleCap;
                break;

            case SkillFrameworkEffectOp.HealSelf:
                side.CurrentHp = Math.Min(side.MaxHp, side.CurrentHp + Math.Max(1, step.I0));
                break;

            case SkillFrameworkEffectOp.DrawSelf:
                Draw(side, Math.Max(1, step.I0));
                break;

            case SkillFrameworkEffectOp.AddEffectLayerSelf:
                if (!string.IsNullOrWhiteSpace(step.S0))
                    AddEffectLayers(side, step.S0, Math.Max(1, step.I0));
                break;

            case SkillFrameworkEffectOp.RemoveEffectLayersAnySelf:
                RemoveAnyEffectLayers(side, Math.Max(1, step.I0));
                break;

            case SkillFrameworkEffectOp.AddExtraPlayPhases:
                state.TotalPlayPhasesThisTurn += Math.Max(1, step.I0);
                break;

            case SkillFrameworkEffectOp.SetPendingBaseDamage:
                state.PendingBaseDamage = Math.Max(0, step.I0);
                break;

            case SkillFrameworkEffectOp.AddPendingBonusDamage:
                state.PendingAttackBonus += Math.Max(0, step.I0);
                break;

            case SkillFrameworkEffectOp.SetIgnoreDefense:
                if (step.I0 != 0)
                    state.PendingIgnoreDefenseReduction = true;
                break;

            case SkillFrameworkEffectOp.AddPostResolveDraw:
                state.PendingPostResolveDrawToAttacker += Math.Max(0, step.I0);
                break;

            case SkillFrameworkEffectOp.AddPostResolveHeal:
                state.PendingPostResolveHealToAttacker += Math.Max(0, step.I0);
                break;

            case SkillFrameworkEffectOp.AddPostResolveMorale:
                state.PendingPostResolveMoraleToAttacker += Math.Max(0, step.I0);
                break;

            case SkillFrameworkEffectOp.MarkTriggeredThisTurn:
                side.TriggeredSkillKeysThisTurn.Add(skillKey);
                break;

            case SkillFrameworkEffectOp.AppendCombatNote:
            default:
                break;
        }
    }

    private static void ApplyAttackPatternRow(AuthoritativeBattleState state, AttackPatternRow row)
    {
        state.PendingBaseDamage = Math.Max(1, row.BaseDamage);
        if (row.Unblockable)
            state.PendingIgnoreDefenseReduction = true;

        state.TotalPlayPhasesThisTurn += Math.Max(0, row.ExtraPlayPhases);
        state.PendingPostResolveDrawToAttacker += Math.Max(0, row.PostDraw);
        state.PendingPostResolveHealToAttacker += Math.Max(0, row.PostHeal);
        state.PendingPostResolveMoraleToAttacker += Math.Max(0, row.PostMorale);
    }

    private static bool RowMatches(AttackPatternRow row, SkillFrameworkCardAnalysis.PlayedHandShape shape, AuthoritativeBattleState state, int attackerSeat, IReadOnlyList<AuthoritativePokerCard> cards)
    {
        if (row.RequireAllRed && !shape.AllRed)
            return false;
        if (row.RequireAllBlack && !shape.AllBlack)
            return false;

        int minStraight = Math.Max(3, row.MinStraightLength);

        switch (row.Kind)
        {
            case AttackPatternKind.SingleCard:
                if (shape.CardCount != 1)
                    return false;
                if (row.MinEffectiveRankExclusive > 0)
                {
                    int r = SkillFrameworkCardAnalysis.GetEffectiveRank(state, attackerSeat, cards[0]);
                    if (r <= row.MinEffectiveRankExclusive)
                        return false;
                }

                return true;

            case AttackPatternKind.Pair:
                return shape.IsPair;

            case AttackPatternKind.TwoPair:
                return shape.IsTwoPair;

            case AttackPatternKind.Triple:
                return shape.IsTriple;

            case AttackPatternKind.FourOfAKind:
                return shape.IsFourOfAKind;

            case AttackPatternKind.FullHouse:
                return shape.IsFullHouse;

            case AttackPatternKind.Straight:
                if (shape.StraightLength < minStraight || shape.StraightLength != shape.CardCount)
                    return false;
                if (row.RequireNotFlush && shape.IsFlush)
                    return false;
                return true;

            case AttackPatternKind.StraightFlush:
                if (shape.StraightLength < minStraight || shape.StraightLength != shape.CardCount)
                    return false;
                return shape.IsFlush;

            default:
                return false;
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

    private static void AddEffectLayers(AuthoritativeSideState side, string effectKey, int count)
    {
        if (string.IsNullOrWhiteSpace(effectKey) || count <= 0)
            return;

        side.EffectLayers.TryGetValue(effectKey, out int current);
        side.EffectLayers[effectKey] = Math.Max(0, current) + count;
    }

    private static int RemoveAnyEffectLayers(AuthoritativeSideState side, int maxCount)
    {
        if (maxCount <= 0 || side.EffectLayers.Count == 0)
            return 0;

        var keys = side.EffectLayers.Keys.ToList();
        int removed = 0;
        for (int i = 0; i < keys.Count && removed < maxCount; i++)
        {
            string key = keys[i];
            if (!side.EffectLayers.TryGetValue(key, out int current) || current <= 0)
                continue;

            int take = Math.Min(current, maxCount - removed);
            current -= take;
            removed += take;
            if (current <= 0)
                side.EffectLayers.Remove(key);
            else
                side.EffectLayers[key] = current;
        }

        return removed;
    }
}
