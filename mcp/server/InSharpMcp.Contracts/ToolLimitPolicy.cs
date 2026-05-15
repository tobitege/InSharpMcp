namespace InSharpMcp.Contracts;

public sealed record ToolLimitPolicy
{
    public ToolLimits Defaults { get; init; } = new();

    public int MinDepth { get; init; } = 1;

    public int MaxDepth { get; init; } = 50;

    public int MinNodes { get; init; } = 1;

    public int MaxNodes { get; init; } = 2_000;

    public int MinTextCharacters { get; init; } = 1_024;

    public int MaxTextCharacters { get; init; } = 256_000;
}
