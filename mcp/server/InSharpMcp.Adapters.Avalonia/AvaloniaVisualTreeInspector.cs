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
                var rootNode = CopyBounded(_root, _root, "0", currentDepth: 1, limits, budget, ref truncated);
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

                return ToolResult.Ok("Element metadata returned.", CreateMetadata(_root, match.Value.Element, match.Value.Identifier, limits));
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
        Visual root,
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

        if (currentDepth >= limits.MaxDepth)
        {
            if (element.GetVisualChildren().OfType<Visual>().Any())
            {
                truncated = true;
            }

            return CreateNode(root, element, identifier, limits, Array.Empty<UiElementNode>());
        }

        var copiedChildren = new List<UiElementNode>();
        var index = 0;
        foreach (var child in element.GetVisualChildren().OfType<Visual>())
        {
            if (budget.RemainingNodes <= 0)
            {
                truncated = true;
                break;
            }

            var copied = CopyBounded(
                root,
                child,
                $"{identifier}/{index.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
                currentDepth + 1,
                limits,
                budget,
                ref truncated);
            if (copied is not null)
            {
                copiedChildren.Add(copied);
            }

            index++;
        }

        return CreateNode(root, element, identifier, limits, copiedChildren);
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
        Visual root,
        Visual element,
        string identifier,
        ToolLimits limits,
        IReadOnlyList<UiElementNode> children)
    {
        var metadata = CreateMetadata(root, element, identifier, limits);
        return new UiElementNode(
            metadata.ElementIdentifier,
            metadata.Type,
            metadata.Name,
            metadata.AutomationId,
            metadata.Text,
            metadata.Role,
            metadata.IsVisible,
            metadata.IsEnabled,
            children,
            metadata.Bounds);
    }

    internal static ElementMetadata CreateMetadata(Visual root, Visual element, string identifier, ToolLimits limits)
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
            control?.IsEnabled,
            GetBounds(root, element));
    }

    internal static UiElementBounds? GetBounds(Visual root, Visual element)
    {
        var bounds = element.Bounds;
        var topLeft = element == root
            ? new Point(0, 0)
            : element.TranslatePoint(new Point(0, 0), root);
        return topLeft is { } point
            ? new UiElementBounds(point.X, point.Y, bounds.Width, bounds.Height)
            : null;
    }

    internal static bool TryGetVisibleBounds(Visual root, Visual element, out UiElementBounds bounds)
    {
        if (GetBounds(root, element) is not { Width: > 0, Height: > 0 } elementBounds)
        {
            bounds = new UiElementBounds(0, 0, 0, 0);
            return false;
        }

        var visible = new Rect(elementBounds.X, elementBounds.Y, elementBounds.Width, elementBounds.Height);
        if (element == root)
        {
            bounds = new UiElementBounds(visible.X, visible.Y, visible.Width, visible.Height);
            return true;
        }

        for (var current = element.GetVisualParent() as Visual; current is not null; current = current.GetVisualParent() as Visual)
        {
            if (GetBounds(root, current) is not { Width: > 0, Height: > 0 } currentBounds)
            {
                bounds = new UiElementBounds(0, 0, 0, 0);
                return false;
            }

            if (current == root || current.ClipToBounds || current.Clip is not null)
            {
                var clip = new Rect(currentBounds.X, currentBounds.Y, currentBounds.Width, currentBounds.Height);
                visible = visible.Intersect(clip);
                if (visible.Width <= 0 || visible.Height <= 0)
                {
                    bounds = new UiElementBounds(0, 0, 0, 0);
                    return false;
                }
            }

            if (current == root)
            {
                bounds = new UiElementBounds(visible.X, visible.Y, visible.Width, visible.Height);
                return true;
            }
        }

        bounds = new UiElementBounds(0, 0, 0, 0);
        return false;
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
