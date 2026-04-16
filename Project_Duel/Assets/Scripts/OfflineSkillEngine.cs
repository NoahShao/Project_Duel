using System;
using System.Collections.Generic;
using UnityEngine;

namespace JunzhenDuijue
{
    public static class OfflineSkillEngine
    {
        public const string ResistEffectKey = "\u62b5\u5fa1";

        private const string StartGameEffectId = "start_game_gain_morale_and_max";
        public const string EmptyHandDrawTwoOncePerTurnEffectId = "empty_hand_draw_two_once_per_turn";
        private const string PlayPhaseStartResistEffectId = "play_phase_start_pay_morale_gain_resist";
        private const string ManualPrimaryEffectId = "manual_primary_effect";
        private const string DiscardEndRenZheWuDiEffectId = "discard_end_draw_reveal_red_heal_black_damage";

        /// <summary>【长吼】攻击技结算伤害时，若打出含红色牌则加伤（在防御减免之后）。</summary>
        public const string AttackRedBonusDamageEffectId = "attack_red_bonus_damage";

        /// <summary>【据水断桥】从弃牌堆回收并依花色数回血、额外出牌阶段。</summary>
        public const string PrimaryRecoverDiscardExtraPhaseEffectId = "primary_recover_discard_gain_extra_phase";

        /// <summary>【虎步关右】在技能规则表中的 EffectId。</summary>
        public const string HuBuGuanYouEffectId = "attack_discard_black_gain_extra_phase";

