using InSharpMcp.Contracts;

namespace InSharpMcp.Limits;

public sealed record LimitClampResult(ToolLimits Limits, IReadOnlyList<string> Warnings);
