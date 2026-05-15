using System.Text.Json;

namespace InSharpMcp.Contracts.LocalTransport;

public static class LocalBrokerRequestKind
{
    public const string Register = "register";
    public const string Heartbeat = "heartbeat";
    public const string Unregister = "unregister";
}

public static class LocalAppOperation
{
    public const string VisualTreeSnapshot = "visualtree_snapshot";
    public const string GetElementMetadata = "get_element_metadata";
    public const string GetElementDataContext = "get_element_datacontext";
    public const string GetScreenshot = "get_screenshot";
    public const string GetAccessibilityTree = "get_accessibility_tree";
    public const string PointerClick = "pointer_click";
    public const string KeyPress = "key_press";
    public const string TypeText = "type_text";
    public const string ElementPeerDefaultAction = "element_peer_default_action";
    public const string Close = "close";
}

public sealed record LocalBrokerRequest(
    string Kind,
    LocalAppRegistrationMessage? Registration = null,
    string? InstanceId = null);

public sealed record LocalBrokerResponse(
    bool Success,
    string? Error = null);

public sealed record LocalAppRegistrationMessage(
    string InstanceId,
    string AppId,
    string AppName,
    int ProcessId,
    string AdapterKind,
    string PlatformTarget,
    string OperatingSystem,
    string AppVersion,
    string[] Capabilities,
    string AppPipeName);

public sealed record LocalAppRequest(
    string Operation,
    ToolLimits? Limits = null,
    string? ElementIdentifier = null,
    double? X = null,
    double? Y = null,
    string? Key = null,
    string[]? Modifiers = null,
    string? Text = null);

public sealed record LocalAppToolResponse(
    bool Success,
    string Message,
    JsonElement? Data = null,
    string? ErrorCode = null)
{
    public static LocalAppToolResponse FromToolResult(ToolResult result) =>
        new(result.Success, result.Message, ToJsonElement(result.Data), result.ErrorCode);

    public ToolResult ToToolResult(Type? dataType)
    {
        object? data = null;
        if (Data is { ValueKind: not JsonValueKind.Null } element)
        {
            data = dataType is null
                ? element.Clone()
                : element.Deserialize(dataType, LocalTransportJson.Options);
        }

        return new ToolResult(Success, Message, data, ErrorCode);
    }

    private static JsonElement? ToJsonElement(object? value)
    {
        if (value is null)
        {
            return null;
        }

        return JsonSerializer.SerializeToElement(value, LocalTransportJson.Options);
    }
}

public static class LocalTransportJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };
}
