namespace InSharpMcp.Limits;

public sealed class ClientLimitConfigurationParser
{
    public const string EnvironmentMaxDepth = "ISM_MAX_DEPTH";
    public const string EnvironmentMaxNodes = "ISM_MAX_NODES";
    public const string EnvironmentMaxTextCharacters = "ISM_MAX_TEXT_CHARACTERS";
    public const string HeaderMaxDepth = "X-InSharpMcp-Max-Depth";
    public const string HeaderMaxNodes = "X-InSharpMcp-Max-Nodes";
    public const string HeaderMaxTextCharacters = "X-InSharpMcp-Max-Text-Characters";

    private static readonly IReadOnlyDictionary<string, string> KnownKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [EnvironmentMaxDepth] = nameof(ClientLimitConfiguration.MaxDepth),
        [EnvironmentMaxNodes] = nameof(ClientLimitConfiguration.MaxNodes),
        [EnvironmentMaxTextCharacters] = nameof(ClientLimitConfiguration.MaxTextCharacters),
        [HeaderMaxDepth] = nameof(ClientLimitConfiguration.MaxDepth),
        [HeaderMaxNodes] = nameof(ClientLimitConfiguration.MaxNodes),
        [HeaderMaxTextCharacters] = nameof(ClientLimitConfiguration.MaxTextCharacters),
        ["MaxDepth"] = nameof(ClientLimitConfiguration.MaxDepth),
        ["MaxNodes"] = nameof(ClientLimitConfiguration.MaxNodes),
        ["MaxTextCharacters"] = nameof(ClientLimitConfiguration.MaxTextCharacters),
    };

    public ParsedClientLimitConfiguration Parse(IReadOnlyDictionary<string, string?> values)
    {
        string? maxDepth = null;
        string? maxNodes = null;
        string? maxTextCharacters = null;
        var unknownKeys = new List<string>();

        foreach (var pair in values)
        {
            if (!KnownKeys.TryGetValue(pair.Key, out var target))
            {
                if (LooksLikeInSharpMcpLimitKey(pair.Key))
                {
                    unknownKeys.Add(pair.Key);
                }

                continue;
            }

            switch (target)
            {
                case nameof(ClientLimitConfiguration.MaxDepth):
                    maxDepth = pair.Value;
                    break;
                case nameof(ClientLimitConfiguration.MaxNodes):
                    maxNodes = pair.Value;
                    break;
                case nameof(ClientLimitConfiguration.MaxTextCharacters):
                    maxTextCharacters = pair.Value;
                    break;
            }
        }

        return new ParsedClientLimitConfiguration(
            new ClientLimitConfiguration(maxDepth, maxNodes, maxTextCharacters),
            unknownKeys);
    }

    private static bool LooksLikeInSharpMcpLimitKey(string key) =>
        key.StartsWith("ISM_", StringComparison.OrdinalIgnoreCase)
        || key.StartsWith("X-InSharpMcp-", StringComparison.OrdinalIgnoreCase);
}

public sealed record ParsedClientLimitConfiguration(
    ClientLimitConfiguration Configuration,
    IReadOnlyList<string> UnknownKeys);
