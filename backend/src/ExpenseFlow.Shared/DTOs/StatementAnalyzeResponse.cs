namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Response from statement analysis endpoint.
/// </summary>
public class StatementAnalyzeResponse
{
    /// <summary>
    /// Temporary ID for this analysis session. Expires in 30 minutes.
    /// </summary>
    public Guid AnalysisId { get; set; }

    /// <summary>
    /// Original filename of uploaded statement.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Total number of data rows (excluding header).
    /// </summary>
    public int RowCount { get; set; }

    /// <summary>
    /// Column headers from the statement file.
    /// </summary>
    public List<string> Headers { get; set; } = new();

    /// <summary>
    /// First 5 data rows for preview.
    /// </summary>
    public List<List<string>> SampleRows { get; set; } = new();

    /// <summary>
    /// Available mapping options (may include both fingerprint and AI inference).
    /// </summary>
    public List<MappingOptionDto> MappingOptions { get; set; } = new();
}
