using System.Globalization;
using InSharpMcp.Contracts;

namespace InSharpMcp.Limits;

public sealed class ToolLimitPolicyEvaluator
{
    private readonly ToolLimitPolicy _policy;

    public ToolLimitPolicyEvaluator(ToolLimitPolicy? policy = null)
    {
        _policy = policy ?? new ToolLimitPolicy();
    }

    public LimitClampResult Evaluate(ClientLimitConfiguration? configuration)
    {
        if (configuration is null)
        {
            return new LimitClampResult(_policy.Defaults, Array.Empty<string>());
        }

        var warnings = new List<string>();
        var maxDepth = ParseAndClamp(
            "MaxDepth",
            configuration.MaxDepth,
            _policy.Defaults.MaxDepth,
            _policy.MinDepth,
            _policy.MaxDepth,
            warnings);
        var maxNodes = ParseAndClamp(
            "MaxNodes",
            configuration.MaxNodes,
            _policy.Defaults.MaxNodes,
            _policy.MinNodes,
            _policy.MaxNodes,
            warnings);
        var maxTextCharacters = ParseAndClamp(
            "MaxTextCharacters",
            configuration.MaxTextCharacters,
            _policy.Defaults.MaxTextCharacters,
            _policy.MinTextCharacters,
            _policy.MaxTextCharacters,
            warnings);

        return new LimitClampResult(
            _policy.Defaults with
            {
                MaxDepth = maxDepth,
                MaxNodes = maxNodes,
                MaxTextCharacters = maxTextCharacters,
            },
            warnings);
    }

    private static int ParseAndClamp(
        string name,
        string? rawValue,
        int defaultValue,
        int minimum,
        int maximum,
        ICollection<string> warnings)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return defaultValue;
        }

        if (!int.TryParse(rawValue, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0)
        {
            warnings.Add($"{name} was invalid and defaulted.");
            return defaultValue;
        }

        if (parsed < minimum)
        {
            warnings.Add($"{name} was clamped to the minimum.");
            return minimum;
        }

        if (parsed > maximum)
        {
            warnings.Add($"{name} was clamped to the maximum.");
            return maximum;
        }

        return parsed;
    }
}
