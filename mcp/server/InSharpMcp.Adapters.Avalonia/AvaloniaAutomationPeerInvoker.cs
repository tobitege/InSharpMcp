using Avalonia;
using Avalonia.Input;
using Avalonia.VisualTree;
using InSharpMcp.Contracts;

namespace InSharpMcp.Adapters.Avalonia;

public sealed class AvaloniaAutomationPeerInvoker : IAutomationPeerInvoker
{
    private readonly Visual _root;
    private readonly IUiDispatcher _dispatcher;

    public AvaloniaAutomationPeerInvoker(Visual root, IUiDispatcher dispatcher)
    {
        _root = root;
        _dispatcher = dispatcher;
    }

    public Task<ToolResult> InvokeDefaultActionAsync(string elementIdentifier, CancellationToken cancellationToken) =>
        _dispatcher.RunAsync(
            token =>
            {
                token.ThrowIfCancellationRequested();
                var match = AvaloniaVisualTreeInspector.Find(_root, elementIdentifier, token);
                if (match is null)
                {
                    return ToolResult.Fail("Element was not found.", "not_found");
                }

                if (match.Value.Element is not ICommandSource { Command: { } command } commandSource)
                {
                    return ToolResult.Fail("Avalonia default action is only supported for controls exposing ICommandSource.Command.", "unsupported");
                }

                var parameter = commandSource.CommandParameter;
                if (!command.CanExecute(parameter))
                {
                    return ToolResult.Fail("Avalonia command cannot execute for the current command parameter.", "command_disabled");
                }

                command.Execute(parameter);
                return ToolResult.Ok("Default action invoked.");
            },
            cancellationToken);
}
