using InSharpMcp.Contracts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace InSharpMcp.Adapters.Uno;

public sealed class UnoPointerInputSimulator : IPointerInputSimulator, IElementClickSimulator
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

    public Task<ToolResult> ElementClickAsync(string elementIdentifier, CancellationToken cancellationToken) =>
        _dispatcher.RunAsync(
            token =>
            {
                token.ThrowIfCancellationRequested();
                if (!TryGetRoot(out var root, out var error))
                {
                    return error;
                }

                var match = UnoVisualTreeInspector.Find(root, elementIdentifier, token);
                if (match is null)
                {
                    return ToolResult.Fail("Element was not found.", "not_found");
                }

                if (!CanClick(match.Value.Element)
                    || !UnoVisualTreeInspector.TryGetVisibleBounds(root, match.Value.Element, out var bounds))
                {
                    return ToolResult.Fail("Element cannot be clicked because it is hidden, disabled, or has empty bounds.", "not_clickable");
                }

                var centerX = bounds.X + bounds.Width / 2;
                var centerY = bounds.Y + bounds.Height / 2;
                if (!HitsElementOrDescendant(root, match.Value.Element, new Point(centerX, centerY)))
                {
                    return ToolResult.Fail("Element center does not hit the requested Uno element.", "not_clickable");
                }

                return TryGetScreenPoint(centerX, centerY, out var screenX, out var screenY, out error)
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
        if (!TryGetRoot(out var root, out error))
        {
            screenX = 0;
            screenY = 0;
            return false;
        }

        if (!IsInsideRoot(root, x, y))
        {
            screenX = 0;
            screenY = 0;
            error = ToolResult.Fail("Pointer coordinates are outside the Uno root bounds.", "out_of_bounds");
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

    private bool TryGetRoot(out UIElement root, out ToolResult error)
    {
        if (_window.Content is UIElement content)
        {
            root = content;
            error = ToolResult.Ok("Root resolved.");
            return true;
        }

        root = null!;
        error = ToolResult.Fail("The Uno window content root is not a UIElement.", "unsupported");
        return false;
    }

    private static bool IsInsideRoot(UIElement root, double x, double y) =>
        root is FrameworkElement frameworkElement
        && x >= 0
        && y >= 0
        && x < frameworkElement.ActualWidth
        && y < frameworkElement.ActualHeight;

    private static bool CanClick(DependencyObject element) =>
        element is FrameworkElement { Visibility: Visibility.Visible }
        && element is not Control { IsEnabled: false };

    private static bool HitsElementOrDescendant(UIElement root, DependencyObject element, Point rootPoint)
    {
        return VisualTreeHelper.FindElementsInHostCoordinates(rootPoint, root)
            .OfType<DependencyObject>()
            .Any(hit => hit == element || IsDescendantOf(hit, element));
    }

    private static bool IsDescendantOf(DependencyObject descendant, DependencyObject ancestor)
    {
        for (var current = VisualTreeHelper.GetParent(descendant); current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current == ancestor)
            {
                return true;
            }
        }

        return false;
    }
}
