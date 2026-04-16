namespace ProjectDuel.Shared.Rules;

/// <summary>
/// 与 Unity 客户端 PokerPatternRules 对齐的牌型工具（权威服）。
/// </summary>
public static class AuthoritativePokerPatternRules
{
    public const string PassiveChaShiJqkEffectId = "passive_chashi_jqk";

    public static bool IsFaceCourtCard(AuthoritativePokerCard card) =>
        card.Rank is >= 11 and <= 13;

    /// <summary>对子/三条/四条等分组用点数：A=1；2–10=面值；人牌 JQK 默认同为 0；察势「作10」时非角色 JQK 为 10。</summary>
    public static int GetComparisonPoint(AuthoritativePokerCard card)
    {
        if (card.Rank == 1)
            return 1;
        if (card.Rank >= 11 && card.Rank <= 13)
        {
            if (card.ChaShiCourtPlayedAsTen)
                return 10;
            return 0;
        }

        return card.Rank;
    }

    public static bool IsPairForCompositeShape(AuthoritativePokerCard a, AuthoritativePokerCard b)
    {
        if (IsFaceCourtCard(a) && IsFaceCourtCard(b))
            return true;
        return GetComparisonPoint(a) == GetComparisonPoint(b);
    }

    public static bool IsTripleForCompositeShape(AuthoritativePokerCard a, AuthoritativePokerCard b, AuthoritativePokerCard c)
    {
        if (IsFaceCourtCard(a) && IsFaceCourtCard(b) && IsFaceCourtCard(c))
            return true;
        int p = GetComparisonPoint(a);
        return GetComparisonPoint(b) == p && GetComparisonPoint(c) == p;
    }

    public static bool IsFullHouseCompositeFive(IReadOnlyList<AuthoritativePokerCard> cards)
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

    public static bool IsTwoPairCompositeFour(IReadOnlyList<AuthoritativePokerCard> cards)
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

    public static bool IsFlexibleStraight(IReadOnlyList<AuthoritativePokerCard> cards, int count)
    {
        if (cards == null || cards.Count != count || count < 2)
            return false;

        var options = new List<int[]>(count);
        for (int i = 0; i < cards.Count; i++)
        {
            int r = cards[i].Rank;
            if (r == 1)
                options.Add(new[] { 1, 14 });
            else if (r is >= 2 and <= 10)
                options.Add(new[] { r });
            else if (r is >= 11 and <= 13)
            {
                if (cards[i].ChaShiCourtPlayedAsTen)
                    options.Add(new[] { 10 });
                else
                    options.Add(new[] { r });
            }
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

    public static bool IsFlush(IReadOnlyList<AuthoritativePokerCard> cards)
    {
        if (cards == null || cards.Count == 0)
            return false;
        string s0 = cards[0].Suit ?? string.Empty;
        for (int i = 1; i < cards.Count; i++)
        {
            if (!string.Equals(cards[i].Suit ?? string.Empty, s0, StringComparison.Ordinal))
                return false;
        }

        return true;
    }

    /// <summary>单牌攻击阈值等：察势作10 时按 10，否则保持牌面 Rank（含 J=11）。</summary>
    public static int GetRankForAttackThreshold(AuthoritativePokerCard c)
    {
        if (c.Rank >= 11 && c.Rank <= 13 && c.ChaShiCourtPlayedAsTen)
            return 10;
        return c.Rank;
    }
}
