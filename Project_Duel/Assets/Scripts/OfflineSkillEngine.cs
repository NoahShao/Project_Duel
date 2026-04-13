using System;
using System.Collections.Generic;
using UnityEngine;

namespace JunzhenDuijue
{
    public static class OfflineSkillEngine
    {
        public const string ResistEffectKey = "\u62b5\u5fa1";

        private const string StartGameEffectId = "start_game_gain_morale_and_max";
        private const string EmptyHandDrawEffectId = "empty_hand_draw_two_once_per_turn";
        private const string PlayPhaseStartResistEffectId = "play_phase_start_pay_morale_gain_resist";
        private const string ManualPrimaryEffectId = "manual_primary_effect";
        private const string DiscardEndRenZheWuDiEffectId = "discard_end_draw_reveal_red_heal_black_damage";

        private const string Hearts = "\u7ea2\u6843";
        private const string Diamonds = "\u65b9\u7247";
        private const string Spades = "\u9ed1\u6843";
        private const string Clubs = "\u6885\u82b1";

        public static void RefreshContinuousState(BattleState state)
        {
            if (state == null)
                return;

            bool init = state.InitiativeSideIsPlayer;
            RefreshSideContinuousState(state, init);
            RefreshSideContinuousState(state, !init);
        }

        /// <summary>
        /// 在战报/横幅节点上结算一条「游戏开始时」士气技能（上限与恢复与点击顺序一致）；重复调用同一槽位会被忽略。
        /// </summary>
        public static void ApplyResolvedGameStartMoraleSkill(BattleState state, GameStartSkillLineEntry entry)
        {
            if (state == null)
                return;

            var side = state.GetSide(entry.SideIsPlayer);
            if (entry.GeneralIndex < 0 || entry.GeneralIndex >= side.GeneralCardIds.Count || !side.IsGeneralFaceUp(entry.GeneralIndex))
                return;

            string cardId = side.GeneralCardIds[entry.GeneralIndex];
            var rule = SkillRuleLoader.GetRule(cardId, entry.SkillIndex);
            if (rule == null || rule.EffectId != StartGameEffectId)
                return;

            var key = (entry.SideIsPlayer, entry.GeneralIndex, entry.SkillIndex);
            if (!state.AppliedGameStartMoraleEffects.Add(key))
                return;

            RefreshSideContinuousState(state, entry.SideIsPlayer);
            int v1 = Mathf.Max(1, rule.Value1);
            side.Morale = Mathf.Min(side.MoraleCap, side.Morale + v1);
        }

        /// <summary>
        /// 【仁德】消耗士气后：若场上翻面武将含刘备且规则为 start_game_gain_morale_and_max，则回复 1 点生命。
        /// </summary>
        public static void ApplyRenDeWhenMoraleSpent(BattleState state, bool sideIsPlayer)
        {
            if (state == null)
                return;

            if (!TryFindFaceUpRule(state, sideIsPlayer, e => string.Equals(e.CardId, "NO001", StringComparison.Ordinal) && e.SkillIndex == 0 && string.Equals(e.EffectId, StartGameEffectId, StringComparison.Ordinal), out _, out _, out string cardIdRd, out SkillRuleEntry ruleRd))
                return;

            var side = state.GetSide(sideIsPlayer);
            side.CurrentHp = Mathf.Min(side.MaxHp, side.CurrentHp + 1);

            SkillEffectBanner.Show(sideIsPlayer, false, SkillEffectBanner.GetRoleNameFromCardId(cardIdRd), ruleRd.SkillName, "\u6062\u590d1\u70b9\u751f\u547d");

            string turn = state.IsPlayerTurn ? "\u3010\u5df1\u65b9\u56de\u5408\u3011" : "\u3010\u654c\u65b9\u56de\u5408\u3011";
            string phaseCn = BattlePhaseDisplay.ToChinese(state.CurrentPhase);
            string playerActor = state.IsPlayerTurn ? "\u5df1\u65b9\u73a9\u5bb6" : "\u654c\u65b9\u73a9\u5bb6";
            string camp = state.IsPlayerTurn ? "\u5df1\u65b9" : "\u654c\u65b9";
            string line = turn + phaseCn + "\uff0c" + playerActor + "\u56e0\u4e3a\u6d88\u8017\u58eb\u6c14\u89e6\u53d1" + camp
                + "\u89d2\u8272\u3010\u5218\u5907\u3011\u7684\u6280\u80fd\u3010\u4ec1\u5fb7\u3011\uff0c\u6062\u590d\u4e861\u70b9\u751f\u547d\u3002";
            BattleFlowLog.Add(line);
        }

