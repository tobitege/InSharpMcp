using InSharpMcp.Concurrency;
using InSharpMcp.Contracts;
using InSharpMcp.Events;
using InSharpMcp.Interaction;
using InSharpMcp.Tools;

namespace InSharpMcp.Tests;

public sealed class InteractionToolTests
{
    [Fact]
    public async Task PointerClick_RejectsNegativeCoordinates()
    {
        var client = ToolRoutingFixture.CreateClient(inputSimulator: new RecordingInputSimulator());
        var router = ToolRoutingFixture.CreateRouter(client);

        var result = await InSharpMcpTools.PointerClick(
            router,
            new InteractionInputValidator(),
            x: -1,
            y: 1,
            cancellationToken: CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("invalid_coordinates", result.ErrorCode);
    }

    [Fact]
    public async Task TypeText_RecordsInteractionEvent()
    {
        var log = new BoundedEventLog();
        var client = ToolRoutingFixture.CreateClient(
            inputSimulator: new RecordingInputSimulator(),
            eventLog: log);
        var router = ToolRoutingFixture.CreateRouter(client);

        var result = await InSharpMcpTools.TypeText(
            router,
            new InteractionInputValidator(),
            "hello",
            cancellationToken: CancellationToken.None);

        Assert.True(result.Success);
        var entry = Assert.Single(log.List(new HashSet<string>(StringComparer.Ordinal) { "interaction" }, maximumCount: 10));
        Assert.Equal("ism_type_text", entry.Message);
    }

    [Fact]
    public async Task KeyPress_RejectsUnsupportedModifier()
    {
        var client = ToolRoutingFixture.CreateClient(inputSimulator: new RecordingInputSimulator());
        var router = ToolRoutingFixture.CreateRouter(client);

        var result = await InSharpMcpTools.KeyPress(
            router,
            new InteractionInputValidator(),
            "A",
            ["unsupported"],
            cancellationToken: CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("invalid_modifier", result.ErrorCode);
    }

    [Fact]
    public async Task ElementPeerDefaultAction_PropagatesUnsupportedResult()
    {
        var client = ToolRoutingFixture.CreateClient(automationPeerInvoker: new UnsupportedAutomationInvoker());
        var router = ToolRoutingFixture.CreateRouter(client);

        var result = await InSharpMcpTools.ElementPeerDefaultAction(
            router,
            "missing",
            cancellationToken: CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("unsupported", result.ErrorCode);
    }

    private sealed class RecordingInputSimulator : IPointerInputSimulator
    {
        public Task<ToolResult> PointerClickAsync(double x, double y, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = x;
            _ = y;
            return Task.FromResult(ToolResult.Ok("clicked"));
        }

        public Task<ToolResult> KeyPressAsync(string key, IReadOnlyList<string> modifiers, CancellationToken cancellationToken)
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

    private sealed class UnsupportedAutomationInvoker : IAutomationPeerInvoker
    {
        public Task<ToolResult> InvokeDefaultActionAsync(string elementIdentifier, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = elementIdentifier;
            return Task.FromResult(ToolResult.Fail("unsupported", "unsupported"));
        }
    }
}
