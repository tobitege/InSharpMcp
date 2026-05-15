using InSharpMcp.Contracts;
using InSharpMcp.Contracts.LocalTransport;
using InSharpMcp.Events;
using InSharpMcp.Routing;
using InSharpMcp.Tracing;

namespace InSharpMcp.Transports;

internal sealed class RemoteAppInstanceClient : IAppInstanceClient
{
    private readonly string _appPipeName;
    private readonly LocalAppTransportOptions _options;

    public RemoteAppInstanceClient(string appPipeName, LocalAppTransportOptions options)
    {
        _appPipeName = appPipeName;
        _options = options;
        EventLog = new BoundedEventLog();
        TraceStore = new BoundedTraceStore();
    }

    public IEventLogProvider EventLog { get; }

    public ITraceStore TraceStore { get; }

    public Task<ToolResult> GetVisualTreeSnapshotAsync(ToolLimits limits, CancellationToken cancellationToken) =>
        SendToolAsync(
            new LocalAppRequest(LocalAppOperation.VisualTreeSnapshot, Limits: limits),
            typeof(UiTreeSnapshot),
            cancellationToken);

    public Task<ToolResult> GetElementMetadataAsync(
        string elementIdentifier,
        ToolLimits limits,
        CancellationToken cancellationToken) =>
        SendToolAsync(
            new LocalAppRequest(LocalAppOperation.GetElementMetadata, limits, ElementIdentifier: elementIdentifier),
            typeof(ElementMetadata),
            cancellationToken);

    public Task<ToolResult> GetElementDataContextAsync(
        string elementIdentifier,
        ToolLimits limits,
        CancellationToken cancellationToken) =>
        SendToolAsync(
            new LocalAppRequest(LocalAppOperation.GetElementDataContext, limits, ElementIdentifier: elementIdentifier),
            typeof(DataContextMetadata),
            cancellationToken);

    public async Task<ScreenshotResult> CaptureScreenshotAsync(CancellationToken cancellationToken)
    {
        var response = await SendToolAsync(
            new LocalAppRequest(LocalAppOperation.GetScreenshot),
            typeof(ScreenshotResult),
            cancellationToken).ConfigureAwait(false);

        return response.Data is ScreenshotResult screenshot
            ? screenshot
            : new ScreenshotResult(response.Success, null, response.Message, response.ErrorCode);
    }

    public Task<ToolResult> GetAccessibilityTreeAsync(ToolLimits limits, CancellationToken cancellationToken) =>
        SendToolAsync(
            new LocalAppRequest(LocalAppOperation.GetAccessibilityTree, Limits: limits),
            typeof(UiTreeSnapshot),
            cancellationToken);

    public Task<ToolResult> PointerClickAsync(double x, double y, CancellationToken cancellationToken) =>
        SendToolAsync(
            new LocalAppRequest(LocalAppOperation.PointerClick, X: x, Y: y),
            dataType: null,
            cancellationToken);

    public Task<ToolResult> KeyPressAsync(
        string key,
        IReadOnlyList<string> modifiers,
        CancellationToken cancellationToken) =>
        SendToolAsync(
            new LocalAppRequest(LocalAppOperation.KeyPress, Key: key, Modifiers: modifiers.ToArray()),
            dataType: null,
            cancellationToken);

    public Task<ToolResult> TypeTextAsync(string text, CancellationToken cancellationToken) =>
        SendToolAsync(
            new LocalAppRequest(LocalAppOperation.TypeText, Text: text),
            dataType: null,
            cancellationToken);

    public Task<ToolResult> InvokeDefaultActionAsync(string elementIdentifier, CancellationToken cancellationToken) =>
        SendToolAsync(
            new LocalAppRequest(LocalAppOperation.ElementPeerDefaultAction, ElementIdentifier: elementIdentifier),
            dataType: null,
            cancellationToken);

    public Task<ToolResult> CloseAsync(CancellationToken cancellationToken) =>
        SendToolAsync(
            new LocalAppRequest(LocalAppOperation.Close),
            dataType: null,
            cancellationToken);

    public void RecordEvent(EventLogEntry entry)
    {
        EventLog.Add(entry);
        TraceStore.Record(entry);
    }

    private async Task<ToolResult> SendToolAsync(
        LocalAppRequest request,
        Type? dataType,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await LocalAppPipe.SendAsync<LocalAppRequest, LocalAppToolResponse>(
                _appPipeName,
                request,
                _options.RequestTimeout,
                cancellationToken).ConfigureAwait(false);
            return response.ToToolResult(dataType);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return ToolResult.Fail("The local app transport call was canceled.", "canceled");
        }
        catch (Exception exception) when (exception is IOException or TimeoutException or InvalidOperationException)
        {
            return ToolResult.Fail("The registered app instance did not respond over the local transport.", "transport_unavailable");
        }
    }
}
