using System.Collections.Generic;
using UnityEngine;

namespace JunzhenDuijue
{
    /// <summary>
    /// 解释执行 <see cref="SkillDefinition"/>：攻击技牌型表、被动时机效果链。
    /// 攻击技：在 <see cref="OfflineSkillEngine.ConfigureAttackSkill"/> 中优先于手写 switch 调用。
    /// </summary>
    public static class SkillFrameworkExecutor
    {
        /// <summary>
        /// 若注册表中有该 SkillKey 且定义了 <see cref="SkillDefinition.AttackPatterns"/>，
        /// 按行顺序匹配第一条规则并写入 <see cref="BattleState"/> 的待结算字段；匹配成功返回 true。
        /// </summary>
        public static bool TryApplyAttackSkillFromRegistry(BattleState state, bool attackerIsPlayer, string skillKey, List<PokerCard> playedCards)
        {
            if (state == null || string.IsNullOrWhiteSpace(skillKey))
                return false;

            SkillFrameworkRegistry.EnsureLoaded();
            if (!SkillFrameworkRegistry.TryGet(skillKey, out SkillDefinition def))
                return false;

            if (def.AttackPatterns == null || def.AttackPatterns.Count == 0)
                return false;

            if (playedCards == null || playedCards.Count == 0)
                return false;

            var shape = SkillFrameworkCardAnalysis.Analyze(state, attackerIsPlayer, playedCards);
            for (int i = 0; i < def.AttackPatterns.Count; i++)
            {
                AttackPatternRow row = def.AttackPatterns[i];
                if (row == null || row.Kind == AttackPatternKind.None)
                    continue;

                if (!RowMatches(row, shape, state, attackerIsPlayer, playedCards))
                    continue;

                ApplyAttackPatternRow(state, row);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 对场上该方所有翻面武将的技能扫描一遍，执行与 <paramref name="evt"/> 匹配的触发块（用于新卡或迁移后的被动）。
        /// 注意：若同一技能仍在 OfflineSkillEngine 里写死同类时机，可能重复触发，迁移时请二选一。
        /// </summary>
        public static void DispatchFrameworkEvent(BattleState state, bool sideIsPlayer, SkillFrameworkEventKind evt)
        {
            if (state == null || evt == SkillFrameworkEventKind.None)
                return;

            SkillFrameworkRegistry.EnsureLoaded();
            SideState side = state.GetSide(sideIsPlayer);
            for (int gi = 0; gi < side.GeneralCardIds.Count; gi++)
            {
                if (!side.IsGeneralFaceUp(gi))
                    continue;

                string cardId = side.GeneralCardIds[gi];
                for (int si = 0; si < 3; si++)
                {
                    string key = SkillRuleHelper.MakeSkillKey(cardId, si);
                    if (!SkillFrameworkRegistry.TryGet(key, out SkillDefinition def) || def.Triggers == null)
                        continue;

                    for (int t = 0; t < def.Triggers.Count; t++)
                    {
                        SkillTriggerBlock block = def.Triggers[t];
                        if (block == null || block.Event != evt)
                            continue;

                        TryRunTriggerBlock(state, sideIsPlayer, key, block);
                    }
                }
            }
        }

        private static bool TryRunTriggerBlock(BattleState state, bool sideIsPlayer, string skillKey, SkillTriggerBlock block)
        {
            SideState side = state.GetSide(sideIsPlayer);
            if (block.LimitPerTurn > 0 && side.TriggeredSkillKeysThisTurn.Contains(skillKey))
                return false;

            if (block.Steps == null || block.Steps.Count == 0)
                return false;

            for (int i = 0; i < block.Steps.Count; i++)
                ApplyEffectStep(state, sideIsPlayer, skillKey, block.Steps[i]);

            if (block.LimitPerTurn > 0)
                side.TriggeredSkillKeysThisTurn.Add(skillKey);

            if (!BattleAttackPreview.SuppressSkillBanners && SkillRuleHelper.TryParseSkillKey(skillKey, out string cid, out int si))
            {
                SkillRuleEntry rl = SkillRuleLoader.GetRule(cid, si);
                string skName = rl != null && !string.IsNullOrWhiteSpace(rl.SkillName) ? rl.SkillName : "\u6280\u80fd";
                SkillEffectBanner.Show(sideIsPlayer, false, SkillEffectBanner.GetRoleNameFromCardId(cid), skName, "\u6548\u679c\u5df2\u751f\u6548");
            }

            return true;
        }

        private static void ApplyEffectStep(BattleState state, bool sideIsPlayer, string skillKey, SkillEffectStep step)
        {
            if (step == null || step.Op == SkillFrameworkEffectOp.None)
                return;

            SideState side = state.GetSide(sideIsPlayer);
            switch (step.Op)
            {
                case SkillFrameworkEffectOp.GainMorale:
                    side.Morale = Mathf.Min(side.MoraleCap, side.Morale + Mathf.Max(1, step.I0));
                    break;

                case SkillFrameworkEffectOp.AddMoraleCap:
                    side.MoraleCap = Mathf.Max(0, side.MoraleCap + Mathf.Max(1, step.I0));
                    if (side.Morale > side.MoraleCap)
                        side.Morale = side.MoraleCap;
                    break;

                case SkillFrameworkEffectOp.HealSelf:
                {
                    int heal = Mathf.Max(1, step.I0);
                    side.CurrentHp = Mathf.Min(side.MaxHp, side.CurrentHp + heal);
                    break;
                }

                case SkillFrameworkEffectOp.DrawSelf:
                    BattleState.Draw(side, Mathf.Max(1, step.I0));
                    break;

                case SkillFrameworkEffectOp.AddEffectLayerSelf:
                    if (!string.IsNullOrWhiteSpace(step.S0))
                        side.AddEffectLayers(step.S0, Mathf.Max(1, step.I0));
                    break;

                case SkillFrameworkEffectOp.RemoveEffectLayersAnySelf:
                    side.RemoveAnyEffectLayers(Mathf.Max(1, step.I0));
                    break;

                case SkillFrameworkEffectOp.AddExtraPlayPhases:
                    state.TotalPlayPhasesThisTurn += Mathf.Max(1, step.I0);
                    break;

                case SkillFrameworkEffectOp.SetPendingBaseDamage:
                    state.PendingBaseDamage = Mathf.Max(0, step.I0);
                    state.PendingDamageCategory = DamageCategory.Generic;
                    state.PendingDamageElement = DamageElement.None;
                    break;

                case SkillFrameworkEffectOp.AddPendingBonusDamage:
                    state.PendingAttackBonus += Mathf.Max(0, step.I0);
                    break;

                case SkillFrameworkEffectOp.SetIgnoreDefense:
                    if (step.I0 != 0)
                        state.PendingIgnoreDefenseReduction = true;
                    break;

                case SkillFrameworkEffectOp.AddPostResolveDraw:
                    state.PendingPostResolveDrawToAttacker += Mathf.Max(0, step.I0);
                    break;

                case SkillFrameworkEffectOp.AddPostResolveHeal:
                    state.PendingPostResolveHealToAttacker += Mathf.Max(0, step.I0);
                    break;

                case SkillFrameworkEffectOp.AddPostResolveMorale:
                    state.PendingPostResolveMoraleToAttacker += Mathf.Max(0, step.I0);
                    break;

                case SkillFrameworkEffectOp.AppendCombatNote:
                    if (!string.IsNullOrWhiteSpace(step.S0))
                        AppendCombatNote(state, step.S0);
                    break;

                case SkillFrameworkEffectOp.MarkTriggeredThisTurn:
                    side.TriggeredSkillKeysThisTurn.Add(skillKey);
                    break;
            }
        }

        private static void ApplyAttackPatternRow(BattleState state, AttackPatternRow row)
        {
            state.PendingBaseDamage = Mathf.Max(1, row.BaseDamage);
            DamageCategory cat = row.DamageCategory;
            if (cat == DamageCategory.None)
                cat = DamageCategory.Generic;
            state.PendingDamageCategory = cat;
            state.PendingDamageElement = cat == DamageCategory.Attribute ? row.DamageElement : DamageElement.None;
            if (row.Unblockable)
                state.PendingIgnoreDefenseReduction = true;

            state.PendingExtraPlayPhasesToGrant += Mathf.Max(0, row.ExtraPlayPhases);
            state.PendingPostResolveDrawToAttacker += Mathf.Max(0, row.PostDraw);
            state.PendingPostResolveHealToAttacker += Mathf.Max(0, row.PostHeal);
            state.PendingPostResolveMoraleToAttacker += Mathf.Max(0, row.PostMorale);

            if (!string.IsNullOrWhiteSpace(row.Note))
                AppendCombatNote(state, row.Note);
        }

        private static bool RowMatches(AttackPatternRow row, SkillFrameworkCardAnalysis.PlayedHandShape shape, BattleState state, bool sideIsPlayer, List<PokerCard> cards)
        {
            if (row.RequireAllRed && !shape.AllRed)
                return false;
            if (row.RequireAllBlack && !shape.AllBlack)
                return false;

            int minStraight = Mathf.Max(3, row.MinStraightLength);

            switch (row.Kind)
            {
                case AttackPatternKind.SingleCard:
                    if (shape.CardCount != 1)
                        return false;
                    if (row.ExcludeFaceCourtWithoutChaShiTen &&
                        PokerPatternRules.IsFaceCourtCard(cards[0]) &&
                        !cards[0].ChaShiCourtPlayedAsTen)
                        return false;
                    if (row.MinEffectiveRankExclusive > 0)
                    {
                        int r = PokerPatternRules.GetRankForAttackThreshold(cards[0]);
                        if (r <= row.MinEffectiveRankExclusive)
                            return false;
                    }

                    if (row.MaxEffectiveRankExclusive > 0)
                    {
                        int rMax = PokerPatternRules.GetRankForAttackThreshold(cards[0]);
                        if (rMax >= row.MaxEffectiveRankExclusive)
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
                    // 同花顺也是顺子；仅当表行要求「纯顺子」时排除同花
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

        private static void AppendCombatNote(BattleState state, string note)
        {
            if (state == null || string.IsNullOrWhiteSpace(note))
                return;

            if (string.IsNullOrWhiteSpace(state.PendingCombatNote))
                state.PendingCombatNote = note;
            else
                state.PendingCombatNote += "\n" + note;
        }
    }
}
