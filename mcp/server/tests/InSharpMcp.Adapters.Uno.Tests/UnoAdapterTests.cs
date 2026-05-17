using InSharpMcp.Adapters.Uno;
using InSharpMcp.Contracts;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Hosting;
using Windows.Foundation;

namespace InSharpMcp.Adapters.Uno.Tests;

public sealed class UnoAdapterTests : IClassFixture<UnoRuntimeFixture>
{
    private readonly UnoRuntimeFixture _fixture;

    public UnoAdapterTests(UnoRuntimeFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AppProvider_CloseAsync_UsesUiDispatcher()
    {
        var dispatcher = new RecordingDispatcher<ToolResult>(
            ToolResult.Ok("Window close requested."));
        var provider = new UnoAppProvider(null!, dispatcher, "App", "1.0", "uno");

        var result = await provider.CloseAsync(TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(1, dispatcher.SyncCallCount);
    }

    [Fact]
    public async Task ScreenshotProvider_CaptureScreenshotAsync_UsesUiDispatcher()
    {
        var expected = new ScreenshotResult(false, null, "Skipped by test.", "test");
        var dispatcher = new RecordingDispatcher<ScreenshotResult>(expected);
        var provider = new UnoScreenshotProvider(null!, dispatcher);

        var result = await provider.CaptureScreenshotAsync(TestContext.Current.CancellationToken);

        Assert.Same(expected, result);
        Assert.Equal(1, dispatcher.AsyncCallCount);
    }

    [Fact]
    public async Task RealUnoControls_CanBeConstructedThroughBootstrappedRuntime()
    {
        var result = await _fixture.Dispatcher.RunAsync(
            token =>
            {
                token.ThrowIfCancellationRequested();
                var button = new Button { Content = "Create" };
                AutomationProperties.SetName(button, "Create item");

                return (button.DispatcherQueue, AutomationProperties.GetName(button));
            },
            TestContext.Current.CancellationToken);

        Assert.NotNull(result.DispatcherQueue);
        Assert.Equal("Create item", result.Item2);
    }

    [Fact]
    public async Task VisualTreeMetadata_UsesRealUnoControlTextRoleAndAutomationName()
    {
        var result = await _fixture.Dispatcher.RunAsync(
            async token =>
            {
                var root = new Grid { Width = 320, Height = 200 };
                var button = new Button
                {
                    Content = "Create",
                    Width = 120,
                    Height = 40,
                };
                AutomationProperties.SetName(button, "Create item");
                AutomationProperties.SetAutomationId(button, "create-button");
                root.Children.Add(button);
                Layout(root, 320, 200);

                var inspector = new UnoVisualTreeInspector(root, _fixture.Dispatcher);
                return await inspector.GetElementMetadataAsync("0/0", new ToolLimits(), token);
            },
            TestContext.Current.CancellationToken);

        Assert.True(result.Success, result.Message);
        var metadata = Assert.IsType<ElementMetadata>(result.Data);
        Assert.Equal("Button", metadata.Type);
        Assert.Equal("Create item", metadata.Name);
        Assert.Equal("create-button", metadata.AutomationId);
        Assert.Equal("Create", metadata.Text);
        Assert.Equal("Button", metadata.Role);
        Assert.True(metadata.IsVisible);
        Assert.True(metadata.IsEnabled);
    }

    [Fact]
    public async Task ElementMetadata_ResolvesSnapshotPathBeyondDefaultNodeBudget()
    {
        var result = await _fixture.Dispatcher.RunAsync(
            async token =>
            {
                var root = new StackPanel { Width = 400, Height = 400 };
                for (var index = 0; index < 560; index++)
                {
                    root.Children.Add(new TextBlock { Text = $"Item {index}" });
                }

                var inspector = new UnoVisualTreeInspector(root, _fixture.Dispatcher);
                var snapshotResult = await inspector.GetVisualTreeSnapshotAsync(
                    new ToolLimits { MaxDepth = 2, MaxNodes = 700 },
                    token);
                Assert.True(snapshotResult.Success, snapshotResult.Message);
                var snapshot = Assert.IsType<UiTreeSnapshot>(snapshotResult.Data);
                Assert.False(snapshot.Truncated);
                Assert.True(snapshot.NodeCount > new ToolLimits().MaxNodes);

                return await inspector.GetElementMetadataAsync(
                    "0/550",
                    new ToolLimits(),
                    token);
            },
            TestContext.Current.CancellationToken);

        Assert.True(result.Success, result.Message);
        var metadata = Assert.IsType<ElementMetadata>(result.Data);
        Assert.Equal("TextBlock", metadata.Type);
        Assert.Equal("Item 550", metadata.Text);
    }

    private static void Layout(FrameworkElement element, double width, double height)
    {
        element.Measure(new Size(width, height));
        element.Arrange(new Rect(0, 0, width, height));
        element.UpdateLayout();
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

public sealed class UnoRuntimeFixture : IAsyncLifetime
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(15);
    private static UnoRuntimeFixture? _current;

    private readonly TaskCompletionSource<RuntimeState> _ready =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private RuntimeState? _state;
    private Task? _hostTask;

    public IUiDispatcher Dispatcher =>
        _state?.Dispatcher ?? throw new InvalidOperationException("The Uno runtime has not started.");

    public async ValueTask InitializeAsync()
    {
        _current = this;
        _hostTask = Task.Run(RunHostAsync);
        _state = await _ready.Task.WaitAsync(StartupTimeout);
    }

    public async ValueTask DisposeAsync()
    {
        _current = null;
        if (_state is { } state)
        {
            using var cancellation = new CancellationTokenSource(ShutdownTimeout);
            await state.Dispatcher.RunAsync(
                token =>
                {
                    token.ThrowIfCancellationRequested();
                    state.Window.Close();
                    return true;
                },
                cancellation.Token);
        }

        if (_hostTask is not null)
        {
            await _hostTask.WaitAsync(ShutdownTimeout);
        }
    }

    internal static void OnAppLaunched(Window window)
    {
        if (_current is not { } fixture)
        {
            throw new InvalidOperationException("No Uno runtime fixture is waiting for the launched app.");
        }

        fixture._ready.TrySetResult(new RuntimeState(window, new UnoUiDispatcher(window.DispatcherQueue)));
    }

    private async Task RunHostAsync()
    {
        try
        {
            var host = UnoPlatformHostBuilder
                .Create()
                .App(() => new TestUnoApplication())
                .UseWin32()
                .Build();

            await host.RunAsync();
        }
        catch (Exception exception)
        {
            _ready.TrySetException(exception);
            throw;
        }
    }

    private sealed record RuntimeState(Window Window, IUiDispatcher Dispatcher);
}

public sealed partial class TestUnoApplication : Application
{
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var window = new Window
        {
            Content = new Grid { Width = 640, Height = 480 },
        };
        UnoRuntimeFixture.OnAppLaunched(window);
        window.Activate();
    }
}
