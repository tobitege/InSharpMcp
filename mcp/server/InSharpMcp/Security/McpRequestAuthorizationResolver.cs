using Microsoft.AspNetCore.Http;

namespace InSharpMcp.Security;

public sealed class McpRequestAuthorizationResolver
{
    private const string TokenHeaderName = "X-InSharpMcp-Token";
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public McpRequestAuthorizationResolver(IHttpContextAccessor? httpContextAccessor = null)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public McpRequestAuthorizationContext Resolve(string? suppliedToken)
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext is null)
        {
            return new McpRequestAuthorizationContext(McpTransportKind.Stdio, suppliedToken);
        }

        return new McpRequestAuthorizationContext(
            McpTransportKind.Http,
            ExtractHttpToken(httpContext) ?? suppliedToken);
    }

    private static string? ExtractHttpToken(HttpContext httpContext)
    {
        var authorization = httpContext.Request.Headers.Authorization.ToString();
        const string bearerPrefix = "Bearer ";
        if (authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return authorization[bearerPrefix.Length..].Trim();
        }

        var headerToken = httpContext.Request.Headers[TokenHeaderName].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(headerToken))
        {
            return headerToken;
        }

        var queryToken = httpContext.Request.Query["authorizationToken"].FirstOrDefault();
        return string.IsNullOrWhiteSpace(queryToken) ? null : queryToken;
    }
}
