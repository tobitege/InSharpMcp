namespace InSharpMcp.Contracts;

public sealed record ElementSelector(
    string? Name = null,
    string? AutomationId = null,
    string? Type = null,
    string? Text = null,
    string? Role = null,
    int? Index = null,
    string? Path = null,
    IReadOnlyDictionary<string, string>? Adapter = null);
