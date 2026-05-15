using InSharpMcp.Contracts;
using System.Windows.Forms;

namespace InSharpMcp.Adapters.WinForms;

public sealed class WinFormsPointerInputSimulator : IPointerInputSimulator
{
    private readonly Control _root;
    private readonly IUiDispatcher _dispatcher;
    private readonly IWinFormsInputInjector _inputInjector;

    public WinFormsPointerInputSimulator(
        Control root,
        IUiDispatcher dispatcher,
        IWinFormsInputInjector? inputInjector = null)
    {
        _root = root;
        _dispatcher = dispatcher;
        _inputInjector = inputInjector ?? new WinFormsWindowsInputInjector();
    }

    public Task<ToolResult> PointerClickAsync(double x, double y, CancellationToken cancellationToken) =>
        _dispatcher.RunAsync(
            token =>
            {
                token.ThrowIfCancellationRequested();
                var point = _root.PointToScreen(new System.Drawing.Point((int)Math.Round(x), (int)Math.Round(y)));
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
