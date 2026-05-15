namespace InSharpMcp.Contracts;

public sealed record ToolResult(
    bool Success,
    string Message,
    object? Data = null,
    string? ErrorCode = null)
{
    public static ToolResult Ok(string message, object? data = null) => new(true, message, data);

    public static ToolResult Fail(string message, string errorCode, object? data = null) =>
        new(false, message, data, errorCode);
}
