namespace InSharpMcp.Registry;

public sealed record AppTargetSelector(
    string? InstanceId = null,
    string? AppId = null,
    string? AdapterKind = null);
