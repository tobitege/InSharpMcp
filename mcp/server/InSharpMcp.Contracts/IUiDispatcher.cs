namespace InSharpMcp.Contracts;

public interface IUiDispatcher
{
    Task<T> RunAsync<T>(Func<CancellationToken, T> action, CancellationToken cancellationToken);

    Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken);
}
