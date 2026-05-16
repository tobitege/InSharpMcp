using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace InSharpMcp.Contracts;

public static class ElementPropertyValueConverter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static bool TryConvert(
        JsonElement value,
        Type targetType,
        out object? convertedValue,
        out string errorMessage,
        out string errorCode)
    {
        convertedValue = null;
        errorMessage = string.Empty;
        errorCode = string.Empty;

        if (value.ValueKind == JsonValueKind.Undefined)
        {
            errorMessage = "Property value is required.";
            errorCode = "invalid_value";
            return false;
        }

        var nullableType = Nullable.GetUnderlyingType(targetType);
        var actualType = nullableType ?? targetType;
        if (value.ValueKind == JsonValueKind.Null)
        {
            if (!actualType.IsValueType || nullableType is not null)
            {
                convertedValue = null;
                return true;
            }

            errorMessage = $"Property type '{targetType.Name}' cannot be set to null.";
            errorCode = "invalid_value";
            return false;
        }

        if (actualType == typeof(string))
        {
            convertedValue = value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText();
            return true;
        }

        if (actualType.IsEnum)
        {
            return TryConvertEnum(value, actualType, out convertedValue, out errorMessage, out errorCode);
        }

        if (TryConvertKnownType(value, actualType, out convertedValue))
        {
            return true;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            var text = value.GetString();
            if (text is not null)
            {
                if (TryConvertFromString(text, actualType, out convertedValue))
                {
                    return true;
                }

                if (TryInvokeParse(text, actualType, out convertedValue))
                {
                    return true;
                }

                if (TryInvokeAssignableParse(text, actualType, "Avalonia.Media.Brush", out convertedValue))
                {
                    return true;
                }
            }
        }

        errorMessage = $"Property type '{targetType.FullName ?? targetType.Name}' is not supported by property editing.";
        errorCode = "unsupported_property_type";
        return false;
    }

    public static string? FormatValue(object? value, string propertyName)
    {
        if (value is null)
        {
            return null;
        }

        if (IsSensitivePropertyName(propertyName))
        {
            return "<redacted>";
        }

        return value switch
        {
            string text => text,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }

    private static bool TryConvertEnum(
        JsonElement value,
        Type enumType,
        out object? convertedValue,
        out string errorMessage,
        out string errorCode)
    {
        convertedValue = null;
        errorMessage = string.Empty;
        errorCode = string.Empty;

        if (value.ValueKind == JsonValueKind.String)
        {
            var text = value.GetString();
            if (!string.IsNullOrWhiteSpace(text)
                && Enum.TryParse(enumType, text, ignoreCase: true, out convertedValue))
            {
                return true;
            }
        }

        try
        {
            var numericValue = value.Deserialize(Enum.GetUnderlyingType(enumType), JsonOptions);
            if (numericValue is not null)
            {
                convertedValue = Enum.ToObject(enumType, numericValue);
                return true;
            }
        }
        catch (JsonException)
        {
        }

        errorMessage = $"Value cannot be converted to enum '{enumType.Name}'.";
        errorCode = "invalid_value";
        return false;
    }

    private static bool TryConvertKnownType(JsonElement value, Type targetType, out object? convertedValue)
    {
        if (!IsKnownJsonType(targetType))
        {
            convertedValue = null;
            return false;
        }

        try
        {
            convertedValue = value.Deserialize(targetType, JsonOptions);
            return convertedValue is not null;
        }
        catch (JsonException)
        {
        }

        convertedValue = null;
        return false;
    }

    private static bool IsKnownJsonType(Type targetType) =>
        targetType == typeof(bool)
        || targetType == typeof(byte)
        || targetType == typeof(sbyte)
        || targetType == typeof(short)
        || targetType == typeof(ushort)
        || targetType == typeof(int)
        || targetType == typeof(uint)
        || targetType == typeof(long)
        || targetType == typeof(ulong)
        || targetType == typeof(float)
        || targetType == typeof(double)
        || targetType == typeof(decimal)
        || targetType == typeof(DateTime)
        || targetType == typeof(DateTimeOffset)
        || targetType == typeof(Guid)
        || targetType == typeof(char);

    private static bool TryConvertFromString(string text, Type targetType, out object? convertedValue)
    {
        var converter = TypeDescriptor.GetConverter(targetType);
        if (converter.CanConvertFrom(typeof(string)))
        {
            try
            {
                convertedValue = converter.ConvertFromInvariantString(text);
                return convertedValue is not null;
            }
            catch (Exception exception) when (exception is FormatException or NotSupportedException)
            {
            }
        }

        convertedValue = null;
        return false;
    }

    private static bool TryInvokeParse(string text, Type targetType, out object? convertedValue)
    {
        var parse = targetType.GetMethod(
            "Parse",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            [typeof(string)],
            modifiers: null);
        if (parse is null)
        {
            convertedValue = null;
            return false;
        }

        try
        {
            convertedValue = parse.Invoke(null, [text]);
            return convertedValue is not null;
        }
        catch (TargetInvocationException)
        {
            convertedValue = null;
            return false;
        }
    }

    private static bool TryInvokeAssignableParse(
        string text,
        Type targetType,
        string parserTypeName,
        out object? convertedValue)
    {
        var parserType = targetType.Assembly.GetType(parserTypeName);
        if (parserType is null || !targetType.IsAssignableFrom(parserType))
        {
            convertedValue = null;
            return false;
        }

        return TryInvokeParse(text, parserType, out convertedValue)
            && convertedValue is not null
            && targetType.IsInstanceOfType(convertedValue);
    }

    private static bool IsSensitivePropertyName(string propertyName) =>
        propertyName.Contains("password", StringComparison.OrdinalIgnoreCase)
        || propertyName.Contains("secret", StringComparison.OrdinalIgnoreCase)
        || propertyName.Contains("token", StringComparison.OrdinalIgnoreCase)
        || propertyName.Contains("key", StringComparison.OrdinalIgnoreCase);
}
