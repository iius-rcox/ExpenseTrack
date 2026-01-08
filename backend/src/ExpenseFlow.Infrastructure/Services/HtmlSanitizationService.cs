using ExpenseFlow.Core.Interfaces;
using Ganss.Xss;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// HTML sanitization service using HtmlSanitizer library.
/// Removes scripts, event handlers, forms, and external resources to prevent XSS attacks.
/// </summary>
public class HtmlSanitizationService : IHtmlSanitizationService
{
    private readonly ILogger<HtmlSanitizationService> _logger;
    private readonly HtmlSanitizer _sanitizer;

    /// <summary>
    /// Allowed HTML tags for receipt display.
    /// </summary>
    private static readonly string[] AllowedTags =
    {
        "div", "span", "p", "br", "hr",
        "table", "tr", "td", "th", "thead", "tbody", "tfoot", "caption", "colgroup", "col",
        "img", "a",
        "b", "strong", "i", "em", "u", "s", "strike", "sub", "sup",
        "h1", "h2", "h3", "h4", "h5", "h6",
        "ul", "ol", "li", "dl", "dt", "dd",
        "blockquote", "pre", "code",
        "center", "font"
    };

    /// <summary>
    /// Allowed HTML attributes for receipt display.
    /// </summary>
    private static readonly string[] AllowedAttributes =
    {
        "class", "style", "id",
        "src", "alt", "title", "width", "height",
        "href", "target",
        "border", "cellpadding", "cellspacing", "align", "valign",
        "colspan", "rowspan",
        "color", "size", "face"
    };

    /// <summary>
    /// Allowed URL schemes for src and href attributes.
    /// </summary>
    private static readonly string[] AllowedSchemes = { "http", "https", "data" };

    public HtmlSanitizationService(ILogger<HtmlSanitizationService> logger)
    {
        _logger = logger;
        _sanitizer = CreateSanitizer();
    }

    private HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = new HtmlSanitizer();

        // Clear defaults and set allowed tags
        sanitizer.AllowedTags.Clear();
        foreach (var tag in AllowedTags)
        {
            sanitizer.AllowedTags.Add(tag);
        }

        // Set allowed attributes
        sanitizer.AllowedAttributes.Clear();
        foreach (var attr in AllowedAttributes)
        {
            sanitizer.AllowedAttributes.Add(attr);
        }

        // Set allowed URL schemes (allows data URIs for inline images)
        sanitizer.AllowedSchemes.Clear();
        foreach (var scheme in AllowedSchemes)
        {
            sanitizer.AllowedSchemes.Add(scheme);
        }

        // Allow inline styles (needed for email formatting)
        sanitizer.AllowedCssProperties.Clear();
        // Add common CSS properties used in email receipts
        var allowedCss = new[]
        {
            "color", "background-color", "background",
            "font-family", "font-size", "font-weight", "font-style", "text-decoration",
            "text-align", "vertical-align", "line-height", "letter-spacing",
            "width", "height", "max-width", "max-height", "min-width", "min-height",
            "margin", "margin-top", "margin-right", "margin-bottom", "margin-left",
            "padding", "padding-top", "padding-right", "padding-bottom", "padding-left",
            "border", "border-width", "border-style", "border-color",
            "border-top", "border-right", "border-bottom", "border-left",
            "border-radius", "border-collapse", "border-spacing",
            "display", "float", "clear", "position",
            "top", "right", "bottom", "left",
            "overflow", "white-space", "word-wrap", "word-break"
        };
        foreach (var css in allowedCss)
        {
            sanitizer.AllowedCssProperties.Add(css);
        }

        // Log removed content for security monitoring
        sanitizer.RemovingTag += (sender, args) =>
        {
            _logger.LogDebug("Sanitizer removing tag: {Tag}", args.Tag.TagName);
        };

        sanitizer.RemovingAttribute += (sender, args) =>
        {
            _logger.LogDebug("Sanitizer removing attribute: {Attribute} from {Tag}",
                args.Attribute.Name, args.Tag.TagName);
        };

        return sanitizer;
    }

    /// <inheritdoc />
    public string Sanitize(string htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            return string.Empty;
        }

        _logger.LogDebug("Sanitizing HTML content ({Length} chars)", htmlContent.Length);

        try
        {
            var sanitized = _sanitizer.Sanitize(htmlContent);

            _logger.LogDebug("HTML sanitization complete. Original: {OriginalLength} chars, Sanitized: {SanitizedLength} chars",
                htmlContent.Length, sanitized.Length);

            return sanitized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sanitizing HTML content");
            // Return empty string on error to prevent any XSS
            return string.Empty;
        }
    }

    /// <inheritdoc />
    public string ExtractText(string htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            return string.Empty;
        }

        _logger.LogDebug("Extracting text from HTML ({Length} chars)", htmlContent.Length);

        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            // Remove script and style elements before extracting text
            var nodesToRemove = doc.DocumentNode
                .SelectNodes("//script|//style|//head|//noscript|//comment()");

            if (nodesToRemove != null)
            {
                foreach (var node in nodesToRemove)
                {
                    node.Remove();
                }
            }

            // Extract inner text
            var text = doc.DocumentNode.InnerText;

            // Clean up whitespace - normalize multiple spaces/newlines to single space
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

            _logger.LogDebug("Text extraction complete. HTML: {HtmlLength} chars, Text: {TextLength} chars",
                htmlContent.Length, text.Length);

            return text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from HTML");
            return string.Empty;
        }
    }
}
