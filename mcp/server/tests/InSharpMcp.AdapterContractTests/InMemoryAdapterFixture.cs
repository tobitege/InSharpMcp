using InSharpMcp.Contracts;

namespace InSharpMcp.AdapterContractTests;

internal sealed class InMemoryAdapterFixture
{
    public IUiDispatcher Dispatcher { get; } = new InMemoryUiDispatcher();

    public IUiTreeInspector TreeInspector { get; } = new InMemoryUiTreeInspector();

    public IScreenshotProvider ScreenshotProvider { get; } = new InMemoryScreenshotProvider();

    public IPointerInputSimulator PointerInput { get; } = new InMemoryPointerInputSimulator();

    public IAutomationPeerInvoker AutomationPeerInvoker { get; } = new InMemoryAutomationPeerInvoker();
}

internal sealed class InMemoryUiDispatcher : IUiDispatcher
{
    public Task<T> RunAsync<T>(Func<CancellationToken, T> action, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(action(cancellationToken));
    }

    public async Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await action(cancellationToken).ConfigureAwait(false);
    }
}

internal sealed class InMemoryUiTreeInspector : IUiTreeInspector
{
    private readonly UiElementNode _tree =
        new(
            "root",
            "Window",
            Name: "MainWindow",
            AutomationId: "main",
            Role: "window",
            IsVisible: true,
            IsEnabled: true,
            Children:
            [
                new(
                    "save-button",
                    "Button",
                    Name: "Save",
                    AutomationId: "saveButton",
                    Text: "Save",
                    Role: "button",
                    IsVisible: true,
                    IsEnabled: true),
                new(
                    "notes-input",
                    "TextBox",
                    Name: "Notes",
                    AutomationId: "notesInput",
                    Text: "Initial notes",
                    Role: "textbox",
                    IsVisible: true,
                    IsEnabled: true),
            ]);

    public Task<ToolResult> GetVisualTreeSnapshotAsync(ToolLimits limits, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var remainingNodes = limits.MaxNodes;
        var truncated = false;
        var root = CopyBounded(_tree, currentDepth: 1, limits, ref remainingNodes, ref truncated);
        var snapshot = new UiTreeSnapshot(root!, limits.MaxNodes - remainingNodes, truncated);
        return Task.FromResult(ToolResult.Ok("Visual tree snapshot returned.", snapshot));
    }

    public Task<ToolResult> GetElementMetadataAsync(
        string elementIdentifier,
        ToolLimits limits,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var match = Find(_tree, elementIdentifier);
        if (match is null)
        {
            return Task.FromResult(ToolResult.Fail("Element was not found.", "not_found"));
        }

        var metadata = new ElementMetadata(
            match.ElementIdentifier,
            match.Type,
            match.Name,
            match.AutomationId,
            Trim(match.Text, limits.MaxTextCharacters),
            match.Role,
            match.IsVisible,
            match.IsEnabled);
        return Task.FromResult(ToolResult.Ok("Element metadata returned.", metadata));
    }

    public Task<ToolResult> GetElementDataContextAsync(
        string elementIdentifier,
        ToolLimits limits,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = elementIdentifier;
        _ = limits;
        return Task.FromResult(ToolResult.Fail("DataContext inspection is unsupported by this adapter.", "unsupported"));
    }

    private static UiElementNode? CopyBounded(
        UiElementNode node,
        int currentDepth,
        ToolLimits limits,
        ref int remainingNodes,
        ref bool truncated)
    {
        if (remainingNodes <= 0)
        {
            truncated = true;
            return null;
        }

        remainingNodes--;
        if (currentDepth >= limits.MaxDepth || node.Children is null || node.Children.Count == 0)
        {
            if (node.Children is { Count: > 0 } && currentDepth >= limits.MaxDepth)
            {
                truncated = true;
            }

            return node with { Children = Array.Empty<UiElementNode>() };
        }

        var children = new List<UiElementNode>();
        foreach (var child in node.Children)
        {
            var copied = CopyBounded(child, currentDepth + 1, limits, ref remainingNodes, ref truncated);
            if (copied is not null)
            {
                children.Add(copied);
            }
        }

        return node with { Children = children };
    }

    private static UiElementNode? Find(UiElementNode node, string elementIdentifier)
    {
        if (string.Equals(node.ElementIdentifier, elementIdentifier, StringComparison.Ordinal))
        {
            return node;
        }

        return node.Children?.Select(child => Find(child, elementIdentifier)).FirstOrDefault(match => match is not null);
    }

    private static string? Trim(string? value, int maxCharacters) =>
        value is not null && value.Length > maxCharacters ? value[..maxCharacters] : value;
}

internal sealed class InMemoryScreenshotProvider : IScreenshotProvider
{
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47];

    public Task<ScreenshotResult> CaptureScreenshotAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new ScreenshotResult(true, PngHeader, "Screenshot captured."));
    }
}

internal sealed class InMemoryPointerInputSimulator : IPointerInputSimulator
{
    public Task<ToolResult> PointerClickAsync(double x, double y, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (x < 0 || y < 0)
        {
            return Task.FromResult(ToolResult.Fail("Coordinates must not be negative.", "invalid_coordinates"));
        }

        return Task.FromResult(ToolResult.Ok("Pointer click accepted."));
    }

    public Task<ToolResult> KeyPressAsync(
        string key,
        IReadOnlyList<string> modifiers,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = modifiers;
        return string.IsNullOrWhiteSpace(key)
            ? Task.FromResult(ToolResult.Fail("Key is required.", "invalid_key"))
            : Task.FromResult(ToolResult.Ok("Key press accepted."));
    }

    public Task<ToolResult> TypeTextAsync(string text, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return text.Length > 1024
            ? Task.FromResult(ToolResult.Fail("Text is too long.", "text_too_long"))
            : Task.FromResult(ToolResult.Ok("Text accepted."));
    }
}

internal sealed class InMemoryAutomationPeerInvoker : IAutomationPeerInvoker
{
    public Task<ToolResult> InvokeDefaultActionAsync(string elementIdentifier, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return string.Equals(elementIdentifier, "save-button", StringComparison.Ordinal)
            ? Task.FromResult(ToolResult.Ok("Default action invoked."))
            : Task.FromResult(ToolResult.Fail("Element is not invokable.", "unsupported"));
    }
}
