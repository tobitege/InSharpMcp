using InSharpMcp.Contracts;

namespace InSharpMcp.Registry;

public sealed class AppInstanceSelector
{
    private readonly AppInstanceRegistry _registry;

    public AppInstanceSelector(AppInstanceRegistry registry)
    {
        _registry = registry;
    }

    public AppSelectionResult Select(AppTargetSelector? selector)
    {
        var candidates = _registry.List();
        if (selector is not null && !string.IsNullOrWhiteSpace(selector.InstanceId))
        {
            candidates = candidates
                .Where(instance => string.Equals(instance.InstanceId, selector.InstanceId, StringComparison.Ordinal))
                .ToArray();
        }
        else if (selector is not null)
        {
            candidates = candidates
                .Where(instance => MatchesOptional(instance.AppId, selector.AppId))
                .Where(instance => MatchesOptional(instance.AdapterKind, selector.AdapterKind))
                .ToArray();
        }

        return candidates.Count switch
        {
            0 => AppSelectionResult.Failure(
                ToolResult.Fail("No registered app instance matched the target selector.", "not_found")),
            1 => AppSelectionResult.Success(candidates.Single()),
            _ => AppSelectionResult.Failure(
                ToolResult.Fail(
                    "Target selector matched more than one app instance.",
                    "ambiguous_target",
                    candidates.Select(ToCandidate).ToArray())),
        };
    }

    private static bool MatchesOptional(string actual, string? expected) =>
        string.IsNullOrWhiteSpace(expected) || string.Equals(actual, expected, StringComparison.Ordinal);

    private static object ToCandidate(AppInstanceDescriptor descriptor) =>
        new
        {
            descriptor.InstanceId,
            descriptor.AppId,
            descriptor.AppName,
            descriptor.AdapterKind,
            descriptor.ProcessId,
        };
}

public sealed record AppSelectionResult(AppInstanceDescriptor? Instance, ToolResult? Error)
{
    public bool Succeeded => Instance is not null;

    public static AppSelectionResult Success(AppInstanceDescriptor instance) => new(instance, null);

    public static AppSelectionResult Failure(ToolResult error) => new(null, error);
}
