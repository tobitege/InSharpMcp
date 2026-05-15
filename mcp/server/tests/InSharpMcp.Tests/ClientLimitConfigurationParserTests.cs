using InSharpMcp.Limits;

namespace InSharpMcp.Tests;

public sealed class ClientLimitConfigurationParserTests
{
    [Fact]
    public void Parse_AcceptsCanonicalEnvironmentKeys()
    {
        var parser = new ClientLimitConfigurationParser();
        var values = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [ClientLimitConfigurationParser.EnvironmentMaxDepth] = "10",
            [ClientLimitConfigurationParser.EnvironmentMaxNodes] = "20",
            [ClientLimitConfigurationParser.EnvironmentMaxTextCharacters] = "3000",
        };

        var result = parser.Parse(values);

        Assert.Equal("10", result.Configuration.MaxDepth);
        Assert.Equal("20", result.Configuration.MaxNodes);
        Assert.Equal("3000", result.Configuration.MaxTextCharacters);
        Assert.Empty(result.UnknownKeys);
    }

    [Fact]
    public void Parse_AcceptsHttpHeaderKeys()
    {
        var parser = new ClientLimitConfigurationParser();
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [ClientLimitConfigurationParser.HeaderMaxDepth] = "11",
            [ClientLimitConfigurationParser.HeaderMaxNodes] = "22",
            [ClientLimitConfigurationParser.HeaderMaxTextCharacters] = "3333",
        };

        var result = parser.Parse(values);

        Assert.Equal("11", result.Configuration.MaxDepth);
        Assert.Equal("22", result.Configuration.MaxNodes);
        Assert.Equal("3333", result.Configuration.MaxTextCharacters);
        Assert.Empty(result.UnknownKeys);
    }

    [Fact]
    public void Parse_ReportsUnknownInSharpMcpLimitKeys()
    {
        var parser = new ClientLimitConfigurationParser();
        var values = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["ISM_TIMEOUT"] = "999",
            ["OTHER_VALUE"] = "ignored",
        };

        var result = parser.Parse(values);

        Assert.Single(result.UnknownKeys);
        Assert.Equal("ISM_TIMEOUT", result.UnknownKeys.Single());
    }
}
