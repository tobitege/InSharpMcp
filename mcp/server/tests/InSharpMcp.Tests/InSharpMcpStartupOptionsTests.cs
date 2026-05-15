using InSharpMcp.Configuration;

namespace InSharpMcp.Tests;

public sealed class InSharpMcpStartupOptionsTests : IDisposable
{
    public void Dispose()
    {
        Environment.SetEnvironmentVariable(InSharpMcpStartupOptions.EnabledEnvironmentVariable, null);
    }

    [Fact]
    public void FromEnvironment_DisablesMcpByDefault()
    {
        Environment.SetEnvironmentVariable(InSharpMcpStartupOptions.EnabledEnvironmentVariable, null);

        var options = InSharpMcpStartupOptions.FromEnvironment();

        Assert.False(options.Enabled);
    }

    [Fact]
    public void FromEnvironment_EnablesMcpOnlyForExplicitOne()
    {
        Environment.SetEnvironmentVariable(InSharpMcpStartupOptions.EnabledEnvironmentVariable, "1");

        var options = InSharpMcpStartupOptions.FromEnvironment();

        Assert.True(options.Enabled);
    }
}
