using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JunzhenDuijue
{
    public enum GenericAttackShapeKind : byte
    {
        StraightFlush = 0,
        FullHouse,
        FourOfAKind,
        Straight,
        Flush,
        TwoPair,
        Triple,
        Pair,
        Single,
    }

    /// <summary>
    /// 通用攻击：从打出牌的所有非空子集中识别可选牌型；结算时只应用玩家或 AI 选定的一种（与打出张数无关）。
    /// 每种 <see cref="GenericAttackShapeKind"/>（含对子、单牌等）在选项列表中至多出现一条，不会因多个子集重复计对子/顺子等多段伤害。
    /// </summary>
    public readonly struct GenericAttackOption
    {
        public readonly GenericAttackShapeKind Kind;
        public readonly int Damage;
        public readonly int PostDraw;
        public readonly string DisplayName;

        public GenericAttackOption(GenericAttackShapeKind kind, int damage, int postDraw, string displayName)
        {
            Kind = kind;
            Damage = damage;
            PostDraw = postDraw;
            DisplayName = displayName ?? string.Empty;
        }
    }

    public static class GenericAttackShapes
    {
        private static int KindSortOrder(GenericAttackShapeKind k) => (int)k;

        /// <summary>与 <see cref="ApplyGenericAttack"/> 一致：用于战报时优先使用 <see cref="BattleState.PendingGenericAttackShapeDisplayName"/>。</summary>
        public static string DescribeShapeForLog(BattleState state, List<PokerCard> cards)
        {
            if (state != null && !string.IsNullOrEmpty(state.PendingGenericAttackShapeDisplayName))
                return state.PendingGenericAttackShapeDisplayName;
            return DescribeShapeFromCardsOnly(cards);
        }

        /// <summary>不读战斗状态时的描述（多牌型时仅作兜底）。</summary>
        public static string DescribeShapeFromCardsOnly(List<PokerCard> cards)
        {
            if (cards == null || cards.Count == 0)
                return "\u65e0\u724c";

            var opts = BuildSortedOptions(cards);
            if (opts.Count == 1)
                return opts[0].DisplayName;
            return "\u591a\u724c\u578b\u53ef\u9009";
        }

        /// <summary>
        /// 枚举本手牌可申报的牌型；按牌型种类去重（例如多组两张可成对时仍只有一条「对子」），与 <see cref="ApplyGenericAttack"/> 一致。
        /// 同花顺同时提供「顺子」选项（点数/摸牌按顺子档），顺子判定仍走 <see cref="PokerPatternRules.IsFlexibleStraight"/>（含 JQK=11/12/13）。
        /// </summary>
        public static List<GenericAttackOption> BuildSortedOptions(List<PokerCard> cards)
        {
            var result = new List<GenericAttackOption>();
            if (cards == null || cards.Count == 0)
                return result;

            int n = Mathf.Min(cards.Count, BattleState.MaxCardsEvaluatedForGenericAttack);
            // 键为牌型种类：对子与单牌等一样全局至多保留一条，结算时玩家只选其一。
            var byKind = new Dictionary<GenericAttackShapeKind, GenericAttackOption>();

            for (int mask = 1; mask < 1 << n; mask++)
            {
                var sub = new List<PokerCard>();
                for (int i = 0; i < n; i++)
                {
                    if ((mask & (1 << i)) != 0)
                        sub.Add(cards[i]);
                }

                if (TryClassifySubset(sub, out GenericAttackOption opt))
                {
                    UpsertBestOption(byKind, opt);
                    if (opt.Kind == GenericAttackShapeKind.StraightFlush)
                    {
                        UpsertBestOption(
                            byKind,
                            new GenericAttackOption(GenericAttackShapeKind.Straight, 3, 1, "\u987a\u5b50"));
                    }
                }
            }

            byKind[GenericAttackShapeKind.Single] = new GenericAttackOption(GenericAttackShapeKind.Single, 1, 0, "\u5355\u5f20");

            foreach (var kv in byKind)
                result.Add(kv.Value);

            result.Sort((a, b) => KindSortOrder(a.Kind).CompareTo(KindSortOrder(b.Kind)));
            return result;
        }

        private static void UpsertBestOption(Dictionary<GenericAttackShapeKind, GenericAttackOption> byKind, GenericAttackOption opt)
        {
            if (!byKind.TryGetValue(opt.Kind, out GenericAttackOption old)
                || opt.Damage > old.Damage
                || (opt.Damage == old.Damage && opt.PostDraw > old.PostDraw))
                byKind[opt.Kind] = opt;
        }

        /// <summary>AI：按与预览相近的权重选最优选项下标。</summary>
        public static int PickBestOptionIndex(IReadOnlyList<GenericAttackOption> options)
        {
            if (options == null || options.Count == 0)
                return -1;
            int best = 0;
            long bestScore = ScoreOptionForAi(options[0]);
            for (int i = 1; i < options.Count; i++)
            {
                long s = ScoreOptionForAi(options[i]);
                if (s > bestScore)
                {
                    bestScore = s;
                    best = i;
                }
            }

            return best;
        }

        private static long ScoreOptionForAi(in GenericAttackOption o) =>
            (long)o.Damage * 10000L + (long)o.PostDraw * 250L;

        /// <summary>通用攻击技弹窗与说明用文案（与数值表一致，不用「基础伤害」等词）。</summary>
        public static string GetShapeEffectDescriptionForUi(GenericAttackShapeKind kind)
        {
            return kind switch
            {
                GenericAttackShapeKind.Single => "\u9020\u62101\u70b9\u901a\u7528\u4f24\u5bb3",
                GenericAttackShapeKind.Pair => "\u9020\u62102\u70b9\u901a\u7528\u4f24\u5bb3",
                GenericAttackShapeKind.Triple => "\u9020\u62103\u70b9\u901a\u7528\u4f24\u5bb3",
                GenericAttackShapeKind.FourOfAKind => "\u9020\u62105\u70b9\u901a\u7528\u4f24\u5bb3",
                GenericAttackShapeKind.TwoPair => "\u9020\u62104\u70b9\u901a\u7528\u4f24\u5bb3",
                GenericAttackShapeKind.Flush => "\u9020\u62104\u70b9\u901a\u7528\u4f24\u5bb3",
                GenericAttackShapeKind.Straight => "\u9020\u62103\u70b9\u901a\u7528\u4f24\u5bb3\uff0c\u6478\u4e00\u5f20\u724c",
                GenericAttackShapeKind.StraightFlush => "\u9020\u62106\u70b9\u901a\u7528\u4f24\u5bb3",
                GenericAttackShapeKind.FullHouse => "\u9020\u62105\u70b9\u901a\u7528\u4f24\u5bb3\uff0c\u6478\u4e24\u5f20\u724c",
                _ => "\u9020\u6210\u901a\u7528\u4f24\u5bb3",
            };
        }

        public static void ApplyGenericAttack(BattleState state, List<PokerCard> cards, bool attackerIsPlayer)
        {
            if (state == null)
                return;

            state.PendingPostResolveDrawToAttacker = 0;
            state.PendingGenericAttackShapeDisplayName = string.Empty;
            state.PendingGenericAttackShapeChoicePending = false;

            if (cards == null || cards.Count == 0)
            {
                state.PendingBaseDamage = 1;
                state.PendingGenericAttackShapeDisplayName = "\u5355\u5f20";
                state.PendingDamageCategory = DamageCategory.Generic;
                state.PendingDamageElement = DamageElement.None;
                return;
            }

            List<GenericAttackOption> opts = BuildSortedOptions(cards);
            if (opts.Count == 0)
            {
                state.PendingBaseDamage = 1;
                state.PendingGenericAttackShapeDisplayName = "\u5355\u5f20";
                state.PendingDamageCategory = DamageCategory.Generic;
                state.PendingDamageElement = DamageElement.None;
                return;
            }

            if (opts.Count == 1)
            {
                ApplyOption(state, opts[0]);
                return;
            }

            int idx = state.PendingGenericAttackOptionIndex;
            if (idx >= 0 && idx < opts.Count)
            {
                ApplyOption(state, opts[idx]);
                return;
            }

            if (!attackerIsPlayer)
            {
                int pick = PickBestOptionIndex(opts);
                if (pick >= 0)
                {
                    state.PendingGenericAttackOptionIndex = pick;
                    ApplyOption(state, opts[pick]);
                }
                else
                {
                    ApplyOption(state, opts[0]);
                }

                return;
            }

            state.PendingGenericAttackShapeChoicePending = true;
            state.PendingBaseDamage = 0;
            state.PendingPostResolveDrawToAttacker = 0;
        }

        private static void ApplyOption(BattleState state, in GenericAttackOption opt)
        {
            state.PendingBaseDamage = opt.Damage;
            state.PendingPostResolveDrawToAttacker = opt.PostDraw;
            state.PendingGenericAttackShapeDisplayName = opt.DisplayName;
            state.PendingDamageCategory = DamageCategory.Generic;
            state.PendingDamageElement = DamageElement.None;
        }

        private static bool TryClassifySubset(List<PokerCard> sub, out GenericAttackOption opt)
        {
            opt = default;
            if (sub == null || sub.Count == 0)
                return false;

            int n = sub.Count;

            if (n >= 4 && PokerPatternRules.IsFlexibleStraight(sub, n) && PokerPatternRules.IsFlush(sub))
            {
                opt = new GenericAttackOption(GenericAttackShapeKind.StraightFlush, 6, 0, "\u540c\u82b1\u987a");
                return true;
            }

            if (n == 5 && IsFullHouse(sub))
            {
                opt = new GenericAttackOption(GenericAttackShapeKind.FullHouse, 5, 2, "\u846b\u82a6");
                return true;
            }

            if (n == 4 && IsFourOfAKind(sub))
            {
                opt = new GenericAttackOption(GenericAttackShapeKind.FourOfAKind, 5, 0, "\u56db\u6761");
                return true;
            }

            // 纯顺子（同花顺已归上一档；BuildSortedOptions 会为同花顺再补一条顺子选项）
            if (n >= 4 && PokerPatternRules.IsFlexibleStraight(sub, n) && !PokerPatternRules.IsFlush(sub))
            {
                opt = new GenericAttackOption(GenericAttackShapeKind.Straight, 3, 1, "\u987a\u5b50");
                return true;
            }

            if (n == 4 && PokerPatternRules.IsFlush(sub) && !PokerPatternRules.IsFlexibleStraight(sub, 4))
            {
                opt = new GenericAttackOption(GenericAttackShapeKind.Flush, 4, 0, "\u540c\u82b1");
                return true;
            }

            if (n == 4 && IsTwoPair(sub))
            {
                opt = new GenericAttackOption(GenericAttackShapeKind.TwoPair, 4, 0, "\u4e24\u5bf9");
                return true;
            }

            if (n == 3 && IsTriple(sub))
            {
                opt = new GenericAttackOption(GenericAttackShapeKind.Triple, 3, 0, "\u4e09\u6761");
                return true;
            }

            if (n == 2 && IsPair(sub))
            {
                opt = new GenericAttackOption(GenericAttackShapeKind.Pair, 2, 0, "\u5bf9\u5b50");
                return true;
            }

            return false;
        }

        private static bool IsPair(List<PokerCard> cards) =>
            cards.Count == 2
            && PokerPatternRules.GetComparisonPoint(cards[0]) == PokerPatternRules.GetComparisonPoint(cards[1]);

        private static bool IsTriple(List<PokerCard> cards) =>
            cards.Count == 3
            && cards.All(c => PokerPatternRules.GetComparisonPoint(c) == PokerPatternRules.GetComparisonPoint(cards[0]));

        private static bool IsFourOfAKind(List<PokerCard> cards) =>
            cards.Count == 4
            && cards.All(c => PokerPatternRules.GetComparisonPoint(c) == PokerPatternRules.GetComparisonPoint(cards[0]));

        private static bool IsTwoPair(List<PokerCard> cards) =>
            cards.Count == 4 && PokerPatternRules.IsTwoPairCompositeFour(cards);

        private static bool IsFullHouse(List<PokerCard> cards) =>
            cards.Count == 5 && PokerPatternRules.IsFullHouseCompositeFive(cards);
    }
}
