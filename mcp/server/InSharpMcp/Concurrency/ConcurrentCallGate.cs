using InSharpMcp.Contracts;

namespace InSharpMcp.Concurrency;

public sealed class ConcurrentCallGate : IDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private bool _disposed;

    public ConcurrentCallGate(InSharpMcpConcurrencyOptions? options = null)
    {
        var configured = options ?? new InSharpMcpConcurrencyOptions();
        var maxConcurrentCalls = Math.Clamp(
            configured.MaxConcurrentCalls,
            1,
            configured.MaximumAllowedConcurrentCalls);
        _semaphore = new SemaphoreSlim(maxConcurrentCalls, maxConcurrentCalls);
    }

    public async Task<ToolResult> RunAsync(
        Func<CancellationToken, Task<ToolResult>> operation,
        TimeSpan waitTimeout,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        using var timeout = new CancellationTokenSource(waitTimeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        try
        {
            await _semaphore.WaitAsync(linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            return ToolResult.Fail("The MCP call concurrency limit is busy.", "busy");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return ToolResult.Fail("The MCP call was cancelled before execution.", "cancelled");
        }

        try
        {
            return await operation(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _semaphore.Dispose();
    }
}
