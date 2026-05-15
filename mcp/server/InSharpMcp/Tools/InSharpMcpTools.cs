using InSharpMcp.Contracts;
using InSharpMcp.Concurrency;
using InSharpMcp.Interaction;
using InSharpMcp.Limits;
using InSharpMcp.Registry;
using InSharpMcp.Security;
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

    [McpServerTool(Name = "ism_pointer_click")]
    public static Task<ToolResult> PointerClick(
        IPointerInputSimulator input,
        IUiOperationQueue uiQueue,
        McpAuthorization authorization,
        InteractionInputValidator validator,
        IEventLogProvider eventLog,
        double x,
        double y,
        string? authorizationToken = null,
        CancellationToken cancellationToken = default)
    {
        var authorized = authorization.AuthorizeTool("ism_pointer_click", McpTransportKind.Stdio, authorizationToken);
        if (!authorized.Success)
        {
            return Task.FromResult(authorized);
        }

        var validation = validator.ValidateCoordinates(x, y);
        if (!validation.Success)
        {
            return Task.FromResult(validation);
        }

        return RunInteractionAsync(
            "ism_pointer_click",
            eventLog,
            uiQueue,
            token => input.PointerClickAsync(x, y, token),
            cancellationToken);
    }

    [McpServerTool(Name = "ism_key_press")]
    public static Task<ToolResult> KeyPress(
        IPointerInputSimulator input,
        IUiOperationQueue uiQueue,
        McpAuthorization authorization,
        InteractionInputValidator validator,
        IEventLogProvider eventLog,
        string key,
        string[]? modifiers = null,
        string? authorizationToken = null,
        CancellationToken cancellationToken = default)
    {
        var authorized = authorization.AuthorizeTool("ism_key_press", McpTransportKind.Stdio, authorizationToken);
        if (!authorized.Success)
        {
            return Task.FromResult(authorized);
        }

        var effectiveModifiers = modifiers ?? [];
        var validation = validator.ValidateKey(key, effectiveModifiers);
        if (!validation.Success)
        {
            return Task.FromResult(validation);
        }

        return RunInteractionAsync(
            "ism_key_press",
            eventLog,
            uiQueue,
            token => input.KeyPressAsync(key, effectiveModifiers, token),
            cancellationToken);
    }

    [McpServerTool(Name = "ism_type_text")]
    public static Task<ToolResult> TypeText(
        IPointerInputSimulator input,
        IUiOperationQueue uiQueue,
        McpAuthorization authorization,
        InteractionInputValidator validator,
        IEventLogProvider eventLog,
        string text,
        string? authorizationToken = null,
        CancellationToken cancellationToken = default)
    {
        var authorized = authorization.AuthorizeTool("ism_type_text", McpTransportKind.Stdio, authorizationToken);
        if (!authorized.Success)
        {
            return Task.FromResult(authorized);
        }

        var validation = validator.ValidateText(text);
        if (!validation.Success)
        {
            return Task.FromResult(validation);
        }

        return RunInteractionAsync(
            "ism_type_text",
            eventLog,
            uiQueue,
            token => input.TypeTextAsync(text, token),
            cancellationToken);
    }

    [McpServerTool(Name = "ism_element_peer_default_action")]
    public static Task<ToolResult> ElementPeerDefaultAction(
        IAutomationPeerInvoker invoker,
        IUiOperationQueue uiQueue,
        McpAuthorization authorization,
        IEventLogProvider eventLog,
        string elementIdentifier,
        string? authorizationToken = null,
        CancellationToken cancellationToken = default)
    {
        var authorized = authorization.AuthorizeTool("ism_element_peer_default_action", McpTransportKind.Stdio, authorizationToken);
        if (!authorized.Success)
        {
            return Task.FromResult(authorized);
        }

        return RunInteractionAsync(
            "ism_element_peer_default_action",
            eventLog,
            uiQueue,
            token => invoker.InvokeDefaultActionAsync(elementIdentifier, token),
            cancellationToken);
    }

    [McpServerTool(Name = "ism_close")]
    public static Task<ToolResult> Close(
        IAppProvider appProvider,
        McpAuthorization authorization,
        IEventLogProvider eventLog,
        string? authorizationToken = null,
        CancellationToken cancellationToken = default)
    {
        var authorized = authorization.AuthorizeTool("ism_close", McpTransportKind.Stdio, authorizationToken);
        if (!authorized.Success)
        {
            return Task.FromResult(authorized);
        }

        return RunInteractionAsync(
            "ism_close",
            eventLog,
            new PassthroughUiOperationQueue(),
            appProvider.CloseAsync,
            cancellationToken);
    }

    private static async Task<ToolResult> RunInteractionAsync(
        string toolName,
        IEventLogProvider eventLog,
        IUiOperationQueue uiQueue,
        Func<CancellationToken, Task<ToolResult>> operation,
        CancellationToken cancellationToken)
    {
        var result = await uiQueue.RunAsync(toolName, operation, new ToolLimits(), cancellationToken).ConfigureAwait(false);
        eventLog.Add(new EventLogEntry(
            DateTimeOffset.UtcNow,
            "interaction",
            toolName,
            new Dictionary<string, string>
            {
                ["success"] = result.Success.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["errorCode"] = result.ErrorCode ?? string.Empty,
            }));
        return result;
    }

    private sealed class PassthroughUiOperationQueue : IUiOperationQueue
    {
        public Task<ToolResult> RunAsync(
            string operationName,
            Func<CancellationToken, Task<ToolResult>> operation,
            ToolLimits limits,
            CancellationToken cancellationToken)
        {
            _ = operationName;
            _ = limits;
            return operation(cancellationToken);
        }
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
