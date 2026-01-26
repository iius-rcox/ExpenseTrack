using ExpenseFlow.Core.Interfaces;
using ImageMagick;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Thumbnail generation service using Magick.NET.
/// Generates 200x200 JPEG thumbnails from receipt images.
/// </summary>
public class ThumbnailService : IThumbnailService
{
    private readonly ILogger<ThumbnailService> _logger;
    private static bool _ghostscriptInitialized;
    private static readonly object _initLock = new();

    private static readonly HashSet<string> SupportedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/heic",
        "image/heif",
        "application/pdf"
    };

    public ThumbnailService(ILogger<ThumbnailService> logger)
    {
        _logger = logger;
        EnsureGhostscriptInitialized();
    }

    /// <summary>
    /// Ensures Ghostscript is configured for Magick.NET PDF rendering.
    /// Tries multiple common paths on Linux/Windows.
    /// </summary>
    private void EnsureGhostscriptInitialized()
    {
        if (_ghostscriptInitialized) return;

        lock (_initLock)
        {
            if (_ghostscriptInitialized) return;

            // Try common Ghostscript paths in order of preference
            var gsPaths = new[]
            {
                Environment.GetEnvironmentVariable("GHOSTSCRIPT_PATH"),
                "/usr/bin",           // Debian/Ubuntu default
                "/usr/local/bin",     // Custom installations
                "/opt/ghostscript/bin" // Alternative location
            };

            foreach (var path in gsPaths.Where(p => !string.IsNullOrEmpty(p)))
            {
                var gsPath = Path.Combine(path!, "gs");
                if (File.Exists(gsPath))
                {
                    try
                    {
                        MagickNET.SetGhostscriptDirectory(path!);
                        _logger.LogInformation(
                            "Ghostscript configured for Magick.NET at: {Path} (gs binary found at {GsPath})",
                            path, gsPath);
                        _ghostscriptInitialized = true;
                        return;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to set Ghostscript directory to {Path}", path);
                    }
                }
            }

            // Also try without checking file existence (let Magick.NET handle it)
            var defaultPath = gsPaths.FirstOrDefault(p => !string.IsNullOrEmpty(p) && Directory.Exists(p));
            if (defaultPath != null)
            {
                try
                {
                    MagickNET.SetGhostscriptDirectory(defaultPath);
                    _logger.LogInformation(
                        "Ghostscript directory set to {Path} (gs binary not verified)",
                        defaultPath);
                    _ghostscriptInitialized = true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to set default Ghostscript directory");
                }
            }

            if (!_ghostscriptInitialized)
            {
                _logger.LogWarning(
                    "Ghostscript not found. PDF thumbnail generation may fail. Checked paths: {Paths}",
                    string.Join(", ", gsPaths.Where(p => !string.IsNullOrEmpty(p))));
            }

            _ghostscriptInitialized = true; // Mark as initialized even if not found
        }
    }

    public async Task<Stream> GenerateThumbnailAsync(
        Stream imageStream,
        string contentType,
        int width = 200,
        int height = 200)
    {
        _logger.LogDebug("Generating {Width}x{Height} thumbnail for content type {ContentType}",
            width, height, contentType);

        // Read stream into memory for ImageMagick
        using var inputStream = new MemoryStream();
        await imageStream.CopyToAsync(inputStream);
        inputStream.Position = 0;

        MagickImage image;

        if (contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            // For PDFs, read only the first page using Ghostscript delegate
            var settings = new MagickReadSettings
            {
                Density = new Density(150), // Good balance between quality and performance
                FrameIndex = 0,
                FrameCount = 1
            };

            try
            {
                _logger.LogDebug("Attempting to read PDF with Magick.NET (Ghostscript delegate)");
                image = new MagickImage(inputStream, settings);
                _logger.LogDebug("Successfully read PDF, dimensions: {Width}x{Height}",
                    image.Width, image.Height);
            }
            catch (MagickDelegateErrorException ex)
            {
                _logger.LogError(ex,
                    "Magick.NET delegate error reading PDF. This usually means Ghostscript is not properly configured. " +
                    "Error: {Message}. Ghostscript path env: {GsPath}",
                    ex.Message,
                    Environment.GetEnvironmentVariable("GHOSTSCRIPT_PATH") ?? "(not set)");
                throw;
            }
            catch (MagickMissingDelegateErrorException ex)
            {
                _logger.LogError(ex,
                    "Magick.NET missing delegate for PDF. Ghostscript is required but not found. " +
                    "Error: {Message}",
                    ex.Message);
                throw;
            }
        }
        else
        {
            image = new MagickImage(inputStream);
        }

        using (image)
        {
            // Resize maintaining aspect ratio, then crop to exact size
            image.Thumbnail(new MagickGeometry((uint)width, (uint)height)
            {
                IgnoreAspectRatio = false,
                FillArea = true
            });

            // Center crop to exact dimensions
            if (image.Width > (uint)width || image.Height > (uint)height)
            {
                var xOffset = (int)((image.Width - (uint)width) / 2);
                var yOffset = (int)((image.Height - (uint)height) / 2);
                image.Crop(new MagickGeometry(xOffset, yOffset, (uint)width, (uint)height));
            }

            // Convert to JPEG
            image.Format = MagickFormat.Jpeg;
            image.Quality = 80;

            var outputStream = new MemoryStream();
            await image.WriteAsync(outputStream);
            outputStream.Position = 0;

            _logger.LogDebug("Generated thumbnail: {Width}x{Height}, {Size} bytes",
                width, height, outputStream.Length);

            return outputStream;
        }
    }

    public bool CanGenerateThumbnail(string contentType)
    {
        return SupportedContentTypes.Contains(contentType);
    }
}
