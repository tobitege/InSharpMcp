using InSharpMcp.Adapters.Avalonia;
using InSharpMcp.Contracts;

namespace InSharpMcp.Adapters.Avalonia.Tests;

public sealed class AvaloniaAdapterTests
{
    [Fact]
    public async Task PointerInputSimulator_ReturnsUnsupported()
    {
        var simulator = new AvaloniaPointerInputSimulator();

        var result = await simulator.PointerClickAsync(10, 20, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("unsupported", result.ErrorCode);
    }

    [Fact]
    public async Task AccessibilityTreeProvider_DelegatesToTreeInspector()
    {
        var expected = ToolResult.Ok(
            "Visual tree snapshot returned.",
            new UiTreeSnapshot(new UiElementNode("0", "Window"), NodeCount: 1, Truncated: false));
        var provider = new AvaloniaAccessibilityTreeProvider(new StubTreeInspector(expected));

        var result = await provider.GetAccessibilityTreeAsync(new ToolLimits(), CancellationToken.None);

        Assert.Same(expected, result);
    }

    private sealed class StubTreeInspector : IUiTreeInspector
    {
        private readonly ToolResult _snapshot;

        public StubTreeInspector(ToolResult snapshot)
        {
            _snapshot = snapshot;
        }

        public Task<ToolResult> GetVisualTreeSnapshotAsync(ToolLimits limits, CancellationToken cancellationToken)
        {
            _ = limits;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_snapshot);
        }

        public Task<ToolResult> GetElementMetadataAsync(
            string elementIdentifier,
            ToolLimits limits,
            CancellationToken cancellationToken)
        {
            _ = elementIdentifier;
            _ = limits;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(ToolResult.Fail("Not implemented by test stub.", "unsupported"));
        }

        public Task<ToolResult> GetElementDataContextAsync(
            string elementIdentifier,
            ToolLimits limits,
            CancellationToken cancellationToken)
        {
            _ = elementIdentifier;
            _ = limits;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(ToolResult.Fail("Not implemented by test stub.", "unsupported"));
        }
    }
}
