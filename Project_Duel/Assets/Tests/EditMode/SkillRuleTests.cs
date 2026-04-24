using System.Collections.Generic;
using NUnit.Framework;
using JunzhenDuijue;

namespace JunzhenDuijue.Tests.EditMode
{
    public class SkillRuleTests
    {
        [Test]
        public void GuessEffectId_AttackTag_ReturnsAttackRule()
        {
            var tags = new System.Collections.Generic.List<string> { "\u653b\u51fb\u6280" };
            Assert.AreEqual("attack_from_played_cards", SkillRuleHelper.GuessEffectId(tags));
            Assert.AreEqual("PlayPhaseAttack", SkillRuleHelper.GuessTriggerHint(tags));
            Assert.False(SkillRuleHelper.GuessAllowOnOpponentTurn(tags));
        }

        [Test]
        public void GuessEffectId_DefenseTag_ReturnsDefenseRule()
        {
            var tags = new System.Collections.Generic.List<string> { "\u9632\u5fa1\u6280" };
            Assert.AreEqual("reduce_damage_flat_1", SkillRuleHelper.GuessEffectId(tags));
            Assert.AreEqual("DefensePhase", SkillRuleHelper.GuessTriggerHint(tags));
            Assert.True(SkillRuleHelper.GuessAllowOnOpponentTurn(tags));
        }

        [Test]
        public void SkillRuleLoader_LoadsCompiledRule()
        {
            Assert.True(SkillRuleLoader.Load());
            var rule = SkillRuleLoader.GetRule("NO002", 0);
            Assert.NotNull(rule);
            Assert.AreEqual("NO002_0", rule.SkillKey);
        }

        /// <summary>【江东猛虎】梅花 J/Q/K + 黑桃 J：划分两对成立时，对子与两对应同时可选（仍打出四张）。</summary>
        [Test]
        public void JiangDongMenghu_FourFaceCompositeTwoPair_AllowsPairAndTwoPairChoice()
        {
            var cards = new List<PokerCard>
            {
                new PokerCard { Suit = "\u6885\u82b1", Rank = 11 },
                new PokerCard { Suit = "\u6885\u82b1", Rank = 12 },
                new PokerCard { Suit = "\u6885\u82b1", Rank = 13 },
                new PokerCard { Suit = "\u9ed1\u6843", Rank = 11 },
            };
            Assert.True(PokerPatternRules.IsTwoPairCompositeFour(cards));
            Assert.True(OfflineSkillEngine.JiangDongMenghuPlayedMatchesTwoPair(cards));
            Assert.True(OfflineSkillEngine.JiangDongMenghuPlayedMatchesPair(cards));
        }
    }
}
