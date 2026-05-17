using InSharpMcp.Contracts;
using InSharpMcp.Tracing;

namespace InSharpMcp.Routing;

public interface IAppInstanceClient
{
    IEventLogProvider EventLog { get; }

    ITraceStore TraceStore { get; }

    Task<ToolResult> GetVisualTreeSnapshotAsync(ToolLimits limits, CancellationToken cancellationToken);

    Task<ToolResult> GetElementMetadataAsync(
        string elementIdentifier,
        ToolLimits limits,
        CancellationToken cancellationToken);

    Task<ToolResult> GetElementDataContextAsync(
        string elementIdentifier,
        ToolLimits limits,
        CancellationToken cancellationToken);

    Task<ScreenshotResult> CaptureScreenshotAsync(CancellationToken cancellationToken);

    Task<ToolResult> GetAccessibilityTreeAsync(ToolLimits limits, CancellationToken cancellationToken);

    Task<ToolResult> PointerClickAsync(double x, double y, CancellationToken cancellationToken);

    Task<ToolResult> ElementClickAsync(string elementIdentifier, CancellationToken cancellationToken);

    Task<ToolResult> KeyPressAsync(
        string key,
        IReadOnlyList<string> modifiers,
        CancellationToken cancellationToken);

    Task<ToolResult> TypeTextAsync(string text, CancellationToken cancellationToken);

    Task<ToolResult> InvokeDefaultActionAsync(string elementIdentifier, CancellationToken cancellationToken);

    Task<ToolResult> SetElementPropertyAsync(
        string elementIdentifier,
        string targetObject,
        string propertyName,
        System.Text.Json.JsonElement value,
        CancellationToken cancellationToken);

    Task<ToolResult> CloseAsync(CancellationToken cancellationToken);

    void RecordEvent(EventLogEntry entry);
}
