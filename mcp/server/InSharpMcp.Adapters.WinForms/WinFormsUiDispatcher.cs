using InSharpMcp.Contracts;
using System.Windows.Forms;

namespace InSharpMcp.Adapters.WinForms;

public sealed class WinFormsUiDispatcher : IUiDispatcher
{
    private readonly Control _control;

    public WinFormsUiDispatcher(Control control)
    {
        _control = control;
    }

    public Task<T> RunAsync<T>(Func<CancellationToken, T> action, CancellationToken cancellationToken)
    {
        if (!_control.InvokeRequired)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(action(cancellationToken));
        }

        return InvokeAsync(token => Task.FromResult(action(token)), cancellationToken);
    }

    public Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        if (!_control.InvokeRequired)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return action(cancellationToken);
        }

        return InvokeAsync(action, cancellationToken);
    }

    private Task<T> InvokeAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        if (_control.IsDisposed)
        {
            return Task.FromException<T>(new ObjectDisposedException(_control.GetType().Name));
        }

        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            _control.BeginInvoke(new MethodInvoker(async () =>
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
            }));
        }
        catch (InvalidOperationException exception)
        {
            completion.SetException(exception);
        }

        return completion.Task;
    }
}
