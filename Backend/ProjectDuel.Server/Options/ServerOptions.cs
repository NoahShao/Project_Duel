namespace ProjectDuel.Server.Options;

public sealed class ServerOptions
{
    public const string SectionName = "Server";
    public string Name { get; set; } = "ProjectDuel Server";
    public bool AllowAnonymous { get; set; } = true;
}
