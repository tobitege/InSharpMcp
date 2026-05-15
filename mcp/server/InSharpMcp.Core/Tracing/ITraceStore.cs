using InSharpMcp.Contracts;

namespace InSharpMcp.Tracing;

public interface ITraceStore
{
    string Start();

    void Record(EventLogEntry entry);

    ToolResult Stop(string traceId);
}
