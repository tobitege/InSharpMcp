using InSharpMcp.Concurrency;
using InSharpMcp.Contracts;
using InSharpMcp.Events;
using InSharpMcp.Registry;
using InSharpMcp.Routing;
using InSharpMcp.Tracing;

namespace InSharpMcp.Tests;

internal static class ToolRoutingFixture
{
    public static AppInstanceRouter CreateRouter(
        IAppInstanceClient client,
        AppInstanceDescriptor? descriptor = null,
        params AppInstanceDescriptor[] additionalDescriptors)
    {
        var registry = new AppInstanceRegistry();
        var connections = new AppInstanceConnectionRegistry();
        var primaryDescriptor = descriptor ?? CreateDescriptor("instance-1");
        registry.Register(primaryDescriptor);
        connections.Register(primaryDescriptor.InstanceId, client);

        foreach (var additionalDescriptor in additionalDescriptors)
        {
            registry.Register(additionalDescriptor);
        }

        return new AppInstanceRouter(new AppInstanceSelector(registry), connections);
    }

    public static AppInstanceRouter CreateRouterWithoutConnection(AppInstanceDescriptor descriptor)
    {
        var registry = new AppInstanceRegistry();
        registry.Register(descriptor);
        return new AppInstanceRouter(new AppInstanceSelector(registry), new AppInstanceConnectionRegistry());
    }

    public static InProcessAppInstanceClient CreateClient(
        IUiTreeInspector? treeInspector = null,
        IScreenshotProvider? screenshotProvider = null,
        IAccessibilityTreeProvider? accessibilityTreeProvider = null,
        IPointerInputSimulator? inputSimulator = null,
        IAutomationPeerInvoker? automationPeerInvoker = null,
        IElementPropertyEditor? propertyEditor = null,
        IAppProvider? appProvider = null,
        IUiOperationQueue? uiQueue = null,
        IEventLogProvider? eventLog = null,
        ITraceStore? traceStore = null) =>
        new(
            treeInspector ?? new EmptyTreeInspector(),
            screenshotProvider ?? new UnsupportedScreenshotProvider(),
            accessibilityTreeProvider ?? new UnsupportedAccessibilityProvider(),
            inputSimulator ?? new NoopInputSimulator(),
            automationPeerInvoker ?? new UnsupportedAutomationPeerInvoker(),
            propertyEditor ?? new UnsupportedElementPropertyEditor(),
            appProvider ?? new NoopAppProvider(),
            uiQueue ?? new UiOperationQueue(),
            eventLog ?? new BoundedEventLog(),
            traceStore ?? new BoundedTraceStore());

    public static AppInstanceDescriptor CreateDescriptor(string instanceId, string appId = "sample-app") =>
        new(
            instanceId,
            appId,
            "Sample App",
            ProcessId: 123,
            AdapterKind: "fake",
            PlatformTarget: "test",
            OperatingSystem: "Windows",
            AppVersion: "1.0.0",
            Capabilities: new HashSet<string>(StringComparer.Ordinal) { "runtime" },
            Endpoint: $"inproc://{instanceId}",
            RegisteredAt: DateTimeOffset.UtcNow,
            LastHeartbeatAt: DateTimeOffset.UtcNow);

    private sealed class EmptyTreeInspector : IUiTreeInspector
    {
        public Task<ToolResult> GetVisualTreeSnapshotAsync(ToolLimits limits, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = limits;
            return Task.FromResult(ToolResult.Ok(
                "ok",
                new UiTreeSnapshot(new UiElementNode("root", "Window"), NodeCount: 1, Truncated: false)));
        }

        public Task<ToolResult> GetElementMetadataAsync(
            string elementIdentifier,
            ToolLimits limits,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = limits;
            return Task.FromResult(ToolResult.Ok("ok", new ElementMetadata(elementIdentifier, "Window")));
        }

        public Task<ToolResult> GetElementDataContextAsync(
            string elementIdentifier,
            ToolLimits limits,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = elementIdentifier;
            _ = limits;
            return Task.FromResult(ToolResult.Ok(
                "ok",
                new DataContextMetadata("<null>", new Dictionary<string, object?>(), Truncated: false)));
        }
    }

    private sealed class UnsupportedScreenshotProvider : IScreenshotProvider
    {
        public Task<ScreenshotResult> CaptureScreenshotAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new ScreenshotResult(false, null, "unsupported", "unsupported"));
        }
    }

    private sealed class UnsupportedAccessibilityProvider : IAccessibilityTreeProvider
    {
        public Task<ToolResult> GetAccessibilityTreeAsync(ToolLimits limits, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = limits;
            return Task.FromResult(ToolResult.Fail("unsupported", "unsupported"));
        }
    }

    private sealed class NoopInputSimulator : IPointerInputSimulator
    {
        public Task<ToolResult> PointerClickAsync(double x, double y, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = x;
            _ = y;
            return Task.FromResult(ToolResult.Ok("clicked"));
        }

        public Task<ToolResult> KeyPressAsync(
            string key,
            IReadOnlyList<string> modifiers,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = key;
            _ = modifiers;
            return Task.FromResult(ToolResult.Ok("pressed"));
        }

        public Task<ToolResult> TypeTextAsync(string text, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = text;
            return Task.FromResult(ToolResult.Ok("typed"));
        }
    }

    private sealed class UnsupportedAutomationPeerInvoker : IAutomationPeerInvoker
    {
        public Task<ToolResult> InvokeDefaultActionAsync(string elementIdentifier, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = elementIdentifier;
            return Task.FromResult(ToolResult.Fail("unsupported", "unsupported"));
        }
    }

    private sealed class UnsupportedElementPropertyEditor : IElementPropertyEditor
    {
        public Task<ToolResult> SetElementPropertyAsync(
            string elementIdentifier,
            string targetObject,
            string propertyName,
            System.Text.Json.JsonElement value,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = elementIdentifier;
            _ = targetObject;
            _ = propertyName;
            _ = value;
            return Task.FromResult(ToolResult.Fail("unsupported", "unsupported"));
        }
    }

    private sealed class NoopAppProvider : IAppProvider
    {
        public int ProcessId => 123;

        public string OperatingSystem => "Windows";

        public string PlatformTarget => "test";

        public string AppName => "Sample App";

        public string AppVersion => "1.0.0";

        public Task<ToolResult> CloseAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(ToolResult.Ok("closed"));
        }
    }
}
