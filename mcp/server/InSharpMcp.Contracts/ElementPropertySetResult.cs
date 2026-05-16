namespace InSharpMcp.Contracts;

public sealed record ElementPropertySetResult(
    string ElementIdentifier,
    string TargetObject,
    string PropertyName,
    string TargetType,
    string PropertyType,
    string? PreviousValue,
    string? NewValue);
