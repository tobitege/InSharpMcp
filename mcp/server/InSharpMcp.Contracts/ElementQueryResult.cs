namespace InSharpMcp.Contracts;

public sealed record ElementQueryResult(
    IReadOnlyList<UiElementNode> Matches,
    bool Truncated);
