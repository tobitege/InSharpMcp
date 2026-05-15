using InSharpMcp.Contracts;

namespace InSharpMcp.Events;

public sealed class BoundedEventLog : IEventLogProvider
{
    private readonly object _gate = new();
    private readonly int _capacity;
    private readonly Queue<EventLogEntry> _entries = new();

    public BoundedEventLog(int capacity = 500)
    {
        _capacity = capacity;
    }

    public void Add(EventLogEntry entry)
    {
        var redacted = entry with { Data = Redact(entry.Data) };
        lock (_gate)
        {
            while (_entries.Count >= _capacity)
            {
                _entries.Dequeue();
            }

            _entries.Enqueue(redacted);
        }
    }

    public IReadOnlyList<EventLogEntry> List(IReadOnlySet<string>? categories, int maximumCount)
    {
        lock (_gate)
        {
            return _entries
                .Where(entry => categories is null || categories.Contains(entry.Category))
                .TakeLast(maximumCount)
                .ToArray();
        }
    }

    private static IReadOnlyDictionary<string, string>? Redact(IReadOnlyDictionary<string, string>? data)
    {
        if (data is null)
        {
            return null;
        }

        return data.ToDictionary(pair => pair.Key, pair => IsSensitive(pair.Key) ? "<redacted>" : pair.Value, StringComparer.Ordinal);
    }

    private static bool IsSensitive(string key) =>
        key.Contains("password", StringComparison.OrdinalIgnoreCase)
        || key.Contains("secret", StringComparison.OrdinalIgnoreCase)
        || key.Contains("token", StringComparison.OrdinalIgnoreCase)
        || key.Contains("key", StringComparison.OrdinalIgnoreCase);
}
