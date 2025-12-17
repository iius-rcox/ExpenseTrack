namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Represents an Excel file export response.
/// </summary>
public class ExcelExportDto
{
    /// <summary>
    /// Suggested filename for the download (e.g., "2025-01-expense-report.xlsx").
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME content type for Excel files.
    /// </summary>
    public string ContentType { get; set; } = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <summary>
    /// Serialized Excel file contents.
    /// </summary>
    public byte[] FileContents { get; set; } = Array.Empty<byte>();
}
