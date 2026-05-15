using InSharpMcp.Contracts;

namespace InSharpMcp.Concurrency;

public sealed class UiOperationQueue : IUiOperationQueue, IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly object _gate = new();
    private readonly int _maxQueuedOperations;
    private int _queuedOperations;
    private bool _disposed;

    public UiOperationQueue(InSharpMcpConcurrencyOptions? options = null)
    {
        _maxQueuedOperations = (options ?? new InSharpMcpConcurrencyOptions()).MaxQueuedUiOperations;
    }

    public async Task<ToolResult> RunAsync(
        string operationName,
        Func<CancellationToken, Task<ToolResult>> operation,
        ToolLimits limits,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        if (!TryEnterQueue())
        {
            return ToolResult.Fail("The UI operation queue is full.", "busy");
        }

        try
        {
            using var queueTimeout = new CancellationTokenSource(limits.QueueTimeout);
            using var queueCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                queueTimeout.Token);

            try
            {
                await _semaphore.WaitAsync(queueCancellation.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (queueTimeout.IsCancellationRequested)
            {
                return ToolResult.Fail($"Timed out waiting to run UI operation '{operationName}'.", "timeout");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return ToolResult.Fail($"Cancelled before running UI operation '{operationName}'.", "cancelled");
            }

            try
            {
                using var operationTimeout = new CancellationTokenSource(limits.Timeout);
                using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    operationTimeout.Token);

                try
                {
                    return await operation(operationCancellation.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (operationTimeout.IsCancellationRequested)
                {
                    return ToolResult.Fail($"Timed out running UI operation '{operationName}'.", "timeout");
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return ToolResult.Fail($"Cancelled UI operation '{operationName}'.", "cancelled");
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
        finally
        {
            LeaveQueue();
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _semaphore.Dispose();
    }

    private bool TryEnterQueue()
    {
        lock (_gate)
        {
            if (_queuedOperations >= _maxQueuedOperations)
            {
                return false;
            }

            _queuedOperations++;
            return true;
        }
    }

    private void LeaveQueue()
    {
        lock (_gate)
        {
            _queuedOperations--;
        }
    }
}
