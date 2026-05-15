using InSharpMcp.Contracts;

namespace InSharpMcp.Security;

public sealed class McpAuthorization
{
    private readonly McpAccessOptions _options;

    public McpAuthorization(McpAccessOptions? options = null)
    {
        _options = options ?? new McpAccessOptions();
    }

    public ToolResult AuthorizeTool(string toolName, McpTransportKind transportKind, string? suppliedToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);

        if (!_options.ProtectedTools.Contains(toolName))
        {
            return ToolResult.Ok("Tool is allowed.");
        }

        if (!_options.RequireTokenForProtectedTools)
        {
            return ToolResult.Ok("Protected tool token requirement is disabled.");
        }

        if (transportKind == McpTransportKind.Http && _options.AllowUnauthenticatedHttp)
        {
            return ToolResult.Ok("HTTP unauthenticated access is allowed by configuration.");
        }

        if (string.IsNullOrEmpty(_options.SharedToken))
        {
            return ToolResult.Fail("Protected tool token is not configured.", "unauthorized");
        }

        return FixedTimeEquals(_options.SharedToken, suppliedToken)
            ? ToolResult.Ok("Protected tool token accepted.")
            : ToolResult.Fail("Protected tool token is invalid.", "unauthorized");
    }

    private static bool FixedTimeEquals(string expected, string? actual)
    {
        if (actual is null || expected.Length != actual.Length)
        {
            return false;
        }

        var different = 0;
        for (var index = 0; index < expected.Length; index++)
        {
            different |= expected[index] ^ actual[index];
        }

        return different == 0;
    }
}
