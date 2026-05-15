using InSharpMcp.Contracts;

namespace InSharpMcp.Concurrency;

public interface IUiOperationQueue
{
    Task<ToolResult> RunAsync(
        string operationName,
        Func<CancellationToken, Task<ToolResult>> operation,
        ToolLimits limits,
        CancellationToken cancellationToken);
}
