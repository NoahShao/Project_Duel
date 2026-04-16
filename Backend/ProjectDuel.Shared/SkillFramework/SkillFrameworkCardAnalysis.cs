using ProjectDuel.Shared.Rules;

namespace ProjectDuel.Shared.SkillFramework;

/// <summary>
/// 分析打出区牌型；花色名与 Unity 客户端一致（红桃、方片、黑桃、梅花）。
/// </summary>
public static class SkillFrameworkCardAnalysis
{
    public readonly struct PlayedHandShape
    {
        public int CardCount { get; init; }
        public bool AllRed { get; init; }
        public bool AllBlack { get; init; }
        public bool IsFlush { get; init; }
        public int StraightLength { get; init; }
        public int PairGroupCount { get; init; }
        public int MaxSameRankCount { get; init; }
        public bool IsPair { get; init; }
        public bool IsTwoPair { get; init; }
        public bool IsTriple { get; init; }
        public bool IsFullHouse { get; init; }
        public bool IsFourOfAKind { get; init; }
    }

    public static PlayedHandShape Analyze(AuthoritativeBattleState state, int attackerSeatIndex, IReadOnlyList<AuthoritativePokerCard> cards)
    {
        _ = state;
        _ = attackerSeatIndex;
        if (cards == null || cards.Count == 0)
            return new PlayedHandShape();

        var shape = new PlayedHandShape { CardCount = cards.Count };
        bool allRed = true;
        bool allBlack = true;
        for (int i = 0; i < cards.Count; i++)
        {
            if (!IsRedSuit(cards[i]))
                allRed = false;
            if (!IsBlackSuit(cards[i]))
                allBlack = false;
        }

        shape = shape with { AllRed = allRed, AllBlack = allBlack, IsFlush = AuthoritativePokerPatternRules.IsFlush(cards) };

        var ranks = cards.Select(AuthoritativePokerPatternRules.GetComparisonPoint).ToList();
        int straightLen = cards.Count >= 2 && AuthoritativePokerPatternRules.IsFlexibleStraight(cards, cards.Count) ? cards.Count : 0;
        int pairGroups = CountPairGroups(ranks);
        int maxSame = ranks.GroupBy(r => r).Select(g => g.Count()).DefaultIfEmpty(0).Max();

        shape = shape with
        {
            StraightLength = straightLen,
            PairGroupCount = pairGroups,
            MaxSameRankCount = maxSame,
            IsPair = cards.Count == 2 && maxSame == 2,
            IsTriple = cards.Count == 3 && maxSame == 3,
            IsFourOfAKind = cards.Count == 4 && maxSame == 4,
            IsTwoPair = cards.Count == 4 && AuthoritativePokerPatternRules.IsTwoPairCompositeFour(cards),
        };

        if (cards.Count == 5)
        {
            var groupSizes = ranks.GroupBy(r => r).Select(g => g.Count()).OrderByDescending(c => c).ToList();
            shape = shape with { IsFullHouse = AuthoritativePokerPatternRules.IsFullHouseCompositeFive(cards) };
        }

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

    private static bool IsRedSuit(AuthoritativePokerCard c)
    {
        string s = c.Suit ?? string.Empty;
        return s == "\u7ea2\u6843" || s == "\u65b9\u7247";
    }

    private static bool IsBlackSuit(AuthoritativePokerCard c)
    {
        string s = c.Suit ?? string.Empty;
        return s == "\u9ed1\u6843" || s == "\u6885\u82b1";
    }
}
