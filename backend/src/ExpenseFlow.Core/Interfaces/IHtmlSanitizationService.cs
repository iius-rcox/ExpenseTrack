namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service for sanitizing HTML content for safe display in browsers.
/// Removes scripts, event handlers, and external resource references to prevent XSS attacks.
/// </summary>
public interface IHtmlSanitizationService
{
    /// <summary>
    /// Sanitizes HTML content for safe display in a browser.
    /// Removes scripts, event handlers, forms, iframes, and external resource references.
    /// Preserves inline styles and data URI images.
    /// </summary>
    /// <param name="htmlContent">Raw HTML content to sanitize</param>
    /// <returns>Sanitized HTML safe for browser rendering</returns>
    string Sanitize(string htmlContent);

    /// <summary>
    /// Extracts plain text from HTML content for AI processing.
    /// Strips all HTML tags, preserving only the text content.
    /// </summary>
    /// <param name="htmlContent">HTML content to extract text from</param>
    /// <returns>Plain text content without HTML markup</returns>
    string ExtractText(string htmlContent);
}