        /// <summary>
        /// 【仁者无敌】弃牌阶段结束时：摸 Value1 张，展示最后一张；红色回复 Value2 生命，黑色对敌方造成 Value2 点通用伤害。
        /// </summary>
        public static bool TryApplyDiscardEndRenZheWuDi(BattleState state, bool sideIsPlayer, out string message)
        {
            message = string.Empty;
            if (state == null)
                return false;

            if (!TryFindFaceUpRule(state, sideIsPlayer, e => string.Equals(e.EffectId, DiscardEndRenZheWuDiEffectId, StringComparison.Ordinal), out _, out _, out string cardIdRz, out SkillRuleEntry rule))
                return false;

            var side = state.GetSide(sideIsPlayer);
            int drawCount = Mathf.Max(1, rule.Value1);
            int amount = Mathf.Max(1, rule.Value2);
            if (side.Deck.Count <= 0 && side.DiscardPile.Count <= 0)
            {
                message = "\u3010\u4ec1\u8005\u65e0\u654c\u3011\u724c\u5e93\u4e0e\u5f03\u724c\u5806\u7686\u7a7a\uff0c\u65e0\u6cd5\u6478\u724c";
                return true;
            }

            int beforeHand = side.Hand.Count;
            int drawn = BattleState.Draw(side, drawCount);
            if (drawn <= 0 || side.Hand.Count <= beforeHand)
            {
                message = "\u3010\u4ec1\u8005\u65e0\u654c\u3011\u65e0\u6cd5\u5b8c\u6210\u6478\u724c";
                return true;
            }

            PokerCard shown = side.Hand[side.Hand.Count - 1];
            string outcome;
            if (IsRed(shown))
            {
                side.CurrentHp = Mathf.Min(side.MaxHp, side.CurrentHp + amount);
                message = "\u3010\u4ec1\u8005\u65e0\u654c\u3011\u5c55\u793a" + shown.DisplayName + "\uff08\u7ea2\u8272\uff09\uff0c\u56de\u590d" + amount + "\u70b9\u751f\u547d";
                outcome = "\u5c55\u793a" + shown.DisplayName + "\uff08\u7ea2\u8272\uff09\uff0c\u56de\u590d" + amount + "\u70b9\u751f\u547d";
            }
            else
            {
                var opp = state.GetSide(!sideIsPlayer);
                opp.CurrentHp = Mathf.Max(0, opp.CurrentHp - amount);
                message = "\u3010\u4ec1\u8005\u65e0\u654c\u3011\u5c55\u793a" + shown.DisplayName + "\uff08\u9ed1\u8272\uff09\uff0c\u5bf9\u654c\u65b9\u9020\u6210" + amount + "\u70b9\u901a\u7528\u4f24\u5bb3";
                outcome = "\u5c55\u793a" + shown.DisplayName + "\uff08\u9ed1\u8272\uff09\uff0c\u5bf9\u654c\u65b9\u9020\u6210" + amount + "\u70b9\u901a\u7528\u4f24\u5bb3";
            }

            SkillEffectBanner.Show(sideIsPlayer, false, SkillEffectBanner.GetRoleNameFromCardId(cardIdRz), rule.SkillName, outcome);
            return true;
        }

