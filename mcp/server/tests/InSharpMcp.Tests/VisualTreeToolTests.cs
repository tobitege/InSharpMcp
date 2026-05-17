using InSharpMcp.Concurrency;
using InSharpMcp.Contracts;
using InSharpMcp.Limits;
using InSharpMcp.Tools;

namespace InSharpMcp.Tests;

public sealed class VisualTreeToolTests
{
    [Fact]
    public async Task VisualTreeSnapshot_UsesClampedLimitsAndUiQueue()
    {
        var inspector = new RecordingTreeInspector();
        var client = ToolRoutingFixture.CreateClient(treeInspector: inspector);
        var router = ToolRoutingFixture.CreateRouter(client);
        var policy = new ToolLimitPolicyEvaluator();

        var result = await InSharpMcpTools.VisualTreeSnapshot(
            router,
            policy,
            maxDepth: 999,
            maxNodes: 999999,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(50, inspector.LastLimits?.MaxDepth);
        Assert.Equal(2_000, inspector.LastLimits?.MaxNodes);
    }

    [Fact]
    public async Task GetElementMetadata_UsesTextLimit()
    {
        var inspector = new RecordingTreeInspector();
        var client = ToolRoutingFixture.CreateClient(treeInspector: inspector);
        var router = ToolRoutingFixture.CreateRouter(client);
        var policy = new ToolLimitPolicyEvaluator();

        var result = await InSharpMcpTools.GetElementMetadata(
            router,
            policy,
            "root",
            maxTextCharacters: 10,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(1_024, inspector.LastLimits?.MaxTextCharacters);
        Assert.Equal("root", inspector.LastElementIdentifier);
    }

    [Fact]
    public async Task GetElementDataContext_UsesNodeAndTextLimits()
    {
        var inspector = new RecordingTreeInspector();
        var client = ToolRoutingFixture.CreateClient(treeInspector: inspector);
        var router = ToolRoutingFixture.CreateRouter(client);
        var policy = new ToolLimitPolicyEvaluator();

        var result = await InSharpMcpTools.GetElementDataContext(
            router,
            policy,
            "root",
            maxNodes: 2,
            maxTextCharacters: 2048,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(2, inspector.LastLimits?.MaxNodes);
        Assert.Equal(2048, inspector.LastLimits?.MaxTextCharacters);
        Assert.Equal("root", inspector.LastElementIdentifier);
    }

    [Fact]
    public async Task GetScreenshot_ReturnsProviderResult()
    {
        var provider = new RecordingScreenshotProvider();
        var client = ToolRoutingFixture.CreateClient(screenshotProvider: provider);
        var router = ToolRoutingFixture.CreateRouter(client);

        var result = await InSharpMcpTools.GetScreenshot(
            router,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal([0x89, 0x50, 0x4E, 0x47], result.PngBytes);
    }

    private sealed class RecordingTreeInspector : IUiTreeInspector
    {
        public ToolLimits? LastLimits { get; private set; }

        public string? LastElementIdentifier { get; private set; }

        public Task<ToolResult> GetVisualTreeSnapshotAsync(ToolLimits limits, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastLimits = limits;
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
            LastElementIdentifier = elementIdentifier;
            LastLimits = limits;
            return Task.FromResult(ToolResult.Ok("ok", new ElementMetadata(elementIdentifier, "Button")));
        }

        public Task<ToolResult> GetElementDataContextAsync(
            string elementIdentifier,
            ToolLimits limits,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastElementIdentifier = elementIdentifier;
            LastLimits = limits;
            return Task.FromResult(ToolResult.Ok(
                "ok",
                new DataContextMetadata("SampleViewModel", new Dictionary<string, object?>(), Truncated: false)));
        }
    }

    private sealed class RecordingScreenshotProvider : IScreenshotProvider
    {
        public Task<ScreenshotResult> CaptureScreenshotAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new ScreenshotResult(true, [0x89, 0x50, 0x4E, 0x47], "ok"));
        }
    }
}
