using InSharpMcp.Contracts;

namespace InSharpMcp.Adapters.Avalonia;

public sealed class AvaloniaPointerInputSimulator : IPointerInputSimulator
{
    public Task<ToolResult> PointerClickAsync(double x, double y, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = x;
        _ = y;
        return Task.FromResult(ToolResult.Fail("Avalonia pointer input is not supported until a proven platform input path is configured.", "unsupported"));
    }

    public Task<ToolResult> KeyPressAsync(
        string key,
        IReadOnlyList<string> modifiers,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = key;
        _ = modifiers;
        return Task.FromResult(ToolResult.Fail("Avalonia key input is not supported until a proven platform input path is configured.", "unsupported"));
    }

    public Task<ToolResult> TypeTextAsync(string text, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = text;
        return Task.FromResult(ToolResult.Fail("Avalonia text input is not supported until a proven platform input path is configured.", "unsupported"));
    }
}
