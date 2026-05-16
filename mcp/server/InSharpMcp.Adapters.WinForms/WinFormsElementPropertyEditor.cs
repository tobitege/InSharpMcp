using InSharpMcp.Adapters.Shared;
using InSharpMcp.Contracts;
using System.Windows.Forms;

namespace InSharpMcp.Adapters.WinForms;

public sealed class WinFormsElementPropertyEditor : ElementPropertyEditor<Control>
{
    public WinFormsElementPropertyEditor(Control root, IUiDispatcher dispatcher)
        : base(root, dispatcher)
    {
    }

    protected override (Control Element, string Identifier)? FindElement(
        Control root,
        string elementIdentifier,
        CancellationToken cancellationToken) =>
        WinFormsVisualTreeInspector.Find(root, elementIdentifier, cancellationToken);

    protected override object? GetDataContext(Control element) => element.Tag;
}
