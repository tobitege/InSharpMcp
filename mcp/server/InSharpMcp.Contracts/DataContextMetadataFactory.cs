using System.Reflection;

namespace InSharpMcp.Contracts;

public static class DataContextMetadataFactory
{
    public static DataContextMetadata Create(object dataContext, ToolLimits limits)
    {
        var properties = new Dictionary<string, object?>(StringComparer.Ordinal);
        var truncated = false;
        foreach (var property in dataContext.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (properties.Count >= limits.MaxNodes)
            {
                truncated = true;
                break;
            }

            if (property.GetIndexParameters().Length > 0 || !IsAllowedProperty(property.PropertyType))
            {
                continue;
            }

            object? value;
            if (IsSensitivePropertyName(property.Name))
            {
                value = "<redacted>";
            }
            else
            {
                value = property.GetValue(dataContext);
                if (value is string text && text.Length > limits.MaxTextCharacters)
                {
                    value = text[..limits.MaxTextCharacters];
                    truncated = true;
                }
            }

            properties[property.Name] = value;
        }

        return new DataContextMetadata(dataContext.GetType().FullName ?? dataContext.GetType().Name, properties, truncated);
    }

    private static bool IsAllowedProperty(Type type)
    {
        var actualType = Nullable.GetUnderlyingType(type) ?? type;
        return actualType.IsPrimitive
            || actualType.IsEnum
            || actualType == typeof(string)
            || actualType == typeof(decimal)
            || actualType == typeof(DateTime)
            || actualType == typeof(DateTimeOffset)
            || actualType == typeof(Guid);
    }

    private static bool IsSensitivePropertyName(string propertyName) =>
        propertyName.Contains("password", StringComparison.OrdinalIgnoreCase)
        || propertyName.Contains("secret", StringComparison.OrdinalIgnoreCase)
        || propertyName.Contains("token", StringComparison.OrdinalIgnoreCase)
        || propertyName.Contains("key", StringComparison.OrdinalIgnoreCase);
}
