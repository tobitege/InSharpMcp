using Avalonia.Threading;
using InSharpMcp.Contracts;

namespace InSharpMcp.Adapters.Avalonia;

public sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    public Task<T> RunAsync<T>(Func<CancellationToken, T> action, CancellationToken cancellationToken)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(action(cancellationToken));
        }

        return Dispatcher.UIThread.InvokeAsync(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return action(cancellationToken);
            }).GetTask();
    }

    public Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            cancellationToken.ThrowIfCancellationRequested();
            return action(cancellationToken);
        }

        return Dispatcher.UIThread.InvokeAsync(
            async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await action(cancellationToken).ConfigureAwait(true);
            });
    }
}
