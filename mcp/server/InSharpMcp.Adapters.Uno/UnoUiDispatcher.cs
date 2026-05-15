using InSharpMcp.Contracts;
using Microsoft.UI.Dispatching;

namespace InSharpMcp.Adapters.Uno;

public sealed class UnoUiDispatcher : IUiDispatcher
{
    private readonly DispatcherQueue _dispatcherQueue;

    public UnoUiDispatcher(DispatcherQueue dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue;
    }

    public Task<T> RunAsync<T>(Func<CancellationToken, T> action, CancellationToken cancellationToken)
    {
        if (_dispatcherQueue.HasThreadAccess)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(action(cancellationToken));
        }

        return EnqueueAsync(
            token => Task.FromResult(action(token)),
            cancellationToken);
    }

    public Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        if (_dispatcherQueue.HasThreadAccess)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return action(cancellationToken);
        }

        return EnqueueAsync(action, cancellationToken);
    }

    private Task<T> EnqueueAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_dispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                completion.SetResult(await action(cancellationToken).ConfigureAwait(true));
            }
            catch (OperationCanceledException exception)
            {
                completion.SetCanceled(exception.CancellationToken);
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        }))
        {
            completion.SetException(new InvalidOperationException("Unable to enqueue work on the Uno UI dispatcher."));
        }

        return completion.Task;
    }
}
