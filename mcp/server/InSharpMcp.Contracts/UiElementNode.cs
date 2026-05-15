namespace InSharpMcp.Contracts;

public sealed record UiElementNode(
    string ElementIdentifier,
    string Type,
    string? Name = null,
    string? AutomationId = null,
    string? Text = null,
    string? Role = null,
    bool? IsVisible = null,
    bool? IsEnabled = null,
    IReadOnlyList<UiElementNode>? Children = null);
