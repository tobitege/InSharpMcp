namespace InSharpMcp.Contracts;

public sealed record UiTreeSnapshot(
    UiElementNode Root,
    int NodeCount,
    bool Truncated);
