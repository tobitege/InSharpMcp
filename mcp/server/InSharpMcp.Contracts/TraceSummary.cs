namespace InSharpMcp.Contracts;

public sealed record TraceSummary(
    string TraceId,
    DateTimeOffset StartedAt,
    DateTimeOffset StoppedAt,
    IReadOnlyList<EventLogEntry> Events,
    bool Truncated);
