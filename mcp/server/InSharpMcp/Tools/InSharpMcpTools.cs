using InSharpMcp.Contracts;
using InSharpMcp.Interaction;
using InSharpMcp.Limits;
using InSharpMcp.Registry;
using InSharpMcp.Routing;
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
        AppInstanceRouter router,
        ToolLimitPolicyEvaluator limitPolicy,
        AppTargetSelector? target = null,
        int? maxDepth = null,
        int? maxNodes = null,
        CancellationToken cancellationToken = default)
    {
        var route = router.Select(target);
        if (!route.Succeeded)
        {
            return Task.FromResult(route.Error!);
        }

        var limits = CreateCallLimits(limitPolicy, maxDepth, maxNodes, maxTextCharacters: null);
        return RunRecordedToolAsync(
            route,
            "ism_visualtree_snapshot",
            "inspection",
            (client, token) => client.GetVisualTreeSnapshotAsync(limits, token),
            cancellationToken);
    }

    [McpServerTool(Name = "ism_get_element_metadata")]
    public static Task<ToolResult> GetElementMetadata(
        AppInstanceRouter router,
        ToolLimitPolicyEvaluator limitPolicy,
        string elementIdentifier,
        AppTargetSelector? target = null,
        int? maxTextCharacters = null,
        CancellationToken cancellationToken = default)
    {
        var route = router.Select(target);
        if (!route.Succeeded)
        {
            return Task.FromResult(route.Error!);
        }

        var limits = CreateCallLimits(limitPolicy, maxDepth: null, maxNodes: null, maxTextCharacters);
        return RunRecordedToolAsync(
            route,
            "ism_get_element_metadata",
            "inspection",
            (client, token) => client.GetElementMetadataAsync(elementIdentifier, limits, token),
            cancellationToken);
    }

    [McpServerTool(Name = "ism_get_element_datacontext")]
    public static Task<ToolResult> GetElementDataContext(
        AppInstanceRouter router,
        ToolLimitPolicyEvaluator limitPolicy,
        McpAuthorization authorization,
        McpRequestAuthorizationResolver authorizationResolver,
        string elementIdentifier,
        AppTargetSelector? target = null,
        int? maxNodes = null,
        int? maxTextCharacters = null,
        string? authorizationToken = null,
        CancellationToken cancellationToken = default)
    {
        var route = router.Select(target);
        if (!route.Succeeded)
        {
            return Task.FromResult(route.Error!);
        }

        var authorized = AuthorizeProtected(
            authorization,
            authorizationResolver,
            "ism_get_element_datacontext",
            authorizationToken);
        if (!authorized.Success)
        {
            return Task.FromResult(authorized);
        }

        var limits = CreateCallLimits(limitPolicy, maxDepth: null, maxNodes, maxTextCharacters);
        return RunRecordedToolAsync(
            route,
            "ism_get_element_datacontext",
            "inspection",
            (client, token) => client.GetElementDataContextAsync(elementIdentifier, limits, token),
            cancellationToken);
    }

    [McpServerTool(Name = "ism_get_screenshot")]
    public static async Task<ScreenshotResult> GetScreenshot(
        AppInstanceRouter router,
        McpAuthorization authorization,
        McpRequestAuthorizationResolver authorizationResolver,
        AppTargetSelector? target = null,
        string? authorizationToken = null,
        CancellationToken cancellationToken = default)
    {
        var route = router.Select(target);
        if (!route.Succeeded)
        {
            return ToScreenshotResult(route.Error!);
        }

        var authorized = AuthorizeProtected(
            authorization,
            authorizationResolver,
            "ism_get_screenshot",
            authorizationToken);
        if (!authorized.Success)
        {
            return ToScreenshotResult(authorized);
        }

        var started = DateTimeOffset.UtcNow;
        var timestamp = TimeProvider.System.GetTimestamp();
        var result = await route.Client!.CaptureScreenshotAsync(cancellationToken).ConfigureAwait(false);
        RecordToolEvent(
            route,
            "ism_get_screenshot",
            "inspection",
            new ToolResult(result.Success, result.Message ?? "Screenshot completed.", result, result.ErrorCode),
            started,
            TimeProvider.System.GetElapsedTime(timestamp));
        return result;
    }

    [McpServerTool(Name = "ism_query_elements")]
    public static async Task<ToolResult> QueryElements(
        AppInstanceRouter router,
        ToolLimitPolicyEvaluator limitPolicy,
        ElementSelectorMatcher matcher,
        ElementSelector selector,
        AppTargetSelector? target = null,
        int? maxDepth = null,
        int? maxNodes = null,
        CancellationToken cancellationToken = default)
    {
        var route = router.Select(target);
        if (!route.Succeeded)
        {
            return route.Error!;
        }

        var limits = CreateCallLimits(limitPolicy, maxDepth, maxNodes, maxTextCharacters: null);
        return await RunRecordedToolAsync(
            route,
            "ism_query_elements",
            "inspection",
            async (client, token) =>
            {
                var snapshotResult = await client.GetVisualTreeSnapshotAsync(limits, token).ConfigureAwait(false);
                return snapshotResult.Data is UiTreeSnapshot snapshot
                    ? matcher.Match(snapshot, selector, limits)
                    : snapshotResult;
            },
            cancellationToken).ConfigureAwait(false);
    }

    [McpServerTool(Name = "ism_wait_for_element")]
    public static async Task<ToolResult> WaitForElement(
        AppInstanceRouter router,
        ToolLimitPolicyEvaluator limitPolicy,
        ElementSelectorMatcher matcher,
        ElementSelector selector,
        AppTargetSelector? target = null,
        int? timeoutMs = null,
        CancellationToken cancellationToken = default)
    {
        var route = router.Select(target);
        if (!route.Succeeded)
        {
            return route.Error!;
        }

        var timeout = TimeSpan.FromMilliseconds(Math.Clamp(timeoutMs ?? 5_000, 100, 30_000));
        var limits = CreateCallLimits(limitPolicy, maxDepth: null, maxNodes: null, maxTextCharacters: null);
        return await RunRecordedToolAsync(
            route,
            "ism_wait_for_element",
            "inspection",
            async (client, token) =>
            {
                var started = TimeProvider.System.GetTimestamp();
                while (TimeProvider.System.GetElapsedTime(started) < timeout)
                {
                    var result = await QueryElementsForClientAsync(client, matcher, selector, limits, token)
                        .ConfigureAwait(false);
                    if (result.Data is ElementQueryResult { Matches.Count: > 0 })
                    {
                        return ToolResult.Ok("Element matched before timeout.", result.Data);
                    }

                    await Task.Delay(50, token).ConfigureAwait(false);
                }

                return ToolResult.Fail("Timed out waiting for element.", "timeout");
            },
            cancellationToken).ConfigureAwait(false);
    }

    [McpServerTool(Name = "ism_get_accessibility_tree")]
    public static Task<ToolResult> GetAccessibilityTree(
        AppInstanceRouter router,
        ToolLimitPolicyEvaluator limitPolicy,
        AppTargetSelector? target = null,
        int? maxDepth = null,
        int? maxNodes = null,
        CancellationToken cancellationToken = default)
    {
        var route = router.Select(target);
        if (!route.Succeeded)
        {
            return Task.FromResult(route.Error!);
        }

        var limits = CreateCallLimits(limitPolicy, maxDepth, maxNodes, maxTextCharacters: null);
        return RunRecordedToolAsync(
            route,
            "ism_get_accessibility_tree",
            "inspection",
            (client, token) => client.GetAccessibilityTreeAsync(limits, token),
            cancellationToken);
    }

    [McpServerTool(Name = "ism_get_event_log")]
    public static ToolResult GetEventLog(
        AppInstanceRouter router,
        AppTargetSelector? target = null,
        string[]? categories = null,
        int maximumCount = 100)
    {
        var route = router.Select(target);
        if (!route.Succeeded)
        {
            return route.Error!;
        }

        var categorySet = categories is null ? null : new HashSet<string>(categories, StringComparer.Ordinal);
        var events = route.Client!.EventLog.List(categorySet, Math.Clamp(maximumCount, 1, 1_000));
        return ToolResult.Ok("Event log returned.", events);
    }

    [McpServerTool(Name = "ism_pointer_click")]
    public static Task<ToolResult> PointerClick(
        AppInstanceRouter router,
        McpAuthorization authorization,
        McpRequestAuthorizationResolver authorizationResolver,
        InteractionInputValidator validator,
        double x,
        double y,
        AppTargetSelector? target = null,
        string? authorizationToken = null,
        CancellationToken cancellationToken = default)
    {
        var route = router.Select(target);
        if (!route.Succeeded)
        {
            return Task.FromResult(route.Error!);
        }

        var authorized = AuthorizeProtected(authorization, authorizationResolver, "ism_pointer_click", authorizationToken);
        if (!authorized.Success)
        {
            return Task.FromResult(authorized);
        }

        var validation = validator.ValidateCoordinates(x, y);
        if (!validation.Success)
        {
            return Task.FromResult(validation);
        }

        return RunRecordedToolAsync(
            route,
            "ism_pointer_click",
            "interaction",
            (client, token) => client.PointerClickAsync(x, y, token),
            cancellationToken);
    }

    [McpServerTool(Name = "ism_key_press")]
    public static Task<ToolResult> KeyPress(
        AppInstanceRouter router,
        McpAuthorization authorization,
        McpRequestAuthorizationResolver authorizationResolver,
        InteractionInputValidator validator,
        string key,
        string[]? modifiers = null,
        AppTargetSelector? target = null,
        string? authorizationToken = null,
        CancellationToken cancellationToken = default)
    {
        var route = router.Select(target);
        if (!route.Succeeded)
        {
            return Task.FromResult(route.Error!);
        }

        var authorized = AuthorizeProtected(authorization, authorizationResolver, "ism_key_press", authorizationToken);
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

        return RunRecordedToolAsync(
            route,
            "ism_key_press",
            "interaction",
            (client, token) => client.KeyPressAsync(key, effectiveModifiers, token),
            cancellationToken);
    }

    [McpServerTool(Name = "ism_type_text")]
    public static Task<ToolResult> TypeText(
        AppInstanceRouter router,
        McpAuthorization authorization,
        McpRequestAuthorizationResolver authorizationResolver,
        InteractionInputValidator validator,
        string text,
        AppTargetSelector? target = null,
        string? authorizationToken = null,
        CancellationToken cancellationToken = default)
    {
        var route = router.Select(target);
        if (!route.Succeeded)
        {
            return Task.FromResult(route.Error!);
        }

        var authorized = AuthorizeProtected(authorization, authorizationResolver, "ism_type_text", authorizationToken);
        if (!authorized.Success)
        {
            return Task.FromResult(authorized);
        }

        var validation = validator.ValidateText(text);
        if (!validation.Success)
        {
            return Task.FromResult(validation);
        }

        return RunRecordedToolAsync(
            route,
            "ism_type_text",
            "interaction",
            (client, token) => client.TypeTextAsync(text, token),
            cancellationToken);
    }

    [McpServerTool(Name = "ism_element_peer_default_action")]
    public static Task<ToolResult> ElementPeerDefaultAction(
        AppInstanceRouter router,
        McpAuthorization authorization,
        McpRequestAuthorizationResolver authorizationResolver,
        string elementIdentifier,
        AppTargetSelector? target = null,
        string? authorizationToken = null,
        CancellationToken cancellationToken = default)
    {
        var route = router.Select(target);
        if (!route.Succeeded)
        {
            return Task.FromResult(route.Error!);
        }

        var authorized = AuthorizeProtected(
            authorization,
            authorizationResolver,
            "ism_element_peer_default_action",
            authorizationToken);
        if (!authorized.Success)
        {
            return Task.FromResult(authorized);
        }

        return RunRecordedToolAsync(
            route,
            "ism_element_peer_default_action",
            "interaction",
            (client, token) => client.InvokeDefaultActionAsync(elementIdentifier, token),
            cancellationToken);
    }

    [McpServerTool(Name = "ism_close")]
    public static Task<ToolResult> Close(
        AppInstanceRouter router,
        McpAuthorization authorization,
        McpRequestAuthorizationResolver authorizationResolver,
        AppTargetSelector? target = null,
        string? authorizationToken = null,
        CancellationToken cancellationToken = default)
    {
        var route = router.Select(target);
        if (!route.Succeeded)
        {
            return Task.FromResult(route.Error!);
        }

        var authorized = AuthorizeProtected(authorization, authorizationResolver, "ism_close", authorizationToken);
        if (!authorized.Success)
        {
            return Task.FromResult(authorized);
        }

        return RunRecordedToolAsync(
            route,
            "ism_close",
            "interaction",
            (client, token) => client.CloseAsync(token),
            cancellationToken);
    }

    [McpServerTool(Name = "ism_start_trace")]
    public static ToolResult StartTrace(AppInstanceRouter router, AppTargetSelector? target = null)
    {
        var route = router.Select(target);
        if (!route.Succeeded)
        {
            return route.Error!;
        }

        var traceId = route.Client!.TraceStore.Start();
        RecordToolEvent(
            route,
            "ism_start_trace",
            "trace",
            ToolResult.Ok("Trace started.", new { TraceId = traceId }),
            DateTimeOffset.UtcNow,
            TimeSpan.Zero);
        return ToolResult.Ok("Trace started.", new { TraceId = traceId });
    }

    [McpServerTool(Name = "ism_stop_trace")]
    public static ToolResult StopTrace(AppInstanceRouter router, string traceId, AppTargetSelector? target = null)
    {
        var route = router.Select(target);
        if (!route.Succeeded)
        {
            return route.Error!;
        }

        RecordToolEvent(
            route,
            "ism_stop_trace",
            "trace",
            ToolResult.Ok("Trace stopped."),
            DateTimeOffset.UtcNow,
            TimeSpan.Zero);
        return route.Client!.TraceStore.Stop(traceId);
    }

    [McpServerTool(Name = "ism_assert_element_exists")]
    public static async Task<ToolResult> AssertElementExists(
        AppInstanceRouter router,
        ToolLimitPolicyEvaluator limitPolicy,
        ElementSelectorMatcher matcher,
        ElementSelector selector,
        AppTargetSelector? target = null,
        CancellationToken cancellationToken = default)
    {
        var route = router.Select(target);
        if (!route.Succeeded)
        {
            return route.Error!;
        }

        var limits = CreateCallLimits(limitPolicy, maxDepth: null, maxNodes: null, maxTextCharacters: null);
        return await RunRecordedToolAsync(
            route,
            "ism_assert_element_exists",
            "assertion",
            async (client, token) =>
            {
                var result = await QueryElementsForClientAsync(client, matcher, selector, limits, token)
                    .ConfigureAwait(false);
                var passed = result.Data is ElementQueryResult { Matches.Count: > 0 };
                return ToolResult.Ok(
                    passed ? "Assertion passed." : "Assertion failed.",
                    new AssertionResult(passed, passed ? "Element exists." : "Element was not found.", result.Data));
            },
            cancellationToken).ConfigureAwait(false);
    }

    [McpServerTool(Name = "ism_assert_element_text")]
    public static async Task<ToolResult> AssertElementText(
        AppInstanceRouter router,
        ToolLimitPolicyEvaluator limitPolicy,
        ElementSelectorMatcher matcher,
        ElementSelector selector,
        string expectedText,
        AppTargetSelector? target = null,
        CancellationToken cancellationToken = default)
    {
        var route = router.Select(target);
        if (!route.Succeeded)
        {
            return route.Error!;
        }

        var limits = CreateCallLimits(limitPolicy, maxDepth: null, maxNodes: null, maxTextCharacters: null);
        return await RunRecordedToolAsync(
            route,
            "ism_assert_element_text",
            "assertion",
            async (client, token) =>
            {
                var result = await QueryElementsForClientAsync(client, matcher, selector, limits, token)
                    .ConfigureAwait(false);
                var actual = (result.Data as ElementQueryResult)?.Matches.FirstOrDefault()?.Text;
                var passed = string.Equals(actual, expectedText, StringComparison.Ordinal);
                return ToolResult.Ok(
                    passed ? "Assertion passed." : "Assertion failed.",
                    new AssertionResult(passed, $"Expected text '{expectedText}'.", actual));
            },
            cancellationToken).ConfigureAwait(false);
    }

    [McpServerTool(Name = "ism_assert_element_enabled")]
    public static async Task<ToolResult> AssertElementEnabled(
        AppInstanceRouter router,
        ToolLimitPolicyEvaluator limitPolicy,
        ElementSelectorMatcher matcher,
        ElementSelector selector,
        bool expectedEnabled,
        AppTargetSelector? target = null,
        CancellationToken cancellationToken = default)
    {
        var route = router.Select(target);
        if (!route.Succeeded)
        {
            return route.Error!;
        }

        var limits = CreateCallLimits(limitPolicy, maxDepth: null, maxNodes: null, maxTextCharacters: null);
        return await RunRecordedToolAsync(
            route,
            "ism_assert_element_enabled",
            "assertion",
            async (client, token) =>
            {
                var result = await QueryElementsForClientAsync(client, matcher, selector, limits, token)
                    .ConfigureAwait(false);
                var actual = (result.Data as ElementQueryResult)?.Matches.FirstOrDefault()?.IsEnabled;
                var passed = actual == expectedEnabled;
                return ToolResult.Ok(
                    passed ? "Assertion passed." : "Assertion failed.",
                    new AssertionResult(passed, $"Expected enabled state '{expectedEnabled}'.", actual));
            },
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<ToolResult> QueryElementsForClientAsync(
        IAppInstanceClient client,
        ElementSelectorMatcher matcher,
        ElementSelector selector,
        ToolLimits limits,
        CancellationToken cancellationToken)
    {
        var snapshotResult = await client.GetVisualTreeSnapshotAsync(limits, cancellationToken).ConfigureAwait(false);
        return snapshotResult.Data is UiTreeSnapshot snapshot
            ? matcher.Match(snapshot, selector, limits)
            : snapshotResult;
    }

    private static async Task<ToolResult> RunRecordedToolAsync(
        AppInstanceRoute route,
        string toolName,
        string category,
        Func<IAppInstanceClient, CancellationToken, Task<ToolResult>> operation,
        CancellationToken cancellationToken)
    {
        var started = DateTimeOffset.UtcNow;
        var timestamp = TimeProvider.System.GetTimestamp();
        ToolResult result;
        try
        {
            result = await operation(route.Client!, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            result = ToolResult.Fail("Operation was canceled.", "canceled");
        }
        catch (Exception exception)
        {
            result = ToolResult.Fail(
                "Tool execution failed.",
                "tool_exception",
                new { ExceptionType = exception.GetType().Name });
        }

        RecordToolEvent(
            route,
            toolName,
            category,
            result,
            started,
            TimeProvider.System.GetElapsedTime(timestamp));
        return result;
    }

    private static void RecordToolEvent(
        AppInstanceRoute route,
        string toolName,
        string category,
        ToolResult result,
        DateTimeOffset started,
        TimeSpan elapsed)
    {
        route.Client!.RecordEvent(new EventLogEntry(
            DateTimeOffset.UtcNow,
            category,
            toolName,
            new Dictionary<string, string>
            {
                ["appId"] = route.Instance!.AppId,
                ["instanceId"] = route.Instance.InstanceId,
                ["startedAt"] = started.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
                ["elapsedMs"] = elapsed.TotalMilliseconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
                ["success"] = result.Success.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["errorCode"] = result.ErrorCode ?? string.Empty,
            }));
    }

    private static ToolResult AuthorizeProtected(
        McpAuthorization authorization,
        McpRequestAuthorizationResolver authorizationResolver,
        string toolName,
        string? authorizationToken)
    {
        var context = authorizationResolver.Resolve(authorizationToken);
        return authorization.AuthorizeTool(toolName, context.TransportKind, context.AuthorizationToken);
    }

    private static ScreenshotResult ToScreenshotResult(ToolResult result) =>
        new(result.Success, null, result.Message, result.ErrorCode);

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
