namespace InSharpMcp.Contracts;

public interface IEventLogProvider
{
    void Add(EventLogEntry entry);

    IReadOnlyList<EventLogEntry> List(IReadOnlySet<string>? categories, int maximumCount);
}
