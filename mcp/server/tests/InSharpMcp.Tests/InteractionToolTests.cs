using InSharpMcp.Concurrency;
using InSharpMcp.Contracts;
using InSharpMcp.Events;
using InSharpMcp.Interaction;
using InSharpMcp.Security;
using InSharpMcp.Tools;

namespace InSharpMcp.Tests;

public sealed class InteractionToolTests
{
    [Fact]
    public async Task PointerClick_RejectsMissingTokenForProtectedTool()
    {
        var result = await InSharpMcpTools.PointerClick(
            new RecordingInputSimulator(),
            new UiOperationQueue(),
            new McpAuthorization(new McpAccessOptions { SharedToken = "secret" }),
            new InteractionInputValidator(),
            new BoundedEventLog(),
            x: 1,
            y: 1,
            authorizationToken: null,
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("unauthorized", result.ErrorCode);
    }

    [Fact]
    public async Task PointerClick_RejectsNegativeCoordinates()
    {
        var result = await InSharpMcpTools.PointerClick(
            new RecordingInputSimulator(),
            new UiOperationQueue(),
            new McpAuthorization(new McpAccessOptions { SharedToken = "secret" }),
            new InteractionInputValidator(),
            new BoundedEventLog(),
            x: -1,
            y: 1,
            authorizationToken: "secret",
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("invalid_coordinates", result.ErrorCode);
    }

    [Fact]
    public async Task TypeText_RecordsInteractionEvent()
    {
        var log = new BoundedEventLog();

        var result = await InSharpMcpTools.TypeText(
            new RecordingInputSimulator(),
            new UiOperationQueue(),
            new McpAuthorization(new McpAccessOptions { SharedToken = "secret" }),
            new InteractionInputValidator(),
            log,
            "hello",
            authorizationToken: "secret",
            CancellationToken.None);

        Assert.True(result.Success);
        var entry = Assert.Single(log.List(new HashSet<string>(StringComparer.Ordinal) { "interaction" }, maximumCount: 10));
        Assert.Equal("ism_type_text", entry.Message);
    }

    [Fact]
    public async Task KeyPress_RejectsUnsupportedModifier()
    {
        var result = await InSharpMcpTools.KeyPress(
            new RecordingInputSimulator(),
            new UiOperationQueue(),
            new McpAuthorization(new McpAccessOptions { SharedToken = "secret" }),
            new InteractionInputValidator(),
            new BoundedEventLog(),
            "A",
            ["unsupported"],
            authorizationToken: "secret",
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("invalid_modifier", result.ErrorCode);
    }

    [Fact]
    public async Task ElementPeerDefaultAction_PropagatesUnsupportedResult()
    {
        var result = await InSharpMcpTools.ElementPeerDefaultAction(
            new UnsupportedAutomationInvoker(),
            new UiOperationQueue(),
            new McpAuthorization(new McpAccessOptions { SharedToken = "secret" }),
            new BoundedEventLog(),
            "missing",
            authorizationToken: "secret",
            CancellationToken.None);

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
