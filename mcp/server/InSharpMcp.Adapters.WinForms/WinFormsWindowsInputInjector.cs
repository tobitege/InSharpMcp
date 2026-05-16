using InSharpMcp.Adapters.Shared;
using InSharpMcp.Contracts;

namespace InSharpMcp.Adapters.WinForms;

public sealed class WinFormsWindowsInputInjector : IWinFormsInputInjector
{
    public ToolResult PointerClick(int screenX, int screenY) =>
        WindowsInput.PointerClick("WinForms", screenX, screenY);

    public ToolResult KeyPress(string key, IReadOnlyList<string> modifiers) =>
        WindowsInput.KeyPress("WinForms", key, modifiers);

    public ToolResult TypeText(string text) =>
        WindowsInput.TypeText("WinForms", text);
}
