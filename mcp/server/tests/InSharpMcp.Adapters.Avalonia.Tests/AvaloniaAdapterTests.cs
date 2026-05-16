using InSharpMcp.Adapters.Avalonia;
using InSharpMcp.Contracts;
using Avalonia.Controls;
using System.Text.Json;
using System.Windows.Input;

namespace InSharpMcp.Adapters.Avalonia.Tests;

public sealed class AvaloniaAdapterTests
{
    [Fact]
    public async Task PointerInputSimulator_ForwardsKeyAndTextInput()
    {
        var input = new RecordingAvaloniaInputInjector();
        var simulator = new AvaloniaPointerInputSimulator(new Button(), new ImmediateDispatcher(), input);

        var keyResult = await simulator.KeyPressAsync("enter", ["ctrl"], CancellationToken.None);
        var textResult = await simulator.TypeTextAsync("hello", CancellationToken.None);

        Assert.True(keyResult.Success);
        Assert.True(textResult.Success);
        Assert.Equal("enter", input.Key);
        Assert.Equal(["ctrl"], input.Modifiers);
        Assert.Equal("hello", input.Text);
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

        var result = await invoker.InvokeDefaultActionAsync("0", CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("payload", command.Parameter);
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
            CancellationToken.None);

        Assert.True(snapshotResult.Success);

        var metadataResult = await inspector.GetElementMetadataAsync(
            "0/599",
            new ToolLimits { MaxNodes = 1 },
            CancellationToken.None);

        Assert.True(metadataResult.Success);
        var metadata = Assert.IsType<ElementMetadata>(metadataResult.Data);
        Assert.Equal("Button599", metadata.Name);

        var dataContextResult = await inspector.GetElementDataContextAsync(
            "0/599",
            new ToolLimits { MaxNodes = 1 },
            CancellationToken.None);

        Assert.True(dataContextResult.Success);
        var dataContext = Assert.IsType<DataContextMetadata>(dataContextResult.Data);
        Assert.EndsWith(nameof(PathLookupDataContext), dataContext.TypeName, StringComparison.Ordinal);

        var invokeResult = await invoker.InvokeDefaultActionAsync("0/599", CancellationToken.None);

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
            CancellationToken.None);
        var dataContextResult = await editor.SetElementPropertyAsync(
            "0/0",
            ElementPropertyTarget.DataContext,
            nameof(MutableDataContext.Count),
            dataContextValue.RootElement,
            CancellationToken.None);

        Assert.True(elementResult.Success);
        Assert.Equal("After", button.Name);
        Assert.True(dataContextResult.Success);
        Assert.Equal(42, Assert.IsType<MutableDataContext>(button.DataContext).Count);
    }

    [Fact]
    public async Task AccessibilityTreeProvider_ReturnsInspectableTreeFromInspector()
    {
        var expected = ToolResult.Ok(
            "Visual tree snapshot returned.",
            new UiTreeSnapshot(new UiElementNode("0", "Window"), NodeCount: 1, Truncated: false));
        var provider = new AvaloniaAccessibilityTreeProvider(new StubTreeInspector(expected));

        var result = await provider.GetAccessibilityTreeAsync(new ToolLimits(), CancellationToken.None);

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

    private sealed class RecordingAvaloniaInputInjector : IAvaloniaInputInjector
    {
        public string? Key { get; private set; }

        public IReadOnlyList<string>? Modifiers { get; private set; }

        public string? Text { get; private set; }

        public ToolResult PointerClick(int screenX, int screenY)
        {
            _ = screenX;
            _ = screenY;
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
