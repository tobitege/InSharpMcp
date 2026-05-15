namespace InSharpMcp.Contracts;

public interface IAppProvider
{
    int ProcessId { get; }

    string OperatingSystem { get; }

    string PlatformTarget { get; }

    string AppName { get; }

    string AppVersion { get; }

    Task<ToolResult> CloseAsync(CancellationToken cancellationToken);
}