        public static bool CanOfferPlayPhaseStartResist(BattleState state, bool sideIsPlayer, out int generalIndex, out int skillIndex, out SkillRuleEntry rule)
        {
            generalIndex = -1;
            skillIndex = -1;
            rule = null;
            if (state == null)
                return false;

            if (!TryFindFaceUpRule(state, sideIsPlayer, entry => entry.EffectId == PlayPhaseStartResistEffectId, out generalIndex, out skillIndex, out _, out rule))
                return false;

            var side = state.GetSide(sideIsPlayer);
            int cost = Mathf.Max(1, rule.Value1);
            return side.Morale >= cost;
        }

        public static string ApplyPlayPhaseStartResist(BattleState state, bool sideIsPlayer, SkillRuleEntry rule)
        {
            if (state == null || rule == null)
                return string.Empty;

            var side = state.GetSide(sideIsPlayer);
            int cost = Mathf.Max(1, rule.Value1);
            int layers = Mathf.Max(1, rule.Value2);
            if (side.Morale < cost)
                return "\u58eb\u6c14\u4e0d\u8db3";

            side.Morale -= cost;
            side.AddEffectLayers(ResistEffectKey, layers);
            string outcome = "\u51cf\u5c11" + cost + "\u70b9\u58eb\u6c14\uff0c\u83b7\u5f97" + layers + "\u5c42\u201c\u62b5\u5fa1\u201d";
            SkillEffectBanner.Show(sideIsPlayer, true, SkillEffectBanner.GetRoleNameFromCardId(rule.CardId), rule.SkillName, outcome);
            return "\u53d1\u52a8\u3010\u636e\u5b88\u3011\uff0c" + outcome;
        }

        public static bool HasRemovableDefenseBuff(BattleState state, bool sideIsPlayer)
        {
            return state != null && state.GetSide(sideIsPlayer).GetEffectLayerCount(ResistEffectKey) > 0;
        }

        public static string ConsumeOneDefenseBuff(BattleState state, bool sideIsPlayer)
        {
            if (state == null)
                return string.Empty;

            var side = state.GetSide(sideIsPlayer);
            if (side.RemoveEffectLayers(ResistEffectKey, 1) <= 0)
                return string.Empty;

            state.PendingDefenseReduction += 1;
            return "\u79fb\u96641\u5c42\u201c\u62b5\u5fa1\u201d\uff0c\u672c\u6b21\u4f24\u5bb3-1";
        }

        public static bool TryTriggerHandEmptyPassive(BattleState state, bool sideIsPlayer, out string message)
        {
            message = string.Empty;
            if (state == null)
                return false;

            var side = state.GetSide(sideIsPlayer);
            if (side.Hand.Count != 0)
                return false;

            if (!TryFindFaceUpRule(state, sideIsPlayer, entry => entry.EffectId == EmptyHandDrawEffectId, out _, out int skillIndex, out string cardId, out SkillRuleEntry rule))
                return false;

            string skillKey = SkillRuleHelper.MakeSkillKey(cardId, skillIndex);
            if (side.TriggeredSkillKeysThisTurn.Contains(skillKey))
                return false;

            side.TriggeredSkillKeysThisTurn.Add(skillKey);
            int drawCount = Mathf.Max(1, rule.Value1);
            BattleState.Draw(side, drawCount);
            message = "\u89e6\u53d1\u3010\u96c4\u624d\u5927\u7565\u3011\uff0c\u6478" + drawCount + "\u5f20\u724c";
            SkillEffectBanner.Show(sideIsPlayer, false, SkillEffectBanner.GetRoleNameFromCardId(cardId), rule.SkillName, "\u6478" + drawCount + "\u5f20\u724c");
            return true;
        }

