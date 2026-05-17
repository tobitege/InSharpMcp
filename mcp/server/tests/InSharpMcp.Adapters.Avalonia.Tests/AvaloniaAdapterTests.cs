using InSharpMcp.Adapters.Avalonia;
using InSharpMcp.Contracts;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using System.Text.Json;
using System.Windows.Input;

[assembly: AvaloniaTestApplication(typeof(InSharpMcp.Adapters.Avalonia.Tests.TestAvaloniaApp))]

namespace InSharpMcp.Adapters.Avalonia.Tests;

public sealed class AvaloniaAdapterTests
{
    [Fact]
    public async Task PointerInputSimulator_ForwardsKeyAndTextInput()
    {
        var input = new RecordingAvaloniaInputInjector();
        var simulator = new AvaloniaPointerInputSimulator(new Button(), new ImmediateDispatcher(), input);

        var keyResult = await simulator.KeyPressAsync("enter", ["ctrl"], TestContext.Current.CancellationToken);
        var textResult = await simulator.TypeTextAsync("hello", TestContext.Current.CancellationToken);

        Assert.True(keyResult.Success);
        Assert.True(textResult.Success);
        Assert.Equal("enter", input.Key);
        Assert.Equal(["ctrl"], input.Modifiers);
        Assert.Equal("hello", input.Text);
    }

