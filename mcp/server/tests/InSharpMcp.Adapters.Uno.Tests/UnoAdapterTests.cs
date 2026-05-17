using InSharpMcp.Adapters.Uno;
using InSharpMcp.Contracts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Text.Json;
using Uno.UI.Hosting;
using Windows.Foundation;
using Windows.UI;

namespace InSharpMcp.Adapters.Uno.Tests;

public sealed class UnoRuntimeAdapterTests : IClassFixture<UnoRuntimeFixture>
{
    private readonly UnoRuntimeFixture _fixture;

    public UnoRuntimeAdapterTests(UnoRuntimeFixture fixture)
    {
        _fixture = fixture;
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
    public async Task AppProvider_CloseAsync_ClosesRealWindowOnDispatcher()
    {
        var result = await _fixture.Dispatcher.RunAsync(
            async token =>
            {
                var window = new Window { Content = new Grid { Width = 40, Height = 40 } };
                var closed = false;
                window.Closed += (_, _) => closed = true;
                window.Activate();

                var provider = new UnoAppProvider(window, _fixture.Dispatcher, "App", "1.0", "uno");
                var closeResult = await provider.CloseAsync(token);
                return (closeResult, closed);
            },
            TestContext.Current.CancellationToken);

        Assert.True(result.closeResult.Success, result.closeResult.Message);
        Assert.True(result.closed);
    }

    [Fact]
    public async Task ScreenshotProvider_CaptureScreenshotAsync_RendersRealUnoContentOrReportsUnsupported()
    {
        var result = await WithWindowContentAsync(
            () => new Grid
            {
                Width = 32,
                Height = 32,
                Background = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)),
            },
            async root =>
            {
                var provider = new UnoScreenshotProvider(root, _fixture.Dispatcher);
                return await provider.CaptureScreenshotAsync(TestContext.Current.CancellationToken);
            });

        if (result.Success)
        {
            Assert.NotNull(result.PngBytes);
            Assert.Equal(
                new byte[] { 0x89, 0x50, 0x4e, 0x47 },
                result.PngBytes.Take(4).ToArray());
        }
        else
        {
            Assert.Equal("unsupported", result.ErrorCode);
        }
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
    public async Task VisualTreeInspector_RejectsMalformedAndOutOfRangeElementIds()
    {
        var result = await _fixture.Dispatcher.RunAsync(
            async token =>
            {
                var root = new StackPanel();
                root.Children.Add(new Border { Width = 10, Height = 10 });
                var inspector = new UnoVisualTreeInspector(root, _fixture.Dispatcher);
                var invoker = new UnoAutomationPeerInvoker(root, _fixture.Dispatcher);
                var editor = new UnoElementPropertyEditor(root, _fixture.Dispatcher);
                using var value = JsonDocument.Parse("\"ignored\"");

                var malformedMetadata = await inspector.GetElementMetadataAsync(
                    "not/a/path",
                    new ToolLimits(),
                    token);
                var outOfRangeDataContext = await inspector.GetElementDataContextAsync(
                    "0/99",
                    new ToolLimits(),
                    token);
                var malformedInvoke = await invoker.InvokeDefaultActionAsync("0/-1", token);
                var outOfRangeEdit = await editor.SetElementPropertyAsync(
                    "0/99",
                    ElementPropertyTarget.Element,
                    nameof(FrameworkElement.Name),
                    value.RootElement,
                    token);

                return (malformedMetadata, outOfRangeDataContext, malformedInvoke, outOfRangeEdit);
            },
            TestContext.Current.CancellationToken);

        Assert.False(result.malformedMetadata.Success);
        Assert.Equal("not_found", result.malformedMetadata.ErrorCode);
        Assert.False(result.outOfRangeDataContext.Success);
        Assert.Equal("not_found", result.outOfRangeDataContext.ErrorCode);
        Assert.False(result.malformedInvoke.Success);
        Assert.Equal("not_found", result.malformedInvoke.ErrorCode);
        Assert.False(result.outOfRangeEdit.Success);
        Assert.Equal("not_found", result.outOfRangeEdit.ErrorCode);
    }

