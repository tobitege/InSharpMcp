using System.Reflection;
using System.Text.Json;
using InSharpMcp.Contracts;

namespace InSharpMcp.Adapters.Shared;

public abstract class ElementPropertyEditor<TElement> : IElementPropertyEditor
    where TElement : class
{
    private readonly TElement _root;
    private readonly IUiDispatcher _dispatcher;

    protected ElementPropertyEditor(TElement root, IUiDispatcher dispatcher)
    {
        _root = root;
        _dispatcher = dispatcher;
    }

    public Task<ToolResult> SetElementPropertyAsync(
        string elementIdentifier,
        string targetObject,
        string propertyName,
        JsonElement value,
        CancellationToken cancellationToken) =>
        _dispatcher.RunAsync(
            token =>
            {
                token.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(propertyName))
                {
                    return ToolResult.Fail("Property name is required.", "invalid_property");
                }

                var match = FindElement(_root, elementIdentifier, token);
                if (match is null)
                {
                    return ToolResult.Fail("Element was not found.", "not_found");
                }

                var normalizedTarget = NormalizeTargetObject(targetObject);
                if (normalizedTarget is null)
                {
                    return ToolResult.Fail("Target object must be 'element' or 'dataContext'.", "invalid_target_object");
                }

                var target = ResolveTarget(match.Value.Element, normalizedTarget);
                if (target is null)
                {
                    return ToolResult.Fail("Target object was not available for the selected element.", "target_unavailable");
                }

                return SetProperty(match.Value.Identifier, normalizedTarget, target, propertyName, value);
            },
            cancellationToken);

    protected abstract (TElement Element, string Identifier)? FindElement(
        TElement root,
        string elementIdentifier,
        CancellationToken cancellationToken);

    protected abstract object? GetDataContext(TElement element);

    private object? ResolveTarget(TElement element, string targetObject)
    {
        return targetObject switch
        {
            ElementPropertyTarget.Element => element,
            ElementPropertyTarget.DataContext => GetDataContext(element),
            _ => null
        };
    }

    private static ToolResult SetProperty(
        string elementIdentifier,
        string targetObject,
        object target,
        string propertyName,
        JsonElement value)
    {
        var property = FindProperty(target.GetType(), propertyName);
        if (property is null)
        {
            return ToolResult.Fail("Property was not found.", "property_not_found");
        }

        if (property.GetIndexParameters().Length > 0)
        {
            return ToolResult.Fail("Indexed properties are not supported.", "unsupported_property");
        }

        if (!property.CanWrite || property.SetMethod is not { IsPublic: true })
        {
            return ToolResult.Fail("Property is not publicly settable.", "property_read_only");
        }

        if (!ElementPropertyValueConverter.TryConvert(
                value,
                property.PropertyType,
                out var convertedValue,
                out var errorMessage,
                out var errorCode))
        {
            return ToolResult.Fail(errorMessage, errorCode);
        }

        string? previousValue = null;
        try
        {
            if (property.CanRead)
            {
                previousValue = ElementPropertyValueConverter.FormatValue(property.GetValue(target), property.Name);
            }
        }
        catch (Exception exception) when (exception is TargetInvocationException or ArgumentException)
        {
        }

        try
        {
            property.SetValue(target, convertedValue);
        }
        catch (Exception exception) when (exception is TargetInvocationException or ArgumentException or MethodAccessException)
        {
            return ToolResult.Fail("Property setter failed.", "property_set_failed", new { ExceptionType = exception.GetType().Name });
        }

        string? newValue = null;
        try
        {
            if (property.CanRead)
            {
                newValue = ElementPropertyValueConverter.FormatValue(property.GetValue(target), property.Name);
            }
        }
        catch (Exception exception) when (exception is TargetInvocationException or ArgumentException)
        {
            newValue = ElementPropertyValueConverter.FormatValue(convertedValue, property.Name);
        }

        return ToolResult.Ok(
            "Element property set.",
            new ElementPropertySetResult(
                elementIdentifier,
                targetObject,
                property.Name,
                target.GetType().FullName ?? target.GetType().Name,
                property.PropertyType.FullName ?? property.PropertyType.Name,
                previousValue,
                newValue));
    }

    private static PropertyInfo? FindProperty(Type targetType, string propertyName)
    {
        var exact = targetType.GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public);
        if (exact is not null)
        {
            return exact;
        }

        var matches = targetType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }

    private static string? NormalizeTargetObject(string targetObject)
    {
        if (string.IsNullOrWhiteSpace(targetObject)
            || string.Equals(targetObject, ElementPropertyTarget.Element, StringComparison.OrdinalIgnoreCase)
            || string.Equals(targetObject, "control", StringComparison.OrdinalIgnoreCase)
            || string.Equals(targetObject, "uiElement", StringComparison.OrdinalIgnoreCase))
        {
            return ElementPropertyTarget.Element;
        }

        return string.Equals(targetObject, ElementPropertyTarget.DataContext, StringComparison.OrdinalIgnoreCase)
            ? ElementPropertyTarget.DataContext
            : null;
    }
}
