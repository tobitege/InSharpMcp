namespace InSharpMcp.Registry;

public sealed record AppRegistrationOptions
{
    public TimeSpan StaleInstanceAge { get; init; } = TimeSpan.FromSeconds(30);
}
