using InSharpMcp.Contracts;
using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace InSharpMcp.Adapters.Uno;

public sealed class UnoPointerInputSimulator : IPointerInputSimulator
{
    private readonly Window _window;
    private readonly IUiDispatcher _dispatcher;
    private readonly IUnoInputInjector _inputInjector;

    public UnoPointerInputSimulator(
        Window window,
        IUiDispatcher dispatcher,
        IUnoInputInjector? inputInjector = null)
    {
        _window = window;
        _dispatcher = dispatcher;
        _inputInjector = inputInjector ?? new UnoWindowsInputInjector();
    }

    public Task<ToolResult> PointerClickAsync(double x, double y, CancellationToken cancellationToken) =>
        _dispatcher.RunAsync(
            token =>
            {
                token.ThrowIfCancellationRequested();
                return TryGetScreenPoint(x, y, out var screenX, out var screenY, out var error)
                    ? _inputInjector.PointerClick(screenX, screenY)
                    : error;
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

    private bool TryGetScreenPoint(
        double x,
        double y,
        out int screenX,
        out int screenY,
        out ToolResult error)
    {
#if WINDOWS
        if (_window.Content is not UIElement root)
        {
            screenX = 0;
            screenY = 0;
            error = ToolResult.Fail("The Uno window content root is not a UIElement.", "unsupported");
            return false;
        }

        var transform = root.TransformToVisual(null);
        var clientPoint = transform.TransformPoint(new Point(x, y));
        var scale = root.XamlRoot?.RasterizationScale ?? 1;
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_window);
        if (hwnd == IntPtr.Zero)
        {
            screenX = 0;
            screenY = 0;
            error = ToolResult.Fail("The Uno window does not expose a native window handle.", "unsupported");
            return false;
        }

        return _inputInjector.TryClientToScreen(
            hwnd,
            (int)Math.Round(clientPoint.X * scale),
            (int)Math.Round(clientPoint.Y * scale),
            out screenX,
            out screenY,
            out error);
#else
        screenX = 0;
        screenY = 0;
        error = ToolResult.Fail("Uno pointer input is supported only when a native Windows window handle is available.", "unsupported");
        return false;
#endif
    }
}
