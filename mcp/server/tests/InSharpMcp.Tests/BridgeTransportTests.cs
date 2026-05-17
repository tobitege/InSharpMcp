using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using InSharpMcp.Bridge;
using InSharpMcp.Contracts;
using InSharpMcp.Contracts.LocalTransport;
using InSharpMcp.Limits;
using InSharpMcp.Registry;
using InSharpMcp.Routing;
using InSharpMcp.Tools;
using InSharpMcp.Transports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InSharpMcp.Tests;

public sealed class BridgeTransportTests
{
    [Fact]
    public async Task Bridge_RegistersAppAndRoutesInspectionThroughBroker()
    {
        var pipeName = $"InSharpMcp.Tests.{Guid.NewGuid():N}";
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddInSharpMcpCore(new BrokerMcpHostOptions
        {
            LocalAppTransport = new LocalAppTransportOptions
            {
                BrokerPipeName = pipeName,
                RequestTimeout = TimeSpan.FromSeconds(5),
            },
        });
        using var host = builder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken);
        var propertyEditor = new TestElementPropertyEditor();

        await using var bridge = new InSharpMcpBridge(
            new TestTreeInspector(),
            new TestScreenshotProvider(),
            new TestAccessibilityTreeProvider(),
            new TestInputSimulator(),
            new TestAutomationPeerInvoker(),
            new TestAppProvider(),
            new LocalBridgeOptions
            {
                BrokerPipeName = pipeName,
                RequestTimeout = TimeSpan.FromSeconds(5),
            },
            propertyEditor,
            new TestElementClickSimulator());

        await bridge.StartAsync(new AppBridgeRegistration(
            "insharpmcp.test",
            "InSharpMcp Test App",
            "test",
            "Test",
            "1.0.0",
            AppBridgeCapabilities.Standard,
            InstanceId: "bridge-test"), TestContext.Current.CancellationToken);

        var registry = host.Services.GetRequiredService<AppInstanceRegistry>();
        var instances = registry.List();
        Assert.Contains(instances, instance => instance.InstanceId == "bridge-test");

