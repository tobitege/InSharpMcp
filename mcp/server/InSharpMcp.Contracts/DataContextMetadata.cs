namespace InSharpMcp.Contracts;

public sealed record DataContextMetadata(
    string TypeName,
    IReadOnlyDictionary<string, object?> Properties,
    bool Truncated);
