using System.Text.Json;

namespace InSharpMcp.Contracts;

public interface IElementPropertyEditor
{
    Task<ToolResult> SetElementPropertyAsync(
        string elementIdentifier,
        string targetObject,
        string propertyName,
        JsonElement value,
        CancellationToken cancellationToken);
}
