using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProjectDuel.Server.Options;
using ProjectDuel.Server.Services;
using Xunit;

namespace ProjectDuel.Server.Tests;

public class AuthoritativeConfigServiceTests
{
    [Fact]
    public void Load_ReadsSkillRulesJson()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new ConfigOptions
        {
            SkillRulesPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Project_Duel", "Assets", "StreamingAssets", "SkillRules.json"))
        });
        var service = new AuthoritativeConfigService(NullLogger<AuthoritativeConfigService>.Instance, options);

        service.Load();

        Assert.True(service.SkillRuleCount > 0);
        Assert.NotNull(service.GetRule("NO002", 0));
    }
}
