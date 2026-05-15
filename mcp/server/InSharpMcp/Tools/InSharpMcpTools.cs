using InSharpMcp.Contracts;
using InSharpMcp.Registry;
using ModelContextProtocol.Server;

namespace InSharpMcp.Tools;

[McpServerToolType]
public sealed class InSharpMcpTools
{
    [McpServerTool(Name = "ism_list_instances")]
    public static ToolResult ListInstances(AppInstanceRegistry registry)
    {
        var instances = registry.List().Select(instance => new
        {
            instance.InstanceId,
            instance.AppId,
            instance.AppName,
            instance.ProcessId,
            instance.AdapterKind,
            instance.PlatformTarget,
            instance.OperatingSystem,
            instance.AppVersion,
            instance.Capabilities,
            instance.RegisteredAt,
            instance.LastHeartbeatAt,
        });

        return ToolResult.Ok("Registered app instances listed.", instances.ToArray());
    }

    [McpServerTool(Name = "ism_get_runtime_info")]
    public static ToolResult GetRuntimeInfo(AppTargetSelector? target, AppInstanceSelector selector)
    {
        var selection = selector.Select(target);
        if (!selection.Succeeded)
        {
            return selection.Error!;
        }

        var instance = selection.Instance!;
        return ToolResult.Ok(
            "Runtime information returned.",
            new
            {
                instance.InstanceId,
                instance.AppId,
                instance.AppName,
                instance.ProcessId,
                instance.AdapterKind,
                instance.PlatformTarget,
                instance.OperatingSystem,
                instance.AppVersion,
            });
    }
}
