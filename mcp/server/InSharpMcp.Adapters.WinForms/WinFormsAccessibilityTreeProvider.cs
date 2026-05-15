using InSharpMcp.Contracts;

namespace InSharpMcp.Adapters.WinForms;

public sealed class WinFormsAccessibilityTreeProvider : IAccessibilityTreeProvider
{
    private readonly IUiTreeInspector _treeInspector;

    public WinFormsAccessibilityTreeProvider(IUiTreeInspector treeInspector)
    {
        _treeInspector = treeInspector;
    }

    public Task<ToolResult> GetAccessibilityTreeAsync(ToolLimits limits, CancellationToken cancellationToken) =>
        _treeInspector.GetVisualTreeSnapshotAsync(limits, cancellationToken);
}
