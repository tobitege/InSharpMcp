using InSharpMcp.Contracts;

namespace InSharpMcp.Adapters.Avalonia;

public sealed class AvaloniaAutomationPeerInvoker : IAutomationPeerInvoker
{
    public Task<ToolResult> InvokeDefaultActionAsync(string elementIdentifier, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = elementIdentifier;
        return Task.FromResult(ToolResult.Fail("Avalonia automation peer invocation is not supported until public invokable patterns are wired.", "unsupported"));
    }
}
