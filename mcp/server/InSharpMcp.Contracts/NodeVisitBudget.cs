namespace InSharpMcp.Contracts;

public sealed class NodeVisitBudget
{
    public NodeVisitBudget(int maxNodes)
    {
        if (maxNodes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxNodes), "Node budget cannot be negative.");
        }

        MaxNodes = maxNodes;
        RemainingNodes = maxNodes;
    }

    public int MaxNodes { get; }

    public int RemainingNodes { get; private set; }

    public int VisitedNodes => MaxNodes - RemainingNodes;

    public bool TryVisit()
    {
        if (RemainingNodes <= 0)
        {
            return false;
        }

        RemainingNodes--;
        return true;
    }
}
