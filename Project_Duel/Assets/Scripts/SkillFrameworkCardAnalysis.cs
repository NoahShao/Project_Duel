using System;
using System.Collections.Generic;
using System.Linq;

namespace JunzhenDuijue
{
    /// <summary>打出区牌型（与通用攻击一致）：对子/三条等用 <see cref="PokerPatternRules.GetComparisonPoint"/>；葫芦、两对用 <see cref="PokerPatternRules.IsFullHouseCompositeFive"/> / <see cref="PokerPatternRules.IsTwoPairCompositeFour"/>；顺子用 <see cref="PokerPatternRules.IsFlexibleStraight"/>。</summary>
    public static class SkillFrameworkCardAnalysis
    {
        public struct PlayedHandShape
        {
            public int CardCount;
            public bool AllRed;
            public bool AllBlack;
            public bool IsFlush;
            public int StraightLength;
            public int PairGroupCount;
            public int MaxSameRankCount;
            public bool IsPair;
            public bool IsTwoPair;
            public bool IsTriple;
            public bool IsFullHouse;
            public bool IsFourOfAKind;
        }

        public static int GetEffectiveRank(BattleState state, bool sideIsPlayer, PokerCard card)
        {
            _ = state;
            _ = sideIsPlayer;
            return PokerPatternRules.GetRankForAttackThreshold(card);
        }

        public static PlayedHandShape Analyze(BattleState state, bool sideIsPlayer, List<PokerCard> cards)
        {
            var shape = new PlayedHandShape();
            if (cards == null || cards.Count == 0)
                return shape;

            shape.CardCount = cards.Count;
            shape.AllRed = cards.TrueForAll(IsRedSuit);
            shape.AllBlack = cards.TrueForAll(IsBlackSuit);
            shape.IsFlush = IsFlush(cards);

            var ranks = cards.Select(c => PokerPatternRules.GetComparisonPoint(c)).ToList();
            // 顺子长度：与策马一致，A 可作 1 或 14。
            shape.StraightLength = cards.Count >= 2 && PokerPatternRules.IsFlexibleStraight(cards, cards.Count) ? cards.Count : 0;
            shape.PairGroupCount = CountPairGroups(ranks);
            shape.MaxSameRankCount = ranks.GroupBy(r => r).Select(g => g.Count()).DefaultIfEmpty(0).Max();

            shape.IsPair = cards.Count == 2 && shape.MaxSameRankCount == 2;
            shape.IsTriple = cards.Count == 3 && shape.MaxSameRankCount == 3;
            shape.IsFourOfAKind = cards.Count == 4 && shape.MaxSameRankCount == 4;
            shape.IsTwoPair = cards.Count == 4 && PokerPatternRules.IsTwoPairCompositeFour(cards);
            shape.IsFullHouse = cards.Count == 5 && PokerPatternRules.IsFullHouseCompositeFive(cards);

            return shape;
        }

        private static int CountPairGroups(List<int> effectiveRanks)
        {
            int n = 0;
            foreach (var g in effectiveRanks.GroupBy(r => r))
            {
                if (g.Count() >= 2)
                    n++;
            }
            return n;
        }

        private static bool IsFlush(List<PokerCard> cards)
        {
            string first = cards[0].Suit ?? string.Empty;
            for (int i = 1; i < cards.Count; i++)
            {
                if (!string.Equals(cards[i].Suit ?? string.Empty, first, System.StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        private static bool IsRedSuit(PokerCard c)
        {
            string s = c.Suit ?? string.Empty;
            return s == "\u7ea2\u6843" || s == "\u65b9\u7247";
        }

        private static bool IsBlackSuit(PokerCard c)
        {
            string s = c.Suit ?? string.Empty;
            return s == "\u9ed1\u6843" || s == "\u6885\u82b1";
        }
    }
}