    [Fact]
    public async Task PointerInputSimulator_RejectsOutOfRootCoordinates()
    {
        var root = new Button();
        root.Measure(new Size(100, 40));
        root.Arrange(new Rect(0, 0, 100, 40));
        var input = new RecordingAvaloniaInputInjector();
        var simulator = new AvaloniaPointerInputSimulator(root, new ImmediateDispatcher(), input);

        var result = await simulator.PointerClickAsync(100, 1, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("out_of_bounds", result.ErrorCode);
        Assert.False(input.PointerClicked);
    }

    [AvaloniaFact]
    public async Task ElementClick_ClicksRootControlCenter()
    {
        var button = new Button
        {
            Width = 100,
            Height = 40,
            Background = Brushes.Transparent,
        };
        var window = new Window
        {
            Width = 100,
            Height = 40,
            Content = button,
        };
        try
        {
            window.Show();
            await WaitForHeadlessWindowAsync();
            var input = new RecordingAvaloniaInputInjector();
            var simulator = new AvaloniaPointerInputSimulator(button, new ImmediateDispatcher(), input);

            var result = await simulator.ElementClickAsync("0", TestContext.Current.CancellationToken);

            Assert.True(result.Success, $"{result.ErrorCode}: {result.Message}");
            Assert.True(input.PointerClicked);
            var expected = button.PointToScreen(new Point(50, 20));
            Assert.Equal(expected.X, input.ScreenX);
            Assert.Equal(expected.Y, input.ScreenY);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task ElementClick_ClicksNestedTranslatedControlCenter()
    {
        var root = new Canvas
        {
            Width = 200,
            Height = 120,
            Background = Brushes.Transparent,
        };
        var panel = new Canvas
        {
            Width = 100,
            Height = 80,
            Background = Brushes.Transparent,
        };
        Canvas.SetLeft(panel, 20);
        Canvas.SetTop(panel, 15);
        var button = new Button
        {
            Name = "TargetButton",
            Width = 50,
            Height = 30,
            Background = Brushes.Transparent,
        };
        Canvas.SetLeft(button, 10);
        Canvas.SetTop(button, 5);
        panel.Children.Add(button);
        root.Children.Add(panel);
        var window = new Window
        {
            Width = 200,
            Height = 120,
            Content = root,
        };
        try
        {
            window.Show();
            await WaitForHeadlessWindowAsync();
            var input = new RecordingAvaloniaInputInjector();
            var simulator = new AvaloniaPointerInputSimulator(root, new ImmediateDispatcher(), input);

            var result = await simulator.ElementClickAsync("0/0/0", TestContext.Current.CancellationToken);

            Assert.True(result.Success, $"{result.ErrorCode}: {result.Message}");
            Assert.True(input.PointerClicked);
            var expected = root.PointToScreen(new Point(55, 35));
            Assert.Equal(expected.X, input.ScreenX);
            Assert.Equal(expected.Y, input.ScreenY);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task ElementClick_AllowsChildOutsideNonClippingParent()
    {
        var root = new Canvas
        {
            Width = 200,
            Height = 120,
            Background = Brushes.Transparent,
        };
        var panel = new Canvas
        {
            Width = 20,
            Height = 30,
            Background = Brushes.Transparent,
        };
        Canvas.SetLeft(panel, 20);
        Canvas.SetTop(panel, 15);
        var button = new Button
        {
            Width = 50,
            Height = 30,
            Background = Brushes.Transparent,
        };
        Canvas.SetLeft(button, 30);
        panel.Children.Add(button);
        root.Children.Add(panel);
        var window = new Window
        {
            Width = 200,
            Height = 120,
            Content = root,
        };
        try
        {
            window.Show();
            await WaitForHeadlessWindowAsync();
            var input = new RecordingAvaloniaInputInjector();
            var simulator = new AvaloniaPointerInputSimulator(root, new ImmediateDispatcher(), input);

            var result = await simulator.ElementClickAsync("0/0/0", TestContext.Current.CancellationToken);

            Assert.True(result.Success, $"{result.ErrorCode}: {result.Message}");
            var expected = root.PointToScreen(new Point(75, 30));
            Assert.Equal(expected.X, input.ScreenX);
            Assert.Equal(expected.Y, input.ScreenY);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task ElementClick_UsesVisibleCenterWhenParentClips()
    {
        var root = new Canvas
        {
            Width = 200,
            Height = 120,
            Background = Brushes.Transparent,
        };
        var panel = new Canvas
        {
            Width = 20,
            Height = 30,
            Background = Brushes.Transparent,
            ClipToBounds = true,
        };
        Canvas.SetLeft(panel, 20);
        Canvas.SetTop(panel, 15);
        var button = new Button
        {
            Width = 50,
            Height = 30,
            Background = Brushes.Transparent,
        };
        Canvas.SetLeft(button, 10);
        panel.Children.Add(button);
        root.Children.Add(panel);
        var window = new Window
        {
            Width = 200,
            Height = 120,
            Content = root,
        };
        try
        {
            window.Show();
            await WaitForHeadlessWindowAsync();
            var input = new RecordingAvaloniaInputInjector();
            var simulator = new AvaloniaPointerInputSimulator(root, new ImmediateDispatcher(), input);

            var result = await simulator.ElementClickAsync("0/0/0", TestContext.Current.CancellationToken);

            Assert.True(result.Success, $"{result.ErrorCode}: {result.Message}");
            var expected = root.PointToScreen(new Point(35, 30));
            Assert.Equal(expected.X, input.ScreenX);
            Assert.Equal(expected.Y, input.ScreenY);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public async Task ElementClick_RejectsDisabledControl()
    {
        var root = new Canvas
        {
            Width = 120,
            Height = 80,
        };
        var button = new Button
        {
            Width = 50,
            Height = 30,
            IsEnabled = false,
        };
        root.Children.Add(button);
        root.Measure(new Size(120, 80));
        root.Arrange(new Rect(0, 0, 120, 80));
        var input = new RecordingAvaloniaInputInjector();
        var simulator = new AvaloniaPointerInputSimulator(root, new ImmediateDispatcher(), input);

        var result = await simulator.ElementClickAsync("0/0", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("not_clickable", result.ErrorCode);
        Assert.False(input.PointerClicked);
    }

    [Fact]
    public async Task ElementClick_RejectsHiddenAndEmptyControls()
    {
        var root = new Canvas
        {
            Width = 120,
            Height = 80,
        };
        var hiddenButton = new Button
        {
            Width = 50,
            Height = 30,
            IsVisible = false,
        };
        var emptyButton = new Button
        {
            Width = 0,
            Height = 30,
        };
        root.Children.Add(hiddenButton);
        root.Children.Add(emptyButton);
        root.Measure(new Size(120, 80));
        root.Arrange(new Rect(0, 0, 120, 80));
        var input = new RecordingAvaloniaInputInjector();
        var simulator = new AvaloniaPointerInputSimulator(root, new ImmediateDispatcher(), input);

        var hiddenResult = await simulator.ElementClickAsync("0/0", TestContext.Current.CancellationToken);
        var emptyResult = await simulator.ElementClickAsync("0/1", TestContext.Current.CancellationToken);

        Assert.False(hiddenResult.Success);
        Assert.Equal("not_clickable", hiddenResult.ErrorCode);
        Assert.False(emptyResult.Success);
        Assert.Equal("not_clickable", emptyResult.ErrorCode);
        Assert.False(input.PointerClicked);
    }

    [Fact]
    public async Task AutomationInvoker_ExecutesCommandSourceDefaultAction()
    {
        var command = new RecordingCommand();
        var button = new Button
        {
            Command = command,
            CommandParameter = "payload",
        };
        var invoker = new AvaloniaAutomationPeerInvoker(button, new ImmediateDispatcher());

        var result = await invoker.InvokeDefaultActionAsync("0", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal("payload", command.Parameter);
    }

    [Fact]
    public async Task VisualTreeInspector_RejectsMalformedAndOutOfRangeElementIds()
    {
        var root = new StackPanel();
        root.Children.Add(new Button());
        var dispatcher = new ImmediateDispatcher();
        var inspector = new AvaloniaVisualTreeInspector(root, dispatcher);
        var invoker = new AvaloniaAutomationPeerInvoker(root, dispatcher);
        var editor = new AvaloniaElementPropertyEditor(root, dispatcher);
        using var value = JsonDocument.Parse("\"ignored\"");

        var malformedMetadata = await inspector.GetElementMetadataAsync(
            "not/a/path",
            new ToolLimits(),
            TestContext.Current.CancellationToken);
        var outOfRangeDataContext = await inspector.GetElementDataContextAsync(
            "0/99",
            new ToolLimits(),
            TestContext.Current.CancellationToken);
        var malformedInvoke = await invoker.InvokeDefaultActionAsync(
            "0/-1",
            TestContext.Current.CancellationToken);
        var outOfRangeEdit = await editor.SetElementPropertyAsync(
            "0/99",
            ElementPropertyTarget.Element,
            nameof(Button.Name),
            value.RootElement,
            TestContext.Current.CancellationToken);

        Assert.False(malformedMetadata.Success);
        Assert.Equal("not_found", malformedMetadata.ErrorCode);
        Assert.False(outOfRangeDataContext.Success);
        Assert.Equal("not_found", outOfRangeDataContext.ErrorCode);
        Assert.False(malformedInvoke.Success);
        Assert.Equal("not_found", malformedInvoke.ErrorCode);
        Assert.False(outOfRangeEdit.Success);
        Assert.Equal("not_found", outOfRangeEdit.ErrorCode);
    }

    [Fact]
    public async Task VisualTreeInspector_TruncatesTextAndReturnsNullDataContext()
    {
        var root = new StackPanel();
        root.Children.Add(new TextBlock { Text = "abcdef" });
        var inspector = new AvaloniaVisualTreeInspector(root, new ImmediateDispatcher());

        var metadataResult = await inspector.GetElementMetadataAsync(
            "0/0",
            new ToolLimits { MaxTextCharacters = 3 },
            TestContext.Current.CancellationToken);
        var dataContextResult = await inspector.GetElementDataContextAsync(
            "0/0",
            new ToolLimits(),
            TestContext.Current.CancellationToken);

        Assert.True(metadataResult.Success);
        var metadata = Assert.IsType<ElementMetadata>(metadataResult.Data);
        Assert.Equal("abc", metadata.Text);
        Assert.True(dataContextResult.Success);
        var dataContext = Assert.IsType<DataContextMetadata>(dataContextResult.Data);
        Assert.Equal("<null>", dataContext.TypeName);
        Assert.Empty(dataContext.Properties);
        Assert.False(dataContext.Truncated);
    }

    [Fact]
    public async Task ElementLookup_UsesPathWithoutDefaultNodeBudget()
    {
        var root = new StackPanel();
        var command = new RecordingCommand();
        for (var index = 0; index < 600; index++)
        {
            var button = new Button
            {
                Name = $"Button{index.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
                Content = $"Button {index.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
            };

            if (index == 599)
            {
                button.DataContext = new PathLookupDataContext("late");
                button.Command = command;
                button.CommandParameter = "payload";
            }

            root.Children.Add(button);
        }

        var inspector = new AvaloniaVisualTreeInspector(root, new ImmediateDispatcher());
        var invoker = new AvaloniaAutomationPeerInvoker(root, new ImmediateDispatcher());
        var snapshotResult = await inspector.GetVisualTreeSnapshotAsync(
            new ToolLimits { MaxDepth = 2, MaxNodes = 700 },
            TestContext.Current.CancellationToken);

        Assert.True(snapshotResult.Success);

        var metadataResult = await inspector.GetElementMetadataAsync(
            "0/599",
            new ToolLimits { MaxNodes = 1 },
            TestContext.Current.CancellationToken);

        Assert.True(metadataResult.Success);
        var metadata = Assert.IsType<ElementMetadata>(metadataResult.Data);
        Assert.Equal("Button599", metadata.Name);

        var dataContextResult = await inspector.GetElementDataContextAsync(
            "0/599",
            new ToolLimits { MaxNodes = 1 },
            TestContext.Current.CancellationToken);

        Assert.True(dataContextResult.Success);
        var dataContext = Assert.IsType<DataContextMetadata>(dataContextResult.Data);
        Assert.EndsWith(nameof(PathLookupDataContext), dataContext.TypeName, StringComparison.Ordinal);

        var invokeResult = await invoker.InvokeDefaultActionAsync("0/599", TestContext.Current.CancellationToken);

        Assert.True(invokeResult.Success);
        Assert.Equal("payload", command.Parameter);
    }

    [Fact]
    public async Task ElementPropertyEditor_SetsElementAndDataContextProperties()
    {
        var root = new StackPanel();
        var button = new Button
        {
            Name = "Before",
            DataContext = new MutableDataContext { Count = 1 },
        };
        root.Children.Add(button);
        var editor = new AvaloniaElementPropertyEditor(root, new ImmediateDispatcher());
        using var nameValue = JsonDocument.Parse("\"After\"");
        using var dataContextValue = JsonDocument.Parse("42");

        var elementResult = await editor.SetElementPropertyAsync(
            "0/0",
            ElementPropertyTarget.Element,
            nameof(Button.Name),
            nameValue.RootElement,
            TestContext.Current.CancellationToken);
        var dataContextResult = await editor.SetElementPropertyAsync(
            "0/0",
            ElementPropertyTarget.DataContext,
            nameof(MutableDataContext.Count),
            dataContextValue.RootElement,
            TestContext.Current.CancellationToken);

        Assert.True(elementResult.Success);
        Assert.Equal("After", button.Name);
        Assert.True(dataContextResult.Success);
        Assert.Equal(42, Assert.IsType<MutableDataContext>(button.DataContext).Count);
    }

    [Fact]
    public async Task ElementPropertyEditor_ReturnsStableErrorsForInvalidRequests()
    {
        var root = new StackPanel();
        root.Children.Add(new Button());
        var editor = new AvaloniaElementPropertyEditor(root, new ImmediateDispatcher());
        using var value = JsonDocument.Parse("\"ignored\"");

        var missingName = await editor.SetElementPropertyAsync(
            "0/0",
            ElementPropertyTarget.Element,
            "",
            value.RootElement,
            TestContext.Current.CancellationToken);
        var invalidTarget = await editor.SetElementPropertyAsync(
            "0/0",
            "bogus",
            nameof(Button.Name),
            value.RootElement,
            TestContext.Current.CancellationToken);
        var unavailableDataContext = await editor.SetElementPropertyAsync(
            "0/0",
            ElementPropertyTarget.DataContext,
            nameof(MutableDataContext.Count),
            value.RootElement,
            TestContext.Current.CancellationToken);
        var missingProperty = await editor.SetElementPropertyAsync(
            "0/0",
            ElementPropertyTarget.Element,
            "MissingProperty",
            value.RootElement,
            TestContext.Current.CancellationToken);

        Assert.False(missingName.Success);
        Assert.Equal("invalid_property", missingName.ErrorCode);
        Assert.False(invalidTarget.Success);
        Assert.Equal("invalid_target_object", invalidTarget.ErrorCode);
        Assert.False(unavailableDataContext.Success);
        Assert.Equal("target_unavailable", unavailableDataContext.ErrorCode);
        Assert.False(missingProperty.Success);
        Assert.Equal("property_not_found", missingProperty.ErrorCode);
    }

    [Fact]
    public async Task AccessibilityTreeProvider_ReturnsInspectableTreeFromInspector()
    {
        var expected = ToolResult.Ok(
            "Visual tree snapshot returned.",
            new UiTreeSnapshot(new UiElementNode("0", "Window"), NodeCount: 1, Truncated: false));
        var provider = new AvaloniaAccessibilityTreeProvider(new StubTreeInspector(expected));

        var result = await provider.GetAccessibilityTreeAsync(new ToolLimits(), TestContext.Current.CancellationToken);

        Assert.Same(expected, result);
    }

    private sealed class StubTreeInspector : IUiTreeInspector
    {
        private readonly ToolResult _snapshot;

        public StubTreeInspector(ToolResult snapshot)
        {
            _snapshot = snapshot;
        }

        public Task<ToolResult> GetVisualTreeSnapshotAsync(ToolLimits limits, CancellationToken cancellationToken)
        {
            _ = limits;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_snapshot);
        }

        public Task<ToolResult> GetElementMetadataAsync(
            string elementIdentifier,
            ToolLimits limits,
            CancellationToken cancellationToken)
        {
            _ = elementIdentifier;
            _ = limits;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(ToolResult.Fail("Not implemented by test stub.", "unsupported"));
        }

        public Task<ToolResult> GetElementDataContextAsync(
            string elementIdentifier,
            ToolLimits limits,
            CancellationToken cancellationToken)
        {
            _ = elementIdentifier;
            _ = limits;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(ToolResult.Fail("Not implemented by test stub.", "unsupported"));
        }
    }

    private sealed class ImmediateDispatcher : IUiDispatcher
    {
        public Task<T> RunAsync<T>(Func<CancellationToken, T> action, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(action(cancellationToken));
        }

        public Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return action(cancellationToken);
        }
    }

    private static async Task WaitForHeadlessWindowAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
    }

    private sealed class RecordingAvaloniaInputInjector : IAvaloniaInputInjector
    {
        public bool PointerClicked { get; private set; }

        public int? ScreenX { get; private set; }

        public int? ScreenY { get; private set; }

        public string? Key { get; private set; }

        public IReadOnlyList<string>? Modifiers { get; private set; }

        public string? Text { get; private set; }

        public ToolResult PointerClick(int screenX, int screenY)
        {
            PointerClicked = true;
            ScreenX = screenX;
            ScreenY = screenY;
            return ToolResult.Ok("Pointer click sent.");
        }

        public ToolResult KeyPress(string key, IReadOnlyList<string> modifiers)
        {
            Key = key;
            Modifiers = modifiers.ToArray();
            return ToolResult.Ok("Key press sent.");
        }

        public ToolResult TypeText(string text)
        {
            Text = text;
            return ToolResult.Ok("Text input sent.");
        }
    }

    private sealed class RecordingCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public object? Parameter { get; private set; }

        public bool CanExecute(object? parameter)
        {
            _ = parameter;
            return true;
        }

        public void Execute(object? parameter)
        {
            Parameter = parameter;
        }

        public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    private sealed record PathLookupDataContext(string Value);

    private sealed class MutableDataContext
    {
        public int Count { get; set; }
    }
}

public sealed class TestAvaloniaApp
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<Application>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