        public static bool TryActivatePrimarySkill(BattleState state, bool sideIsPlayer, int generalIndex, int skillIndex, out string message)
        {
            message = string.Empty;
            if (!TryGetFaceUpRule(state, sideIsPlayer, generalIndex, skillIndex, out string cardId, out SkillRuleEntry rule))
            {
                message = "\u672a\u627e\u5230\u6280\u80fd\u89c4\u5219";
                return false;
            }

            var side = state.GetSide(sideIsPlayer);
            switch (rule.EffectId)
            {
                case ManualPrimaryEffectId:
                    if (!TryActivateGenericPrimarySkill(state, side, rule, out message))
                        return false;
                    SkillEffectBanner.Show(sideIsPlayer, true, SkillEffectBanner.GetRoleNameFromCardId(cardId), rule.SkillName, message);
                    return true;

                default:
                    message = "\u8be5\u4e3b\u52a8\u6280\u79bb\u7ebf\u6548\u679c\u5c1a\u672a\u5b9e\u73b0";
                    return false;
            }
        }

        /// <summary>【策马斩将】两张红色单牌：打出区中至少含任意 2 张红色牌即可选用该分支。</summary>
        public static bool CeMaTwoRedSinglesMatches(List<PokerCard> cards) =>
            cards != null && CountRedCards(cards) >= 2;

        /// <summary>【策马斩将】红色顺子：4 张均为红色且点数连续（A 可作 1 或 14）。</summary>
        public static bool CeMaRedStraightMatches(List<PokerCard> cards) =>
            cards != null && cards.Count == 4 && ContainsOnlyRedCards(cards) && PokerPatternRules.IsFlexibleStraight(cards, 4);

        /// <summary>【策马斩将】红色同花顺：在红色顺子基础上同花色。</summary>
        public static bool CeMaRedStraightFlushMatches(List<PokerCard> cards) =>
            CeMaRedStraightMatches(cards) && PokerPatternRules.IsFlush(cards);

        /// <summary>对手 AI：在可满足的牌型中选最优（同花顺 &gt; 顺子 &gt; 双红单）。</summary>
        public static void AutoPickCeMaPatternVariant(BattleState state, List<PokerCard> cards)
        {
            if (state == null)
                return;
            if (CeMaRedStraightFlushMatches(cards))
                state.PendingAttackPatternVariant = 2;
            else if (CeMaRedStraightMatches(cards))
                state.PendingAttackPatternVariant = 1;
            else if (CeMaTwoRedSinglesMatches(cards))
                state.PendingAttackPatternVariant = 0;
            else
                state.PendingAttackPatternVariant = -1;
        }

        public static void ConfigureAttackSkill(BattleState state, bool attackerIsPlayer, int generalIndex, int skillIndex)
        {
            if (state == null)
                return;

            state.PendingBaseDamage = 0;
            state.PendingDamageCategory = DamageCategory.None;
            state.PendingDamageElement = DamageElement.None;
            state.PendingAttackBonus = 0;
            state.PendingIgnoreDefenseReduction = false;
            state.PendingPostResolveDrawToAttacker = 0;
            state.PendingPostResolveHealToAttacker = 0;
            state.PendingPostResolveMoraleToAttacker = 0;
            state.PendingExtraPlayPhasesToGrant = 0;
            state.PendingCombatNote = string.Empty;

            if (!TryGetFaceUpRule(state, attackerIsPlayer, generalIndex, skillIndex, out string cardId, out _))
            {
                GenericAttackShapes.ApplyGenericAttack(state, state.ActiveSide.PlayedThisPhase, attackerIsPlayer);
                return;
            }

            string skillKey = SkillRuleHelper.MakeSkillKey(cardId, skillIndex);
            var cards = state.ActiveSide.PlayedThisPhase;
            bool handled = false;

            if (SkillFrameworkExecutor.TryApplyAttackSkillFromRegistry(state, attackerIsPlayer, skillKey, cards))
                handled = true;
            else
            {
                switch (skillKey)
                {
                    case "NO002_0":
                        handled = ConfigureCeMaZhanJiang(state, attackerIsPlayer, cards);
                        break;
                }
            }

            if (!handled)
            {
                GenericAttackShapes.ApplyGenericAttack(state, cards, attackerIsPlayer);
                if (generalIndex >= 0)
                {
                    state.PendingAttackGeneralIndex = -1;
                    state.PendingAttackSkillIndex = -1;
                    state.PendingAttackSkillKind = SelectedSkillKind.GenericAttack;
                    state.PendingAttackSkillName = "\u901a\u7528\u653b\u51fb";
                }
            }

            TryShowAttackDeclareBanner(state, attackerIsPlayer);
        }