    [Fact]
    public async Task VisualTreeInspector_TruncatesTextAndReturnsNullDataContext()
    {
        var result = await _fixture.Dispatcher.RunAsync(
            async token =>
            {
                var root = new StackPanel();
                root.Children.Add(new TextBlock { Text = "abcdef" });
                var inspector = new UnoVisualTreeInspector(root, _fixture.Dispatcher);
                var metadataResult = await inspector.GetElementMetadataAsync(
                    "0/0",
                    new ToolLimits { MaxTextCharacters = 3 },
                    token);
                var dataContextResult = await inspector.GetElementDataContextAsync(
                    "0/0",
                    new ToolLimits(),
                    token);

                return (metadataResult, dataContextResult);
            },
            TestContext.Current.CancellationToken);

        Assert.True(result.metadataResult.Success);
        var metadata = Assert.IsType<ElementMetadata>(result.metadataResult.Data);
        Assert.Equal("abc", metadata.Text);
        Assert.True(result.dataContextResult.Success);
        var dataContext = Assert.IsType<DataContextMetadata>(result.dataContextResult.Data);
        Assert.Equal("<null>", dataContext.TypeName);
        Assert.Empty(dataContext.Properties);
        Assert.False(dataContext.Truncated);
    }

    [Fact]
    public async Task ElementMetadata_ResolvesIdentifierReturnedByHighBudgetSnapshotBeyondDefaultNodeBudget()
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
                var identifier = Assert.IsType<UiElementNode>(snapshot.Root.Children?[550]).ElementIdentifier;

