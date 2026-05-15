namespace InSharpMcp.Contracts;

public interface IScreenshotProvider
{
    Task<ScreenshotResult> CaptureScreenshotAsync(CancellationToken cancellationToken);
}

public sealed record ScreenshotResult(
    bool Success,
    byte[]? PngBytes,
    string? Message,
    string? ErrorCode = null);
