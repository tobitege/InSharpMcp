using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using InSharpMcp.Contracts;

namespace InSharpMcp.Adapters.Avalonia;

public sealed class AvaloniaVisualTreeInspector : IUiTreeInspector
{
    private readonly Visual _root;
    private readonly IUiDispatcher _dispatcher;

    public AvaloniaVisualTreeInspector(Visual root, IUiDispatcher dispatcher)
    {
        _root = root;
        _dispatcher = dispatcher;
    }

    public Task<ToolResult> GetVisualTreeSnapshotAsync(ToolLimits limits, CancellationToken cancellationToken) =>
        _dispatcher.RunAsync(
            token =>
            {
                token.ThrowIfCancellationRequested();
                var budget = new NodeVisitBudget(limits.MaxNodes);
                var truncated = false;
                var rootNode = CopyBounded(_root, "0", currentDepth: 1, limits, budget, ref truncated);
                var snapshot = new UiTreeSnapshot(rootNode!, budget.VisitedNodes, truncated);
                return ToolResult.Ok("Visual tree snapshot returned.", snapshot);
            },
            cancellationToken);

    public Task<ToolResult> GetElementMetadataAsync(
        string elementIdentifier,
        ToolLimits limits,
        CancellationToken cancellationToken) =>
        _dispatcher.RunAsync(
            token =>
            {
                token.ThrowIfCancellationRequested();
                var match = Find(_root, elementIdentifier, token);
                if (match is null)
                {
                    return ToolResult.Fail("Element was not found.", "not_found");
                }

                return ToolResult.Ok("Element metadata returned.", CreateMetadata(match.Value.Element, match.Value.Identifier, limits));
            },
            cancellationToken);

    public Task<ToolResult> GetElementDataContextAsync(
        string elementIdentifier,
        ToolLimits limits,
        CancellationToken cancellationToken) =>
        _dispatcher.RunAsync(
            token =>
            {
                token.ThrowIfCancellationRequested();
                var match = Find(_root, elementIdentifier, token);
                if (match is null)
                {
                    return ToolResult.Fail("Element was not found.", "not_found");
                }

                if (match.Value.Element is not StyledElement { DataContext: { } dataContext })
                {
                    return ToolResult.Ok(
                        "Element has no DataContext.",
                        new DataContextMetadata("<null>", new Dictionary<string, object?>(), Truncated: false));
                }

                return ToolResult.Ok("DataContext metadata returned.", DataContextMetadataFactory.Create(dataContext, limits));
            },
            cancellationToken);

    private static UiElementNode? CopyBounded(
        Visual element,
        string identifier,
        int currentDepth,
        ToolLimits limits,
        NodeVisitBudget budget,
        ref bool truncated)
    {
        if (!budget.TryVisit())
        {
            truncated = true;
            return null;
        }

        var children = element.GetVisualChildren().OfType<Visual>().ToArray();
        if (currentDepth >= limits.MaxDepth || children.Length == 0)
        {
            if (children.Length > 0 && currentDepth >= limits.MaxDepth)
            {
                truncated = true;
            }

            return CreateNode(element, identifier, limits, Array.Empty<UiElementNode>());
        }

        var copiedChildren = new List<UiElementNode>();
        for (var index = 0; index < children.Length; index++)
        {
            var copied = CopyBounded(
                children[index],
                $"{identifier}/{index.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
                currentDepth + 1,
                limits,
                budget,
                ref truncated);
            if (copied is not null)
            {
                copiedChildren.Add(copied);
            }
        }

        return CreateNode(element, identifier, limits, copiedChildren);
    }

    internal static (Visual Element, string Identifier)? Find(
        Visual element,
        string elementIdentifier,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryParseIdentifier(elementIdentifier, out var path))
        {
            return null;
        }

        var current = element;
        for (var pathIndex = 1; pathIndex < path.Length; pathIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var children = current.GetVisualChildren().OfType<Visual>().ToArray();
            var childIndex = path[pathIndex];
            if (childIndex >= children.Length)
            {
                return null;
            }

            current = children[childIndex];
        }

        return (current, elementIdentifier);
    }

    private static UiElementNode CreateNode(
        Visual element,
        string identifier,
        ToolLimits limits,
        IReadOnlyList<UiElementNode> children)
    {
        var metadata = CreateMetadata(element, identifier, limits);
        return new UiElementNode(
            metadata.ElementIdentifier,
            metadata.Type,
            metadata.Name,
            metadata.AutomationId,
            metadata.Text,
            metadata.Role,
            metadata.IsVisible,
            metadata.IsEnabled,
            children);
    }

    private static ElementMetadata CreateMetadata(Visual element, string identifier, ToolLimits limits)
    {
        var control = element as Control;
        var name = control?.Name;
        var automationId = control is null ? null : AutomationProperties.GetAutomationId(control);
        var automationName = control is null ? null : AutomationProperties.GetName(control);
        var text = GetText(element);
        if (text is { Length: > 0 } && text.Length > limits.MaxTextCharacters)
        {
            text = text[..limits.MaxTextCharacters];
        }

        return new ElementMetadata(
            identifier,
            element.GetType().Name,
            FirstNonWhiteSpace(name, automationName),
            string.IsNullOrWhiteSpace(automationId) ? null : automationId,
            text,
            control is null ? null : element.GetType().Name,
            control?.IsVisible,
            control?.IsEnabled);
    }

    private static string? GetText(Visual element) =>
        element switch
        {
            TextBlock textBlock => textBlock.Text,
            TextBox textBox => textBox.Text,
            ContentControl { Content: string text } => text,
            ContentControl { Content: { } content } => content.ToString(),
            HeaderedContentControl { Header: string header } => header,
            HeaderedContentControl { Header: { } header } => header.ToString(),
            _ => null
        };

    private static string? FirstNonWhiteSpace(string? first, string? second)
    {
        if (!string.IsNullOrWhiteSpace(first))
        {
            return first;
        }

        return string.IsNullOrWhiteSpace(second) ? null : second;
    }

    private static bool TryParseIdentifier(string elementIdentifier, out int[] path)
    {
        path = [];
        var segments = elementIdentifier.Split('/');
        if (segments.Length == 0 || segments[0] != "0")
        {
            return false;
        }

        path = new int[segments.Length];
        for (var index = 0; index < segments.Length; index++)
        {
            if (!int.TryParse(
                    segments[index],
                    System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var pathSegment)
                || pathSegment < 0)
            {
                path = [];
                return false;
            }

            path[index] = pathSegment;
        }

        return true;
    }
}