                return await inspector.GetElementMetadataAsync(identifier, new ToolLimits(), token);
            },
            TestContext.Current.CancellationToken);

        Assert.True(result.Success, result.Message);
        var metadata = Assert.IsType<ElementMetadata>(result.Data);
        Assert.Equal("TextBlock", metadata.Type);
        Assert.Equal("Item 550", metadata.Text);
    }

    [Fact]
    public async Task ElementPropertyEditor_ReturnsStableErrorsForInvalidRequests()
    {
        var result = await _fixture.Dispatcher.RunAsync(
            async token =>
            {
                var root = new StackPanel();
                root.Children.Add(new Border());
                var editor = new UnoElementPropertyEditor(root, _fixture.Dispatcher);
                using var value = JsonDocument.Parse("\"ignored\"");

                var missingName = await editor.SetElementPropertyAsync(
                    "0/0",
                    ElementPropertyTarget.Element,
                    "",
                    value.RootElement,
                    token);
                var invalidTarget = await editor.SetElementPropertyAsync(
                    "0/0",
                    "bogus",
                    nameof(FrameworkElement.Name),
                    value.RootElement,
                    token);
                var unavailableDataContext = await editor.SetElementPropertyAsync(
                    "0/0",
                    ElementPropertyTarget.DataContext,
                    nameof(MutableDataContext.Count),
                    value.RootElement,
                    token);
                var missingProperty = await editor.SetElementPropertyAsync(
                    "0/0",
                    ElementPropertyTarget.Element,
                    "MissingProperty",
                    value.RootElement,
                    token);

                return (missingName, invalidTarget, unavailableDataContext, missingProperty);
            },
            TestContext.Current.CancellationToken);

        Assert.False(result.missingName.Success);
        Assert.Equal("invalid_property", result.missingName.ErrorCode);
        Assert.False(result.invalidTarget.Success);
        Assert.Equal("invalid_target_object", result.invalidTarget.ErrorCode);
        Assert.False(result.unavailableDataContext.Success);
        Assert.Equal("target_unavailable", result.unavailableDataContext.ErrorCode);
        Assert.False(result.missingProperty.Success);
        Assert.Equal("property_not_found", result.missingProperty.ErrorCode);
    }

    [Fact]
    public async Task ElementClick_ClicksInsideRequestedControl()
    {
        var injector = new RecordingUnoInputInjector();
        var result = await WithCanvasButtonContentAsync(
            async (root, expectedIdentifier) =>
            {
                var simulator = new UnoPointerInputSimulator(_fixture.Window, _fixture.Dispatcher, injector);
                var clickResult = await simulator.ElementClickAsync(expectedIdentifier, TestContext.Current.CancellationToken);
                var inspector = new UnoVisualTreeInspector(root, _fixture.Dispatcher);
                var metadataResult = await inspector.GetElementMetadataAsync(
                    expectedIdentifier,
                    new ToolLimits(),
                    TestContext.Current.CancellationToken);
                var metadata = Assert.IsType<ElementMetadata>(metadataResult.Data);
                return (clickResult, metadata.Bounds);
            });

        Assert.True(result.clickResult.Success, result.clickResult.Message);
        Assert.Equal(1, injector.PointerClickCount);
        var bounds = Assert.IsType<UiElementBounds>(result.Bounds);
        Assert.InRange(injector.LastClientX, (int)Math.Ceiling(bounds.X), (int)Math.Floor(bounds.X + bounds.Width - 1));
        Assert.InRange(injector.LastClientY, (int)Math.Ceiling(bounds.Y), (int)Math.Floor(bounds.Y + bounds.Height - 1));
    }

    [Fact]
    public async Task ElementClick_RejectsDisabledControlWithoutNativeClick()
    {
        var injector = new RecordingUnoInputInjector();
        var result = await WithCanvasButtonContentAsync(
            async (_, identifier) =>
            {
                var simulator = new UnoPointerInputSimulator(_fixture.Window, _fixture.Dispatcher, injector);
                return await simulator.ElementClickAsync(identifier, TestContext.Current.CancellationToken);
            },
            isEnabled: false);

        Assert.False(result.Success);
        Assert.Equal("not_clickable", result.ErrorCode);
        Assert.Equal(0, injector.PointerClickCount);
    }

    [Fact]
    public async Task ElementClick_RejectsNonHitTestVisibleControlWithoutNativeClick()
    {
        var injector = new RecordingUnoInputInjector();
        var result = await WithCanvasButtonContentAsync(
            async (_, identifier) =>
            {
                var simulator = new UnoPointerInputSimulator(_fixture.Window, _fixture.Dispatcher, injector);
                return await simulator.ElementClickAsync(identifier, TestContext.Current.CancellationToken);
            },
            isHitTestVisible: false);

        Assert.False(result.Success);
        Assert.Equal("not_clickable", result.ErrorCode);
        Assert.Equal(0, injector.PointerClickCount);
    }

    [Fact]
    public async Task ElementClick_RejectsOccludedControlWithoutNativeClick()
    {
        var injector = new RecordingUnoInputInjector();
        var result = await WithCanvasButtonContentAsync(
            async (_, identifier) =>
            {
                var simulator = new UnoPointerInputSimulator(_fixture.Window, _fixture.Dispatcher, injector);
                return await simulator.ElementClickAsync(identifier, TestContext.Current.CancellationToken);
            },
            covered: true);

        Assert.False(result.Success);
        Assert.Equal("not_clickable", result.ErrorCode);
        Assert.Equal(0, injector.PointerClickCount);
    }

    [Fact]
    public async Task PointerClick_RejectsCoordinatesOutsideRootWithoutNativeClick()
    {
        var injector = new RecordingUnoInputInjector();
        var result = await WithWindowContentAsync(
            () => new Grid { Width = 100, Height = 50 },
            async _ =>
            {
                var simulator = new UnoPointerInputSimulator(_fixture.Window, _fixture.Dispatcher, injector);
                return await simulator.PointerClickAsync(120, 20, TestContext.Current.CancellationToken);
            });

        Assert.False(result.Success);
        Assert.Equal("out_of_bounds", result.ErrorCode);
        Assert.Equal(0, injector.PointerClickCount);
    }

    private async Task<T> WithCanvasButtonContentAsync<T>(
        Func<UIElement, string, Task<T>> action,
        bool isEnabled = true,
        bool isHitTestVisible = true,
        bool covered = false)
    {
        var buttonIdentifier = "";
        return await WithWindowContentAsync(
            () => CreateCanvasWithButton(out buttonIdentifier, isEnabled, isHitTestVisible, covered),
            root => action(root, buttonIdentifier));
    }

    private async Task<T> WithWindowContentAsync<T>(Func<FrameworkElement> createRoot, Func<UIElement, Task<T>> action)
    {
        FrameworkElement? root = null;
        await _fixture.Dispatcher.RunAsync(
            async token =>
            {
                token.ThrowIfCancellationRequested();
                root = createRoot();
                _fixture.Window.Content = root;
                Layout(root, root.Width, root.Height);
                await Task.Yield();
                Layout(root, root.Width, root.Height);
                return true;
            },
            TestContext.Current.CancellationToken);

        try
        {
            return await action(root!);
        }
        finally
        {
            await _fixture.Dispatcher.RunAsync(
                token =>
                {
                    token.ThrowIfCancellationRequested();
                    _fixture.Window.Content = new Grid { Width = 640, Height = 480 };
                    return true;
                },
                TestContext.Current.CancellationToken);
        }
    }

    private static Canvas CreateCanvasWithButton(
        out string buttonIdentifier,
        bool isEnabled = true,
        bool isHitTestVisible = true,
        bool covered = false)
    {
        var root = new Canvas { Width = 200, Height = 100 };
        FrameworkElement target = isEnabled
            ? new Border
            {
                Width = 80,
                Height = 30,
                Background = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0)),
                IsHitTestVisible = isHitTestVisible,
            }
            : new Button
            {
                Width = 80,
                Height = 30,
                IsEnabled = false,
                IsHitTestVisible = isHitTestVisible,
            };
        Canvas.SetLeft(target, 20);
        Canvas.SetTop(target, 10);
        root.Children.Add(target);
        buttonIdentifier = "0/0";

        if (covered)
        {
            var cover = new Border
            {
                Width = 80,
                Height = 30,
                Background = new SolidColorBrush(Color.FromArgb(255, 0, 0, 255)),
            };
            Canvas.SetLeft(cover, 20);
            Canvas.SetTop(cover, 10);
            root.Children.Add(cover);
        }

        return root;
    }

    private static void Layout(FrameworkElement element, double width, double height)
    {
        element.Measure(new Size(width, height));
        element.Arrange(new Rect(0, 0, width, height));
        element.UpdateLayout();
    }

    private sealed class RecordingUnoInputInjector : IUnoInputInjector
    {
        public int PointerClickCount { get; private set; }

        public int LastClientX { get; private set; }

        public int LastClientY { get; private set; }

        public ToolResult PointerClick(int screenX, int screenY)
        {
            PointerClickCount++;
            return ToolResult.Ok($"Clicked {screenX},{screenY}.");
        }

        public ToolResult KeyPress(string key, IReadOnlyList<string> modifiers) =>
            ToolResult.Ok($"Pressed {key}.");

        public ToolResult TypeText(string text) =>
            ToolResult.Ok($"Typed {text.Length} characters.");

        public bool TryClientToScreen(
            IntPtr hwnd,
            int clientX,
            int clientY,
            out int screenX,
            out int screenY,
            out ToolResult error)
        {
            Assert.NotEqual(IntPtr.Zero, hwnd);
            LastClientX = clientX;
            LastClientY = clientY;
            screenX = clientX + 1000;
            screenY = clientY + 2000;
            error = ToolResult.Ok("Coordinates translated.");
            return true;
        }
    }

    private sealed class MutableDataContext
    {
        public int Count { get; set; }
    }
}

