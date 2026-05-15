using InSharpMcp.Concurrency;
using InSharpMcp.Contracts;

namespace InSharpMcp.Tests;

public sealed class ConcurrentCallGateTests
{
    [Fact]
    public async Task RunAsync_ReturnsBusy_WhenLimitIsAlreadyOccupied()
    {
        using var gate = new ConcurrentCallGate(new InSharpMcpConcurrencyOptions { MaxConcurrentCalls = 1 });
        using var release = new ManualResetEventSlim();
        var running = gate.RunAsync(
            _ => Task.Run(
                () =>
                {
                    release.Wait(TimeSpan.FromSeconds(5));
                    return ToolResult.Ok("done");
                }),
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None);

        var blocked = await gate.RunAsync(
            _ => Task.FromResult(ToolResult.Ok("unexpected")),
            TimeSpan.FromMilliseconds(10),
            CancellationToken.None);

        release.Set();
        await running;

        Assert.False(blocked.Success);
        Assert.Equal("busy", blocked.ErrorCode);
    }
}
