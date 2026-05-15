using InSharpMcp.Contracts;
using InSharpMcp.Limits;

namespace InSharpMcp.Tests;

public sealed class ToolLimitPolicyEvaluatorTests
{
    [Fact]
    public void Evaluate_UsesDefaults_WhenConfigurationIsAbsent()
    {
        var evaluator = new ToolLimitPolicyEvaluator();

        var result = evaluator.Evaluate(null);

        Assert.Equal(new ToolLimits(), result.Limits);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Evaluate_ClampsValidValuesToPolicyRange()
    {
        var evaluator = new ToolLimitPolicyEvaluator();
        var configuration = new ClientLimitConfiguration(
            MaxDepth: "999",
            MaxNodes: "999999",
            MaxTextCharacters: "999999");

        var result = evaluator.Evaluate(configuration);

        Assert.Equal(50, result.Limits.MaxDepth);
        Assert.Equal(2_000, result.Limits.MaxNodes);
        Assert.Equal(256_000, result.Limits.MaxTextCharacters);
        Assert.Equal(3, result.Warnings.Count);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("-1")]
    [InlineData("0")]
    [InlineData("1.5")]
    public void Evaluate_DefaultsInvalidValues(string invalidValue)
    {
        var evaluator = new ToolLimitPolicyEvaluator();

        var result = evaluator.Evaluate(new ClientLimitConfiguration(MaxDepth: invalidValue));

        Assert.Equal(new ToolLimits().MaxDepth, result.Limits.MaxDepth);
        Assert.Single(result.Warnings);
    }
}
