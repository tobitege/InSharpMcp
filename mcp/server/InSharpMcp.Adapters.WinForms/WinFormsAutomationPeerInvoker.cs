using InSharpMcp.Contracts;
using System.Windows.Forms;

namespace InSharpMcp.Adapters.WinForms;

public sealed class WinFormsAutomationPeerInvoker : IAutomationPeerInvoker
{
    private readonly Control _root;
    private readonly IUiDispatcher _dispatcher;

    public WinFormsAutomationPeerInvoker(Control root, IUiDispatcher dispatcher)
    {
        _root = root;
        _dispatcher = dispatcher;
    }

    public Task<ToolResult> InvokeDefaultActionAsync(string elementIdentifier, CancellationToken cancellationToken) =>
        _dispatcher.RunAsync(
            token =>
            {
                token.ThrowIfCancellationRequested();
                var match = WinFormsVisualTreeInspector.Find(_root, elementIdentifier, token);
                if (match is null)
                {
                    return ToolResult.Fail("Element was not found.", "not_found");
                }

                if (match.Value.Element is not IButtonControl button)
                {
                    return ToolResult.Fail("WinForms default action is only supported for IButtonControl controls.", "unsupported");
                }

                button.PerformClick();
                return ToolResult.Ok("Default action invoked.");
            },
            cancellationToken);
}
