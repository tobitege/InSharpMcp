using InSharpMcp.Concurrency;
using InSharpMcp.Contracts;
using InSharpMcp.Events;
using InSharpMcp.Interaction;
using InSharpMcp.Tools;
using System.Text.Json;

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

    [Fact]
    public async Task SetElementProperty_RoutesToPropertyEditorAndRecordsEvent()
    {
        var log = new BoundedEventLog();
        var editor = new RecordingElementPropertyEditor();
        var client = ToolRoutingFixture.CreateClient(
            propertyEditor: editor,
            eventLog: log);
        var router = ToolRoutingFixture.CreateRouter(client);
        using var document = JsonDocument.Parse("\"Updated\"");

        var result = await InSharpMcpTools.SetElementProperty(
            router,
            "0/1",
            "Text",
            document.RootElement,
            cancellationToken: CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("0/1", editor.ElementIdentifier);
        Assert.Equal(ElementPropertyTarget.Element, editor.TargetObject);
        Assert.Equal("Text", editor.PropertyName);
        Assert.Equal(JsonValueKind.String, editor.ValueKind);
        var entry = Assert.Single(log.List(new HashSet<string>(StringComparer.Ordinal) { "interaction" }, maximumCount: 10));
        Assert.Equal("ism_set_element_property", entry.Message);
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

    private sealed class RecordingElementPropertyEditor : IElementPropertyEditor
    {
        public string? ElementIdentifier { get; private set; }

        public string? TargetObject { get; private set; }

        public string? PropertyName { get; private set; }

        public JsonValueKind ValueKind { get; private set; }

        public Task<ToolResult> SetElementPropertyAsync(
            string elementIdentifier,
            string targetObject,
            string propertyName,
            JsonElement value,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ElementIdentifier = elementIdentifier;
            TargetObject = targetObject;
            PropertyName = propertyName;
            ValueKind = value.ValueKind;
            return Task.FromResult(ToolResult.Ok("Element property set."));
        }
    }
}
