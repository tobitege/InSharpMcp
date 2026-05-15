using InSharpMcp.Contracts;
using System.Windows.Forms;

namespace InSharpMcp.Adapters.WinForms;

public sealed class WinFormsAppProvider : IAppProvider
{
    private readonly Form _form;
    private readonly IUiDispatcher _dispatcher;

    public WinFormsAppProvider(
        Form form,
        IUiDispatcher dispatcher,
        string appName,
        string appVersion,
        string platformTarget)
    {
        _form = form;
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
                _form.Close();
                return ToolResult.Ok("Window close requested.");
            },
            cancellationToken);
}
