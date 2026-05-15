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
        var start = InSharpMcpTools.StartTrace(store);
        var traceId = (string)start.Data!.GetType().GetProperty("TraceId")!.GetValue(start.Data)!;
        store.Record(new EventLogEntry(DateTimeOffset.UtcNow, "tool", "called"));

        var stop = InSharpMcpTools.StopTrace(store, traceId);

        var summary = Assert.IsType<TraceSummary>(stop.Data);
        Assert.Equal(traceId, summary.TraceId);
        Assert.Single(summary.Events);
    }

    [Fact]
    public async Task AssertElementExists_ReturnsPassingAssertion()
    {
        var result = await InSharpMcpTools.AssertElementExists(
            new StaticTreeInspector(),
            new UiOperationQueue(),
            new ToolLimitPolicyEvaluator(),
            new ElementSelectorMatcher(),
            new ElementSelector(Name: "Save"),
            CancellationToken.None);

        var assertion = Assert.IsType<AssertionResult>(result.Data);
        Assert.True(assertion.Passed);
    }

    [Fact]
    public async Task AssertElementText_ReturnsFailingAssertionWithoutThrowing()
    {
        var result = await InSharpMcpTools.AssertElementText(
            new StaticTreeInspector(),
            new UiOperationQueue(),
            new ToolLimitPolicyEvaluator(),
            new ElementSelectorMatcher(),
            new ElementSelector(Name: "Save"),
            "Cancel",
            CancellationToken.None);

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
