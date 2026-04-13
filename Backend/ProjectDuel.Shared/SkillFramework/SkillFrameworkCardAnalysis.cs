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

    public static int GetEffectiveRank(AuthoritativeBattleState state, int attackerSeatIndex, AuthoritativePokerCard card)
    {
        int rank = card.Rank;
        if (rank is >= 11 and <= 13 && HasFaceUpSkillKey(state, attackerSeatIndex, "NO004_0"))
            return 10;
        return rank;
    }

    private static bool HasFaceUpSkillKey(AuthoritativeBattleState state, int seatIndex, string skillKey)
    {
        if (state == null || string.IsNullOrWhiteSpace(skillKey) || seatIndex < 0 || seatIndex >= state.Sides.Length)
            return false;

        var side = state.Sides[seatIndex];
        for (int generalIndex = 0; generalIndex < side.GeneralCardIds.Count; generalIndex++)
        {
            if (generalIndex >= side.GeneralFaceUp.Count || !side.GeneralFaceUp[generalIndex])
                continue;

            string cardId = side.GeneralCardIds[generalIndex] ?? string.Empty;
            for (int skillIndex = 0; skillIndex < 3; skillIndex++)
            {
                if (string.Equals($"{cardId}_{skillIndex}", skillKey, StringComparison.Ordinal))
                    return true;
            }
        }

        return false;
    }

    public static PlayedHandShape Analyze(AuthoritativeBattleState state, int attackerSeatIndex, IReadOnlyList<AuthoritativePokerCard> cards)
    {
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

        shape = shape with { AllRed = allRed, AllBlack = allBlack, IsFlush = IsFlush(cards) };

        var ranks = cards.Select(c => GetEffectiveRank(state, attackerSeatIndex, c)).ToList();
        int straightLen = ComputeStraightLength(ranks, cards.Count);
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
            IsTwoPair = cards.Count == 4 && pairGroups == 2,
        };

        if (cards.Count == 5)
        {
            var groupSizes = ranks.GroupBy(r => r).Select(g => g.Count()).OrderByDescending(c => c).ToList();
            shape = shape with { IsFullHouse = groupSizes.Count == 2 && groupSizes[0] == 3 && groupSizes[1] == 2 };
        }

        return shape;
    }

    private static int ComputeStraightLength(List<int> effectiveRanks, int cardCount)
    {
        var distinct = effectiveRanks.Distinct().OrderBy(r => r).ToList();
        if (distinct.Count != cardCount || cardCount == 0)
            return 0;
        for (int i = 1; i < distinct.Count; i++)
        {
            if (distinct[i] != distinct[i - 1] + 1)
                return 0;
        }

        return distinct.Count;
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

    private static bool IsFlush(IReadOnlyList<AuthoritativePokerCard> cards)
    {
        string first = cards[0].Suit ?? string.Empty;
        for (int i = 1; i < cards.Count; i++)
        {
            if (!string.Equals(cards[i].Suit ?? string.Empty, first, StringComparison.Ordinal))
                return false;
        }

        return true;
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
