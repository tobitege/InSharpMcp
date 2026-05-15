using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using InSharpMcp.Contracts;

namespace InSharpMcp.Adapters.Avalonia;

public sealed class AvaloniaScreenshotProvider : IScreenshotProvider
{
    private readonly Control _root;
    private readonly IUiDispatcher _dispatcher;

    public AvaloniaScreenshotProvider(Control root, IUiDispatcher dispatcher)
    {
        _root = root;
        _dispatcher = dispatcher;
    }

    public Task<ScreenshotResult> CaptureScreenshotAsync(CancellationToken cancellationToken) =>
        _dispatcher.RunAsync(
            token =>
            {
                token.ThrowIfCancellationRequested();
                var width = (int)Math.Ceiling(_root.Bounds.Width);
                var height = (int)Math.Ceiling(_root.Bounds.Height);
                if (width <= 0 || height <= 0)
                {
                    return new ScreenshotResult(
                        false,
                        null,
                        "Screenshot capture requires a measured Avalonia root.",
                        "unavailable");
                }

                var renderTarget = new RenderTargetBitmap(new PixelSize(width, height), new Vector(96, 96));
                renderTarget.Render(_root);

                using var stream = new MemoryStream();
                renderTarget.Save(stream);
                return new ScreenshotResult(true, stream.ToArray(), "Screenshot captured.");
            },
            cancellationToken);
}
