namespace InSharpMcp.Security;

public sealed record McpRequestAuthorizationContext(
    McpTransportKind TransportKind,
    string? AuthorizationToken);