        var result = await InSharpMcpTools.VisualTreeSnapshot(
            host.Services.GetRequiredService<AppInstanceRouter>(),
            host.Services.GetRequiredService<ToolLimitPolicyEvaluator>(),
            new AppTargetSelector(InstanceId: "bridge-test"),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        var snapshot = Assert.IsType<UiTreeSnapshot>(result.Data);
        Assert.Equal("BridgeRoot", snapshot.Root.Name);
        Assert.Equal("BridgeButton", snapshot.Root.Children?.Single().Name);

        var metadataResult = await InSharpMcpTools.GetElementMetadata(
            host.Services.GetRequiredService<AppInstanceRouter>(),
            host.Services.GetRequiredService<ToolLimitPolicyEvaluator>(),
            "0/0",
            new AppTargetSelector(InstanceId: "bridge-test"),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(metadataResult.Success);
        var metadata = Assert.IsType<ElementMetadata>(metadataResult.Data);
        Assert.Equal("0/0", metadata.ElementIdentifier);

        using var propertyValue = JsonDocument.Parse("\"Changed\"");
        var propertyResult = await InSharpMcpTools.SetElementProperty(
            host.Services.GetRequiredService<AppInstanceRouter>(),
            "0/0",
            "Text",
            propertyValue.RootElement,
            target: new AppTargetSelector(InstanceId: "bridge-test"),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(propertyResult.Success);
        Assert.Equal("0/0", propertyEditor.ElementIdentifier);
        Assert.Equal("Text", propertyEditor.PropertyName);
        Assert.Equal(JsonValueKind.String, propertyEditor.ValueKind);
        Assert.IsType<ElementPropertySetResult>(propertyResult.Data);

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Bridge_SendsHeartbeatAndUnregistersOnDispose()
    {
        var pipeName = $"InSharpMcp.Tests.{Guid.NewGuid():N}";
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddInSharpMcpCore(new BrokerMcpHostOptions
        {
            LocalAppTransport = new LocalAppTransportOptions
            {
                BrokerPipeName = pipeName,
                HeartbeatInterval = TimeSpan.FromMilliseconds(50),
                RequestTimeout = TimeSpan.FromSeconds(5),
                StaleInstanceAge = TimeSpan.FromSeconds(10),
            },
        });
        using var host = builder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken);

        var bridge = new InSharpMcpBridge(
            new TestTreeInspector(),
            new TestScreenshotProvider(),
            new TestAccessibilityTreeProvider(),
            new TestInputSimulator(),
            new TestAutomationPeerInvoker(),
            new TestAppProvider(),
            new LocalBridgeOptions
            {
                BrokerPipeName = pipeName,
                HeartbeatInterval = TimeSpan.FromMilliseconds(50),
                RequestTimeout = TimeSpan.FromSeconds(5),
            });

        await bridge.StartAsync(new AppBridgeRegistration(
            "insharpmcp.test",
            "InSharpMcp Test App",
            "test",
            "Test",
            "1.0.0",
            AppBridgeCapabilities.Standard,
            InstanceId: "bridge-heartbeat-test"), TestContext.Current.CancellationToken);

        var registry = host.Services.GetRequiredService<AppInstanceRegistry>();
        var firstHeartbeat = registry.List().Single(instance => instance.InstanceId == "bridge-heartbeat-test").LastHeartbeatAt;

        await Task.Delay(200, TestContext.Current.CancellationToken);

        var secondHeartbeat = registry.List().Single(instance => instance.InstanceId == "bridge-heartbeat-test").LastHeartbeatAt;
        Assert.True(secondHeartbeat > firstHeartbeat);

        await bridge.DisposeAsync();

        Assert.DoesNotContain(registry.List(), instance => instance.InstanceId == "bridge-heartbeat-test");
        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task BrokerPipe_ReturnsStructuredErrorForInvalidJson()
    {
        var pipeName = $"InSharpMcp.Tests.{Guid.NewGuid():N}";
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddInSharpMcpCore(new BrokerMcpHostOptions
        {
            LocalAppTransport = new LocalAppTransportOptions
            {
                BrokerPipeName = pipeName,
                RequestTimeout = TimeSpan.FromSeconds(5),
            },
        });
        using var host = builder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken);

        await using var pipe = new NamedPipeClientStream(
            ".",
            pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await pipe.ConnectAsync(timeout.Token);
        var invalidBytes = Encoding.UTF8.GetBytes("{not-json}\n");
        await pipe.WriteAsync(invalidBytes, timeout.Token);
        await pipe.FlushAsync(timeout.Token);

        using var reader = new StreamReader(pipe, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var line = await reader.ReadLineAsync(timeout.Token);
        var response = JsonSerializer.Deserialize<LocalBrokerResponse>(line!, LocalTransportJson.Options);

        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("Invalid local broker request.", response.Error);
        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    private sealed class TestTreeInspector : IUiTreeInspector
    {
        public Task<ToolResult> GetVisualTreeSnapshotAsync(ToolLimits limits, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = limits;
            var root = new UiElementNode(
                "0",
                "Window",
                Name: "BridgeRoot",
                Children: [new UiElementNode("0/0", "Button", Name: "BridgeButton")]);
            return Task.FromResult(ToolResult.Ok("Visual tree snapshot returned.", new UiTreeSnapshot(root, 2, Truncated: false)));
        }

        public Task<ToolResult> GetElementMetadataAsync(
            string elementIdentifier,
            ToolLimits limits,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = limits;
            return Task.FromResult(ToolResult.Ok("Element metadata returned.", new ElementMetadata(elementIdentifier, "Button")));
        }

        public Task<ToolResult> GetElementDataContextAsync(
            string elementIdentifier,
            ToolLimits limits,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = elementIdentifier;
            _ = limits;
            return Task.FromResult(ToolResult.Ok(
                "Element has no DataContext.",
                new DataContextMetadata("<null>", new Dictionary<string, object?>(), Truncated: false)));
        }
    }

    private sealed class TestScreenshotProvider : IScreenshotProvider
    {
        public Task<ScreenshotResult> CaptureScreenshotAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new ScreenshotResult(false, null, "Screenshot unsupported.", "unsupported"));
        }
    }

    private sealed class TestAccessibilityTreeProvider : IAccessibilityTreeProvider
    {
        public Task<ToolResult> GetAccessibilityTreeAsync(ToolLimits limits, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = limits;
            return Task.FromResult(ToolResult.Fail("Accessibility unsupported.", "unsupported"));
        }
    }

    private sealed class TestInputSimulator : IPointerInputSimulator
    {
        public Task<ToolResult> PointerClickAsync(double x, double y, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = x;
            _ = y;
            return Task.FromResult(ToolResult.Ok("Clicked."));
        }

        public Task<ToolResult> KeyPressAsync(
            string key,
            IReadOnlyList<string> modifiers,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = key;
            _ = modifiers;
            return Task.FromResult(ToolResult.Ok("Pressed."));
        }

        public Task<ToolResult> TypeTextAsync(string text, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = text;
            return Task.FromResult(ToolResult.Ok("Typed."));
        }
    }

    private sealed class TestElementClickSimulator : IElementClickSimulator
    {
        public Task<ToolResult> ElementClickAsync(string elementIdentifier, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = elementIdentifier;
            return Task.FromResult(ToolResult.Ok("Element clicked."));
        }
    }

    private sealed class TestAutomationPeerInvoker : IAutomationPeerInvoker
    {
        public Task<ToolResult> InvokeDefaultActionAsync(string elementIdentifier, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = elementIdentifier;
            return Task.FromResult(ToolResult.Ok("Invoked."));
        }
    }

    private sealed class TestElementPropertyEditor : IElementPropertyEditor
    {
        public string? ElementIdentifier { get; private set; }

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
            PropertyName = propertyName;
            ValueKind = value.ValueKind;
            return Task.FromResult(ToolResult.Ok(
                "Element property set.",
                new ElementPropertySetResult(
                    elementIdentifier,
                    targetObject,
                    propertyName,
                    "TestTarget",
                    "System.String",
                    "Before",
                    value.GetString())));
        }
    }

    private sealed class TestAppProvider : IAppProvider
    {
        public int ProcessId => Environment.ProcessId;

        public string OperatingSystem => "Test OS";

        public string PlatformTarget => "Test";

        public string AppName => "InSharpMcp Test App";

        public string AppVersion => "1.0.0";

        public Task<ToolResult> CloseAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(ToolResult.Ok("Closed."));
        }
    }
}
