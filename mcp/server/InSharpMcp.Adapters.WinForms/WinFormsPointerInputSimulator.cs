using InSharpMcp.Contracts;
using System.Windows.Forms;

namespace InSharpMcp.Adapters.WinForms;

public sealed class WinFormsPointerInputSimulator : IPointerInputSimulator, IElementClickSimulator
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
                if (!IsInsideRoot(x, y))
                {
                    return ToolResult.Fail("Pointer coordinates are outside the WinForms root bounds.", "out_of_bounds");
                }

                var point = _root.PointToScreen(new System.Drawing.Point((int)Math.Floor(x), (int)Math.Floor(y)));
                return _inputInjector.PointerClick(point.X, point.Y);
            },
            cancellationToken);

    public Task<ToolResult> ElementClickAsync(string elementIdentifier, CancellationToken cancellationToken) =>
        _dispatcher.RunAsync(
            token =>
            {
                token.ThrowIfCancellationRequested();
                var match = WinFormsVisualTreeInspector.Find(_root, elementIdentifier, token);
                if (match is null)
                {
                    return ToolResult.Fail("Element was not found.", "not_found");
                }

                var element = match.Value.Element;
                if (!CanClick(element)
                    || !WinFormsVisualTreeInspector.TryGetVisibleBounds(_root, element, out var bounds))
                {
                    return ToolResult.Fail("Element cannot be clicked because it is hidden, disabled, or has empty bounds.", "not_clickable");
                }

                var centerX = bounds.X + bounds.Width / 2;
                var centerY = bounds.Y + bounds.Height / 2;
                if (!IsInsideRoot(centerX, centerY))
                {
                    return ToolResult.Fail("Element center is outside the WinForms root bounds.", "out_of_bounds");
                }

                var point = _root.PointToScreen(new System.Drawing.Point((int)Math.Floor(centerX), (int)Math.Floor(centerY)));
                if (!HitsElementOrDescendant(element, point))
                {
                    return ToolResult.Fail("Element center does not hit the requested WinForms element.", "not_clickable");
                }

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
        && x < _root.ClientSize.Width
        && y < _root.ClientSize.Height;

    private static bool CanClick(Control element) =>
        element.Visible
        && element.Enabled;

    private bool HitsElementOrDescendant(Control element, System.Drawing.Point screenPoint)
    {
        var hit = DeepestChildAtScreenPoint(_root, screenPoint);
        return hit is not null && (hit == element || IsDescendantOf(hit, element));
    }

    private static Control? DeepestChildAtScreenPoint(Control root, System.Drawing.Point screenPoint)
    {
        var current = root;
        while (true)
        {
            var localPoint = current.PointToClient(screenPoint);
            var child = current.GetChildAtPoint(
                localPoint,
                GetChildAtPointSkip.Invisible | GetChildAtPointSkip.Disabled);
            if (child is null)
            {
                return current;
            }

            current = child;
        }
    }

    private static bool IsDescendantOf(Control control, Control ancestor)
    {
        for (var current = control.Parent; current is not null; current = current.Parent)
        {
            if (current == ancestor)
            {
                return true;
            }
        }

        return false;
    }
}
