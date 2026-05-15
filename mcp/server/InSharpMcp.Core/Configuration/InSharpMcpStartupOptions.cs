namespace InSharpMcp.Configuration;

public sealed record InSharpMcpStartupOptions
{
    public const string EnabledEnvironmentVariable = "ISM_ENABLED";

    public bool Enabled { get; init; }

    public static InSharpMcpStartupOptions FromEnvironment() =>
        new()
        {
            Enabled = string.Equals(
                Environment.GetEnvironmentVariable(EnabledEnvironmentVariable),
                "1",
                StringComparison.Ordinal),
        };
}
