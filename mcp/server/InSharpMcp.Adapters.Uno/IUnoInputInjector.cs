using InSharpMcp.Contracts;

namespace InSharpMcp.Adapters.Uno;

public interface IUnoInputInjector
{
    ToolResult PointerClick(int screenX, int screenY);

    ToolResult KeyPress(string key, IReadOnlyList<string> modifiers);

    ToolResult TypeText(string text);

    bool TryClientToScreen(
        IntPtr hwnd,
        int clientX,
        int clientY,
        out int screenX,
        out int screenY,
        out ToolResult error);
}
