using Avalonia;
using InSharpMcp.Adapters.Shared;
using InSharpMcp.Contracts;

namespace InSharpMcp.Adapters.Avalonia;

public sealed class AvaloniaElementPropertyEditor : ElementPropertyEditor<Visual>
{
    public AvaloniaElementPropertyEditor(Visual root, IUiDispatcher dispatcher)
        : base(root, dispatcher)
    {
    }

    protected override (Visual Element, string Identifier)? FindElement(
        Visual root,
        string elementIdentifier,
        CancellationToken cancellationToken) =>
        AvaloniaVisualTreeInspector.Find(root, elementIdentifier, cancellationToken);

    protected override object? GetDataContext(Visual element) =>
        element is StyledElement styledElement ? styledElement.DataContext : null;
}
