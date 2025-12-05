namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service for generating receipt thumbnails.
/// </summary>
public interface IThumbnailService
{
    /// <summary>
    /// Generates a thumbnail from an image stream.
    /// </summary>
    /// <param name="imageStream">Original image stream</param>
    /// <param name="contentType">MIME type of the original image</param>
    /// <param name="width">Target width (default 200)</param>
    /// <param name="height">Target height (default 200)</param>
    /// <returns>Stream containing JPEG thumbnail data</returns>
    Task<Stream> GenerateThumbnailAsync(Stream imageStream, string contentType, int width = 200, int height = 200);

    /// <summary>
    /// Checks if a thumbnail can be generated for the given content type.
    /// </summary>
    bool CanGenerateThumbnail(string contentType);
}
