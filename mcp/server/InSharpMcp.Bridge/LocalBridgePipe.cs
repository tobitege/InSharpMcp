using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using InSharpMcp.Contracts.LocalTransport;

namespace InSharpMcp.Bridge;

internal static class LocalBridgePipe
{
    public static async Task<TResponse> SendAsync<TRequest, TResponse>(
        string pipeName,
        TRequest request,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        await using var pipe = new NamedPipeClientStream(
            ".",
            pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);
        await pipe.ConnectAsync(timeoutSource.Token).ConfigureAwait(false);

        await WriteLineAsync(pipe, request, timeoutSource.Token).ConfigureAwait(false);
        return await ReadLineAsync<TResponse>(pipe, timeoutSource.Token).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The local bridge transport returned an empty response.");
    }

    public static async Task WriteLineAsync<TMessage>(
        Stream stream,
        TMessage message,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(message, LocalTransportJson.Options);
        var bytes = Encoding.UTF8.GetBytes(json + "\n");
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async Task<TMessage?> ReadLineAsync<TMessage>(
        Stream stream,
        CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        var oneByte = new byte[1];
        while (true)
        {
            var read = await stream.ReadAsync(oneByte, cancellationToken).ConfigureAwait(false);
            if (read == 0 || oneByte[0] == (byte)'\n')
            {
                break;
            }

            buffer.WriteByte(oneByte[0]);
        }

        if (buffer.Length == 0)
        {
            return default;
        }

        var json = Encoding.UTF8.GetString(buffer.ToArray());
        return JsonSerializer.Deserialize<TMessage>(json, LocalTransportJson.Options);
    }
}
