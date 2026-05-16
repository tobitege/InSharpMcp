using InSharpMcp.Adapters.Shared;
using InSharpMcp.Contracts;
using Microsoft.UI.Xaml;

namespace InSharpMcp.Adapters.Uno;

public sealed class UnoElementPropertyEditor : ElementPropertyEditor<DependencyObject>
{
    public UnoElementPropertyEditor(DependencyObject root, IUiDispatcher dispatcher)
        : base(root, dispatcher)
    {
    }

    protected override (DependencyObject Element, string Identifier)? FindElement(
        DependencyObject root,
        string elementIdentifier,
        CancellationToken cancellationToken) =>
        UnoVisualTreeInspector.Find(root, elementIdentifier, cancellationToken);

    protected override object? GetDataContext(DependencyObject element) =>
        element is FrameworkElement frameworkElement ? frameworkElement.DataContext : null;
}
