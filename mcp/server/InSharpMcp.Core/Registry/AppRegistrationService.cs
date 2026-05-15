using InSharpMcp.Routing;

namespace InSharpMcp.Registry;

public sealed class AppRegistrationService
{
    private readonly AppInstanceRegistry _registry;
    private readonly AppInstanceConnectionRegistry? _connections;

    public AppRegistrationService(AppInstanceRegistry registry)
        : this(registry, connections: null)
    {
    }

    public AppRegistrationService(AppInstanceRegistry registry, AppInstanceConnectionRegistry? connections)
    {
        _registry = registry;
        _connections = connections;
    }

    public AppRegistration Register(AppInstanceDescriptor descriptor, IAppInstanceClient? client = null)
    {
        _registry.Register(descriptor);
        if (client is not null)
        {
            _connections?.Register(descriptor.InstanceId, client);
        }

        return new AppRegistration(_registry, _connections, descriptor.InstanceId);
    }

    public void Unregister(string instanceId)
    {
        _registry.Unregister(instanceId);
        _connections?.Unregister(instanceId);
    }

    public bool TryHeartbeat(string instanceId, DateTimeOffset heartbeatAt) =>
        _registry.TryHeartbeat(instanceId, heartbeatAt, out _);

    public IReadOnlyCollection<AppInstanceDescriptor> ExpireStale(DateTimeOffset now, AppRegistrationOptions? options = null)
    {
        var effectiveOptions = options ?? new AppRegistrationOptions();
        var expired = _registry.ExpireStale(now, effectiveOptions.StaleInstanceAge);
        foreach (var instance in expired)
        {
            _connections?.Unregister(instance.InstanceId);
        }

        return expired;
    }
}

public sealed class AppRegistration : IAsyncDisposable, IDisposable
{
    private readonly AppInstanceRegistry _registry;
    private readonly AppInstanceConnectionRegistry? _connections;
    private readonly string _instanceId;
    private bool _disposed;

    public AppRegistration(
        AppInstanceRegistry registry,
        AppInstanceConnectionRegistry? connections,
        string instanceId)
    {
        _registry = registry;
        _connections = connections;
        _instanceId = instanceId;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _registry.Unregister(_instanceId);
        _connections?.Unregister(_instanceId);
        _disposed = true;
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
