using InSharpMcp.Registry;
using InSharpMcp.Tools;

namespace InSharpMcp.Tests;

public sealed class InSharpMcpToolsTests
{
    [Fact]
    public void ListInstances_ReturnsRegisteredInstances()
    {
        var registry = new AppInstanceRegistry();
        registry.Register(CreateDescriptor("instance-1"));

        var result = InSharpMcpTools.ListInstances(registry);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public void GetRuntimeInfo_ReturnsRuntimeDataForSelectedInstance()
    {
        var registry = new AppInstanceRegistry();
        registry.Register(CreateDescriptor("instance-1"));
        var selector = new AppInstanceSelector(registry);

        var result = InSharpMcpTools.GetRuntimeInfo(new AppTargetSelector(InstanceId: "instance-1"), selector);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task GetRuntimeInfo_AllowsConcurrentCallsWithIsolatedSelectors()
    {
        var registry = new AppInstanceRegistry();
        registry.Register(CreateDescriptor("instance-1"));
        var selector = new AppInstanceSelector(registry);
        var tasks = Enumerable.Range(0, 16)
            .Select(_ => Task.Run(() => InSharpMcpTools.GetRuntimeInfo(new AppTargetSelector(InstanceId: "instance-1"), selector)))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, result => Assert.True(result.Success));
    }

    private static AppInstanceDescriptor CreateDescriptor(string instanceId) =>
        new(
            instanceId,
            "sample-app",
            "Sample App",
            ProcessId: 123,
            AdapterKind: "fake",
            PlatformTarget: "test",
            OperatingSystem: "Windows",
            AppVersion: "1.0.0",
            Capabilities: new HashSet<string>(StringComparer.Ordinal) { "runtime" },
            Endpoint: "pipe://sample",
            RegisteredAt: DateTimeOffset.UtcNow,
            LastHeartbeatAt: DateTimeOffset.UtcNow);
}
