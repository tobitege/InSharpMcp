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
        Assert.Contains("ism_query_elements", toolNames);
        Assert.Contains("ism_wait_for_element", toolNames);
        Assert.Contains("ism_get_accessibility_tree", toolNames);
        Assert.Contains("ism_get_event_log", toolNames);
        Assert.Contains("ism_pointer_click", toolNames);
        Assert.Contains("ism_key_press", toolNames);
        Assert.Contains("ism_type_text", toolNames);
        Assert.Contains("ism_element_peer_default_action", toolNames);
        Assert.Contains("ism_close", toolNames);
        Assert.Contains("ism_start_trace", toolNames);
        Assert.Contains("ism_stop_trace", toolNames);
        Assert.Contains("ism_assert_element_exists", toolNames);
        Assert.Contains("ism_assert_element_text", toolNames);
        Assert.Contains("ism_assert_element_enabled", toolNames);
        Assert.All(toolNames, name => Assert.StartsWith("ism_", name, StringComparison.Ordinal));
    }
}
