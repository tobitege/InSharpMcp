using InSharpMcp.Adapters.Uno;
using InSharpMcp.Contracts;

namespace InSharpMcp.Adapters.Uno.Tests;

public sealed class UnoAdapterTests
{
    [Fact]
    public async Task AppProvider_CloseAsync_UsesUiDispatcher()
    {
        var dispatcher = new RecordingDispatcher<ToolResult>(
            ToolResult.Ok("Window close requested."));
        var provider = new UnoAppProvider(null!, dispatcher, "App", "1.0", "uno");

        var result = await provider.CloseAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, dispatcher.SyncCallCount);
    }

    [Fact]
    public async Task ScreenshotProvider_CaptureScreenshotAsync_UsesUiDispatcher()
    {
        var expected = new ScreenshotResult(false, null, "Skipped by test.", "test");
        var dispatcher = new RecordingDispatcher<ScreenshotResult>(expected);
        var provider = new UnoScreenshotProvider(null!, dispatcher);

        var result = await provider.CaptureScreenshotAsync(CancellationToken.None);

        Assert.Same(expected, result);
        Assert.Equal(1, dispatcher.AsyncCallCount);
    }

    private sealed class RecordingDispatcher<TResult> : IUiDispatcher
    {
        private readonly TResult _result;

        public RecordingDispatcher(TResult result)
        {
            _result = result;
        }

        public int SyncCallCount { get; private set; }

        public int AsyncCallCount { get; private set; }

        public Task<T> RunAsync<T>(Func<CancellationToken, T> action, CancellationToken cancellationToken)
        {
            _ = action;
            cancellationToken.ThrowIfCancellationRequested();
            SyncCallCount++;
            return Task.FromResult((T)(object)_result!);
        }

        public Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
        {
            _ = action;
            cancellationToken.ThrowIfCancellationRequested();
            AsyncCallCount++;
            return Task.FromResult((T)(object)_result!);
        }
    }

}