        /// <summary>【八门金锁】翻开牌库顶，小点数减伤否则入手牌。</summary>
        public const string DefenseRevealSmall8ReduceElseGainEffectId = "defense_reveal_small8_reduce2_else_draw";

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
        /// 「结算使用攻击技造成的伤害时」：在防御技等登记的减伤与抵御等已从本次伤害中扣减之后，再叠加【长吼】加伤（不再受防御技/抵御影响）。
        /// </summary>
        public static int GetChangHouBonusWhenResolvingAttackDamage(BattleState state)
        {
            if (state == null)
                return 0;
            if (state.PendingAttackSkillKind == SelectedSkillKind.None)
                return 0;

            var played = state.ActiveSide.PlayedThisPhase;
            if (played == null || played.Count == 0)
                return 0;

            bool anyRed = false;
            for (int i = 0; i < played.Count; i++)
            {
                if (PokerPatternRules.IsRedCard(played[i]))
                {
                    anyRed = true;
                    break;
                }
            }

            if (!anyRed)
                return 0;

            bool attackerIsPlayer = state.IsPlayerTurn;
            if (!TryFindFaceUpRule(state, attackerIsPlayer, e => string.Equals(e.EffectId, AttackRedBonusDamageEffectId, StringComparison.Ordinal), out _, out _, out _, out SkillRuleEntry rule))
                return 0;

            return Mathf.Max(0, rule.Value1);
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
        /// 【仁德】消耗士气后：若场上翻面武将含刘备且规则为 start_game_gain_morale_and_max，则恢复 1 点生命。
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
        /// 【仁者无敌】弃牌阶段（开始或结束节点）：摸 Value1 张，展示最后一张；红色恢复 Value2 生命；黑色造成 Value2 点通用伤害。
        /// 若规则未将 <see cref="SkillRuleEntry.StringValue2"/> 设为 <see cref="SkillRuleDamageFlags.NonAttackDamageLocksToEnemyPlayerOnly"/>，则离线己方回合由玩家选择伤害落在己方或敌方玩家；否则仅对敌方玩家。
        /// </summary>
        /// <param name="battleLogUseDiscardPhaseStartTiming">战报中写「弃牌阶段开始」而非「弃牌阶段结束」。</param>
        /// <returns>true 表示已弹出目标选择，调用方须等待 <paramref name="onFullyResolved"/> 再推进阶段；false 表示已同步结算完毕（或未触发），可立即调用 <paramref name="onFullyResolved"/>。</returns>
        public static bool TryApplyDiscardEndRenZheWuDi(BattleState state, bool sideIsPlayer, System.Action onFullyResolved, out string message, bool battleLogUseDiscardPhaseStartTiming = false)
        {
            message = string.Empty;
            if (state == null)
            {
                onFullyResolved?.Invoke();
                return false;
            }

            if (!TryFindFaceUpRule(state, sideIsPlayer, e => string.Equals(e.EffectId, DiscardEndRenZheWuDiEffectId, StringComparison.Ordinal), out _, out _, out string cardIdRz, out SkillRuleEntry rule))
            {
                onFullyResolved?.Invoke();
                return false;
            }

            string phaseMid = battleLogUseDiscardPhaseStartTiming
                ? "\u5f03\u724c\u9636\u6bb5\u5f00\u59cb\uff0c\u3010\u4ec1\u8005\u65e0\u654c\u3011\uff1a"
                : "\u5f03\u724c\u9636\u6bb5\u7ed3\u675f\uff0c\u3010\u4ec1\u8005\u65e0\u654c\u3011\uff1a";

            var side = state.GetSide(sideIsPlayer);
            int drawCount = Mathf.Max(1, rule.Value1);
            int amount = Mathf.Max(1, rule.Value2);
            if (side.Deck.Count <= 0 && side.DiscardPile.Count <= 0)
            {
                message = "\u3010\u4ec1\u8005\u65e0\u654c\u3011\u724c\u5e93\u4e0e\u5f03\u724c\u5806\u7686\u7a7a\uff0c\u65e0\u6cd5\u6478\u724c";
                onFullyResolved?.Invoke();
                return false;
            }

            int beforeHand = side.Hand.Count;
            int drawn = BattleState.Draw(side, drawCount);
            if (drawn <= 0 || side.Hand.Count <= beforeHand)
            {
                message = "\u3010\u4ec1\u8005\u65e0\u654c\u3011\u65e0\u6cd5\u5b8c\u6210\u6478\u724c";
                onFullyResolved?.Invoke();
                return false;
            }

            state.RenZheWuDiHandledThisDiscardPhase = true;
            PokerCard shown = side.Hand[side.Hand.Count - 1];
            if (IsRed(shown))
            {
                side.CurrentHp = Mathf.Min(side.MaxHp, side.CurrentHp + amount);
                message = "\u3010\u4ec1\u8005\u65e0\u654c\u3011\u5c55\u793a" + shown.DisplayName + "\uff08\u7ea2\u8272\uff09\uff0c\u6062\u590d" + amount + "\u70b9\u751f\u547d";
                string outcome = "\u5c55\u793a" + shown.DisplayName + "\uff08\u7ea2\u8272\uff09\uff0c\u6062\u590d" + amount + "\u70b9\u751f\u547d";
                SkillEffectBanner.Show(sideIsPlayer, false, SkillEffectBanner.GetRoleNameFromCardId(cardIdRz), rule.SkillName, outcome);
                onFullyResolved?.Invoke();
                return false;
            }

            bool lockEnemy = rule.LocksNonAttackDamageToEnemyPlayerOnly();
            if (lockEnemy || !sideIsPlayer || GameUI.IsOnlineBattle())
            {
                ApplyRenZheWuDiBlackDamageToTarget(state, sideIsPlayer, damageOpponent: true, amount, finalAmt =>
                {
                    string fragment = "\u3010\u4ec1\u8005\u65e0\u654c\u3011\u5c55\u793a" + (shown.DisplayName ?? string.Empty) + "\uff08\u9ed1\u8272\uff09\uff0c\u5bf9\u654c\u65b9\u73a9\u5bb6\u9020\u6210" + finalAmt + "\u70b9\u901a\u7528\u4f24\u5bb3";
                    BattleFlowLog.Add(BattlePhaseManager.FormatFlowTurnBracketForBattleLog(sideIsPlayer) + phaseMid + fragment);
                    string outcome = "\u5c55\u793a" + (shown.DisplayName ?? string.Empty) + "\uff08\u9ed1\u8272\uff09\uff0c\u5bf9\u654c\u65b9\u73a9\u5bb6\u9020\u6210" + finalAmt + "\u70b9\u901a\u7528\u4f24\u5bb3";
                    SkillEffectBanner.Show(sideIsPlayer, false, SkillEffectBanner.GetRoleNameFromCardId(cardIdRz), rule.SkillName, outcome);
                    onFullyResolved?.Invoke();
                });
                message = string.Empty;
                return false;
            }

            GameUI.BeginNonAttackDamageTargetPick(state, sideIsPlayer, amount, cardIdRz, rule.SkillName, shown, onFullyResolved, battleLogUseDiscardPhaseStartTiming);
            message = string.Empty;
            return true;
        }

        /// <summary>离线己方弃牌阶段：是否可询问发动【仁者无敌】（与结算入口共用规则判定）。</summary>
        public static bool CanOfferRenZheWuDiDiscard(BattleState state, bool sideIsPlayer)
        {
            if (state == null || !sideIsPlayer || GameUI.IsOnlineBattle())
                return false;
            if (!TryFindFaceUpRule(state, sideIsPlayer, e => string.Equals(e.EffectId, DiscardEndRenZheWuDiEffectId, StringComparison.Ordinal), out _, out _, out _, out _))
                return false;
            var side = state.GetSide(sideIsPlayer);
            return side.Deck.Count > 0 || side.DiscardPile.Count > 0;
        }

        /// <param name="damageOpponent">true \u2192 \u5bf9\u654c\u65b9\u73a9\u5bb6\u9020\u6210\u4f24\u5bb3\uff1bfalse \u2192 \u5bf9\u5df1\u65b9\u73a9\u5bb6\u3002</param>
        public static void ApplyRenZheWuDiBlackDamageToTarget(BattleState state, bool sideIsPlayer, bool damageOpponent, int amount, System.Action<int> onAppliedWithFinalDamage = null)
        {
            if (state == null || amount <= 0)
            {
                onAppliedWithFinalDamage?.Invoke(0);
                return;
            }

            bool victimIsPlayer = sideIsPlayer ^ damageOpponent;
            GameUI.ApplyHpDamageWithOptionalResist(victimIsPlayer, amount, onAppliedWithFinalDamage);
        }

        public static string FormatRenZheWuDiBlackDamageLogFragment(bool damageOpponent, PokerCard shown, int amount) =>
            "\u5c55\u793a" + (shown.DisplayName ?? string.Empty) + "\uff08\u9ed1\u8272\uff09\uff0c\u5bf9" + (damageOpponent ? "\u654c\u65b9\u73a9\u5bb6" : "\u5df1\u65b9\u73a9\u5bb6") + "\u9020\u6210" + amount + "\u70b9\u901a\u7528\u4f24\u5bb3";

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
            ApplyRenDeWhenMoraleSpent(state, sideIsPlayer);
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

            return "\u79fb\u96641\u5c42\u201c\u62b5\u5fa1\u201d\uff0c\u672c\u6b21\u53d7\u5230\u7684\u4f24\u5bb3\u51cf\u534a\uff08\u5411\u4e0a\u53d6\u6574\u51cf\u514d\u91cf\uff09";
        }

