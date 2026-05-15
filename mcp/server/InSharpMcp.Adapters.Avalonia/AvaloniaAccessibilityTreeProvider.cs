using InSharpMcp.Contracts;

namespace InSharpMcp.Adapters.Avalonia;

public sealed class AvaloniaAccessibilityTreeProvider : IAccessibilityTreeProvider
{
    private readonly IUiTreeInspector _treeInspector;

    public AvaloniaAccessibilityTreeProvider(IUiTreeInspector treeInspector)
    {
        _treeInspector = treeInspector;
    }

    public Task<ToolResult> GetAccessibilityTreeAsync(ToolLimits limits, CancellationToken cancellationToken) =>
        _treeInspector.GetVisualTreeSnapshotAsync(limits, cancellationToken);
}
