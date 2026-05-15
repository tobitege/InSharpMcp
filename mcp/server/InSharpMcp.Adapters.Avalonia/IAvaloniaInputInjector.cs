using InSharpMcp.Contracts;

namespace InSharpMcp.Adapters.Avalonia;

public interface IAvaloniaInputInjector
{
    ToolResult PointerClick(int screenX, int screenY);

    ToolResult KeyPress(string key, IReadOnlyList<string> modifiers);

    ToolResult TypeText(string text);
}
