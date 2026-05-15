using System.Drawing.Imaging;
using InSharpMcp.Contracts;
using System.Drawing;
using System.Windows.Forms;

namespace InSharpMcp.Adapters.WinForms;

public sealed class WinFormsScreenshotProvider : IScreenshotProvider
{
    private readonly Control _root;
    private readonly IUiDispatcher _dispatcher;

    public WinFormsScreenshotProvider(Control root, IUiDispatcher dispatcher)
    {
        _root = root;
        _dispatcher = dispatcher;
    }

    public Task<ScreenshotResult> CaptureScreenshotAsync(CancellationToken cancellationToken) =>
        _dispatcher.RunAsync(
            token =>
            {
                token.ThrowIfCancellationRequested();
                if (_root.Width <= 0 || _root.Height <= 0)
                {
                    return new ScreenshotResult(
                        false,
                        null,
                        "Screenshot capture requires a measured WinForms root.",
                        "unavailable");
                }

                using var bitmap = new Bitmap(_root.Width, _root.Height);
                _root.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                using var stream = new MemoryStream();
                bitmap.Save(stream, ImageFormat.Png);
                return new ScreenshotResult(true, stream.ToArray(), "Screenshot captured.");
            },
            cancellationToken);
}
