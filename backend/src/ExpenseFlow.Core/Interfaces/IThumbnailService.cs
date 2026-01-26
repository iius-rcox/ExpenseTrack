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
    /// <param name="width">Target width (default 800 for crisp PDF display)</param>
    /// <param name="height">Target height (default 800 for crisp PDF display)</param>
    /// <returns>Stream containing JPEG thumbnail data</returns>
    Task<Stream> GenerateThumbnailAsync(Stream imageStream, string contentType, int width = 800, int height = 800);

    /// <summary>
    /// Checks if a thumbnail can be generated for the given content type.
    /// </summary>
    bool CanGenerateThumbnail(string contentType);
}
