using System;

namespace JunzhenDuijue
{
    /// <summary>【察势】被动：非角色 J/Q/K 可声明作 10 点；配置见 SkillRules <c>passive_chashi_jqk</c>。</summary>
    public static class ChaShiSkillRules
    {
        public const string PassiveEffectId = "passive_chashi_jqk";

        public static bool SideHasFaceUpChaShiPassive(BattleState state, bool sideIsPlayer)
        {
            if (state == null)
                return false;

            var side = state.GetSide(sideIsPlayer);
            for (int gi = 0; gi < side.GeneralCardIds.Count; gi++)
            {
                if (!side.IsGeneralFaceUp(gi))
                    continue;

                string cid = side.GeneralCardIds[gi] ?? string.Empty;
                for (int sk = 0; sk < 3; sk++)
                {
                    SkillRuleEntry rule = SkillRuleLoader.GetRule(cid, sk);
                    if (rule != null && string.Equals(rule.EffectId, PassiveEffectId, StringComparison.Ordinal))
                        return true;
                }
            }

            return false;
        }

        public static bool HandDeckCardNeedsChaShiChoice(BattleState state, bool sideIsPlayer, PokerCard card)
        {
            if (state == null || card.PlayedAsGeneral)
                return false;
            if (card.Rank < 11 || card.Rank > 13)
                return false;
            return SideHasFaceUpChaShiPassive(state, sideIsPlayer);
        }
    }
}
