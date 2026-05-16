using InSharpMcp.Contracts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace InSharpMcp.Adapters.Uno;

public sealed class UnoVisualTreeInspector : IUiTreeInspector
{
    private readonly DependencyObject _root;
    private readonly IUiDispatcher _dispatcher;

    public UnoVisualTreeInspector(DependencyObject root, IUiDispatcher dispatcher)
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

                if (match.Value.Element is not FrameworkElement { DataContext: { } dataContext })
                {
                    return ToolResult.Ok(
                        "Element has no DataContext.",
                        new DataContextMetadata("<null>", new Dictionary<string, object?>(), Truncated: false));
                }

                return ToolResult.Ok("DataContext metadata returned.", DataContextMetadataFactory.Create(dataContext, limits));
            },
            cancellationToken);

    private static UiElementNode? CopyBounded(
        DependencyObject element,
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

        var childCount = VisualTreeHelper.GetChildrenCount(element);
        if (currentDepth >= limits.MaxDepth || childCount == 0)
        {
            if (childCount > 0 && currentDepth >= limits.MaxDepth)
            {
                truncated = true;
            }

            return CreateNode(element, identifier, limits, Array.Empty<UiElementNode>());
        }

        var children = new List<UiElementNode>();
        for (var index = 0; index < childCount; index++)
        {
            var child = VisualTreeHelper.GetChild(element, index);
            var copied = CopyBounded(
                child,
                $"{identifier}/{index.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
                currentDepth + 1,
                limits,
                budget,
                ref truncated);
            if (copied is not null)
            {
                children.Add(copied);
            }
        }

        return CreateNode(element, identifier, limits, children);
    }

    internal static (DependencyObject Element, string Identifier)? Find(
        DependencyObject element,
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
            var childCount = VisualTreeHelper.GetChildrenCount(current);
            var childIndex = path[pathIndex];
            if (childIndex >= childCount)
            {
                return null;
            }

            current = VisualTreeHelper.GetChild(current, childIndex);
        }

        return (current, elementIdentifier);
    }

    private static UiElementNode CreateNode(
        DependencyObject element,
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

    private static ElementMetadata CreateMetadata(
        DependencyObject element,
        string identifier,
        ToolLimits limits)
    {
        var frameworkElement = element as FrameworkElement;
        var control = element as Control;
        var automationId = AutomationProperties.GetAutomationId(element);
        var automationName = AutomationProperties.GetName(element);
        var name = frameworkElement?.Name;
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
            frameworkElement?.Visibility == Visibility.Visible,
            control?.IsEnabled);
    }

    private static string? GetText(DependencyObject element) =>
        element switch
        {
            TextBlock textBlock => textBlock.Text,
            TextBox textBox => textBox.Text,
            ContentControl { Content: string text } => text,
            ContentControl { Content: { } content } => content.ToString(),
            _ => AutomationProperties.GetName(element),
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