        public static void ConfigureDefenseSkill(BattleState state, bool defenderIsPlayer, int generalIndex, int skillIndex)
        {
            if (state == null)
                return;

            if (!TryGetFaceUpRule(state, defenderIsPlayer, generalIndex, skillIndex, out _, out _))
            {
                state.PendingDefenseReduction = Mathf.Max(state.PendingDefenseReduction, 1);
                return;
            }

            state.PendingDefenseReduction = Mathf.Max(state.PendingDefenseReduction, 1);
            AppendCombatNote(state, "\u9632\u5fa1\u6280\u9ed8\u8ba4\u51cf\u4f24+1");
        }

        private static bool TryActivateGenericPrimarySkill(BattleState state, SideState side, SkillRuleEntry rule, out string message)
        {
            message = string.Empty;
            if (rule == null)
                return false;

            if (string.Equals(rule.StringValue1, "draw", StringComparison.OrdinalIgnoreCase))
            {
                int drawCount = Mathf.Max(1, rule.Value1);
                BattleState.Draw(side, drawCount);
                message = "\u6478" + drawCount + "\u5f20\u724c";
                return true;
            }

            if (string.Equals(rule.StringValue1, "heal", StringComparison.OrdinalIgnoreCase))
            {
                int heal = Mathf.Max(1, rule.Value1);
                side.CurrentHp = Mathf.Min(side.MaxHp, side.CurrentHp + heal);
                message = "\u6062\u590d" + heal + "\u70b9\u751f\u547d";
                return true;
            }

            if (string.Equals(rule.StringValue1, "extra_phase", StringComparison.OrdinalIgnoreCase))
            {
                int extraCount = Mathf.Max(1, rule.Value1 == 0 ? 1 : rule.Value1);
                state.TotalPlayPhasesThisTurn += extraCount;
                message = "\u989d\u5916\u83b7\u5f97" + extraCount + "\u4e2a\u51fa\u724c\u9636\u6bb5";
                return true;
            }

            message = "\u8be5\u4e3b\u52a8\u6280\u79bb\u7ebf\u6548\u679c\u5c1a\u672a\u5b9e\u73b0";
            return false;
        }

        private static bool ConfigureCeMaZhanJiang(BattleState state, bool attackerIsPlayer, List<PokerCard> cards)
        {
            int variant = state.PendingAttackPatternVariant;
            if (variant < 0)
                AutoPickCeMaPatternVariant(state, cards);

            variant = state.PendingAttackPatternVariant;
            state.PendingAttackPatternVariant = -1;

            return variant switch
            {
                2 => TryApplyCeMaStraightFlush(state, cards),
                1 => TryApplyCeMaRedStraightOnly(state, cards),
                0 => TryApplyCeMaTwoRedSingles(state, cards),
                _ => false,
            };
        }

