using Avalonia.Controls;
using InSharpMcp.Contracts;

namespace InSharpMcp.Adapters.Avalonia;

public sealed class AvaloniaAppProvider : IAppProvider
{
    private readonly Window _window;
    private readonly IUiDispatcher _dispatcher;

    public AvaloniaAppProvider(
        Window window,
        IUiDispatcher dispatcher,
        string appName,
        string appVersion,
        string platformTarget)
    {
        _window = window;
        _dispatcher = dispatcher;
        AppName = appName;
        AppVersion = appVersion;
        PlatformTarget = platformTarget;
    }

    public int ProcessId => Environment.ProcessId;

    public string OperatingSystem => Environment.OSVersion.Platform.ToString();

    public string PlatformTarget { get; }

    public string AppName { get; }

    public string AppVersion { get; }

    public Task<ToolResult> CloseAsync(CancellationToken cancellationToken) =>
        _dispatcher.RunAsync(
            token =>
            {
                token.ThrowIfCancellationRequested();
                _window.Close();
                return ToolResult.Ok("Window close requested.");
            },
            cancellationToken);
}
