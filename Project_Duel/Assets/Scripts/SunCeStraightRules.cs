using System.Collections.Generic;
using UnityEngine;

namespace JunzhenDuijue
{
    /// <summary>
    /// 【孙策·转斗千里】自由顺子 / 自由同花顺：长度 ≥3 且 <see cref="PokerPatternRules.IsFlexibleStraight"/>。
    /// 打出区张数 ≤5 时：允许带「不参与顺子的废牌」，只要存在 ≥3 张的子集构成顺子即可宣言；同花顺同理看子集。
    /// 张数 &gt;5 时：整叠须能构成自由顺子（与同花顺）；打出顺序不要求末张落在顺子端点。
    /// </summary>
    public static class SunCeStraightRules
    {
        /// <summary>至少 3 张且构成同花顺（与同花顺长度一致）。</summary>
        public static bool IsSunCeStraightFlush(IReadOnlyList<PokerCard> cards) =>
            cards != null
            && cards.Count >= 3
            && PokerPatternRules.IsFlexibleStraight(cards, cards.Count)
            && PokerPatternRules.IsFlush(cards);

        /// <summary>至少 3 张构成顺子且<strong>非</strong>全同花（与同花顺区分）。</summary>
        public static bool IsSunCeStraightOnly(IReadOnlyList<PokerCard> cards) =>
            cards != null
            && cards.Count >= 3
            && PokerPatternRules.IsFlexibleStraight(cards, cards.Count)
            && !PokerPatternRules.IsFlush(cards);

        /// <summary>打出序下是否允许当前张数（含将牌当牌）。</summary>
        public static bool IsValidSunCeBuildInPlayOrder(IReadOnlyList<PokerCard> cardsInPlayOrder)
        {
            if (cardsInPlayOrder == null)
                return false;
            int n = cardsInPlayOrder.Count;
            if (n == 0 || n == 1 || n == 2)
                return true;
            return PokerPatternRules.IsFlexibleStraight(cardsInPlayOrder, n);
        }

        /// <summary>
        /// 宣言时：≤5 张为「存在 ≥3 张的顺子子集」；&gt;5 张为整叠 <see cref="IsValidSunCeBuildInPlayOrder"/>。
        /// </summary>
        public static bool IsValidSunCeDeclareShape(IReadOnlyList<PokerCard> cardsInPlayOrder)
        {
            if (cardsInPlayOrder == null || cardsInPlayOrder.Count < 3)
                return false;
            int n = cardsInPlayOrder.Count;
            if (n <= 5)
                return GetMaxFlexibleStraightSubsetLength(cardsInPlayOrder) >= 3;
            return IsValidSunCeBuildInPlayOrder(cardsInPlayOrder);
        }

        /// <summary>打出区 ≤5 张时：子集中能构成的最长顺子长度（无则 0）。</summary>
        public static int GetMaxFlexibleStraightSubsetLength(IReadOnlyList<PokerCard> cards)
        {
            if (cards == null || cards.Count < 3)
                return 0;
            int total = cards.Count;
            if (total > 5)
                return 0;

            int best = 0;
            int limit = 1 << total;
            for (int mask = 1; mask < limit; mask++)
            {
                int k = PopCount(mask);
                if (k < 3)
                    continue;
                var sub = new List<PokerCard>(k);
                for (int i = 0; i < total; i++)
                {
                    if ((mask & (1 << i)) != 0)
                        sub.Add(cards[i]);
                }

                if (PokerPatternRules.IsFlexibleStraight(sub, k))
                    best = Mathf.Max(best, k);
            }

            return best;
        }

        /// <summary>打出区 ≤5 张时：子集中能构成的最长同花顺长度（无则 0）。</summary>
        public static int GetMaxStraightFlushSubsetLength(IReadOnlyList<PokerCard> cards)
        {
            if (cards == null || cards.Count < 3)
                return 0;
            int total = cards.Count;
            if (total > 5)
                return 0;

            int best = 0;
            int limit = 1 << total;
            for (int mask = 1; mask < limit; mask++)
            {
                int k = PopCount(mask);
                if (k < 3)
                    continue;
                var sub = new List<PokerCard>(k);
                for (int i = 0; i < total; i++)
                {
                    if ((mask & (1 << i)) != 0)
                        sub.Add(cards[i]);
                }

                if (PokerPatternRules.IsFlexibleStraight(sub, k) && PokerPatternRules.IsFlush(sub))
                    best = Mathf.Max(best, k);
            }

            return best;
        }

        private static int PopCount(int mask)
        {
            int c = 0;
            while (mask != 0)
            {
                c++;
                mask &= mask - 1;
            }

            return c;
        }

        /// <summary>
        /// 出牌阶段追加一张（含将牌当牌）：张数 ≤5 时不校验；&gt;5 时须整叠满足 <see cref="IsValidSunCeBuildInPlayOrder"/>。
        /// </summary>
        public static bool AllowsSunCeStackAppendAfterAdd(IReadOnlyList<PokerCard> playedInOrderBeforeAdd, PokerCard cardToAdd)
        {
            int nextCount = (playedInOrderBeforeAdd?.Count ?? 0) + 1;
            if (nextCount <= 5)
                return true;

            var combined = new List<PokerCard>(nextCount);
            if (playedInOrderBeforeAdd != null && playedInOrderBeforeAdd.Count > 0)
                combined.AddRange(playedInOrderBeforeAdd);
            combined.Add(cardToAdd);
            return IsValidSunCeBuildInPlayOrder(combined);
        }
    }
}
