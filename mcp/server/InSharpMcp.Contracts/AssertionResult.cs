namespace InSharpMcp.Contracts;

public sealed record AssertionResult(
    bool Passed,
    string Message,
    object? Actual = null);
