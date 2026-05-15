using InSharpMcp.Contracts;

namespace InSharpMcp.Tracing;

public sealed class BoundedTraceStore : ITraceStore
{
    private readonly object _gate = new();
    private readonly int _maximumEvents;
    private TraceSession? _active;

    public BoundedTraceStore(int maximumEvents = 1_000)
    {
        _maximumEvents = maximumEvents;
    }

    public string Start()
    {
        lock (_gate)
        {
            var traceId = Guid.NewGuid().ToString("N");
            _active = new TraceSession(traceId, DateTimeOffset.UtcNow);
            return traceId;
        }
    }

    public void Record(EventLogEntry entry)
    {
        lock (_gate)
        {
            if (_active is null)
            {
                return;
            }

            if (_active.Events.Count >= _maximumEvents)
            {
                _active.Truncated = true;
                return;
            }

            _active.Events.Add(entry);
        }
    }

    public ToolResult Stop(string traceId)
    {
        lock (_gate)
        {
            if (_active is null || !string.Equals(_active.TraceId, traceId, StringComparison.Ordinal))
            {
                return ToolResult.Fail("Trace was not found.", "not_found");
            }

            var summary = new TraceSummary(
                _active.TraceId,
                _active.StartedAt,
                DateTimeOffset.UtcNow,
                _active.Events.ToArray(),
                _active.Truncated);
            _active = null;
            return ToolResult.Ok("Trace stopped.", summary);
        }
    }

    private sealed class TraceSession
    {
        public TraceSession(string traceId, DateTimeOffset startedAt)
        {
            TraceId = traceId;
            StartedAt = startedAt;
        }

        public string TraceId { get; }

        public DateTimeOffset StartedAt { get; }

        public List<EventLogEntry> Events { get; } = [];

        public bool Truncated { get; set; }
    }
}
