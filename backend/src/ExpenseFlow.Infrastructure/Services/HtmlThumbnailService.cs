using ExpenseFlow.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// HTML thumbnail generation service using PuppeteerSharp (headless Chromium).
/// Renders HTML content in a browser viewport and captures a screenshot,
/// then crops to the requested thumbnail dimensions.
/// </summary>
/// <remarks>
/// The service uses lazy browser initialization to avoid startup overhead
/// when thumbnails aren't needed. A single browser instance is shared across
/// requests, with individual pages created per thumbnail generation.
///
/// In containerized environments, the Chromium executable path must be set via
/// the PUPPETEER_EXECUTABLE_PATH environment variable (configured in Dockerfile).
/// </remarks>
public class HtmlThumbnailService : IHtmlThumbnailService, IAsyncDisposable, IDisposable
{
    private readonly ILogger<HtmlThumbnailService> _logger;
    private readonly int _viewportWidth;
    private readonly int _viewportHeight;
    private readonly SemaphoreSlim _browserLock = new(1, 1);
    private IBrowser? _browser;
    private bool _browserInitFailed;
    private bool _disposed;

    public HtmlThumbnailService(
        IConfiguration configuration,
        ILogger<HtmlThumbnailService> logger)
    {
        _logger = logger;
        _viewportWidth = configuration.GetValue("ReceiptProcessing:Html:ThumbnailViewportWidth", 800);
        _viewportHeight = configuration.GetValue("ReceiptProcessing:Html:ThumbnailViewportHeight", 600);
    }

    /// <inheritdoc />
    public async Task<Stream> GenerateThumbnailAsync(
        string htmlContent,
        int width = 800,
        int height = 800,
        CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var browser = await GetOrCreateBrowserAsync();
        if (browser == null)
        {
            throw new InvalidOperationException(
                "Chromium browser is not available. " +
                "Ensure PUPPETEER_EXECUTABLE_PATH is set or Chromium is installed.");
        }

        _logger.LogDebug(
            "Generating HTML thumbnail: viewport {ViewportWidth}x{ViewportHeight}, output {Width}x{Height}",
            _viewportWidth, _viewportHeight, width, height);

        // Create a new page for this thumbnail (pages are lightweight)
        await using var page = await browser.NewPageAsync();

        try
        {
            // Set viewport size for consistent rendering
            await page.SetViewportAsync(new ViewPortOptions
            {
                Width = _viewportWidth,
                Height = _viewportHeight,
                DeviceScaleFactor = 1
            });

            // Set content and wait for DOM and fonts to load
            await page.SetContentAsync(htmlContent, new NavigationOptions
            {
                WaitUntil = [WaitUntilNavigation.DOMContentLoaded],
                Timeout = 10000 // 10 second timeout
            });

            // Small delay to ensure images and styles are applied
            await Task.Delay(500, ct);

            // Capture screenshot as PNG (lossless for processing)
            var screenshotData = await page.ScreenshotDataAsync(new ScreenshotOptions
            {
                Type = ScreenshotType.Png,
                Clip = new Clip
                {
                    X = 0,
                    Y = 0,
                    Width = _viewportWidth,
                    Height = _viewportHeight
                }
            });

            // Resize and crop to final thumbnail dimensions using ImageSharp
            using var image = Image.Load(screenshotData);

            // Resize maintaining aspect ratio to cover the target size
            image.Mutate(x => x
                .Resize(new ResizeOptions
                {
                    Size = new Size(width, height),
                    Mode = ResizeMode.Crop,
                    Position = AnchorPositionMode.TopLeft
                }));

            // Convert to JPEG and return
            var outputStream = new MemoryStream();
            await image.SaveAsync(outputStream, new JpegEncoder { Quality = 80 }, ct);
            outputStream.Position = 0;

            _logger.LogDebug(
                "Generated HTML thumbnail: {Width}x{Height}, {Size} bytes",
                width, height, outputStream.Length);

            return outputStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate HTML thumbnail");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsAvailableAsync()
    {
        if (_browserInitFailed)
        {
            return false;
        }

        try
        {
            var browser = await GetOrCreateBrowserAsync();
            return browser != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets or lazily creates the shared browser instance.
    /// Uses double-checked locking for thread safety.
    /// </summary>
    private async Task<IBrowser?> GetOrCreateBrowserAsync()
    {
        if (_browser != null && _browser.IsConnected)
        {
            return _browser;
        }

        if (_browserInitFailed)
        {
            return null;
        }

        await _browserLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_browser != null && _browser.IsConnected)
            {
                return _browser;
            }

            if (_browserInitFailed)
            {
                return null;
            }

            _logger.LogInformation("Initializing headless Chromium browser for HTML thumbnails");

            // Check for custom executable path (set in Docker environment)
            var executablePath = Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH");

            var launchOptions = new LaunchOptions
            {
                Headless = true,
                Args = new[]
                {
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                    "--disable-dev-shm-usage",
                    "--disable-gpu",
                    "--disable-software-rasterizer",
                    "--single-process"
                }
            };

            if (!string.IsNullOrEmpty(executablePath))
            {
                _logger.LogInformation("Using Chromium at custom path: {Path}", executablePath);
                launchOptions.ExecutablePath = executablePath;

                // Verify the executable exists
                if (!File.Exists(executablePath))
                {
                    _logger.LogError(
                        "Chromium executable not found at {Path}. HTML thumbnails will be unavailable.",
                        executablePath);
                    _browserInitFailed = true;
                    return null;
                }
            }
            else
            {
                // Try to fetch/use bundled Chromium (works in local dev)
                _logger.LogDebug("No custom Chromium path set, checking for browser");

                var browserFetcher = new BrowserFetcher();
                var installedBrowser = browserFetcher.GetInstalledBrowsers().FirstOrDefault();

                if (installedBrowser == null)
                {
                    _logger.LogWarning(
                        "No Chromium found. Set PUPPETEER_EXECUTABLE_PATH or install via BrowserFetcher. " +
                        "HTML thumbnails will be unavailable.");
                    _browserInitFailed = true;
                    return null;
                }

                launchOptions.ExecutablePath = installedBrowser.GetExecutablePath();
            }

            _logger.LogInformation(
                "Launching Chromium browser with executable: {Path}, Args: {Args}",
                launchOptions.ExecutablePath,
                string.Join(" ", launchOptions.Args ?? Array.Empty<string>()));

            _browser = await Puppeteer.LaunchAsync(launchOptions);
            _logger.LogInformation("Headless Chromium browser initialized successfully at PID: {Pid}",
                _browser.Process?.Id ?? -1);

            return _browser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Chromium browser. HTML thumbnails will be unavailable.");
            _browserInitFailed = true;
            return null;
        }
        finally
        {
            _browserLock.Release();
        }
    }

    /// <summary>
    /// Disposes the browser instance when the service is disposed.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_browser != null)
        {
            await _browser.CloseAsync();
            _browser.Dispose();
            _browser = null;
        }

        _browserLock.Dispose();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Synchronous dispose for compatibility with DI container scope disposal.
    /// Blocks on async disposal to ensure browser is properly cleaned up.
    /// </summary>
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
