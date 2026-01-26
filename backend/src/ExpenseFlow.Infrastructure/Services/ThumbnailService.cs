using Docnet.Core;
using Docnet.Core.Models;
using ExpenseFlow.Core.Interfaces;
using ImageMagick;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Thumbnail generation service using Magick.NET for images and Docnet (PDFium) for PDFs.
/// Generates 200x200 JPEG thumbnails from receipt images and PDFs.
/// </summary>
/// <remarks>
/// PDF thumbnail generation uses Docnet.Core which wraps Google's PDFium library.
/// This provides reliable PDF rendering without requiring external Ghostscript installation.
/// Image thumbnail generation still uses Magick.NET for formats like HEIC/HEIF.
/// </remarks>
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

        // Read stream into memory
        using var inputStream = new MemoryStream();
        await imageStream.CopyToAsync(inputStream);
        var inputBytes = inputStream.ToArray();

        if (contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return await GeneratePdfThumbnailAsync(inputBytes, width, height);
        }
        else
        {
            return await GenerateImageThumbnailAsync(inputBytes, width, height);
        }
    }

    /// <summary>
    /// Generates a thumbnail from a PDF using PDFium (via Docnet).
    /// Renders the first page at 150 DPI and resizes to thumbnail dimensions.
    /// </summary>
    private async Task<Stream> GeneratePdfThumbnailAsync(byte[] pdfBytes, int width, int height)
    {
        _logger.LogDebug("Generating PDF thumbnail using Docnet (PDFium)");

        try
        {
            // Use PDFium to render the first page
            // PageDimensions uses a scaling factor - we'll get the actual dimensions from the page
            using var docReader = DocLib.Instance.GetDocReader(pdfBytes, new PageDimensions(1, 1));

            if (docReader.GetPageCount() == 0)
            {
                throw new InvalidOperationException("PDF has no pages");
            }

            using var pageReader = docReader.GetPageReader(0);

            // Get page dimensions at 150 DPI (native is 72 DPI)
            // Scale factor: 150/72 ≈ 2.08
            var scaleFactor = 150.0 / 72.0;
            var pageWidth = (int)(pageReader.GetPageWidth() * scaleFactor);
            var pageHeight = (int)(pageReader.GetPageHeight() * scaleFactor);

            _logger.LogDebug("PDF page dimensions at 150 DPI: {Width}x{Height}", pageWidth, pageHeight);

            // Re-read with correct dimensions for rendering
            using var docReaderScaled = DocLib.Instance.GetDocReader(
                pdfBytes,
                new PageDimensions(pageWidth, pageHeight));
            using var pageReaderScaled = docReaderScaled.GetPageReader(0);

            // Get raw BGRA pixel data
            var rawBytes = pageReaderScaled.GetImage();
            var renderWidth = pageReaderScaled.GetPageWidth();
            var renderHeight = pageReaderScaled.GetPageHeight();

            _logger.LogDebug("Rendered PDF page: {Width}x{Height}, {Bytes} bytes raw",
                renderWidth, renderHeight, rawBytes.Length);

            // Convert BGRA to RGBA for ImageSharp
            // PDFium returns BGRA, ImageSharp expects RGBA
            for (int i = 0; i < rawBytes.Length; i += 4)
            {
                // Swap B and R channels
                (rawBytes[i], rawBytes[i + 2]) = (rawBytes[i + 2], rawBytes[i]);
            }

            // Create ImageSharp image from raw pixel data
            using var image = Image.LoadPixelData<Rgba32>(rawBytes, renderWidth, renderHeight);

            // Resize to thumbnail dimensions
            image.Mutate(x => x
                .Resize(new ResizeOptions
                {
                    Size = new Size(width, height),
                    Mode = ResizeMode.Crop,
                    Position = AnchorPositionMode.TopLeft
                }));

            // Convert to JPEG
            var outputStream = new MemoryStream();
            await image.SaveAsync(outputStream, new JpegEncoder { Quality = 80 });
            outputStream.Position = 0;

            _logger.LogDebug("Generated PDF thumbnail: {Width}x{Height}, {Size} bytes",
                width, height, outputStream.Length);

            return outputStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate PDF thumbnail using Docnet. Error: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Generates a thumbnail from an image using Magick.NET.
    /// Supports JPEG, PNG, HEIC, HEIF and other image formats.
    /// </summary>
    private async Task<Stream> GenerateImageThumbnailAsync(byte[] imageBytes, int width, int height)
    {
        _logger.LogDebug("Generating image thumbnail using Magick.NET");

        using var inputStream = new MemoryStream(imageBytes);
        using var image = new MagickImage(inputStream);

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

        _logger.LogDebug("Generated image thumbnail: {Width}x{Height}, {Size} bytes",
            width, height, outputStream.Length);

        return outputStream;
    }

    public bool CanGenerateThumbnail(string contentType)
    {
        return SupportedContentTypes.Contains(contentType);
    }
}
