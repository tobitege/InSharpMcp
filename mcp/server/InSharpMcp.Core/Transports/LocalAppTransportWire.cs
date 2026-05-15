using InSharpMcp.Contracts.LocalTransport;
using InSharpMcp.Registry;

namespace InSharpMcp.Transports;

internal static class LocalAppTransportWire
{
    public static LocalAppRegistrationMessage ToRegistrationMessage(
        AppInstanceDescriptor descriptor,
        string appPipeName) =>
        new(
            descriptor.InstanceId,
            descriptor.AppId,
            descriptor.AppName,
            descriptor.ProcessId,
            descriptor.AdapterKind,
            descriptor.PlatformTarget,
            descriptor.OperatingSystem,
            descriptor.AppVersion,
            descriptor.Capabilities.ToArray(),
            appPipeName);

    public static AppInstanceDescriptor ToDescriptor(LocalAppRegistrationMessage message, DateTimeOffset now) =>
        new(
            message.InstanceId,
            message.AppId,
            message.AppName,
            message.ProcessId,
            message.AdapterKind,
            message.PlatformTarget,
            message.OperatingSystem,
            message.AppVersion,
            new HashSet<string>(message.Capabilities, StringComparer.Ordinal),
            $"pipe://{message.AppPipeName}",
            now,
            now);
}
