namespace InSharpMcp.Registry;

public sealed class AppRegistrationService
{
    private readonly AppInstanceRegistry _registry;

    public AppRegistrationService(AppInstanceRegistry registry)
    {
        _registry = registry;
    }

    public AppRegistration Register(AppInstanceDescriptor descriptor)
    {
        _registry.Register(descriptor);
        return new AppRegistration(_registry, descriptor.InstanceId);
    }

    public IReadOnlyCollection<AppInstanceDescriptor> ExpireStale(DateTimeOffset now, AppRegistrationOptions? options = null)
    {
        var effectiveOptions = options ?? new AppRegistrationOptions();
        return _registry.ExpireStale(now, effectiveOptions.StaleInstanceAge);
    }
}

public sealed class AppRegistration : IAsyncDisposable, IDisposable
{
    private readonly AppInstanceRegistry _registry;
    private readonly string _instanceId;
    private bool _disposed;

    public AppRegistration(AppInstanceRegistry registry, string instanceId)
    {
        _registry = registry;
        _instanceId = instanceId;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _registry.Unregister(_instanceId);
        _disposed = true;
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