        private static void TryShowAttackDeclareBanner(BattleState state, bool attackerIsPlayer)
        {
            if (BattleAttackPreview.SuppressSkillBanners || state == null)
                return;
            if (state.PendingGenericAttackShapeChoicePending)
                return;

            int declared = Mathf.Max(0, state.PendingBaseDamage + state.PendingAttackBonus);
            if (declared <= 0 && state.PendingPostResolveDrawToAttacker <= 0 && state.PendingExtraPlayPhasesToGrant <= 0 && !state.PendingIgnoreDefenseReduction)
                return;

            var sb = new System.Text.StringBuilder();
            sb.Append("\u5df2\u7533\u660e\u653b\u51fb\uff0c\u7ed3\u7b97\u65f6\u5c06\u9020\u6210").Append(declared).Append("\u70b9\u4f24\u5bb3");
            if (state.PendingExtraPlayPhasesToGrant > 0)
                sb.Append("\uff1b\u989d\u5916\u51fa\u724c\u9636\u6bb5+").Append(state.PendingExtraPlayPhasesToGrant);
            if (state.PendingPostResolveDrawToAttacker > 0)
                sb.Append("\uff1b\u7ed3\u7b97\u540e\u6478").Append(state.PendingPostResolveDrawToAttacker).Append("\u5f20");
            if (state.PendingPostResolveHealToAttacker > 0)
                sb.Append("\uff1b\u7ed3\u7b97\u540e\u56de\u590d").Append(state.PendingPostResolveHealToAttacker).Append("\u70b9\u751f\u547d");
            if (state.PendingPostResolveMoraleToAttacker > 0)
                sb.Append("\uff1b\u7ed3\u7b97\u540e\u58eb\u6c14+").Append(state.PendingPostResolveMoraleToAttacker);
            if (state.PendingIgnoreDefenseReduction)
                sb.Append("\uff1b\u672c\u6b21\u4e0d\u53ef\u9632\u5fa1\u51cf\u4f24");

            string outcome = sb.ToString();
            if (state.PendingAttackSkillKind == SelectedSkillKind.GeneralSkill && state.PendingAttackGeneralIndex >= 0)
            {
                if (TryGetFaceUpRule(state, attackerIsPlayer, state.PendingAttackGeneralIndex, state.PendingAttackSkillIndex, out string atkCardId, out SkillRuleEntry atkRule) && atkRule != null)
                    SkillEffectBanner.Show(attackerIsPlayer, true, SkillEffectBanner.GetRoleNameFromCardId(atkCardId), atkRule.SkillName, outcome);
            }
            else if (state.PendingAttackSkillKind == SelectedSkillKind.GenericAttack)
            {
                SkillEffectBanner.Show(attackerIsPlayer, true, "\u901a\u7528", "\u901a\u7528\u653b\u51fb", outcome);
            }
        }

        private static bool TryApplyCeMaTwoRedSingles(BattleState state, List<PokerCard> cards)
        {
            if (!CeMaTwoRedSinglesMatches(cards))
                return false;

            state.PendingBaseDamage = 3;
            state.PendingDamageCategory = DamageCategory.Generic;
            state.PendingDamageElement = DamageElement.None;
            AppendCombatNote(state, "\u3010\u7b56\u9a6c\u65a9\u5c06\u3011\u81f3\u5c112\u5f20\u7ea2\u724c\uff08\u542b\u4e8e\u6253\u51fa\u533a\uff09\uff0c\u4f24\u5bb3\u6539\u4e3a3");
            return true;
        }

        private static bool TryApplyCeMaRedStraightOnly(BattleState state, List<PokerCard> cards)
        {
            if (!CeMaRedStraightMatches(cards))
                return false;

            state.PendingBaseDamage = 6;
            state.PendingDamageCategory = DamageCategory.Generic;
            state.PendingDamageElement = DamageElement.None;
            state.PendingExtraPlayPhasesToGrant += 1;
            AppendCombatNote(state, "\u3010\u7b56\u9a6c\u65a9\u5c06\u3011\u7ea2\u8272\u987a\u5b50\uff0c\u4f24\u5bb3\u6539\u4e3a6\uff0c\u5e76\u4e14\u989d\u5916\u83b7\u5f971\u4e2a\u51fa\u724c\u9636\u6bb5\uff08\u7ed3\u7b97\u65f6\u751f\u6548\uff09");
            return true;
        }

