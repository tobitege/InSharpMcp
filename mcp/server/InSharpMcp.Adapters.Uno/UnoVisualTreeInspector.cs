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
                var remainingNodes = limits.MaxNodes;
                var truncated = false;
                var rootNode = CopyBounded(_root, "0", currentDepth: 1, limits, ref remainingNodes, ref truncated);
                var snapshot = new UiTreeSnapshot(rootNode!, limits.MaxNodes - remainingNodes, truncated);
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
                var match = Find(_root, elementIdentifier, "0", limits.MaxNodes, token);
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
                var match = Find(_root, elementIdentifier, "0", limits.MaxNodes, token);
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
        ref int remainingNodes,
        ref bool truncated)
    {
        if (remainingNodes <= 0)
        {
            truncated = true;
            return null;
        }

        remainingNodes--;
        var childCount = VisualTreeHelper.GetChildrenCount(element);
        if (currentDepth >= limits.MaxDepth || childCount == 0)
        {
            if (childCount > 0 && currentDepth >= limits.MaxDepth)
            {
                truncated = true;
            }

            return CreateNode(element, identifier, Array.Empty<UiElementNode>());
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
                ref remainingNodes,
                ref truncated);
            if (copied is not null)
            {
                children.Add(copied);
            }
        }

        return CreateNode(element, identifier, children);
    }

    private static (DependencyObject Element, string Identifier)? Find(
        DependencyObject element,
        string elementIdentifier,
        string currentIdentifier,
        int remainingNodes,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (remainingNodes <= 0)
        {
            return null;
        }

        if (string.Equals(currentIdentifier, elementIdentifier, StringComparison.Ordinal))
        {
            return (element, currentIdentifier);
        }

        var childCount = VisualTreeHelper.GetChildrenCount(element);
        for (var index = 0; index < childCount; index++)
        {
            var childIdentifier = $"{currentIdentifier}/{index.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            var found = Find(
                VisualTreeHelper.GetChild(element, index),
                elementIdentifier,
                childIdentifier,
                remainingNodes - 1,
                cancellationToken);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private static UiElementNode CreateNode(
        DependencyObject element,
        string identifier,
        IReadOnlyList<UiElementNode> children)
    {
        var metadata = CreateMetadata(element, identifier, new ToolLimits());
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
        var textBlock = element as TextBlock;
        var automationId = AutomationProperties.GetAutomationId(element);
        var name = frameworkElement?.Name;
        var text = textBlock?.Text;
        if (text is { Length: > 0 } && text.Length > limits.MaxTextCharacters)
        {
            text = text[..limits.MaxTextCharacters];
        }

        return new ElementMetadata(
            identifier,
            element.GetType().Name,
            string.IsNullOrWhiteSpace(name) ? null : name,
            string.IsNullOrWhiteSpace(automationId) ? null : automationId,
            text,
            control is null ? null : "control",
            frameworkElement?.Visibility == Visibility.Visible,
            control?.IsEnabled);
    }

}