        /// <summary>移除一层抵御并将伤害按「减半（减免量向上取整）」规则降低；若无法移除则返回原伤害。</summary>
        public static int ApplyOneResistHalvingToDamageAmount(BattleState state, bool victimIsPlayer, int rawDamage)
        {
            if (state == null || rawDamage <= 0)
                return rawDamage;
            var side = state.GetSide(victimIsPlayer);
            if (side.RemoveEffectLayers(ResistEffectKey, 1) <= 0)
                return rawDamage;
            return Mathf.Max(0, rawDamage - Mathf.CeilToInt(rawDamage / 2f));
        }

        /// <summary>非攻击直伤：受害方为对手 AI 时，较高伤害下概率消耗一层抵御。</summary>
        public static void MaybeAutoConsumeResistForDirectDamage(BattleState state, bool victimIsPlayer, ref int amount)
        {
            if (state == null || amount <= 1 || victimIsPlayer)
                return;
            if (!HasRemovableDefenseBuff(state, false))
                return;
            if (UnityEngine.Random.value > 0.5f)
                return;
            int before = amount;
            amount = ApplyOneResistHalvingToDamageAmount(state, false, amount);
            if (amount != before)
                BattleFlowLog.Add(BattlePhaseManager.FormatFlowTurnBracketForBattleLog(state.IsPlayerTurn) + "\u901a\u7528\u4f24\u5bb3\u7ed3\u7b97\u524d\uff0c\u654c\u65b9\u79fb\u96641\u5c42\u300c\u62b5\u5fa1\u300d\uff0c\u4f24\u5bb3\u7531" + before + "\u53d8\u4e3a" + amount + "\u3002");
        }

        /// <summary>对手防御流程：在声明防御技前，若持有抵御且本次伤害较高，概率消耗一层减半。</summary>
        public static void MaybeAutoUseResistBeforeDefenseSkill(BattleState state, bool defenderIsPlayer)
        {
            if (state == null || defenderIsPlayer)
                return;
            if (!HasRemovableDefenseBuff(state, false))
                return;
            int raw = Mathf.Max(0, state.PendingBaseDamage + state.PendingAttackBonus);
            if (raw < 4)
                return;
            if (UnityEngine.Random.value > 0.45f)
                return;
            string msg = ConsumeOneDefenseBuff(state, false);
            if (!string.IsNullOrEmpty(msg))
            {
                state.PendingHalveIncomingDamageWithResist = true;
                BattleFlowLog.Add(BattlePhaseManager.FormatFlowTurnBracketForBattleLog(state.IsPlayerTurn) + "\u9632\u5fa1\u9636\u6bb5\uff08\u81ea\u52a8\uff09\uff0c\u654c\u65b9" + msg + "\u3002");
            }
        }

        /// <summary>由 <see cref="HandEmptyPassiveCoordinator"/> 在同节点顺序结算时调用。</summary>
        public static void ApplyEmptyHandDrawOnce(BattleState state, bool sideIsPlayer, HandEmptyPassiveEntry entry, Action onAfterBanner = null)
        {
            if (state == null)
                return;

            var side = state.GetSide(sideIsPlayer);
            SkillRuleEntry rule = entry.Rule;
            if (rule == null || !string.Equals(rule.EffectId, EmptyHandDrawTwoOncePerTurnEffectId, StringComparison.Ordinal))
                return;

            string skillKey = SkillRuleHelper.MakeSkillKey(entry.CardId, entry.SkillIndex);
            if (side.TriggeredSkillKeysThisTurn.Contains(skillKey))
            {
                onAfterBanner?.Invoke();
                return;
            }

            side.TriggeredSkillKeysThisTurn.Add(skillKey);
            int drawCount = Mathf.Max(1, rule.Value1);
            BattleState.Draw(side, drawCount);
            SkillEffectBanner.Show(sideIsPlayer, false, SkillEffectBanner.GetRoleNameFromCardId(entry.CardId), rule.SkillName, "\u6478" + drawCount + "\u5f20\u724c", onAfterBanner);
        }

