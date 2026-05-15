using InSharpMcp.Contracts;

namespace InSharpMcp.Tests;

public sealed class NodeVisitBudgetTests
{
    [Fact]
    public void TryVisit_ConsumesGlobalBudgetAcrossSiblings()
    {
        var budget = new NodeVisitBudget(maxNodes: 2);

        Assert.True(budget.TryVisit());
        Assert.True(budget.TryVisit());
        Assert.False(budget.TryVisit());
        Assert.Equal(2, budget.VisitedNodes);
        Assert.Equal(0, budget.RemainingNodes);
    }
}
