using InSharpMcp.Adapters.Shared;
using InSharpMcp.Contracts;
using System.Runtime.InteropServices;

namespace InSharpMcp.Adapters.Uno;

public sealed class UnoWindowsInputInjector : IUnoInputInjector
{
    public ToolResult PointerClick(int screenX, int screenY) =>
        WindowsInput.PointerClick("Uno", screenX, screenY);

    public ToolResult KeyPress(string key, IReadOnlyList<string> modifiers) =>
        WindowsInput.KeyPress("Uno", key, modifiers);

    public ToolResult TypeText(string text) =>
        WindowsInput.TypeText("Uno", text);

    public bool TryClientToScreen(
        IntPtr hwnd,
        int clientX,
        int clientY,
        out int screenX,
        out int screenY,
        out ToolResult error)
    {
        var point = new PointData { X = clientX, Y = clientY };
        if (!ClientToScreen(hwnd, ref point))
        {
            screenX = 0;
            screenY = 0;
            error = WindowsInput.Win32Failure("Unable to translate Uno client coordinates to screen coordinates.");
            return false;
        }

        screenX = point.X;
        screenY = point.Y;
        error = ToolResult.Ok("Coordinates translated.");
        return true;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ClientToScreen(IntPtr hwnd, ref PointData point);

    [StructLayout(LayoutKind.Sequential)]
    private struct PointData
    {
        public int X;
        public int Y;
    }
}
