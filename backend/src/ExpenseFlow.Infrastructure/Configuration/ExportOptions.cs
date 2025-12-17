namespace ExpenseFlow.Infrastructure.Configuration;

/// <summary>
/// Configuration options for export functionality.
/// </summary>
public class ExportOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Export";

    /// <summary>
    /// Blob storage container name for templates.
    /// </summary>
    public string TemplateBlobContainer { get; set; } = "templates";

    /// <summary>
    /// File name of the Excel template.
    /// </summary>
    public string TemplateFileName { get; set; } = "expense-report-template.xlsx";

    /// <summary>
    /// Maximum number of receipts allowed in a single PDF.
    /// </summary>
    public int MaxReceiptsPerPdf { get; set; } = 100;

    /// <summary>
    /// Date format for Excel export (e.g., "MM/dd/yy").
    /// </summary>
    public string DateFormat { get; set; } = "MM/dd/yy";
}
