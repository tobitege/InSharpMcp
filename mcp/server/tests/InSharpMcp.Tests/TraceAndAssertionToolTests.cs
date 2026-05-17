using InSharpMcp.Concurrency;
using InSharpMcp.Contracts;
using InSharpMcp.Limits;
using InSharpMcp.Selectors;
using InSharpMcp.Tools;
using InSharpMcp.Tracing;

namespace InSharpMcp.Tests;

public sealed class TraceAndAssertionToolTests
{
    [Fact]
    public void StartAndStopTrace_ReturnsSummary()
    {
        var store = new BoundedTraceStore();
        var client = ToolRoutingFixture.CreateClient(traceStore: store);
        var router = ToolRoutingFixture.CreateRouter(client);

        var start = InSharpMcpTools.StartTrace(router);
        var traceId = (string)start.Data!.GetType().GetProperty("TraceId")!.GetValue(start.Data)!;

        var stop = InSharpMcpTools.StopTrace(router, traceId);

        var summary = Assert.IsType<TraceSummary>(stop.Data);
        Assert.Equal(traceId, summary.TraceId);
        Assert.Equal(2, summary.Events.Count);
    }

    [Fact]
    public async Task AssertElementExists_ReturnsPassingAssertion()
    {
        var client = ToolRoutingFixture.CreateClient(treeInspector: new StaticTreeInspector());
        var router = ToolRoutingFixture.CreateRouter(client);

        var result = await InSharpMcpTools.AssertElementExists(
            router,
            new ToolLimitPolicyEvaluator(),
            new ElementSelectorMatcher(),
            new ElementSelector(Name: "Save"),
            cancellationToken: TestContext.Current.CancellationToken);

        var assertion = Assert.IsType<AssertionResult>(result.Data);
        Assert.True(assertion.Passed);
    }

    [Fact]
    public async Task AssertElementText_ReturnsFailingAssertionWithoutThrowing()
    {
        var client = ToolRoutingFixture.CreateClient(treeInspector: new StaticTreeInspector());
        var router = ToolRoutingFixture.CreateRouter(client);

        var result = await InSharpMcpTools.AssertElementText(
            router,
            new ToolLimitPolicyEvaluator(),
            new ElementSelectorMatcher(),
            new ElementSelector(Name: "Save"),
            "Cancel",
            cancellationToken: TestContext.Current.CancellationToken);

        var assertion = Assert.IsType<AssertionResult>(result.Data);
        Assert.False(assertion.Passed);
        Assert.Equal("Save", assertion.Actual);
    }

    private sealed class StaticTreeInspector : IUiTreeInspector
    {
        public Task<ToolResult> GetVisualTreeSnapshotAsync(ToolLimits limits, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = limits;
            return Task.FromResult(ToolResult.Ok(
                "ok",
                new UiTreeSnapshot(
                    new UiElementNode(
                        "root",
                        "Window",
                        Children:
                        [
                            new("save", "Button", Name: "Save", Text: "Save", IsEnabled: true),
                        ]),
                    NodeCount: 2,
                    Truncated: false)));
        }

        public Task<ToolResult> GetElementMetadataAsync(string elementIdentifier, ToolLimits limits, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ToolResult> GetElementDataContextAsync(string elementIdentifier, ToolLimits limits, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
