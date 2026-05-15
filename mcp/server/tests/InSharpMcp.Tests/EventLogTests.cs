using InSharpMcp.Contracts;
using InSharpMcp.Events;
using InSharpMcp.Tools;

namespace InSharpMcp.Tests;

public sealed class EventLogTests
{
    [Fact]
    public void BoundedEventLog_RedactsSensitiveDataAndFiltersCategories()
    {
        var log = new BoundedEventLog();
        log.Add(new EventLogEntry(
            DateTimeOffset.UtcNow,
            "tool",
            "called",
            new Dictionary<string, string> { ["token"] = "secret", ["safe"] = "value" }));
        log.Add(new EventLogEntry(DateTimeOffset.UtcNow, "nav", "changed"));

        var result = InSharpMcpTools.GetEventLog(log, ["tool"], maximumCount: 10);

        var events = Assert.IsType<EventLogEntry[]>(result.Data);
        var entry = Assert.Single(events);
        Assert.Equal("<redacted>", entry.Data?["token"]);
        Assert.Equal("value", entry.Data?["safe"]);
    }
}
