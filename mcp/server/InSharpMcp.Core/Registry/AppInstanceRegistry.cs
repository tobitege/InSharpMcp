using System.Collections.Concurrent;

namespace InSharpMcp.Registry;

public sealed class AppInstanceRegistry
{
    private readonly ConcurrentDictionary<string, AppInstanceDescriptor> _instances = new(StringComparer.Ordinal);

    public IReadOnlyCollection<AppInstanceDescriptor> List() =>
        _instances.Values
            .OrderBy(instance => instance.AppId, StringComparer.Ordinal)
            .ThenBy(instance => instance.InstanceId, StringComparer.Ordinal)
            .ToArray();

    public AppInstanceDescriptor Register(AppInstanceDescriptor descriptor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(descriptor.InstanceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(descriptor.AppId);
        ArgumentException.ThrowIfNullOrWhiteSpace(descriptor.AdapterKind);

        _instances[descriptor.InstanceId] = descriptor;
        return descriptor;
    }

    public bool Unregister(string instanceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        return _instances.TryRemove(instanceId, out _);
    }

    public bool TryHeartbeat(string instanceId, DateTimeOffset heartbeatAt, out AppInstanceDescriptor descriptor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);

        while (_instances.TryGetValue(instanceId, out var current))
        {
            var updated = current with { LastHeartbeatAt = heartbeatAt };
            if (_instances.TryUpdate(instanceId, updated, current))
            {
                descriptor = updated;
                return true;
            }
        }

        descriptor = null!;
        return false;
    }

    public IReadOnlyCollection<AppInstanceDescriptor> ExpireStale(DateTimeOffset now, TimeSpan maxAge)
    {
        var expired = new List<AppInstanceDescriptor>();
        foreach (var instance in _instances.Values)
        {
            if (now - instance.LastHeartbeatAt <= maxAge)
            {
                continue;
            }

            if (_instances.TryRemove(instance.InstanceId, out var removed))
            {
                expired.Add(removed);
            }
        }

        return expired;
    }
}
