namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service for generating thumbnail images from HTML content.
/// Uses headless browser rendering (Chromium via PuppeteerSharp) to capture
/// visual snapshots of HTML receipts for display in receipt lists.
/// </summary>
public interface IHtmlThumbnailService
{
    /// <summary>
    /// Generates a thumbnail image from HTML content.
    /// Renders the HTML in a headless browser and captures a screenshot.
    /// </summary>
    /// <param name="htmlContent">HTML content to render</param>
    /// <param name="width">Target thumbnail width in pixels (default: 800 for crisp PDF display)</param>
    /// <param name="height">Target thumbnail height in pixels (default: 800 for crisp PDF display)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Stream containing the thumbnail image (JPEG format)</returns>
    Task<Stream> GenerateThumbnailAsync(
        string htmlContent,
        int width = 800,
        int height = 800,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if the thumbnail service is available (Chromium installed and accessible).
    /// Use this to gracefully degrade if the headless browser is not configured.
    /// </summary>
    /// <returns>True if thumbnails can be generated, false otherwise</returns>
    Task<bool> IsAvailableAsync();
}