        public static bool TryTriggerHandEmptyPassive(BattleState state, bool sideIsPlayer, out string message)
        {
            message = string.Empty;
            BattleState.NotifyHandMaybeBecameZero(state, sideIsPlayer);
            return false;
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

        /// <summary>策马牌型判定用素材：将牌打出不计入（与「多带无关牌」一致，只看扑克部分能否成式）。</summary>
        public static List<PokerCard> CeMaAttackMaterial(List<PokerCard> cards)
        {
            if (cards == null || cards.Count == 0)
                return new List<PokerCard>();
            var r = new List<PokerCard>(cards.Count);
            for (int i = 0; i < cards.Count; i++)
            {
                if (!cards[i].PlayedAsGeneral)
                    r.Add(cards[i]);
            }

            return r;
        }

        /// <summary>【策马斩将】两张红色单牌：素材中至少 2 张红色扑克即可。</summary>
        public static bool CeMaTwoRedSinglesMatches(List<PokerCard> cards) =>
            cards != null && CountRedCards(CeMaAttackMaterial(cards)) >= 2;

        private static bool CeMaFourIsRedStraight(List<PokerCard> four) =>
            four != null && four.Count == 4 && ContainsOnlyRedCards(four) && PokerPatternRules.IsFlexibleStraight(four, 4);

        private static bool CeMaFourIsRedStraightFlush(List<PokerCard> four) =>
            CeMaFourIsRedStraight(four) && PokerPatternRules.IsFlush(four);

        private static bool CeMaEvalAnyFourSubset(List<PokerCard> material, System.Func<List<PokerCard>, bool> testFour)
        {
            if (material == null || testFour == null)
                return false;
            int n = material.Count;
            if (n < 4)
                return false;
            for (int i0 = 0; i0 < n; i0++)
            {
                for (int i1 = i0 + 1; i1 < n; i1++)
                {
                    for (int i2 = i1 + 1; i2 < n; i2++)
                    {
                        for (int i3 = i2 + 1; i3 < n; i3++)
                        {
                            var four = new List<PokerCard>(4) { material[i0], material[i1], material[i2], material[i3] };
                            if (testFour(four))
                                return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>【策马斩将】红色顺子：素材中存在任意 4 张红色且构成顺子（可混带其余牌）。</summary>
        public static bool CeMaRedStraightMatches(List<PokerCard> cards) =>
            cards != null && CeMaEvalAnyFourSubset(CeMaAttackMaterial(cards), CeMaFourIsRedStraight);

        /// <summary>【策马斩将】红色同花顺：素材中存在任意 4 张构成红色同花顺。</summary>
        public static bool CeMaRedStraightFlushMatches(List<PokerCard> cards) =>
            cards != null && CeMaEvalAnyFourSubset(CeMaAttackMaterial(cards), CeMaFourIsRedStraightFlush);

        /// <summary>与选牌弹窗按钮文案一致，用于武将攻击技横幅副文案中的「牌型为【…】」。若 <paramref name="state"/> 上已写入本次策马分支则优先用之。</summary>
        public static string DescribeCeMaPatternShapeForBanner(List<PokerCard> cards, BattleState state = null)
        {
            if (state != null && state.PendingCeMaBannerShapeKind >= 0)
            {
                return state.PendingCeMaBannerShapeKind switch
                {
                    2 => "\u7ea2\u8272\u540c\u82b1\u987a\uff08\u56db\u5f20\uff09",
                    1 => "\u7ea2\u8272\u987a\u5b50\uff08\u56db\u5f20\uff09",
                    0 => "\u4e24\u5f20\u7ea2\u8272\u5355\u724c",
                    _ => string.Empty,
                };
            }

            if (cards == null)
                return string.Empty;
            if (CeMaRedStraightFlushMatches(cards))
                return "\u7ea2\u8272\u540c\u82b1\u987a\uff08\u56db\u5f20\uff09";
            if (CeMaRedStraightMatches(cards))
                return "\u7ea2\u8272\u987a\u5b50\uff08\u56db\u5f20\uff09";
            if (CeMaTwoRedSinglesMatches(cards))
                return "\u4e24\u5f20\u7ea2\u8272\u5355\u724c";
            return string.Empty;
        }

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

        /// <summary>【远矢连珠】大10点档：与技能框架首行一致（有效点数 &gt; 9，人牌须察势作10）。</summary>
        public static bool YuanShuTier10RowMatches(PokerCard c) =>
            !c.PlayedAsGeneral && YuanShuSingleCardMatchesRankWindow(c, minExclusive: 9, maxExclusive: 0, excludeFaceWithoutChaShiTen: true);

        /// <summary>【远矢连珠】大7点档：与技能框架次行一致（6 &lt; 有效点数 &lt; 10）。</summary>
        public static bool YuanShuTier7RowMatches(PokerCard c) =>
            !c.PlayedAsGeneral && YuanShuSingleCardMatchesRankWindow(c, minExclusive: 6, maxExclusive: 10, excludeFaceWithoutChaShiTen: true);

        /// <summary>弹窗「大7点」是否可选：满足大7点档，或满足大10点档时允许自愿按大7点结算（如黑桃10）。</summary>
        public static bool YuanShuCanSelectTier7Damage(PokerCard c) =>
            YuanShuTier7RowMatches(c) || YuanShuTier10RowMatches(c);

        /// <summary>弹窗「大10点」是否可选。</summary>
        public static bool YuanShuCanSelectTier10Damage(PokerCard c) => YuanShuTier10RowMatches(c);

        /// <summary>打出区中是否存在至少一张可作【远矢连珠】结算的非将牌（可混带其余牌）。</summary>
        public static bool YuanShuHasAnyPatternOptionForPlayed(List<PokerCard> played)
        {
            if (played == null || played.Count == 0)
                return false;
            for (int i = 0; i < played.Count; i++)
            {
                PokerCard c = played[i];
                if (c.PlayedAsGeneral)
                    continue;
                if (YuanShuTier7RowMatches(c) || YuanShuTier10RowMatches(c))
                    return true;
            }

            return false;
        }

        /// <summary>对手 AI / 预览：打出区中任一张满足大10点档则优先该档，否则任一张满足大7点档。</summary>
        public static void AutoPickYuanShuPatternVariant(BattleState state, List<PokerCard> cards)
        {
            if (state == null)
                return;
            if (cards == null || cards.Count == 0)
            {
                state.PendingAttackPatternVariant = -1;
                return;
            }

            bool any10 = false;
            bool any7Strict = false;
            for (int i = 0; i < cards.Count; i++)
            {
                PokerCard c = cards[i];
                if (c.PlayedAsGeneral)
                    continue;
                if (YuanShuCanSelectTier10Damage(c))
                    any10 = true;
                if (YuanShuTier7RowMatches(c))
                    any7Strict = true;
            }

            if (any10)
                state.PendingAttackPatternVariant = 1;
            else if (any7Strict)
                state.PendingAttackPatternVariant = 0;
            else
                state.PendingAttackPatternVariant = -1;
        }

        /// <summary>与横幅「牌型为【…】」一致：依本次登记的基础伤害区分大7点 / 大10点单牌。</summary>
        public static string DescribeYuanShuLianZhuShapeForBanner(BattleState state)
        {
            if (state == null)
                return string.Empty;
            return state.PendingBaseDamage >= 2 ? "\u592710\u70b9\u5355\u724c" : "\u59277\u70b9\u5355\u724c";
        }

        private static bool YuanShuSingleCardMatchesRankWindow(PokerCard c, int minExclusive, int maxExclusive, bool excludeFaceWithoutChaShiTen)
        {
            if (excludeFaceWithoutChaShiTen && PokerPatternRules.IsFaceCourtCard(c) && !c.ChaShiCourtPlayedAsTen)
                return false;

            int r = PokerPatternRules.GetRankForAttackThreshold(c);
            if (minExclusive > 0 && r <= minExclusive)
                return false;
            if (maxExclusive > 0 && r >= maxExclusive)
                return false;
            return true;
        }

        public static void ConfigureAttackSkill(BattleState state, bool attackerIsPlayer, int generalIndex, int skillIndex, Action afterAttackDeclareBanner = null)
        {
            if (state == null)
                return;

            state.HuBuGuanYouWindowConsumedForCurrentAttack = false;
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
            state.PendingCeMaBannerShapeKind = -1;

            if (!TryGetFaceUpRule(state, attackerIsPlayer, generalIndex, skillIndex, out string cardId, out _))
            {
                GenericAttackShapes.ApplyGenericAttack(state, state.ActiveSide.PlayedThisPhase, attackerIsPlayer);
                state.PendingAttackSkillKind = SelectedSkillKind.GenericAttack;
                state.PendingAttackSkillName = "\u901a\u7528\u653b\u51fb";
                TryShowAttackDeclareBanner(state, attackerIsPlayer, afterAttackDeclareBanner);
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
                    case "NO005_0":
                        handled = ConfigureYuanShuLianZhu(state, attackerIsPlayer, cards);
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

            TryShowAttackDeclareBanner(state, attackerIsPlayer, afterAttackDeclareBanner);
        }

        /// <returns>若 true，表示防御宣告的战报与横幅需延后到 <see cref="GameUI.BeginBamenJinsuoChaShiResolve"/> 察势完成后再走 <see cref="BattlePhaseManager.CompleteDefenseDeclareAfterDeferredBamen"/>。</returns>
        public static bool ConfigureDefenseSkill(BattleState state, bool defenderIsPlayer, int generalIndex, int skillIndex)
        {
            if (state == null)
                return false;

            state.PendingDefenseBamenReveal = null;

            if (!TryGetFaceUpRule(state, defenderIsPlayer, generalIndex, skillIndex, out string cardId, out SkillRuleEntry rule) || rule == null)
            {
                state.PendingDefenseReduction = Mathf.Max(state.PendingDefenseReduction, 1);
                return false;
            }

            if (string.Equals(rule.EffectId, DefenseRevealSmall8ReduceElseGainEffectId, StringComparison.Ordinal))
                return ApplyDefenseBamenJinsuo(state, defenderIsPlayer, cardId, rule);

            state.PendingDefenseReduction = Mathf.Max(state.PendingDefenseReduction, 1);
            AppendCombatNote(state, "\u9632\u5fa1\u6280\u9ed8\u8ba4\u51cf\u4f24+1");
            return false;
        }

        /// <summary>【八门金锁】在翻出牌并已确定察势（若需）后，按比对点数入弃牌/手牌并更新减伤与战报。</summary>
        public static void FinishBamenJinsuoAfterReveal(BattleState state, bool defenderIsPlayer, PokerCard revealed, string cardId, SkillRuleEntry rule, int capRank, int bonusReduce)
        {
            if (state == null || revealed == null)
                return;

            state.PendingDefenseBamenReveal = null;
            var defSide = state.GetSide(defenderIsPlayer);
            int eff = PokerPatternRules.GetComparisonPoint(revealed);
            string cardLabel = revealed.DisplayName ?? string.Empty;
            string skillName = rule != null ? rule.SkillName : "\u516b\u95e8\u91d1\u9501";

            if (eff <= capRank)
            {
                defSide.DiscardPile.Add(revealed);
                state.PendingDefenseReduction = Mathf.Max(state.PendingDefenseReduction, bonusReduce);
                string outcome = "\u7ffb\u5f00\u724c\u5e93\u9876\u3010" + cardLabel + "\u3011\uff08\u5c0f" + capRank + "\u70b9\u5224\u5b9a\uff1a\u8ba1\u4f5c" + eff + "\u70b9\uff09\uff0c\u672c\u6b21\u53d7\u5230\u7684\u4f24\u5bb3-" + bonusReduce;
                AppendCombatNote(state, "\u3010\u516b\u95e8\u91d1\u9501\u3011" + outcome);
                if (!BattleAttackPreview.SuppressSkillBanners)
                    SkillEffectBanner.Show(defenderIsPlayer, true, SkillEffectBanner.GetRoleNameFromCardId(cardId), skillName, outcome);
            }
            else
            {
                defSide.Hand.Add(revealed);
                string outcome = "\u7ffb\u5f00\u724c\u5e93\u9876\u3010" + cardLabel + "\u3011\uff08\u5c0f" + capRank + "\u70b9\u5224\u5b9a\uff1a\u8ba1\u4f5c" + eff + "\u70b9\uff09\uff0c\u83b7\u5f97\u8be5\u724c";
                AppendCombatNote(state, "\u3010\u516b\u95e8\u91d1\u9501\u3011" + outcome);
                if (!BattleAttackPreview.SuppressSkillBanners)
                    SkillEffectBanner.Show(defenderIsPlayer, true, SkillEffectBanner.GetRoleNameFromCardId(cardId), skillName, outcome);
            }
        }

        private static bool ApplyDefenseBamenJinsuo(BattleState state, bool defenderIsPlayer, string cardId, SkillRuleEntry rule)
        {
            var defSide = state.GetSide(defenderIsPlayer);
            state.PendingDefenseReduction = Mathf.Max(state.PendingDefenseReduction, 1);
            if (!BattleState.TryPopTopDeckCardForReveal(defSide, out PokerCard revealed))
            {
                AppendCombatNote(state, "\u3010\u516b\u95e8\u91d1\u9501\u3011\u724c\u5e93\u7a7a\uff0c\u4ec5\u767b\u8bb0\u57fa\u7840\u9632\u5fa1\u51cf\u4f24");
                return false;
            }

            int capRank = rule.Value2 > 0 ? rule.Value2 : 8;
            int bonusReduce = Mathf.Max(1, rule.Value1);
            bool needsChaShi = ChaShiSkillRules.HandDeckCardNeedsChaShiChoice(state, defenderIsPlayer, revealed);

            if (needsChaShi && !GameUI.IsOnlineBattle() && defenderIsPlayer)
            {
                revealed.ChaShiCourtPlayedAsTen = false;
                state.PendingDefenseBamenReveal = revealed;
                GameUI.BeginBamenJinsuoChaShiResolve(defenderIsPlayer);
                return true;
            }

            if (needsChaShi)
                revealed.ChaShiCourtPlayedAsTen = false;

            FinishBamenJinsuoAfterReveal(state, defenderIsPlayer, revealed, cardId, rule, capRank, bonusReduce);
            return false;
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

            bool ok = variant switch
            {
                2 => TryApplyCeMaStraightFlush(state, cards),
                1 => TryApplyCeMaRedStraightOnly(state, cards),
                0 => TryApplyCeMaTwoRedSingles(state, cards),
                _ => false,
            };
            if (ok)
                state.PendingCeMaBannerShapeKind = variant;
            return ok;
        }

        private static bool ConfigureYuanShuLianZhu(BattleState state, bool attackerIsPlayer, List<PokerCard> cards)
        {
            if (cards == null || cards.Count == 0)
                return false;

            bool can10 = false;
            bool can7 = false;
            for (int i = 0; i < cards.Count; i++)
            {
                PokerCard c = cards[i];
                if (c.PlayedAsGeneral)
                    continue;
                if (YuanShuCanSelectTier10Damage(c))
                    can10 = true;
                if (YuanShuCanSelectTier7Damage(c))
                    can7 = true;
            }

            if (!can7 && !can10)
                return false;

            int variant = state.PendingAttackPatternVariant;
            if (variant < 0)
                AutoPickYuanShuPatternVariant(state, cards);

            variant = state.PendingAttackPatternVariant;
            state.PendingAttackPatternVariant = -1;

            if (variant == 1 && can10)
                return TryApplyYuanShuTier10(state, cards);
            if (variant == 0 && can7)
                return TryApplyYuanShuTier7(state, cards);
            return false;
        }

        private static bool TryApplyYuanShuTier10(BattleState state, List<PokerCard> cards)
        {
            if (cards == null)
                return false;
            bool anchor = false;
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].PlayedAsGeneral)
                    continue;
                if (YuanShuTier10RowMatches(cards[i]))
                {
                    anchor = true;
                    break;
                }
            }

            if (!anchor)
                return false;

            state.PendingBaseDamage = 2;
            state.PendingDamageCategory = DamageCategory.Blade;
            state.PendingDamageElement = DamageElement.None;
            state.PendingIgnoreDefenseReduction = true;
            AppendCombatNote(state, "\u3010\u8fdc\u77e2\u8fde\u73e0\u3011\u592710\u70b9\u5355\u724c\uff0c\u4f24\u5bb3\u6539\u4e3a2\u70b9\u4e0d\u53ef\u9632\u5fa1\u5175\u5203\u4f24\u5bb3");
            return true;
        }

        private static bool TryApplyYuanShuTier7(BattleState state, List<PokerCard> cards)
        {
            if (cards == null)
                return false;
            bool anchor = false;
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].PlayedAsGeneral)
                    continue;
                if (YuanShuCanSelectTier7Damage(cards[i]))
                {
                    anchor = true;
                    break;
                }
            }

            if (!anchor)
                return false;

            state.PendingBaseDamage = 1;
            state.PendingDamageCategory = DamageCategory.Blade;
            state.PendingDamageElement = DamageElement.None;
            state.PendingIgnoreDefenseReduction = true;
            AppendCombatNote(state, "\u3010\u8fdc\u77e2\u8fde\u73e0\u3011\u59277\u70b9\u5355\u724c\uff0c\u4f24\u5bb3\u6539\u4e3a1\u70b9\u4e0d\u53ef\u9632\u5fa1\u5175\u5203\u4f24\u5bb3");
            return true;
        }

        private static void AppendPendingAttackDeclareModifiers(System.Text.StringBuilder line, BattleState state)
        {
            if (state == null || line == null)
                return;
            if (state.PendingExtraPlayPhasesToGrant > 0)
                line.Append("\uff1b\u989d\u5916\u51fa\u724c\u9636\u6bb5+").Append(state.PendingExtraPlayPhasesToGrant);
            if (state.PendingPostResolveDrawToAttacker > 0)
                line.Append("\uff1b\u7ed3\u7b97\u540e\u6478").Append(state.PendingPostResolveDrawToAttacker).Append("\u5f20");
            if (state.PendingPostResolveHealToAttacker > 0)
                line.Append("\uff1b\u7ed3\u7b97\u540e\u6062\u590d").Append(state.PendingPostResolveHealToAttacker).Append("\u70b9\u751f\u547d");
            if (state.PendingPostResolveMoraleToAttacker > 0)
                line.Append("\uff1b\u7ed3\u7b97\u540e\u58eb\u6c14+").Append(state.PendingPostResolveMoraleToAttacker);
            if (state.PendingIgnoreDefenseReduction)
                line.Append("\uff1b\u672c\u6b21\u4e0d\u53ef\u9632\u5fa1\u51cf\u4f24");
        }

        private static void TryShowAttackDeclareBanner(BattleState state, bool attackerIsPlayer, Action onAfterDeclareBanner)
        {
            void finish()
            {
                onAfterDeclareBanner?.Invoke();
            }

            if (BattleAttackPreview.SuppressSkillBanners || state == null)
            {
                finish();
                return;
            }

            if (state.PendingGenericAttackShapeChoicePending)
            {
                finish();
                return;
            }

            int declared = Mathf.Max(0, state.PendingBaseDamage + state.PendingAttackBonus);
            bool isGeneric = state.PendingAttackSkillKind == SelectedSkillKind.GenericAttack;
            if (!isGeneric && declared <= 0 && state.PendingPostResolveDrawToAttacker <= 0 && state.PendingExtraPlayPhasesToGrant <= 0 && !state.PendingIgnoreDefenseReduction)
            {
                finish();
                return;
            }

            DamageCategory damageCat = state.PendingDamageCategory == DamageCategory.None ? DamageCategory.Generic : state.PendingDamageCategory;
            DamageElement damageEl = state.PendingDamageElement;
            string damageClause = DamageTypeLabels.FormatDeclarePendingDamageClause(declared, damageCat, damageEl);

            if (state.PendingAttackSkillKind == SelectedSkillKind.GeneralSkill && state.PendingAttackGeneralIndex >= 0)
            {
                if (TryGetFaceUpRule(state, attackerIsPlayer, state.PendingAttackGeneralIndex, state.PendingAttackSkillIndex, out string atkCardId, out SkillRuleEntry atkRule) && atkRule != null)
                {
                    var played = state.ActiveSide.PlayedThisPhase;
                    string skillKey = SkillRuleHelper.MakeSkillKey(atkCardId, state.PendingAttackSkillIndex);
                    string shape = string.Equals(skillKey, "NO002_0", StringComparison.Ordinal)
                        ? DescribeCeMaPatternShapeForBanner(played, state)
                        : string.Equals(skillKey, "NO005_0", StringComparison.Ordinal)
                            ? DescribeYuanShuLianZhuShapeForBanner(state)
                            : GenericAttackShapes.DescribeShapeForLog(state, played);

                    var sb = new System.Text.StringBuilder();
                    if (!string.IsNullOrEmpty(shape))
                        sb.Append("\u724c\u578b\u4e3a\u3010").Append(shape).Append("\u3011\uff0c");
                    sb.Append(damageClause);
                    AppendPendingAttackDeclareModifiers(sb, state);
                    SkillEffectBanner.Show(attackerIsPlayer, true, SkillEffectBanner.GetRoleNameFromCardId(atkCardId), atkRule.SkillName, sb.ToString(), finish);
                    return;
                }
            }
            else if (isGeneric)
            {
                string shape = GenericAttackShapes.DescribeShapeForLog(state, state.ActiveSide.PlayedThisPhase);
                string camp = attackerIsPlayer ? "\u5df1\u65b9" : "\u654c\u65b9";
                var sb = new System.Text.StringBuilder();
                sb.Append(camp).Append("\u73a9\u5bb6\u4f7f\u7528\u3010\u901a\u7528\u653b\u51fb\u6280\u3011\uff0c\u724c\u578b\u4e3a\u3010").Append(shape).Append("\u3011\uff0c");
                sb.Append(DamageTypeLabels.FormatResolvedDamageLine(declared, damageCat, damageEl));
                AppendPendingAttackDeclareModifiers(sb, state);
                SkillEffectBanner.ShowRawLine(sb.ToString(), finish);
                return;
            }

            finish();
        }

        private static bool TryApplyCeMaTwoRedSingles(BattleState state, List<PokerCard> cards)
        {
            if (!CeMaTwoRedSinglesMatches(cards))
                return false;

            state.PendingBaseDamage = 3;
            state.PendingDamageCategory = DamageCategory.Blade;
            state.PendingDamageElement = DamageElement.None;
            AppendCombatNote(state, "\u3010\u7b56\u9a6c\u65a9\u5c06\u3011\u81f3\u5c112\u5f20\u7ea2\u724c\uff08\u542b\u4e8e\u6253\u51fa\u533a\uff09\uff0c\u4f24\u5bb3\u6539\u4e3a3");
            return true;
        }

        private static bool TryApplyCeMaRedStraightOnly(BattleState state, List<PokerCard> cards)
        {
            if (!CeMaRedStraightMatches(cards))
                return false;

            state.PendingBaseDamage = 6;
            state.PendingDamageCategory = DamageCategory.Blade;
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
            state.PendingDamageCategory = DamageCategory.Blade;
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

        public static bool SideHasFaceUpHuBuGuanYou(BattleState state, bool sideIsPlayer)
        {
            if (state == null)
                return false;
            SideState side = state.GetSide(sideIsPlayer);
            for (int gi = 0; gi < side.GeneralCardIds.Count; gi++)
            {
                if (!side.IsGeneralFaceUp(gi))
                    continue;
                string cid = side.GeneralCardIds[gi] ?? string.Empty;
                SkillRuleEntry r = SkillRuleLoader.GetRule(cid, 1);
                if (r != null && string.Equals(r.EffectId, HuBuGuanYouEffectId, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static bool CardDataHasAttackSkillTag(CardData data, int skillIndex)
        {
            if (data == null)
                return false;
            if (!string.IsNullOrWhiteSpace(data.CardId) && SkillRuleLoader.HasTag(data.CardId, skillIndex, "\u653b\u51fb\u6280"))
                return true;
            List<string> tags = skillIndex switch
            {
                0 => data.SkillTags1,
                1 => data.SkillTags2,
                2 => data.SkillTags3,
                _ => null
            };
            if (tags == null)
                return false;
            for (int i = 0; i < tags.Count; i++)
            {
                if (string.Equals(tags[i], "\u653b\u51fb\u6280", StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 将要使用攻击技造成伤害时：含【通用攻击技】与任意翻面武将的「攻击技」标签技能；场上存在可发动的【虎步关右】时，在进入防御阶段前询问攻击方。
        /// </summary>
        public static bool ShouldOfferHuBuGuanYouBeforeDefense(BattleState state, bool attackerIsPlayer)
        {
            if (state == null)
                return false;
            if (state.HuBuGuanYouWindowConsumedForCurrentAttack)
                return false;
            if (state.PendingBaseDamage + state.PendingAttackBonus <= 0)
                return false;
            if (!SideHasFaceUpHuBuGuanYou(state, attackerIsPlayer))
                return false;

            if (state.PendingAttackSkillKind == SelectedSkillKind.GenericAttack)
                return true;

            if (state.PendingAttackSkillKind != SelectedSkillKind.GeneralSkill)
                return false;
            if (state.PendingAttackGeneralIndex < 0)
                return false;
            SideState atk = state.GetSide(attackerIsPlayer);
            if (state.PendingAttackGeneralIndex >= atk.GeneralCardIds.Count)
                return false;
            string cid = atk.GeneralCardIds[state.PendingAttackGeneralIndex] ?? string.Empty;
            CardData data = CardTableLoader.GetCard(CardTableLoader.CardIdToNumber(cid));
            return CardDataHasAttackSkillTag(data, state.PendingAttackSkillIndex);
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
