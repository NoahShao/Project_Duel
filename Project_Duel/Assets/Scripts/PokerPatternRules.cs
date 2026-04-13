using System.Collections.Generic;

namespace JunzhenDuijue
{
    /// <summary>
    /// 花色与 <see cref="BattleState.Suits"/> 一致；红=红桃+方片，黑=黑桃+梅花。
    /// 规则就两句：<see cref="GetComparisonPoint"/> 是默认点数（JQK 全是 0，故 JQ、QK、JK 都算同点可成对/三条等）；
    /// 只有顺子/同花顺用牌面 Rank（J=11、Q=12、K=13，A 可 1 或 14），见 <see cref="IsFlexibleStraight"/>。
    /// </summary>
    public static class PokerPatternRules
    {
        private const string Hearts = "\u7ea2\u6843";
        private const string Diamonds = "\u65b9\u7247";
        private const string Spades = "\u9ed1\u6843";
        private const string Clubs = "\u6885\u82b1";

        public static bool IsRedSuit(string suit)
        {
            if (string.IsNullOrEmpty(suit))
                return false;
            return suit == Hearts || suit == Diamonds;
        }

        public static bool IsBlackSuit(string suit)
        {
            if (string.IsNullOrEmpty(suit))
                return false;
            return suit == Spades || suit == Clubs;
        }

        public static bool IsRedCard(PokerCard card) => IsRedSuit(card.Suit);

        public static bool IsBlackCard(PokerCard card) => IsBlackSuit(card.Suit);

        /// <summary>默认点数（对子、两对、三条……都按这个分组）。A=1，2–10=面值，J/Q/K=0。</summary>
        public static int GetComparisonPoint(PokerCard card)
        {
            if (card.Rank == 1)
                return 1;
            if (card.Rank >= 11 && card.Rank <= 13)
                return 0;
            return card.Rank;
        }

        /// <summary>
        /// 当前打出牌是否构成指定张数的顺子（点数连续、张张不同点）。
        /// </summary>
        public static bool IsFlexibleStraight(IReadOnlyList<PokerCard> cards, int count)
        {
            if (cards == null || cards.Count != count || count < 2)
                return false;

            var options = new List<int[]>(count);
            for (int i = 0; i < cards.Count; i++)
            {
                int r = cards[i].Rank;
                if (r == 1)
                    options.Add(new[] { 1, 14 });
                else if (r >= 2 && r <= 13)
                    options.Add(new[] { r });
                else
                    return false;
            }

            var assigned = new int[count];
            var found = false;

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

        public static bool IsFlush(IReadOnlyList<PokerCard> cards)
        {
            if (cards == null || cards.Count == 0)
                return false;
            string s0 = cards[0].Suit ?? string.Empty;
            for (int i = 1; i < cards.Count; i++)
            {
                if ((cards[i].Suit ?? string.Empty) != s0)
                    return false;
            }

            return true;
        }
    }
}
