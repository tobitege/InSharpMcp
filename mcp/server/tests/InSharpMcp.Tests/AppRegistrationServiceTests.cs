using InSharpMcp.Registry;

namespace InSharpMcp.Tests;

public sealed class AppRegistrationServiceTests
{
    [Fact]
    public void Dispose_UnregistersInstance()
    {
        var registry = new AppInstanceRegistry();
        var service = new AppRegistrationService(registry);

        using (service.Register(CreateDescriptor("instance-1")))
        {
            Assert.Single(registry.List());
        }

        Assert.Empty(registry.List());
    }

    [Fact]
    public void ExpireStale_UsesConfiguredMaximumAge()
    {
        var now = DateTimeOffset.UtcNow;
        var registry = new AppInstanceRegistry();
        var service = new AppRegistrationService(registry);
        registry.Register(CreateDescriptor("stale") with { LastHeartbeatAt = now.AddSeconds(-10) });

        var expired = service.ExpireStale(now, new AppRegistrationOptions { StaleInstanceAge = TimeSpan.FromSeconds(5) });

        Assert.Single(expired);
        Assert.Empty(registry.List());
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
