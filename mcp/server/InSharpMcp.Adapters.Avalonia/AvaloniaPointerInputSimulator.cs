using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using InSharpMcp.Contracts;

namespace InSharpMcp.Adapters.Avalonia;

public sealed class AvaloniaPointerInputSimulator : IPointerInputSimulator, IElementClickSimulator
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
                if (!IsInsideRoot(x, y))
                {
                    return ToolResult.Fail("Pointer coordinates are outside the Avalonia root bounds.", "out_of_bounds");
                }

                var point = _root.PointToScreen(new Point(x, y));
                return _inputInjector.PointerClick(point.X, point.Y);
            },
            cancellationToken);

    public Task<ToolResult> ElementClickAsync(string elementIdentifier, CancellationToken cancellationToken) =>
        _dispatcher.RunAsync(
            token =>
            {
                token.ThrowIfCancellationRequested();
                var match = AvaloniaVisualTreeInspector.Find(_root, elementIdentifier, token);
                if (match is null)
                {
                    return ToolResult.Fail("Element was not found.", "not_found");
                }

                var element = match.Value.Element;
                if (!CanClick(element)
                    || !AvaloniaVisualTreeInspector.TryGetVisibleBounds(_root, element, out var bounds))
                {
                    return ToolResult.Fail("Element cannot be clicked because it is hidden, disabled, or has empty bounds.", "not_clickable");
                }

                var centerX = bounds.X + bounds.Width / 2;
                var centerY = bounds.Y + bounds.Height / 2;
                if (!IsInsideRoot(centerX, centerY))
                {
                    return ToolResult.Fail("Element center is outside the Avalonia root bounds.", "out_of_bounds");
                }

                var rootPoint = new Point(centerX, centerY);
                if (!HitsElementOrDescendant(element, rootPoint))
                {
                    return ToolResult.Fail("Element center does not hit the requested Avalonia element.", "not_clickable");
                }

                var point = _root.PointToScreen(rootPoint);
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

    private bool IsInsideRoot(double x, double y) =>
        x >= 0
        && y >= 0
        && x < _root.Bounds.Width
        && y < _root.Bounds.Height;

    private static bool CanClick(Visual element) =>
        element is not Control { IsVisible: false }
        && element is not Control { IsEnabled: false }
        && element is not IInputElement { IsHitTestVisible: false };

    private bool HitsElementOrDescendant(Visual element, Point rootPoint)
    {
        var topLevel = TopLevel.GetTopLevel(_root);
        if (topLevel is IInputElement topLevelInput
            && _root.TranslatePoint(rootPoint, topLevel) is { } topLevelPoint)
        {
            return HitsElementOrDescendant(topLevelInput, element, topLevelPoint);
        }

        return _root is IInputElement inputRoot
            && HitsElementOrDescendant(inputRoot, element, rootPoint);
    }

    private static bool HitsElementOrDescendant(IInputElement inputRoot, Visual element, Point point) =>
        inputRoot.GetInputElementsAt(point)
            .OfType<Visual>()
            .Any(visual => visual == element || IsDescendantOf(visual, element));

    private static bool IsDescendantOf(Visual visual, Visual ancestor)
    {
        for (var current = visual.GetVisualParent() as Visual; current is not null; current = current.GetVisualParent() as Visual)
        {
            if (current == ancestor)
            {
                return true;
            }
        }

        return false;
    }
}
