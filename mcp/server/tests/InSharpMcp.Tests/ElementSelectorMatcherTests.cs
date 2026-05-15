using System.Text.Json;
using InSharpMcp.Contracts;
using InSharpMcp.Selectors;

namespace InSharpMcp.Tests;

public sealed class ElementSelectorMatcherTests
{
    [Fact]
    public void ElementSelector_DeserializesFromStructuredJson()
    {
        var json = """{"role":"button","name":"Save"}""";

        var selector = JsonSerializer.Deserialize<ElementSelector>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Equal("button", selector?.Role);
        Assert.Equal("Save", selector?.Name);
    }

    [Fact]
    public void Match_ReturnsDeterministicBoundedMatches()
    {
        var matcher = new ElementSelectorMatcher();
        var snapshot = CreateSnapshot();

        var result = matcher.Match(snapshot, new ElementSelector(Role: "button"), new ToolLimits { MaxNodes = 1 });

        var query = Assert.IsType<ElementQueryResult>(result.Data);
        Assert.Single(query.Matches);
        Assert.Equal("save", query.Matches[0].ElementIdentifier);
        Assert.True(query.Truncated);
    }

    [Fact]
    public void Match_RejectsInvalidSelector()
    {
        var matcher = new ElementSelectorMatcher();

        var result = matcher.Match(CreateSnapshot(), new ElementSelector(Index: -1), new ToolLimits());

        Assert.False(result.Success);
        Assert.Equal("invalid_selector", result.ErrorCode);
    }

    private static UiTreeSnapshot CreateSnapshot() =>
        new(
            new UiElementNode(
                "root",
                "Window",
                Children:
                [
                    new("save", "Button", Name: "Save", Role: "button"),
                    new("cancel", "Button", Name: "Cancel", Role: "button"),
                ]),
            NodeCount: 3,
            Truncated: false);
}
