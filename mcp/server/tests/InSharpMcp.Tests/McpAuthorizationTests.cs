using InSharpMcp.Security;

namespace InSharpMcp.Tests;

public sealed class McpAuthorizationTests
{
    [Fact]
    public void AuthorizeTool_AllowsUnprotectedToolWithoutToken()
    {
        var authorization = new McpAuthorization();

        var result = authorization.AuthorizeTool("ism_get_runtime_info", McpTransportKind.Http, suppliedToken: null);

        Assert.True(result.Success);
    }

    [Fact]
    public void AuthorizeTool_RejectsProtectedToolWithoutConfiguredToken()
    {
        var authorization = new McpAuthorization();

        var result = authorization.AuthorizeTool("ism_close", McpTransportKind.Http, suppliedToken: null);

        Assert.False(result.Success);
        Assert.Equal("unauthorized", result.ErrorCode);
    }

    [Fact]
    public void AuthorizeTool_AcceptsProtectedToolWithMatchingToken()
    {
        var authorization = new McpAuthorization(new McpAccessOptions { SharedToken = "secret" });

        var result = authorization.AuthorizeTool("ism_close", McpTransportKind.Http, "secret");

        Assert.True(result.Success);
    }
}
