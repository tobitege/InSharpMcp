using System.Collections.Concurrent;

namespace InSharpMcp.Routing;

public sealed class AppInstanceConnectionRegistry
{
    private readonly ConcurrentDictionary<string, IAppInstanceClient> _clients = new(StringComparer.Ordinal);

    public void Register(string instanceId, IAppInstanceClient client)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        ArgumentNullException.ThrowIfNull(client);

        _clients[instanceId] = client;
    }

    public bool Unregister(string instanceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        return _clients.TryRemove(instanceId, out _);
    }

    public bool TryGet(string instanceId, out IAppInstanceClient client)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        return _clients.TryGetValue(instanceId, out client!);
    }
}
