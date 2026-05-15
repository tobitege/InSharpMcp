using InSharpMcp.Contracts;

namespace InSharpMcp.Adapters.Uno;

public sealed class UnoAccessibilityTreeProvider : IAccessibilityTreeProvider
{
    private readonly IUiTreeInspector _treeInspector;

    public UnoAccessibilityTreeProvider(IUiTreeInspector treeInspector)
    {
        _treeInspector = treeInspector;
    }

    public Task<ToolResult> GetAccessibilityTreeAsync(ToolLimits limits, CancellationToken cancellationToken) =>
        _treeInspector.GetVisualTreeSnapshotAsync(limits, cancellationToken);
}
