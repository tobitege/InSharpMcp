using InSharpMcp.Contracts;
using InSharpMcp.Registry;

namespace InSharpMcp.Routing;

public sealed class AppInstanceRouter
{
    private readonly AppInstanceSelector _selector;
    private readonly AppInstanceConnectionRegistry _connections;

    public AppInstanceRouter(AppInstanceSelector selector, AppInstanceConnectionRegistry connections)
    {
        _selector = selector;
        _connections = connections;
    }

    public AppInstanceRoute Select(AppTargetSelector? target)
    {
        var selection = _selector.Select(target);
        if (!selection.Succeeded)
        {
            return AppInstanceRoute.Fail(selection.Error!);
        }

        var instance = selection.Instance!;
        if (!_connections.TryGet(instance.InstanceId, out var client))
        {
            return AppInstanceRoute.Fail(ToolResult.Fail(
                "Selected app instance has no active connection.",
                "stale_instance",
                new
                {
                    instance.InstanceId,
                    instance.AppId,
                    instance.Endpoint,
                }));
        }

        return AppInstanceRoute.Ok(instance, client);
    }
}

public sealed record AppInstanceRoute(
    bool Succeeded,
    AppInstanceDescriptor? Instance,
    IAppInstanceClient? Client,
    ToolResult? Error)
{
    public static AppInstanceRoute Ok(AppInstanceDescriptor instance, IAppInstanceClient client) =>
        new(true, instance, client, null);

    public static AppInstanceRoute Fail(ToolResult error) =>
        new(false, null, null, error);
}
