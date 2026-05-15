using InSharpMcp.Contracts;

namespace InSharpMcp.Adapters.Uno;

public sealed class UnoScreenshotProvider : IScreenshotProvider
{
    public Task<ScreenshotResult> CaptureScreenshotAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new ScreenshotResult(
            false,
            null,
            "Screenshot capture is implemented in the screenshot phase.",
            "unsupported"));
    }
}
