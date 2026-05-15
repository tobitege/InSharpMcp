using InSharpMcp.Concurrency;
using InSharpMcp.Contracts;
using InSharpMcp.Tracing;

namespace InSharpMcp.Routing;

public sealed class InProcessAppInstanceClient : IAppInstanceClient
{
    private readonly IUiTreeInspector _treeInspector;
    private readonly IScreenshotProvider _screenshotProvider;
    private readonly IAccessibilityTreeProvider _accessibilityTreeProvider;
    private readonly IPointerInputSimulator _inputSimulator;
    private readonly IAutomationPeerInvoker _automationPeerInvoker;
    private readonly IAppProvider _appProvider;
    private readonly IUiOperationQueue _uiQueue;

    public InProcessAppInstanceClient(
        IUiTreeInspector treeInspector,
        IScreenshotProvider screenshotProvider,
        IAccessibilityTreeProvider accessibilityTreeProvider,
        IPointerInputSimulator inputSimulator,
        IAutomationPeerInvoker automationPeerInvoker,
        IAppProvider appProvider,
        IUiOperationQueue uiQueue,
        IEventLogProvider eventLog,
        ITraceStore traceStore)
    {
        _treeInspector = treeInspector;
        _screenshotProvider = screenshotProvider;
        _accessibilityTreeProvider = accessibilityTreeProvider;
        _inputSimulator = inputSimulator;
        _automationPeerInvoker = automationPeerInvoker;
        _appProvider = appProvider;
        _uiQueue = uiQueue;
        EventLog = eventLog;
        TraceStore = traceStore;
    }

    public IEventLogProvider EventLog { get; }

    public ITraceStore TraceStore { get; }

    public Task<ToolResult> GetVisualTreeSnapshotAsync(ToolLimits limits, CancellationToken cancellationToken) =>
        RunUiAsync(
            "visualtree_snapshot",
            token => _treeInspector.GetVisualTreeSnapshotAsync(limits, token),
            limits,
            cancellationToken);

    public Task<ToolResult> GetElementMetadataAsync(
        string elementIdentifier,
        ToolLimits limits,
        CancellationToken cancellationToken) =>
        RunUiAsync(
            "get_element_metadata",
            token => _treeInspector.GetElementMetadataAsync(elementIdentifier, limits, token),
            limits,
            cancellationToken);

    public Task<ToolResult> GetElementDataContextAsync(
        string elementIdentifier,
        ToolLimits limits,
        CancellationToken cancellationToken) =>
        RunUiAsync(
            "get_element_datacontext",
            token => _treeInspector.GetElementDataContextAsync(elementIdentifier, limits, token),
            limits,
            cancellationToken);

    public Task<ScreenshotResult> CaptureScreenshotAsync(CancellationToken cancellationToken) =>
        _uiQueue.RunAsync(
            "get_screenshot",
            async token =>
            {
                var result = await _screenshotProvider.CaptureScreenshotAsync(token).ConfigureAwait(false);
                return result.Success
                    ? ToolResult.Ok(result.Message ?? "Screenshot captured.", result)
                    : ToolResult.Fail(result.Message ?? "Screenshot capture failed.", result.ErrorCode ?? "screenshot_failed", result);
            },
            new ToolLimits(),
            cancellationToken).ContinueWith(
                static task =>
                {
                    var result = task.GetAwaiter().GetResult();
                    return result.Data is ScreenshotResult screenshot
                        ? screenshot
                        : new ScreenshotResult(false, null, result.Message, result.ErrorCode);
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

    public Task<ToolResult> GetAccessibilityTreeAsync(ToolLimits limits, CancellationToken cancellationToken) =>
        RunUiAsync(
            "get_accessibility_tree",
            token => _accessibilityTreeProvider.GetAccessibilityTreeAsync(limits, token),
            limits,
            cancellationToken);

    public Task<ToolResult> PointerClickAsync(double x, double y, CancellationToken cancellationToken) =>
        RunUiAsync(
            "pointer_click",
            token => _inputSimulator.PointerClickAsync(x, y, token),
            new ToolLimits(),
            cancellationToken);

    public Task<ToolResult> KeyPressAsync(
        string key,
        IReadOnlyList<string> modifiers,
        CancellationToken cancellationToken) =>
        RunUiAsync(
            "key_press",
            token => _inputSimulator.KeyPressAsync(key, modifiers, token),
            new ToolLimits(),
            cancellationToken);

    public Task<ToolResult> TypeTextAsync(string text, CancellationToken cancellationToken) =>
        RunUiAsync(
            "type_text",
            token => _inputSimulator.TypeTextAsync(text, token),
            new ToolLimits(),
            cancellationToken);

    public Task<ToolResult> InvokeDefaultActionAsync(string elementIdentifier, CancellationToken cancellationToken) =>
        RunUiAsync(
            "element_peer_default_action",
            token => _automationPeerInvoker.InvokeDefaultActionAsync(elementIdentifier, token),
            new ToolLimits(),
            cancellationToken);

    public Task<ToolResult> CloseAsync(CancellationToken cancellationToken) =>
        RunUiAsync(
            "close",
            _appProvider.CloseAsync,
            new ToolLimits(),
            cancellationToken);

    public void RecordEvent(EventLogEntry entry)
    {
        EventLog.Add(entry);
        TraceStore.Record(entry);
    }

    private Task<ToolResult> RunUiAsync(
        string operationName,
        Func<CancellationToken, Task<ToolResult>> operation,
        ToolLimits limits,
        CancellationToken cancellationToken) =>
        _uiQueue.RunAsync(operationName, operation, limits, cancellationToken);
}
