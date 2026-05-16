namespace InSharpMcp.Bridge;

public sealed record AppBridgeRegistration(
    string AppId,
    string AppName,
    string AdapterKind,
    string PlatformTarget,
    string AppVersion,
    IReadOnlySet<string> Capabilities,
    string? InstanceId = null,
    string? OperatingSystem = null,
    int? ProcessId = null);

public static class AppBridgeCapabilities
{
    public static IReadOnlySet<string> Standard { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "visualtree",
        "metadata",
        "datacontext",
        "screenshot",
        "accessibility",
        "input",
        "default-action",
        "property-editing",
        "close",
    };
}
