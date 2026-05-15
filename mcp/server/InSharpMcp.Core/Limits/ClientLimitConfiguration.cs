namespace InSharpMcp.Limits;

public sealed record ClientLimitConfiguration(
    string? MaxDepth = null,
    string? MaxNodes = null,
    string? MaxTextCharacters = null);
