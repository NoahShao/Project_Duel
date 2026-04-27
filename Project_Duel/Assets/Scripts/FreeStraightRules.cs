using System.Collections.Generic;

namespace JunzhenDuijue
{
    /// <summary>
    /// 通用「自由顺子 / 自由同花顺」规则。
    /// 支持长度范围与 A 高低位策略配置，供不同技能复用。
    /// </summary>
    public static class FreeStraightRules
    {
        public readonly struct AcePolicy
        {
            public readonly bool RequireJqkForHighAce;
            public readonly bool ForceLowWhenHas234;

            public AcePolicy(bool requireJqkForHighAce, bool forceLowWhenHas234)
            {
                RequireJqkForHighAce = requireJqkForHighAce;
                ForceLowWhenHas234 = forceLowWhenHas234;
            }
        }

        /// <summary>当前项目通用策略：有 JQK 且无 234 时 A=14，否则 A=1。</summary>
        public static readonly AcePolicy JqkWithout234HighElseLow = new AcePolicy(requireJqkForHighAce: true, forceLowWhenHas234: true);

        public static bool IsValidBuildInPlayOrder(IReadOnlyList<PokerCard> cardsInPlayOrder, int minLen, int maxLen)
        {
            if (cardsInPlayOrder == null)
                return false;

            int n = cardsInPlayOrder.Count;
            if (n < minLen)
                return n <= 2;
            if (n > maxLen)
                return false;
            return PokerPatternRules.IsFlexibleStraight(cardsInPlayOrder, n);
        }

        public static bool IsValidDeclareShape(IReadOnlyList<PokerCard> cardsInPlayOrder, int minLen, int maxLen)
        {
            if (cardsInPlayOrder == null)
                return false;

            int n = cardsInPlayOrder.Count;
            if (n < minLen || n > maxLen)
                return false;
            return IsValidBuildInPlayOrder(cardsInPlayOrder, minLen, maxLen);
        }

        public static int GetStraightLengthIfValid(IReadOnlyList<PokerCard> cards, int minLen, int maxLen)
        {
            if (cards == null || cards.Count < minLen || cards.Count > maxLen)
                return 0;
            return PokerPatternRules.IsFlexibleStraight(cards, cards.Count) ? cards.Count : 0;
        }

        public static int GetStraightFlushLengthIfValid(IReadOnlyList<PokerCard> cards, int minLen, int maxLen)
        {
            if (cards == null || cards.Count < minLen || cards.Count > maxLen)
                return 0;
            return (PokerPatternRules.IsFlexibleStraight(cards, cards.Count) && PokerPatternRules.IsFlush(cards)) ? cards.Count : 0;
        }

        public static bool AllowsAppendAfterAdd(IReadOnlyList<PokerCard> playedInOrderBeforeAdd, PokerCard cardToAdd, int minLen, int maxLen)
        {
            int nextCount = (playedInOrderBeforeAdd?.Count ?? 0) + 1;
            if (nextCount <= 2)
                return true;
            if (nextCount > maxLen)
                return false;

            var combined = new List<PokerCard>(nextCount);
            if (playedInOrderBeforeAdd != null && playedInOrderBeforeAdd.Count > 0)
                combined.AddRange(playedInOrderBeforeAdd);
            combined.Add(cardToAdd);
            return IsValidBuildInPlayOrder(combined, minLen, maxLen);
        }

        public static int GetEffectiveAceRank(IReadOnlyList<PokerCard> cards, int aceIndex, AcePolicy policy)
        {
            if (cards == null || aceIndex < 0 || aceIndex >= cards.Count || cards[aceIndex].Rank != 1)
                return 1;

            bool hasJ = false, hasQ = false, hasK = false;
            bool has2 = false, has3 = false, has4 = false;
            for (int i = 0; i < cards.Count; i++)
            {
                PokerCard c = cards[i];
                if (c.Rank == 11) hasJ = true;
                else if (c.Rank == 12) hasQ = true;
                else if (c.Rank == 13) hasK = true;
                else if (c.Rank == 2) has2 = true;
                else if (c.Rank == 3) has3 = true;
                else if (c.Rank == 4) has4 = true;
            }

            bool hasJqk = hasJ && hasQ && hasK;
            bool has234 = has2 && has3 && has4;

            if (policy.RequireJqkForHighAce && !hasJqk)
                return 1;
            if (policy.ForceLowWhenHas234 && has234)
                return 1;
            return 14;
        }
    }
}
