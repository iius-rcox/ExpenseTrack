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
            // For PDFs, read only the first page
            var settings = new MagickReadSettings
            {
                Density = new Density(150), // Good balance between quality and performance
                FrameIndex = 0,
                FrameCount = 1
            };
            image = new MagickImage(inputStream, settings);
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
