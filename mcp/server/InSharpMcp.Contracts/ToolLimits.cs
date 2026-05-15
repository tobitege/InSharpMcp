namespace InSharpMcp.Contracts;

public sealed record ToolLimits
{
    public int MaxDepth { get; init; } = 20;

    public int MaxNodes { get; init; } = 500;

    public int MaxTextCharacters { get; init; } = 64_000;

    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(5);

    public TimeSpan QueueTimeout { get; init; } = TimeSpan.FromSeconds(2);
}
