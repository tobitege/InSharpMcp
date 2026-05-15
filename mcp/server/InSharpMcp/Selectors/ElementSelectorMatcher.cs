using InSharpMcp.Contracts;

namespace InSharpMcp.Selectors;

public sealed class ElementSelectorMatcher
{
    public ToolResult Match(UiTreeSnapshot snapshot, ElementSelector selector, ToolLimits limits)
    {
        var validation = Validate(selector);
        if (!validation.Success)
        {
            return validation;
        }

        var matches = new List<UiElementNode>();
        var candidates = Flatten(snapshot.Root).Where(node => Matches(node, selector)).ToArray();
        if (selector.Index is { } index)
        {
            if (index < candidates.Length)
            {
                matches.Add(candidates[index]);
            }
        }
        else
        {
            matches.AddRange(candidates.Take(limits.MaxNodes));
        }

        return ToolResult.Ok(
            "Selector query completed.",
            new ElementQueryResult(matches, candidates.Length > matches.Count));
    }

    private static ToolResult Validate(ElementSelector selector)
    {
        if (selector.Index is < 0)
        {
            return ToolResult.Fail("Selector index must not be negative.", "invalid_selector");
        }

        if (selector is
            {
                Name: null,
                AutomationId: null,
                Type: null,
                Text: null,
                Role: null,
                Index: null,
                Path: null,
                Adapter: null,
            })
        {
            return ToolResult.Fail("Selector must include at least one field.", "invalid_selector");
        }

        return ToolResult.Ok("Selector is valid.");
    }

    private static IEnumerable<UiElementNode> Flatten(UiElementNode root)
    {
        yield return root;
        if (root.Children is null)
        {
            yield break;
        }

        foreach (var child in root.Children)
        {
            foreach (var node in Flatten(child))
            {
                yield return node;
            }
        }
    }

    private static bool Matches(UiElementNode node, ElementSelector selector)
    {
        return MatchesOptional(node.Name, selector.Name)
            && MatchesOptional(node.AutomationId, selector.AutomationId)
            && MatchesOptional(node.Type, selector.Type)
            && MatchesOptional(node.Text, selector.Text)
            && MatchesOptional(node.Role, selector.Role)
            && MatchesOptional(node.ElementIdentifier, selector.Path);
    }

    private static bool MatchesOptional(string? actual, string? expected) =>
        expected is null || string.Equals(actual, expected, StringComparison.Ordinal);
}
