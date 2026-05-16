using InSharpMcp.Concurrency;
using InSharpMcp.Contracts;
using InSharpMcp.Limits;
using InSharpMcp.Selectors;
using InSharpMcp.Tools;

namespace InSharpMcp.Tests;

public sealed class WaitAndAccessibilityToolTests
{
    [Fact]
    public async Task WaitForElement_ReturnsMatchBeforeTimeout()
    {
        var inspector = new DelayedMatchInspector();
        var client = ToolRoutingFixture.CreateClient(treeInspector: inspector);
        var router = ToolRoutingFixture.CreateRouter(client);
        var result = await InSharpMcpTools.WaitForElement(
            router,
            new ToolLimitPolicyEvaluator(),
            new ElementSelectorMatcher(),
            new ElementSelector(Name: "Ready"),
            timeoutMs: 10_000,
            cancellationToken: CancellationToken.None);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task GetAccessibilityTree_UsesProviderAndQueue()
    {
        var provider = new RecordingAccessibilityProvider();
        var client = ToolRoutingFixture.CreateClient(accessibilityTreeProvider: provider);
        var router = ToolRoutingFixture.CreateRouter(client);

        var result = await InSharpMcpTools.GetAccessibilityTree(
            router,
            new ToolLimitPolicyEvaluator(),
            maxNodes: 4,
            cancellationToken: CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(4, provider.LastLimits?.MaxNodes);
    }

    private sealed class DelayedMatchInspector : IUiTreeInspector
    {
        private int _callCount;

        public Task<ToolResult> GetVisualTreeSnapshotAsync(ToolLimits limits, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = limits;
            _callCount++;
            var childName = _callCount < 2 ? "Waiting" : "Ready";
            return Task.FromResult(ToolResult.Ok(
                "ok",
                new UiTreeSnapshot(
                    new UiElementNode("root", "Window", Children: [new("child", "TextBlock", Name: childName)]),
                    NodeCount: 2,
                    Truncated: false)));
        }

        public Task<ToolResult> GetElementMetadataAsync(string elementIdentifier, ToolLimits limits, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ToolResult> GetElementDataContextAsync(string elementIdentifier, ToolLimits limits, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingAccessibilityProvider : IAccessibilityTreeProvider
    {
        public ToolLimits? LastLimits { get; private set; }

        public Task<ToolResult> GetAccessibilityTreeAsync(ToolLimits limits, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastLimits = limits;
            return Task.FromResult(ToolResult.Ok(
                "ok",
                new UiTreeSnapshot(new UiElementNode("root", "Window", Role: "window"), NodeCount: 1, Truncated: false)));
        }
    }
}
