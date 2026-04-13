using System.Collections.Generic;

namespace JunzhenDuijue
{
    /// <summary>
    /// 花色与 <see cref="BattleState.Suits"/> 一致；红=红桃+方片，黑=黑桃+梅花。
    /// <see cref="GetComparisonPoint"/> 是默认点数（JQK 全是 0，故 JQ、QK、JK 都算同点可成对/三条等）；
    /// 顺子/同花顺用牌面 Rank（J=11、Q=12、K=13，A 可 1 或 14），见 <see cref="IsFlexibleStraight"/>。
    /// 葫芦、两对为「划分」型牌型：三条=同点三张或任意三张人牌；对子=同点两张或任意两张人牌，见 <see cref="IsFullHouseCompositeFive"/>、<see cref="IsTwoPairCompositeFour"/>。
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

        /// <summary>人牌 J/Q/K（Rank 11–13）。</summary>
        public static bool IsFaceCourtCard(PokerCard card) => card.Rank >= 11 && card.Rank <= 13;

        /// <summary>葫芦、两对中的对子：同 <see cref="GetComparisonPoint"/>，或两张均为人牌（可 JQ、JK、QK 混搭）。</summary>
        public static bool IsPairForCompositeShape(PokerCard a, PokerCard b)
        {
            if (IsFaceCourtCard(a) && IsFaceCourtCard(b))
                return true;
            return GetComparisonPoint(a) == GetComparisonPoint(b);
        }

        /// <summary>葫芦中的三条：同点三张，或三牌均为人牌（JQK 任意三张混搭）。</summary>
        public static bool IsTripleForCompositeShape(PokerCard a, PokerCard b, PokerCard c)
        {
            if (IsFaceCourtCard(a) && IsFaceCourtCard(b) && IsFaceCourtCard(c))
                return true;
            int p = GetComparisonPoint(a);
            return GetComparisonPoint(b) == p && GetComparisonPoint(c) == p;
        }

        /// <summary>五张能否分成一组三条 + 一组对子（两组「点数」可都落在人牌上，只看能否构成）。</summary>
        public static bool IsFullHouseCompositeFive(IReadOnlyList<PokerCard> cards)
        {
            if (cards == null || cards.Count != 5)
                return false;

            for (int i = 0; i < 5; i++)
            {
                for (int j = i + 1; j < 5; j++)
                {
                    for (int k = j + 1; k < 5; k++)
                    {
                        if (!IsTripleForCompositeShape(cards[i], cards[j], cards[k]))
                            continue;

                        var used = new bool[5];
                        used[i] = used[j] = used[k] = true;
                        int r0 = -1, r1 = -1;
                        for (int t = 0; t < 5; t++)
                        {
                            if (used[t])
                                continue;
                            if (r0 < 0)
                                r0 = t;
                            else
                                r1 = t;
                        }

                        if (IsPairForCompositeShape(cards[r0], cards[r1]))
                            return true;
                    }
                }
            }

            return false;
        }

        /// <summary>四张能否分成两对（每对满足 <see cref="IsPairForCompositeShape"/>）。</summary>
        public static bool IsTwoPairCompositeFour(IReadOnlyList<PokerCard> cards)
        {
            if (cards == null || cards.Count != 4)
                return false;

            for (int i = 0; i < 4; i++)
            {
                for (int j = i + 1; j < 4; j++)
                {
                    if (!IsPairForCompositeShape(cards[i], cards[j]))
                        continue;

                    int u0 = -1, u1 = -1;
                    for (int u = 0; u < 4; u++)
                    {
                        if (u == i || u == j)
                            continue;
                        if (u0 < 0)
                            u0 = u;
                        else
                            u1 = u;
                    }

                    if (IsPairForCompositeShape(cards[u0], cards[u1]))
                        return true;
                }
            }

            return false;
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
