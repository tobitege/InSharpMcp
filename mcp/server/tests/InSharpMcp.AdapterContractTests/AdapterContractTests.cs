using InSharpMcp.Concurrency;
using InSharpMcp.Contracts;

namespace InSharpMcp.AdapterContractTests;

public sealed class AdapterContractTests
{
    private readonly InMemoryAdapterFixture _fixture = new();

    [Fact]
    public async Task Dispatcher_RunsSynchronousUiWork()
    {
        var result = await _fixture.Dispatcher.RunAsync(_ => 42, TestContext.Current.CancellationToken);

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
        var result = await _fixture.TreeInspector.GetVisualTreeSnapshotAsync(new ToolLimits(), TestContext.Current.CancellationToken);

        var snapshot = Assert.IsType<UiTreeSnapshot>(result.Data);
        Assert.True(result.Success);
        Assert.Equal("Window", snapshot.Root.Type);
        Assert.Contains(snapshot.Root.Children ?? [], node => node.Role == "button");
    }

    [Fact]
    public async Task VisualTreeSnapshot_RespectsNodeLimit()
    {
        var limits = new ToolLimits { MaxNodes = 1 };

        var result = await _fixture.TreeInspector.GetVisualTreeSnapshotAsync(limits, TestContext.Current.CancellationToken);

        var snapshot = Assert.IsType<UiTreeSnapshot>(result.Data);
        Assert.True(snapshot.Truncated);
        Assert.Equal(1, snapshot.NodeCount);
    }

    [Fact]
    public async Task Metadata_ReturnsBoundedFrameworkNeutralShape()
    {
        var limits = new ToolLimits { MaxTextCharacters = 4 };

        var result = await _fixture.TreeInspector.GetElementMetadataAsync("notes-input", limits, TestContext.Current.CancellationToken);

        var metadata = Assert.IsType<ElementMetadata>(result.Data);
        Assert.True(result.Success);
        Assert.Equal("TextBox", metadata.Type);
        Assert.Equal("Init", metadata.Text);
        Assert.Equal(new UiElementBounds(10, 50, 160, 30), metadata.Bounds);
    }

    [Fact]
    public async Task VisualTreeSnapshot_ReturnsRootRelativeBounds()
    {
        var result = await _fixture.TreeInspector.GetVisualTreeSnapshotAsync(new ToolLimits(), TestContext.Current.CancellationToken);

        var snapshot = Assert.IsType<UiTreeSnapshot>(result.Data);
        var button = Assert.Single(snapshot.Root.Children ?? [], node => node.ElementIdentifier == "save-button");
        Assert.Equal(new UiElementBounds(10, 10, 80, 30), button.Bounds);
    }

    [Fact]
    public async Task UnsupportedDataContext_ReturnsStructuredUnsupportedResult()
    {
        var result = await _fixture.TreeInspector.GetElementDataContextAsync(
            "notes-input",
            new ToolLimits(),
            TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("unsupported", result.ErrorCode);
    }

    [Fact]
    public async Task Screenshot_ReturnsPngBytes()
    {
        var result = await _fixture.ScreenshotProvider.CaptureScreenshotAsync(TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.NotNull(result.PngBytes);
        Assert.Equal([0x89, 0x50, 0x4E, 0x47], result.PngBytes);
    }

    [Fact]
    public async Task PointerInput_RejectsNegativeCoordinates()
    {
        var result = await _fixture.PointerInput.PointerClickAsync(-1, 0, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("invalid_coordinates", result.ErrorCode);
    }

    [Fact]
    public async Task PointerInput_ClicksElementByIdentifier()
    {
        var result = await _fixture.ElementClick.ElementClickAsync("save-button", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
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
            TestContext.Current.CancellationToken);
        var second = queue.RunAsync(
            "second",
            _ =>
            {
                order.Add(2);
                return Task.FromResult(ToolResult.Ok("second"));
            },
            new ToolLimits(),
            TestContext.Current.CancellationToken);

        await Task.WhenAll(first, second);

        Assert.Equal([1, 2], order);
    }
}
