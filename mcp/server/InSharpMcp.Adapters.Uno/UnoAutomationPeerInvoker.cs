using InSharpMcp.Contracts;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace InSharpMcp.Adapters.Uno;

public sealed class UnoAutomationPeerInvoker : IAutomationPeerInvoker
{
    private readonly Microsoft.UI.Xaml.DependencyObject _root;
    private readonly IUiDispatcher _dispatcher;

    public UnoAutomationPeerInvoker(Microsoft.UI.Xaml.DependencyObject root, IUiDispatcher dispatcher)
    {
        _root = root;
        _dispatcher = dispatcher;
    }

    public Task<ToolResult> InvokeDefaultActionAsync(string elementIdentifier, CancellationToken cancellationToken) =>
        _dispatcher.RunAsync(
            token =>
            {
                token.ThrowIfCancellationRequested();
                var budget = new NodeVisitBudget(new ToolLimits().MaxNodes);
                var match = UnoVisualTreeInspector.Find(_root, elementIdentifier, "0", budget, token);
                if (match is null)
                {
                    return ToolResult.Fail("Element was not found.", "not_found");
                }

                if (match.Value.Element is not ButtonBase { Command: { } command } button)
                {
                    return ToolResult.Fail("Uno default action is only supported for ButtonBase controls exposing Command.", "unsupported");
                }

                var parameter = button.CommandParameter;
                if (!command.CanExecute(parameter))
                {
                    return ToolResult.Fail("Uno command cannot execute for the current command parameter.", "command_disabled");
                }

                command.Execute(parameter);
                return ToolResult.Ok("Default action invoked.");
            },
            cancellationToken);
}
