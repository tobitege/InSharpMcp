using InSharpMcp.Contracts;
using Microsoft.UI.Xaml;

namespace InSharpMcp.Adapters.Uno;

public sealed class UnoAppProvider : IAppProvider
{
    private readonly Window _window;

    public UnoAppProvider(Window window, string appName, string appVersion, string platformTarget)
    {
        _window = window;
        AppName = appName;
        AppVersion = appVersion;
        PlatformTarget = platformTarget;
    }

    public int ProcessId => Environment.ProcessId;

    public string OperatingSystem => Environment.OSVersion.Platform.ToString();

    public string PlatformTarget { get; }

    public string AppName { get; }

    public string AppVersion { get; }

    public Task<ToolResult> CloseAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _window.Close();
        return Task.FromResult(ToolResult.Ok("Window close requested."));
    }
}
