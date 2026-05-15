using InSharpMcp.Registry;

namespace InSharpMcp.Tests;

public sealed class AppInstanceRegistryTests
{
    [Fact]
    public void Register_AllowsMultipleInstancesForSameApp()
    {
        var registry = new AppInstanceRegistry();
        var first = CreateDescriptor("instance-1", "sample-app");
        var second = CreateDescriptor("instance-2", "sample-app");

        registry.Register(first);
        registry.Register(second);

        var instances = registry.List();
        Assert.Equal(2, instances.Count);
        Assert.Contains(instances, instance => instance.InstanceId == "instance-1");
        Assert.Contains(instances, instance => instance.InstanceId == "instance-2");
    }

    [Fact]
    public void Selector_ReturnsAmbiguousTarget_WhenMoreThanOneInstanceMatches()
    {
        var registry = new AppInstanceRegistry();
        registry.Register(CreateDescriptor("instance-1", "sample-app"));
        registry.Register(CreateDescriptor("instance-2", "sample-app"));
        var selector = new AppInstanceSelector(registry);

        var result = selector.Select(new AppTargetSelector(AppId: "sample-app"));

        Assert.False(result.Succeeded);
        Assert.Equal("ambiguous_target", result.Error?.ErrorCode);
    }

    [Fact]
    public void Selector_UsesInstanceId_WhenProvided()
    {
        var registry = new AppInstanceRegistry();
        registry.Register(CreateDescriptor("instance-1", "sample-app"));
        registry.Register(CreateDescriptor("instance-2", "sample-app"));
        var selector = new AppInstanceSelector(registry);

        var result = selector.Select(new AppTargetSelector(InstanceId: "instance-2", AppId: "sample-app"));

        Assert.True(result.Succeeded);
        Assert.Equal("instance-2", result.Instance?.InstanceId);
    }

    [Fact]
    public void ExpireStale_RemovesInstancesOlderThanMaximumAge()
    {
        var registry = new AppInstanceRegistry();
        var now = DateTimeOffset.UtcNow;
        registry.Register(CreateDescriptor("fresh", "sample-app") with { LastHeartbeatAt = now });
        registry.Register(CreateDescriptor("stale", "sample-app") with { LastHeartbeatAt = now.AddMinutes(-10) });

        var expired = registry.ExpireStale(now, TimeSpan.FromMinutes(5));

        Assert.Single(expired);
        Assert.Equal("stale", expired.Single().InstanceId);
        Assert.DoesNotContain(registry.List(), instance => instance.InstanceId == "stale");
    }

    private static AppInstanceDescriptor CreateDescriptor(string instanceId, string appId) =>
        new(
            instanceId,
            appId,
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
