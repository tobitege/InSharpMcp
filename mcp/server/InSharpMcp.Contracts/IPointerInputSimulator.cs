namespace InSharpMcp.Contracts;

public interface IPointerInputSimulator
{
    Task<ToolResult> PointerClickAsync(double x, double y, CancellationToken cancellationToken);

    Task<ToolResult> KeyPressAsync(
        string key,
        IReadOnlyList<string> modifiers,
        CancellationToken cancellationToken);

    Task<ToolResult> TypeTextAsync(string text, CancellationToken cancellationToken);
}
