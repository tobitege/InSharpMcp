using InSharpMcp.Contracts;
using System.Windows.Forms;

namespace InSharpMcp.Adapters.WinForms;

public sealed class WinFormsVisualTreeInspector : IUiTreeInspector
{
    private readonly Control _root;
    private readonly IUiDispatcher _dispatcher;

    public WinFormsVisualTreeInspector(Control root, IUiDispatcher dispatcher)
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

                if (match.Value.Element.Tag is not { } dataContext)
                {
                    return ToolResult.Ok(
                        "Element has no DataContext.",
                        new DataContextMetadata("<null>", new Dictionary<string, object?>(), Truncated: false));
                }

                return ToolResult.Ok("DataContext metadata returned.", DataContextMetadataFactory.Create(dataContext, limits));
            },
            cancellationToken);

    internal static (Control Element, string Identifier)? Find(
        Control element,
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
            var childIndex = path[pathIndex];
            if (childIndex >= current.Controls.Count)
            {
                return null;
            }

            current = current.Controls[childIndex];
        }

        return (current, elementIdentifier);
    }

    private static UiElementNode? CopyBounded(
        Control element,
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

        if (currentDepth >= limits.MaxDepth || element.Controls.Count == 0)
        {
            if (element.Controls.Count > 0 && currentDepth >= limits.MaxDepth)
            {
                truncated = true;
            }

            return CreateNode(element, identifier, limits, Array.Empty<UiElementNode>());
        }

        var children = new List<UiElementNode>();
        for (var index = 0; index < element.Controls.Count; index++)
        {
            var copied = CopyBounded(
                element.Controls[index],
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

    private static UiElementNode CreateNode(
        Control element,
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

    private static ElementMetadata CreateMetadata(Control element, string identifier, ToolLimits limits)
    {
        var text = string.IsNullOrWhiteSpace(element.Text) ? null : element.Text;
        if (text is { Length: > 0 } && text.Length > limits.MaxTextCharacters)
        {
            text = text[..limits.MaxTextCharacters];
        }

        var accessibleRole = element.AccessibleRole == AccessibleRole.Default
            ? element.GetType().Name
            : element.AccessibleRole.ToString();

        return new ElementMetadata(
            identifier,
            element.GetType().Name,
            FirstNonWhiteSpace(element.Name, element.AccessibleName),
            string.IsNullOrWhiteSpace(element.AccessibleName) ? null : element.AccessibleName,
            text,
            accessibleRole,
            element.Visible,
            element.Enabled);
    }

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
