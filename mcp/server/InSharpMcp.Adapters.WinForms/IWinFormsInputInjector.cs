using InSharpMcp.Contracts;

namespace InSharpMcp.Adapters.WinForms;

public interface IWinFormsInputInjector
{
    ToolResult PointerClick(int screenX, int screenY);

    ToolResult KeyPress(string key, IReadOnlyList<string> modifiers);

    ToolResult TypeText(string text);
}