        private static bool TryApplyCeMaStraightFlush(BattleState state, List<PokerCard> cards)
        {
            if (!CeMaRedStraightFlushMatches(cards))
                return false;

            state.PendingBaseDamage = 7;
            state.PendingDamageCategory = DamageCategory.Generic;
            state.PendingDamageElement = DamageElement.None;
            state.PendingIgnoreDefenseReduction = true;
            state.PendingPostResolveDrawToAttacker += 3;
            state.PendingExtraPlayPhasesToGrant += 1;
            AppendCombatNote(state, "\u3010\u7b56\u9a6c\u65a9\u5c06\u3011\u7ea2\u8272\u540c\u82b1\u987a\uff0c7\u70b9\u4e0d\u53ef\u9632\u5fa1\u4f24\u5bb3\uff0c\u5e76\u4e14\u989d\u5916\u83b7\u5f971\u4e2a\u51fa\u724c\u9636\u6bb5\u4e0e\u64783\u5f20\uff08\u7ed3\u7b97\u65f6\u751f\u6548\uff09");
            return true;
        }

        private static void RefreshSideContinuousState(BattleState state, bool sideIsPlayer)
        {
            var side = state.GetSide(sideIsPlayer);
            int moraleCap = BattleState.DefaultMoraleCap;
            for (int generalIndex = 0; generalIndex < side.GeneralCardIds.Count; generalIndex++)
            {
                if (!side.IsGeneralFaceUp(generalIndex))
                    continue;

                string cardId = side.GeneralCardIds[generalIndex];
                for (int skillIndex = 0; skillIndex < 3; skillIndex++)
                {
                    var rule = SkillRuleLoader.GetRule(cardId, skillIndex);
                    if (rule == null || rule.EffectId != StartGameEffectId)
                        continue;

                    if (!state.AppliedGameStartMoraleEffects.Contains((sideIsPlayer, generalIndex, skillIndex)))
                        continue;

                    moraleCap += Mathf.Max(1, rule.Value2);
                }
            }

            side.MoraleCap = Mathf.Max(BattleState.DefaultMoraleCap, moraleCap);
            if (side.Morale > side.MoraleCap)
                side.Morale = side.MoraleCap;
        }

        private static bool TryFindFaceUpRule(BattleState state, bool sideIsPlayer, Func<SkillRuleEntry, bool> predicate, out int generalIndex, out int skillIndex, out string cardId, out SkillRuleEntry rule)
        {
            generalIndex = -1;
            skillIndex = -1;
            cardId = string.Empty;
            rule = null;
            if (state == null || predicate == null)
                return false;

            var side = state.GetSide(sideIsPlayer);
            for (int currentGeneralIndex = 0; currentGeneralIndex < side.GeneralCardIds.Count; currentGeneralIndex++)
            {
                if (!side.IsGeneralFaceUp(currentGeneralIndex))
                    continue;

                string currentCardId = side.GeneralCardIds[currentGeneralIndex];
                for (int currentSkillIndex = 0; currentSkillIndex < 3; currentSkillIndex++)
                {
                    var currentRule = SkillRuleLoader.GetRule(currentCardId, currentSkillIndex);
                    if (currentRule == null || !predicate(currentRule))
                        continue;

                    generalIndex = currentGeneralIndex;
                    skillIndex = currentSkillIndex;
                    cardId = currentCardId;
                    rule = currentRule;
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetFaceUpRule(BattleState state, bool sideIsPlayer, int generalIndex, int skillIndex, out string cardId, out SkillRuleEntry rule)
        {
            cardId = string.Empty;
            rule = null;
            if (state == null)
                return false;

            var side = state.GetSide(sideIsPlayer);
            if (generalIndex < 0 || generalIndex >= side.GeneralCardIds.Count || !side.IsGeneralFaceUp(generalIndex))
                return false;

            cardId = side.GeneralCardIds[generalIndex];
            rule = SkillRuleLoader.GetRule(cardId, skillIndex);
            return rule != null;
        }

        private static bool ContainsOnlyRedCards(List<PokerCard> cards)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (!IsRed(cards[i]))
                    return false;
            }
            return cards.Count > 0;
        }

        private static int CountRedCards(List<PokerCard> cards)
        {
            if (cards == null)
                return 0;
            int n = 0;
            for (int i = 0; i < cards.Count; i++)
            {
                if (IsRed(cards[i]))
                    n++;
            }

            return n;
        }

        private static bool IsRed(PokerCard card) => PokerPatternRules.IsRedCard(card);

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
