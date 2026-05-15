using Avalonia;
using Avalonia.VisualTree;
using InSharpMcp.Contracts;

namespace InSharpMcp.Adapters.Avalonia;

public sealed class AvaloniaPointerInputSimulator : IPointerInputSimulator
{
    private readonly Visual _root;
    private readonly IUiDispatcher _dispatcher;
    private readonly IAvaloniaInputInjector _inputInjector;

    public AvaloniaPointerInputSimulator(
        Visual root,
        IUiDispatcher dispatcher,
        IAvaloniaInputInjector? inputInjector = null)
    {
        _root = root;
        _dispatcher = dispatcher;
        _inputInjector = inputInjector ?? new AvaloniaWindowsInputInjector();
    }

    public Task<ToolResult> PointerClickAsync(double x, double y, CancellationToken cancellationToken) =>
        _dispatcher.RunAsync(
            token =>
            {
                token.ThrowIfCancellationRequested();
                var point = _root.PointToScreen(new Point(x, y));
                return _inputInjector.PointerClick(point.X, point.Y);
            },
            cancellationToken);

    public Task<ToolResult> KeyPressAsync(
        string key,
        IReadOnlyList<string> modifiers,
        CancellationToken cancellationToken) =>
        _dispatcher.RunAsync(
            token =>
            {
                token.ThrowIfCancellationRequested();
                return _inputInjector.KeyPress(key, modifiers);
            },
            cancellationToken);

    public Task<ToolResult> TypeTextAsync(string text, CancellationToken cancellationToken) =>
        _dispatcher.RunAsync(
            token =>
            {
                token.ThrowIfCancellationRequested();
                return _inputInjector.TypeText(text);
            },
            cancellationToken);
}
