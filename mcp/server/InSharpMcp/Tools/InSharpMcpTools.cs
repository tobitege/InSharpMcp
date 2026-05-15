using InSharpMcp.Contracts;
using InSharpMcp.Concurrency;
using InSharpMcp.Limits;
using InSharpMcp.Registry;
using InSharpMcp.Selectors;
using ModelContextProtocol.Server;

namespace InSharpMcp.Tools;

[McpServerToolType]
public sealed class InSharpMcpTools
{
    [McpServerTool(Name = "ism_list_instances")]
    public static ToolResult ListInstances(AppInstanceRegistry registry)
    {
        var instances = registry.List().Select(instance => new
        {
            instance.InstanceId,
            instance.AppId,
            instance.AppName,
            instance.ProcessId,
            instance.AdapterKind,
            instance.PlatformTarget,
            instance.OperatingSystem,
            instance.AppVersion,
            instance.Capabilities,
            instance.RegisteredAt,
            instance.LastHeartbeatAt,
        });

        return ToolResult.Ok("Registered app instances listed.", instances.ToArray());
    }

    [McpServerTool(Name = "ism_get_runtime_info")]
    public static ToolResult GetRuntimeInfo(AppTargetSelector? target, AppInstanceSelector selector)
    {
        var selection = selector.Select(target);
        if (!selection.Succeeded)
        {
            return selection.Error!;
        }

        var instance = selection.Instance!;
        return ToolResult.Ok(
            "Runtime information returned.",
            new
            {
                instance.InstanceId,
                instance.AppId,
                instance.AppName,
                instance.ProcessId,
                instance.AdapterKind,
                instance.PlatformTarget,
                instance.OperatingSystem,
                instance.AppVersion,
            });
    }

    [McpServerTool(Name = "ism_visualtree_snapshot")]
    public static Task<ToolResult> VisualTreeSnapshot(
        IUiTreeInspector inspector,
        IUiOperationQueue uiQueue,
        ToolLimitPolicyEvaluator limitPolicy,
        int? maxDepth = null,
        int? maxNodes = null,
        CancellationToken cancellationToken = default)
    {
        var limits = CreateCallLimits(limitPolicy, maxDepth, maxNodes, maxTextCharacters: null);
        return uiQueue.RunAsync(
            "visualtree_snapshot",
            token => inspector.GetVisualTreeSnapshotAsync(limits, token),
            limits,
            cancellationToken);
    }

    [McpServerTool(Name = "ism_get_element_metadata")]
    public static Task<ToolResult> GetElementMetadata(
        IUiTreeInspector inspector,
        IUiOperationQueue uiQueue,
        ToolLimitPolicyEvaluator limitPolicy,
        string elementIdentifier,
        int? maxTextCharacters = null,
        CancellationToken cancellationToken = default)
    {
        var limits = CreateCallLimits(limitPolicy, maxDepth: null, maxNodes: null, maxTextCharacters);
        return uiQueue.RunAsync(
            "get_element_metadata",
            token => inspector.GetElementMetadataAsync(elementIdentifier, limits, token),
            limits,
            cancellationToken);
    }

    [McpServerTool(Name = "ism_get_element_datacontext")]
    public static Task<ToolResult> GetElementDataContext(
        IUiTreeInspector inspector,
        IUiOperationQueue uiQueue,
        ToolLimitPolicyEvaluator limitPolicy,
        string elementIdentifier,
        int? maxNodes = null,
        int? maxTextCharacters = null,
        CancellationToken cancellationToken = default)
    {
        var limits = CreateCallLimits(limitPolicy, maxDepth: null, maxNodes, maxTextCharacters);
        return uiQueue.RunAsync(
            "get_element_datacontext",
            token => inspector.GetElementDataContextAsync(elementIdentifier, limits, token),
            limits,
            cancellationToken);
    }

    [McpServerTool(Name = "ism_get_screenshot")]
    public static async Task<ScreenshotResult> GetScreenshot(
        IScreenshotProvider screenshotProvider,
        CancellationToken cancellationToken = default)
    {
        return await screenshotProvider.CaptureScreenshotAsync(cancellationToken).ConfigureAwait(false);
    }

    [McpServerTool(Name = "ism_query_elements")]
    public static async Task<ToolResult> QueryElements(
        IUiTreeInspector inspector,
        IUiOperationQueue uiQueue,
        ToolLimitPolicyEvaluator limitPolicy,
        ElementSelectorMatcher matcher,
        ElementSelector selector,
        int? maxDepth = null,
        int? maxNodes = null,
        CancellationToken cancellationToken = default)
    {
        var limits = CreateCallLimits(limitPolicy, maxDepth, maxNodes, maxTextCharacters: null);
        var snapshotResult = await uiQueue.RunAsync(
            "query_elements_snapshot",
            token => inspector.GetVisualTreeSnapshotAsync(limits, token),
            limits,
            cancellationToken).ConfigureAwait(false);

        return snapshotResult.Data is UiTreeSnapshot snapshot
            ? matcher.Match(snapshot, selector, limits)
            : snapshotResult;
    }

    [McpServerTool(Name = "ism_wait_for_element")]
    public static async Task<ToolResult> WaitForElement(
        IUiTreeInspector inspector,
        IUiOperationQueue uiQueue,
        ToolLimitPolicyEvaluator limitPolicy,
        ElementSelectorMatcher matcher,
        ElementSelector selector,
        int? timeoutMs = null,
        CancellationToken cancellationToken = default)
    {
        var timeout = TimeSpan.FromMilliseconds(Math.Clamp(timeoutMs ?? 5_000, 100, 30_000));
        var started = TimeProvider.System.GetTimestamp();
        while (TimeProvider.System.GetElapsedTime(started) < timeout)
        {
            var result = await QueryElements(
                inspector,
                uiQueue,
                limitPolicy,
                matcher,
                selector,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            if (result.Data is ElementQueryResult { Matches.Count: > 0 })
            {
                return ToolResult.Ok("Element matched before timeout.", result.Data);
            }

            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }

        return ToolResult.Fail("Timed out waiting for element.", "timeout");
    }

    [McpServerTool(Name = "ism_get_accessibility_tree")]
    public static Task<ToolResult> GetAccessibilityTree(
        IAccessibilityTreeProvider provider,
        IUiOperationQueue uiQueue,
        ToolLimitPolicyEvaluator limitPolicy,
        int? maxDepth = null,
        int? maxNodes = null,
        CancellationToken cancellationToken = default)
    {
        var limits = CreateCallLimits(limitPolicy, maxDepth, maxNodes, maxTextCharacters: null);
        return uiQueue.RunAsync(
            "get_accessibility_tree",
            token => provider.GetAccessibilityTreeAsync(limits, token),
            limits,
            cancellationToken);
    }

    [McpServerTool(Name = "ism_get_event_log")]
    public static ToolResult GetEventLog(IEventLogProvider eventLog, string[]? categories = null, int maximumCount = 100)
    {
        var categorySet = categories is null ? null : new HashSet<string>(categories, StringComparer.Ordinal);
        var events = eventLog.List(categorySet, Math.Clamp(maximumCount, 1, 1_000));
        return ToolResult.Ok("Event log returned.", events);
    }

    private static ToolLimits CreateCallLimits(
        ToolLimitPolicyEvaluator limitPolicy,
        int? maxDepth,
        int? maxNodes,
        int? maxTextCharacters)
    {
        var result = limitPolicy.Evaluate(new ClientLimitConfiguration(
            maxDepth?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            maxNodes?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            maxTextCharacters?.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        return result.Limits;
    }
}
