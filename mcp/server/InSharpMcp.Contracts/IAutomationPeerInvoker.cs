namespace InSharpMcp.Contracts;

public interface IAutomationPeerInvoker
{
    Task<ToolResult> InvokeDefaultActionAsync(string elementIdentifier, CancellationToken cancellationToken);
}
