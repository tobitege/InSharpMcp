using InSharpMcp.Adapters.Shared;
using InSharpMcp.Contracts;

namespace InSharpMcp.Adapters.Avalonia;

public sealed class AvaloniaWindowsInputInjector : IAvaloniaInputInjector
{
    public ToolResult PointerClick(int screenX, int screenY) =>
        WindowsInput.PointerClick("Avalonia", screenX, screenY);

    public ToolResult KeyPress(string key, IReadOnlyList<string> modifiers) =>
        WindowsInput.KeyPress("Avalonia", key, modifiers);

    public ToolResult TypeText(string text) =>
        WindowsInput.TypeText("Avalonia", text);
}
