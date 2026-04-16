using System;
using System.Collections.Generic;
using UnityEngine;

namespace JunzhenDuijue
{
    /// <summary>
    /// 「手牌数为 0」同节点多条 <c>empty_hand_draw_two_once_per_turn</c>：先收集快照，再按顺序结算，
    /// 避免先发动导致摸牌后后续技能因手牌非空而无法触发。
    /// 须在<strong>本回合行动方</strong>将打出区扑克牌移入弃牌堆、且相关结算/摸牌已写入状态后再调用（见 <see cref="BattlePhaseManager.AdvanceFromPlayPhase"/>），
    /// 不得在仅将牌移入打出区时调用。
    /// </summary>
    public readonly struct HandEmptyPassiveEntry
    {
        public readonly int GeneralIndex;
        public readonly int SkillIndex;
        public readonly string CardId;
        public readonly SkillRuleEntry Rule;

        public HandEmptyPassiveEntry(int generalIndex, int skillIndex, string cardId, SkillRuleEntry rule)
        {
            GeneralIndex = generalIndex;
            SkillIndex = skillIndex;
            CardId = cardId ?? string.Empty;
            Rule = rule;
        }
    }

    public static class HandEmptyPassiveCoordinator
    {
        public static void OnHandMaybeBecameZero(BattleState state, bool sideIsPlayer)
        {
            if (state == null)
                return;

            SideState side = state.GetSide(sideIsPlayer);
            if (side.Hand.Count != 0)
                return;

            List<HandEmptyPassiveEntry> entries = CollectEntries(state, sideIsPlayer);
            if (entries.Count == 0)
                return;

            entries.Sort((a, b) =>
            {
                int c = a.GeneralIndex.CompareTo(b.GeneralIndex);
                return c != 0 ? c : a.SkillIndex.CompareTo(b.SkillIndex);
            });

            if (!sideIsPlayer)
            {
                RunAllInOrder(state, sideIsPlayer, entries, null);
                return;
            }

            if (entries.Count == 1)
            {
                RunAllInOrder(state, sideIsPlayer, entries, null);
                return;
            }

            if (entries.Count == 2)
            {
                GameUI.BeginHandEmptyTwoOrderFlow(entries[0], entries[1], () =>
                {
                    if (BattlePhaseManager.GetState() != null)
                        GameUI.NotifyPhaseChanged();
                });
                return;
            }

            string autoMsg = "\u5171" + entries.Count + "\u4e2a\u540c\u8282\u70b9\u6280\u80fd\u6ee1\u8db3\u300c\u624b\u724c\u4e3a\u7a7a\u300d\uff0c\u5df2\u6309\u4e0a\u573a\u987a\u5e8f\u81ea\u52a8\u7ed3\u7b97\u3002";
            ToastUI.Show(autoMsg, 2.8f);
            RunAllInOrder(state, sideIsPlayer, entries, null);
        }

        private static List<HandEmptyPassiveEntry> CollectEntries(BattleState state, bool sideIsPlayer)
        {
            var list = new List<HandEmptyPassiveEntry>();
            SideState side = state.GetSide(sideIsPlayer);
            for (int gi = 0; gi < side.GeneralCardIds.Count; gi++)
            {
                if (!side.IsGeneralFaceUp(gi))
                    continue;

                string cid = side.GeneralCardIds[gi] ?? string.Empty;
                for (int sk = 0; sk < 3; sk++)
                {
                    SkillRuleEntry rule = SkillRuleLoader.GetRule(cid, sk);
                    if (rule == null || !string.Equals(rule.EffectId, OfflineSkillEngine.EmptyHandDrawTwoOncePerTurnEffectId, StringComparison.Ordinal))
                        continue;

                    string skillKey = SkillRuleHelper.MakeSkillKey(cid, sk);
                    if (side.TriggeredSkillKeysThisTurn.Contains(skillKey))
                        continue;

                    list.Add(new HandEmptyPassiveEntry(gi, sk, cid, rule));
                }
            }

            return list;
        }

        public static void RunAllInOrder(BattleState state, bool sideIsPlayer, List<HandEmptyPassiveEntry> entries, Action onComplete)
        {
            if (entries == null || entries.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            void Step(int i)
            {
                if (i >= entries.Count)
                {
                    onComplete?.Invoke();
                    return;
                }

                OfflineSkillEngine.ApplyEmptyHandDrawOnce(state, sideIsPlayer, entries[i], () => Step(i + 1));
            }

            Step(0);
        }
    }
}
