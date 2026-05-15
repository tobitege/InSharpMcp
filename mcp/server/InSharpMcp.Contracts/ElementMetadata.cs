namespace InSharpMcp.Contracts;

public sealed record ElementMetadata(
    string ElementIdentifier,
    string Type,
    string? Name = null,
    string? AutomationId = null,
    string? Text = null,
    string? Role = null,
    bool? IsVisible = null,
    bool? IsEnabled = null);
