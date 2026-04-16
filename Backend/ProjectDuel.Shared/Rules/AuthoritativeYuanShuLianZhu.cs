using System.Collections.Generic;

namespace ProjectDuel.Shared.Rules;

/// <summary>
/// 【远矢连珠】NO005_0：与 Unity 客户端 OfflineSkillEngine 档位判定一致。
/// 权威服无二级弹窗时按「大10点优先，否则大7点」自动结算。
/// </summary>
public static class AuthoritativeYuanShuLianZhu
{
    public static bool Tier10Matches(AuthoritativePokerCard c) =>
        c != null && SingleCardRankMatches(c, minExclusive: 9, maxExclusive: 0, excludeFaceWithoutChaShiTen: true);

    public static bool Tier7Matches(AuthoritativePokerCard c) =>
        c != null && SingleCardRankMatches(c, minExclusive: 6, maxExclusive: 10, excludeFaceWithoutChaShiTen: true);

    public static bool TryAutoConfigureAttack(AuthoritativeBattleState state, IReadOnlyList<AuthoritativePokerCard> played)
    {
        if (state == null || played == null || played.Count == 0)
            return false;

        for (int i = 0; i < played.Count; i++)
        {
            AuthoritativePokerCard c = played[i];
            if (Tier10Matches(c))
            {
                state.PendingBaseDamage = 2;
                state.PendingIgnoreDefenseReduction = true;
                return true;
            }
        }

        for (int j = 0; j < played.Count; j++)
        {
            AuthoritativePokerCard c2 = played[j];
            if (Tier7Matches(c2))
            {
                state.PendingBaseDamage = 1;
                state.PendingIgnoreDefenseReduction = true;
                return true;
            }
        }

        return false;
    }

    private static bool SingleCardRankMatches(AuthoritativePokerCard c, int minExclusive, int maxExclusive, bool excludeFaceWithoutChaShiTen)
    {
        if (excludeFaceWithoutChaShiTen && c.Rank is >= 11 and <= 13 && !c.ChaShiCourtPlayedAsTen)
            return false;

        int r = AuthoritativePokerPatternRules.GetRankForAttackThreshold(c);
        if (minExclusive > 0 && r <= minExclusive)
            return false;
        if (maxExclusive > 0 && r >= maxExclusive)
            return false;
        return true;
    }
}
