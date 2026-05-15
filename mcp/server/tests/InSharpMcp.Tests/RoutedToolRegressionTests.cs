using InSharpMcp.Concurrency;
using InSharpMcp.Contracts;
using InSharpMcp.Events;
using InSharpMcp.Interaction;
using InSharpMcp.Limits;
using InSharpMcp.Registry;
using InSharpMcp.Routing;
using InSharpMcp.Security;
using InSharpMcp.Selectors;
using InSharpMcp.Tools;
using InSharpMcp.Tracing;
using Microsoft.AspNetCore.Http;

namespace InSharpMcp.Tests;

public sealed class RoutedToolRegressionTests
{
    [Fact]
    public async Task VisualTreeSnapshot_RejectsAmbiguousTargetBeforeDispatch()
    {
        var router = ToolRoutingFixture.CreateRouter(
            ToolRoutingFixture.CreateClient(),
            ToolRoutingFixture.CreateDescriptor("instance-1"),
            ToolRoutingFixture.CreateDescriptor("instance-2"));

        var result = await InSharpMcpTools.VisualTreeSnapshot(
            router,
            new ToolLimitPolicyEvaluator(),
            cancellationToken: CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("ambiguous_target", result.ErrorCode);
    }

    [Fact]
    public async Task VisualTreeSnapshot_RejectsSelectedInstanceWithoutConnection()
    {
        var descriptor = ToolRoutingFixture.CreateDescriptor("instance-1");
        var router = ToolRoutingFixture.CreateRouterWithoutConnection(descriptor);

        var result = await InSharpMcpTools.VisualTreeSnapshot(
            router,
            new ToolLimitPolicyEvaluator(),
            new AppTargetSelector(InstanceId: "instance-1"),
            cancellationToken: CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("stale_instance", result.ErrorCode);
    }

    [Fact]
    public async Task GetElementMetadata_RoutesToSelectedConnectedInstance()
    {
        var firstInspector = new RecordingTreeInspector();
        var secondInspector = new RecordingTreeInspector();
        var firstDescriptor = ToolRoutingFixture.CreateDescriptor("instance-1");
        var secondDescriptor = ToolRoutingFixture.CreateDescriptor("instance-2");
        var registry = new AppInstanceRegistry();
        var connections = new AppInstanceConnectionRegistry();
        registry.Register(firstDescriptor);
        registry.Register(secondDescriptor);
        connections.Register(firstDescriptor.InstanceId, ToolRoutingFixture.CreateClient(treeInspector: firstInspector));
        connections.Register(secondDescriptor.InstanceId, ToolRoutingFixture.CreateClient(treeInspector: secondInspector));
        var router = new AppInstanceRouter(new AppInstanceSelector(registry), connections);

        var result = await InSharpMcpTools.GetElementMetadata(
            router,
            new ToolLimitPolicyEvaluator(),
            "root",
            new AppTargetSelector(InstanceId: "instance-2"),
            cancellationToken: CancellationToken.None);

        Assert.True(result.Success);
        Assert.Null(firstInspector.LastElementIdentifier);
        Assert.Equal("root", secondInspector.LastElementIdentifier);
    }

    [Fact]
    public async Task PointerClick_AcceptsHttpBearerTokenFromRequestContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = "Bearer secret";
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var router = ToolRoutingFixture.CreateRouter(ToolRoutingFixture.CreateClient());

        var result = await InSharpMcpTools.PointerClick(
            router,
            new McpAuthorization(new McpAccessOptions { SharedToken = "secret" }),
            new McpRequestAuthorizationResolver(accessor),
            new InteractionInputValidator(),
            x: 1,
            y: 1,
            cancellationToken: CancellationToken.None);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task Close_RunsThroughSelectedClientUiQueue()
    {
        var queue = new RecordingUiOperationQueue();
        var provider = new RecordingAppProvider();
        var client = ToolRoutingFixture.CreateClient(
            appProvider: provider,
            uiQueue: queue);
        var router = ToolRoutingFixture.CreateRouter(client);

        var result = await InSharpMcpTools.Close(
            router,
            new McpAuthorization(new McpAccessOptions { SharedToken = "secret" }),
            new McpRequestAuthorizationResolver(),
            authorizationToken: "secret",
            cancellationToken: CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(provider.Closed);
        Assert.Contains("close", queue.OperationNames);
    }

    [Fact]
    public async Task TraceCapturesActualToolExecution()
    {
        var traceStore = new BoundedTraceStore();
        var client = ToolRoutingFixture.CreateClient(
            eventLog: new BoundedEventLog(),
            traceStore: traceStore);
        var router = ToolRoutingFixture.CreateRouter(client);

        var start = InSharpMcpTools.StartTrace(router);
        var traceId = (string)start.Data!.GetType().GetProperty("TraceId")!.GetValue(start.Data)!;
        var typeResult = await InSharpMcpTools.TypeText(
            router,
            new McpAuthorization(new McpAccessOptions { SharedToken = "secret" }),
            new McpRequestAuthorizationResolver(),
            new InteractionInputValidator(),
            "hello",
            authorizationToken: "secret",
            cancellationToken: CancellationToken.None);

        var stop = InSharpMcpTools.StopTrace(router, traceId);

        Assert.True(typeResult.Success);
        var summary = Assert.IsType<TraceSummary>(stop.Data);
        Assert.Contains(summary.Events, entry => entry.Message == "ism_type_text");
    }

    private sealed class RecordingTreeInspector : IUiTreeInspector
    {
        public string? LastElementIdentifier { get; private set; }

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
            LastElementIdentifier = elementIdentifier;
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

    private sealed class RecordingUiOperationQueue : IUiOperationQueue
    {
        public List<string> OperationNames { get; } = [];

        public async Task<ToolResult> RunAsync(
            string operationName,
            Func<CancellationToken, Task<ToolResult>> operation,
            ToolLimits limits,
            CancellationToken cancellationToken)
        {
            _ = limits;
            OperationNames.Add(operationName);
            return await operation(cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class RecordingAppProvider : IAppProvider
    {
        public int ProcessId => 123;

        public string OperatingSystem => "Windows";

        public string PlatformTarget => "test";

        public string AppName => "Sample App";

        public string AppVersion => "1.0.0";

        public bool Closed { get; private set; }

        public Task<ToolResult> CloseAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Closed = true;
            return Task.FromResult(ToolResult.Ok("closed"));
        }
    }
}
