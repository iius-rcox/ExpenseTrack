using ExpenseFlow.Core.Interfaces;
using ImageMagick;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// HEIC to JPEG conversion service using Magick.NET.
/// Note: SkiaSharp does NOT support HEIC format - Magick.NET is required.
/// </summary>
public class HeicConversionService : IHeicConversionService
{
    private readonly ILogger<HeicConversionService> _logger;

    private static readonly HashSet<string> HeicContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/heic",
        "image/heif",
        "image/heic-sequence",
        "image/heif-sequence"
    };

    public HeicConversionService(ILogger<HeicConversionService> logger)
    {
        _logger = logger;
    }

    public async Task<Stream> ConvertToJpegAsync(Stream heicStream)
    {
        _logger.LogDebug("Starting HEIC to JPEG conversion");

        // Read the HEIC stream into a memory stream for ImageMagick
        using var inputStream = new MemoryStream();
        await heicStream.CopyToAsync(inputStream);
        inputStream.Position = 0;

        using var image = new MagickImage(inputStream);

        // Convert to JPEG format
        image.Format = MagickFormat.Jpeg;
        image.Quality = 85; // Good balance between quality and size

        var outputStream = new MemoryStream();
        await image.WriteAsync(outputStream);
        outputStream.Position = 0;

        _logger.LogInformation(
            "Converted HEIC image to JPEG. Original size: {OriginalSize} bytes, Converted size: {ConvertedSize} bytes",
            inputStream.Length,
            outputStream.Length);

        return outputStream;
    }

    public bool IsHeicFormat(string contentType)
    {
        return HeicContentTypes.Contains(contentType);
    }
}