public sealed class UnoRuntimeFixture : IAsyncLifetime
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(15);

    private readonly TaskCompletionSource<RuntimeState> _ready =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private RuntimeState? _state;
    private Task? _hostTask;

    public IUiDispatcher Dispatcher =>
        _state?.Dispatcher ?? throw new InvalidOperationException("The Uno runtime has not started.");

    public Window Window =>
        _state?.Window ?? throw new InvalidOperationException("The Uno runtime has not started.");

    public async ValueTask InitializeAsync()
    {
        _hostTask = Task.Run(RunHostAsync);
        _state = await _ready.Task.WaitAsync(StartupTimeout);
    }

    public async ValueTask DisposeAsync()
    {
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

    private void OnAppLaunched(Window window)
    {
        _ready.TrySetResult(new RuntimeState(window, new UnoUiDispatcher(window.DispatcherQueue)));
    }

    private async Task RunHostAsync()
    {
        try
        {
            var host = UnoPlatformHostBuilder
                .Create()
                .App(() => new TestUnoApplication(OnAppLaunched))
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
    private readonly Action<Window> _onLaunched;

    public TestUnoApplication(Action<Window> onLaunched)
    {
        _onLaunched = onLaunched;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var window = new Window
        {
            Content = new Grid { Width = 640, Height = 480 },
        };
        _onLaunched(window);
        window.Activate();
    }
}
