using InSharpMcp.Contracts;
using Microsoft.UI.Xaml;

#if WINDOWS
using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
#endif

namespace InSharpMcp.Adapters.Uno;

public sealed class UnoScreenshotProvider : IScreenshotProvider
{
    private readonly UIElement _root;
    private readonly IUiDispatcher _dispatcher;

    public UnoScreenshotProvider(UIElement root, IUiDispatcher dispatcher)
    {
        _root = root;
        _dispatcher = dispatcher;
    }

    public Task<ScreenshotResult> CaptureScreenshotAsync(CancellationToken cancellationToken) =>
        _dispatcher.RunAsync(CaptureOnUiThreadAsync, cancellationToken);

    private async Task<ScreenshotResult> CaptureOnUiThreadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
#if WINDOWS
        var bitmap = new RenderTargetBitmap();
        await bitmap.RenderAsync(_root);
        cancellationToken.ThrowIfCancellationRequested();

        var pixels = await bitmap.GetPixelsAsync();
        using var stream = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            (uint)bitmap.PixelWidth,
            (uint)bitmap.PixelHeight,
            dpiX: 96,
            dpiY: 96,
            pixels.ToArray());
        await encoder.FlushAsync();
        cancellationToken.ThrowIfCancellationRequested();

        stream.Seek(0);
        var bytes = new byte[stream.Size];
        using var reader = new DataReader(stream.GetInputStreamAt(0));
        await reader.LoadAsync((uint)stream.Size);
        reader.ReadBytes(bytes);
        return new ScreenshotResult(true, bytes, "Screenshot captured.");
#else
        return new ScreenshotResult(
            false,
            null,
            "Screenshot capture is unsupported for this Uno target.",
            "unsupported");
#endif
    }
}
