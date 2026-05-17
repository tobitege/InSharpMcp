namespace InSharpMcp.Contracts;

public interface IElementClickSimulator
{
    Task<ToolResult> ElementClickAsync(string elementIdentifier, CancellationToken cancellationToken);
}
