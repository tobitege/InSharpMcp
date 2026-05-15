using InSharpMcp.Contracts;

namespace InSharpMcp.Adapters.Uno;

public sealed class UnoAutomationPeerInvoker : IAutomationPeerInvoker
{
    public Task<ToolResult> InvokeDefaultActionAsync(string elementIdentifier, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = elementIdentifier;
        return Task.FromResult(ToolResult.Fail("Uno automation peer invocation is not supported until public invokable patterns are wired.", "unsupported"));
    }
}
