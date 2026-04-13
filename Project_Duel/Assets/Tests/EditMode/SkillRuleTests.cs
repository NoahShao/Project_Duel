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
    }
}
