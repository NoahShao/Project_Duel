using System.Collections.Generic;
using UnityEngine;

namespace JunzhenDuijue
{
    /// <summary>
    /// 【孙策·转斗千里】自由顺子 / 自由同花顺：整叠需满足自由顺子，长度范围 3~13。
    /// 不再允许「≤5 张时仅看子集」的宽松模式。
    /// </summary>
    public static class SunCeStraightRules
    {
        public const int MinLen = 3;
        public const int MaxLen = 13;

        /// <summary>至少 3 张且构成同花顺（与同花顺长度一致）。</summary>
        public static bool IsSunCeStraightFlush(IReadOnlyList<PokerCard> cards) =>
            FreeStraightRules.GetStraightFlushLengthIfValid(cards, MinLen, MaxLen) >= MinLen;

        /// <summary>至少 3 张构成顺子且<strong>非</strong>全同花（与同花顺区分）。</summary>
        public static bool IsSunCeStraightOnly(IReadOnlyList<PokerCard> cards) =>
            cards != null
            && cards.Count >= MinLen
            && cards.Count <= MaxLen
            && PokerPatternRules.IsFlexibleStraight(cards, cards.Count)
            && !PokerPatternRules.IsFlush(cards);

        /// <summary>打出序下是否允许当前张数（含将牌当牌）。</summary>
        public static bool IsValidSunCeBuildInPlayOrder(IReadOnlyList<PokerCard> cardsInPlayOrder)
        {
            if (cardsInPlayOrder == null)
                return false;

            int n = cardsInPlayOrder.Count;
            // 前五张允许任意组合，不限制牌型（只控制上限）。
            if (n <= 5)
                return true;
            if (n > MaxLen)
                return false;
            return PokerPatternRules.IsFlexibleStraight(cardsInPlayOrder, n);
        }

        /// <summary>
        /// 宣言时：整叠必须是 3~13 张的自由顺子。
        /// </summary>
        public static bool IsValidSunCeDeclareShape(IReadOnlyList<PokerCard> cardsInPlayOrder)
        {
            if (cardsInPlayOrder == null || cardsInPlayOrder.Count < MinLen || cardsInPlayOrder.Count > MaxLen)
                return false;

            int n = cardsInPlayOrder.Count;
            // 1~5 张宣言时，沿用「可从中取 >=3 张构成自由顺子」。
            if (n <= 5)
                return GetMaxFlexibleStraightSubsetLength(cardsInPlayOrder) >= MinLen;

            return IsValidSunCeBuildInPlayOrder(cardsInPlayOrder);
        }

        /// <summary>当前整叠可构成自由顺子时返回张数，否则 0。</summary>
        public static int GetMaxFlexibleStraightSubsetLength(IReadOnlyList<PokerCard> cards)
        {
            if (cards == null || cards.Count < MinLen || cards.Count > MaxLen)
                return 0;

            int total = cards.Count;
            if (total > 5)
                return PokerPatternRules.IsFlexibleStraight(cards, total) ? total : 0;

            int best = 0;
            int limit = 1 << total;
            for (int mask = 1; mask < limit; mask++)
            {
                int k = PopCount(mask);
                if (k < MinLen)
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

        /// <summary>当前整叠可构成自由同花顺时返回张数，否则 0。</summary>
        public static int GetMaxStraightFlushSubsetLength(IReadOnlyList<PokerCard> cards)
        {
            if (cards == null || cards.Count < MinLen || cards.Count > MaxLen)
                return 0;

            int total = cards.Count;
            if (total > 5)
                return (PokerPatternRules.IsFlexibleStraight(cards, total) && PokerPatternRules.IsFlush(cards)) ? total : 0;

            int best = 0;
            int limit = 1 << total;
            for (int mask = 1; mask < limit; mask++)
            {
                int k = PopCount(mask);
                if (k < MinLen)
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

        /// <summary>
        /// 出牌阶段追加一张（含将牌当牌）：总张数上限 13；3 张起要求整叠满足自由顺子。
        /// </summary>
        public static bool AllowsSunCeStackAppendAfterAdd(IReadOnlyList<PokerCard> playedInOrderBeforeAdd, PokerCard cardToAdd)
        {
            int nextCount = (playedInOrderBeforeAdd?.Count ?? 0) + 1;
            // 前五张自由出。
            if (nextCount <= 5)
                return true;
            if (nextCount > MaxLen)
                return false;

            var combined = new List<PokerCard>(nextCount);
            if (playedInOrderBeforeAdd != null && playedInOrderBeforeAdd.Count > 0)
                combined.AddRange(playedInOrderBeforeAdd);
            combined.Add(cardToAdd);
            return IsValidSunCeBuildInPlayOrder(combined);
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
    }
}
