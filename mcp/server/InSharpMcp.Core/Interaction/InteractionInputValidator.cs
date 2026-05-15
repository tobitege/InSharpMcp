using InSharpMcp.Contracts;

namespace InSharpMcp.Interaction;

public sealed class InteractionInputValidator
{
    private static readonly IReadOnlySet<string> AllowedModifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "alt",
        "control",
        "ctrl",
        "shift",
        "meta",
        "win",
    };

    private static readonly IReadOnlySet<string> AllowedNamedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "enter",
        "escape",
        "tab",
        "backspace",
        "delete",
        "space",
        "arrowup",
        "arrowdown",
        "arrowleft",
        "arrowright",
        "home",
        "end",
        "pageup",
        "pagedown",
    };

    public ToolResult ValidateCoordinates(double x, double y)
    {
        return double.IsFinite(x) && double.IsFinite(y) && x >= 0 && y >= 0
            ? ToolResult.Ok("Coordinates are valid.")
            : ToolResult.Fail("Coordinates must be finite and non-negative.", "invalid_coordinates");
    }

    public ToolResult ValidateKey(string key, IReadOnlyList<string> modifiers)
    {
        if (string.IsNullOrWhiteSpace(key) || !IsAllowedKey(key))
        {
            return ToolResult.Fail("Key is unsupported.", "invalid_key");
        }

        if (modifiers.Any(modifier => !AllowedModifiers.Contains(modifier)))
        {
            return ToolResult.Fail("One or more key modifiers are unsupported.", "invalid_modifier");
        }

        return ToolResult.Ok("Key input is valid.");
    }

    public ToolResult ValidateText(string text)
    {
        return text.Length <= 4_096
            ? ToolResult.Ok("Text input is valid.")
            : ToolResult.Fail("Text input exceeds the maximum length.", "text_too_long");
    }

    private static bool IsAllowedKey(string key)
    {
        if (key.Length == 1 && char.IsLetterOrDigit(key[0]))
        {
            return true;
        }

        if (AllowedNamedKeys.Contains(key))
        {
            return true;
        }

        return key.Length is >= 2 and <= 3
            && key[0] == 'F'
            && int.TryParse(key[1..], out var functionKey)
            && functionKey is >= 1 and <= 12;
    }
}
