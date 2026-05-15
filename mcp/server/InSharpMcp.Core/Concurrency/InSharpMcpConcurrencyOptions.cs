namespace InSharpMcp.Concurrency;

public sealed record InSharpMcpConcurrencyOptions
{
    public int MaxConcurrentCalls { get; init; } = 1;

    public int MaximumAllowedConcurrentCalls { get; init; } = 5;

    public int MaxQueuedUiOperations { get; init; } = 8;
}
