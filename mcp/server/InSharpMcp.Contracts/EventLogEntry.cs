namespace InSharpMcp.Contracts;

public sealed record EventLogEntry(
    DateTimeOffset Timestamp,
    string Category,
    string Message,
    IReadOnlyDictionary<string, string>? Data = null);
