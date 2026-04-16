using ProjectDuel.Shared.Protocol;
using ProjectDuel.Shared.Rules;
using ProjectDuel.Shared.SkillFramework;
using Xunit;

namespace ProjectDuel.Shared.Tests;

public class SkillFrameworkSyncTests
{
    private static DeckSelectionDto MakeDeck(string id, params string[] cards)
    {
        return new DeckSelectionDto
        {
            DeckId = id,
            DisplayName = id,
            RemovedSuit = string.Empty,
            CardIds = cards.ToList(),
        };
    }

    [Fact]
    public void TrySelectAttackSkill_InDefense_AppliesFrameworkRedSingle()
    {
        string path = Path.Combine(Path.GetTempPath(), "skill-fw-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(
                path,
                """
                {"Definitions":[{"SkillKey":"NO002_0","DisplayName":"策马斩将","AttackPatterns":[
                  {"Kind":1,"RequireAllRed":true,"BaseDamage":3,"Note":"红单"}
                ]}]}
                """);

            SkillFrameworkRegistry.ResetForTests();
            SkillFrameworkRegistry.Load(path);

            var state = AuthoritativeBattleEngine.StartMatch(
                MakeDeck("A", "NO002", "NO003", "NO004"),
                MakeDeck("B", "NO005", "NO006", "NO007"));
            state.Phase = DuelPhaseName.Defense;
            state.ActiveSeatIndex = 0;
            state.Sides[0].PlayedThisPhase.Add(new AuthoritativePokerCard { Suit = "\u7ea2\u6843", Rank = 5 });

            bool ok = AuthoritativeBattleEngine.TrySelectAttackSkill(state, 0, 0, 0, "\u7b56\u9a6c\u65a9\u5c06");

            Assert.True(ok);
            Assert.Equal(3, state.PendingBaseDamage);
            Assert.Equal(0, state.PendingAttackBonus);
        }
        finally
        {
            try
            {
                File.Delete(path);
            }
            catch
            {
                // ignore
            }

            SkillFrameworkRegistry.ResetForTests();
        }
    }

    [Fact]
    public void ResolveDamage_IgnoresDefense_WhenUnblockableFromFramework()
    {
        string path = Path.Combine(Path.GetTempPath(), "skill-fw-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(
                path,
                """
                {"Definitions":[{"SkillKey":"NO005_0","AttackPatterns":[
                  {"Kind":1,"MinEffectiveRankExclusive":9,"ExcludeFaceCourtWithoutChaShiTen":true,"BaseDamage":2,"Unblockable":true},
                  {"Kind":1,"MinEffectiveRankExclusive":6,"MaxEffectiveRankExclusive":10,"ExcludeFaceCourtWithoutChaShiTen":true,"BaseDamage":1,"Unblockable":true}
                ]}]}
                """);

            SkillFrameworkRegistry.ResetForTests();
            SkillFrameworkRegistry.Load(path);

            var state = AuthoritativeBattleEngine.StartMatch(
                MakeDeck("A", "NO005", "NO003", "NO004"),
                MakeDeck("B", "NO006", "NO007", "NO008"));
            state.Phase = DuelPhaseName.Defense;
            state.ActiveSeatIndex = 0;
            state.Sides[0].PlayedThisPhase.Add(new AuthoritativePokerCard { Suit = "\u9ed1\u6843", Rank = 10 });
            AuthoritativeBattleEngine.TrySelectAttackSkill(state, 0, 0, 0, "\u8fdc\u77e2\u8fde\u73e0");
            state.PendingDefenseReduction = 5;

            AuthoritativeBattleEngine.AdvancePhase(state);
            Assert.Equal(DuelPhaseName.Resolve, state.Phase);
            AuthoritativeBattleEngine.AdvancePhase(state);

            int expectedHp = 30 - 2;
            Assert.Equal(expectedHp, state.Sides[1].CurrentHp);
        }
        finally
        {
            try
            {
                File.Delete(path);
            }
            catch
            {
                // ignore
            }

            SkillFrameworkRegistry.ResetForTests();
        }
    }

}
