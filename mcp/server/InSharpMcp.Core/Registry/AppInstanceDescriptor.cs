namespace InSharpMcp.Registry;

public sealed record AppInstanceDescriptor(
    string InstanceId,
    string AppId,
    string AppName,
    int ProcessId,
    string AdapterKind,
    string PlatformTarget,
    string OperatingSystem,
    string AppVersion,
    IReadOnlySet<string> Capabilities,
    string Endpoint,
    DateTimeOffset RegisteredAt,
    DateTimeOffset LastHeartbeatAt);
