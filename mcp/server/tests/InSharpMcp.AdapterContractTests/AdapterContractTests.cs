using InSharpMcp.Concurrency;
using InSharpMcp.Contracts;

namespace InSharpMcp.AdapterContractTests;

public sealed class AdapterContractTests
{
    private readonly InMemoryAdapterFixture _fixture = new();

    [Fact]
    public async Task Dispatcher_RunsSynchronousUiWork()
    {
        var result = await _fixture.Dispatcher.RunAsync(_ => 42, CancellationToken.None);

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task Dispatcher_HonorsCancellationBeforeDispatch()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _fixture.Dispatcher.RunAsync(_ => 42, cancellation.Token));
    }

    [Fact]
    public async Task VisualTreeSnapshot_ReturnsFrameworkNeutralTree()
    {
        var result = await _fixture.TreeInspector.GetVisualTreeSnapshotAsync(new ToolLimits(), CancellationToken.None);

        var snapshot = Assert.IsType<UiTreeSnapshot>(result.Data);
        Assert.True(result.Success);
        Assert.Equal("Window", snapshot.Root.Type);
        Assert.Contains(snapshot.Root.Children ?? [], node => node.Role == "button");
    }

    [Fact]
    public async Task VisualTreeSnapshot_RespectsNodeLimit()
    {
        var limits = new ToolLimits { MaxNodes = 1 };

        var result = await _fixture.TreeInspector.GetVisualTreeSnapshotAsync(limits, CancellationToken.None);

        var snapshot = Assert.IsType<UiTreeSnapshot>(result.Data);
        Assert.True(snapshot.Truncated);
        Assert.Equal(1, snapshot.NodeCount);
    }

    [Fact]
    public async Task Metadata_ReturnsBoundedFrameworkNeutralShape()
    {
        var limits = new ToolLimits { MaxTextCharacters = 4 };

        var result = await _fixture.TreeInspector.GetElementMetadataAsync("notes-input", limits, CancellationToken.None);

        var metadata = Assert.IsType<ElementMetadata>(result.Data);
        Assert.True(result.Success);
        Assert.Equal("TextBox", metadata.Type);
        Assert.Equal("Init", metadata.Text);
    }

    [Fact]
    public async Task UnsupportedDataContext_ReturnsStructuredUnsupportedResult()
    {
        var result = await _fixture.TreeInspector.GetElementDataContextAsync(
            "notes-input",
            new ToolLimits(),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("unsupported", result.ErrorCode);
    }

    [Fact]
    public async Task Screenshot_ReturnsPngBytes()
    {
        var result = await _fixture.ScreenshotProvider.CaptureScreenshotAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.PngBytes);
        Assert.Equal([0x89, 0x50, 0x4E, 0x47], result.PngBytes);
    }

    [Fact]
    public async Task PointerInput_RejectsNegativeCoordinates()
    {
        var result = await _fixture.PointerInput.PointerClickAsync(-1, 0, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("invalid_coordinates", result.ErrorCode);
    }

    [Fact]
    public async Task UiOperationQueue_SerializesAdapterUiWork()
    {
        using var queue = new UiOperationQueue();
        var order = new List<int>();
        var first = queue.RunAsync(
            "first",
            async token =>
            {
                await Task.Delay(50, token);
                order.Add(1);
                return ToolResult.Ok("first");
            },
            new ToolLimits(),
            CancellationToken.None);
        var second = queue.RunAsync(
            "second",
            _ =>
            {
                order.Add(2);
                return Task.FromResult(ToolResult.Ok("second"));
            },
            new ToolLimits(),
            CancellationToken.None);

        await Task.WhenAll(first, second);

        Assert.Equal([1, 2], order);
    }
}
