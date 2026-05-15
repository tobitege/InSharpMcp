using InSharpMcp.Tools;

namespace InSharpMcp.Tests;

public sealed class InSharpMcpToolCatalogTests
{
    [Fact]
    public void ListToolNames_ReturnsExpectedInitialToolNames()
    {
        var catalog = new InSharpMcpToolCatalog();

        var toolNames = catalog.ListToolNames();

        Assert.Contains("ism_list_instances", toolNames);
        Assert.Contains("ism_get_runtime_info", toolNames);
        Assert.Contains("ism_visualtree_snapshot", toolNames);
        Assert.Contains("ism_get_element_metadata", toolNames);
        Assert.Contains("ism_get_element_datacontext", toolNames);
        Assert.Contains("ism_get_screenshot", toolNames);
        Assert.All(toolNames, name => Assert.StartsWith("ism_", name, StringComparison.Ordinal));
    }
}
