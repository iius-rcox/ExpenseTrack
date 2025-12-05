namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service for converting HEIC images to JPEG format.
/// </summary>
public interface IHeicConversionService
{
    /// <summary>
    /// Converts a HEIC image stream to JPEG format.
    /// </summary>
    /// <param name="heicStream">Input stream containing HEIC image data</param>
    /// <returns>Stream containing JPEG image data</returns>
    Task<Stream> ConvertToJpegAsync(Stream heicStream);

    /// <summary>
    /// Checks if the given content type is HEIC/HEIF format.
    /// </summary>
    /// <param name="contentType">MIME type to check</param>
    /// <returns>True if the content type is HEIC/HEIF</returns>
    bool IsHeicFormat(string contentType);
}
