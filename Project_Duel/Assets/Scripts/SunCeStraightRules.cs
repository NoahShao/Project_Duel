using System.Collections.Generic;
using UnityEngine;

namespace JunzhenDuijue
{
    /// <summary>
    /// 【孙策·转斗千里】自由顺子 / 自由同花顺：长度 ≥3 且 <see cref="PokerPatternRules.IsFlexibleStraight"/>；
    /// 张数 &gt;5 时，按<strong>打出顺序</strong>最后一张须在合法赋值下处于顺子端点（延伸连续段）。
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
            if (!PokerPatternRules.IsFlexibleStraight(cardsInPlayOrder, n))
                return false;
            if (n <= 5)
                return true;
            return IsFlexibleStraightWithLastAtEndpoint(cardsInPlayOrder);
        }

        /// <summary>
        /// 与 <see cref="IsValidSunCeBuildInPlayOrder"/> 相同，但要求 n≥3 且已能构成顺子或同花顺（用于宣言配置）。
        /// </summary>
        public static bool IsValidSunCeDeclareShape(IReadOnlyList<PokerCard> cardsInPlayOrder)
        {
            if (cardsInPlayOrder == null || cardsInPlayOrder.Count < 3)
                return false;
            return IsValidSunCeBuildInPlayOrder(cardsInPlayOrder);
        }

        /// <summary>
        /// 出牌阶段追加一张（含将牌当牌）：张数 ≤5 时不校验；&gt;5 时须满足 <see cref="IsValidSunCeBuildInPlayOrder"/>（含端点延伸）。
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

        /// <summary>与 <see cref="PokerPatternRules.IsFlexibleStraight"/> 同源 DFS，但 n&gt;5 时要求末张（打出序最后一牌）赋值落在顺子端点。</summary>
        private static bool IsFlexibleStraightWithLastAtEndpoint(IReadOnlyList<PokerCard> cards)
        {
            int count = cards.Count;
            if (count < 6)
                return true;

            var options = new List<int[]>(count);
            for (int i = 0; i < count; i++)
            {
                PokerCard c = cards[i];
                int r = c.Rank;
                if (r == 1)
                    options.Add(new[] { 1, 14 });
                else if (r is >= 2 and <= 10)
                    options.Add(new[] { r });
                else if (r is >= 11 and <= 13)
                {
                    if (!c.PlayedAsGeneral && c.ChaShiCourtPlayedAsTen)
                        options.Add(new[] { 10 });
                    else
                        options.Add(new[] { r });
                }
                else
                    return false;
            }

            var assigned = new int[count];
            bool found = false;

            void Dfs(int index)
            {
                if (found)
                    return;
                if (index >= count)
                {
                    var sorted = new List<int>(assigned);
                    sorted.Sort();
                    for (int k = 1; k < sorted.Count; k++)
                    {
                        if (sorted[k] != sorted[k - 1] + 1)
                            return;
                    }

                    int last = assigned[count - 1];
                    if (last != sorted[0] && last != sorted[sorted.Count - 1])
                        return;
                    found = true;
                    return;
                }

                foreach (int v in options[index])
                {
                    bool dup = false;
                    for (int j = 0; j < index; j++)
                    {
                        if (assigned[j] == v)
                        {
                            dup = true;
                            break;
                        }
                    }

                    if (dup)
                        continue;
                    assigned[index] = v;
                    Dfs(index + 1);
                }
            }

            Dfs(0);
            return found;
        }
    }
}
