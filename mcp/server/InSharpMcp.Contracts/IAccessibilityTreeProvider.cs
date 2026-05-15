namespace InSharpMcp.Contracts;

public interface IAccessibilityTreeProvider
{
    Task<ToolResult> GetAccessibilityTreeAsync(ToolLimits limits, CancellationToken cancellationToken);
}
