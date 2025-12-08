using System.ComponentModel.DataAnnotations;

namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Request to import transactions from an analyzed statement.
/// </summary>
public class StatementImportRequest
{
    /// <summary>
    /// Analysis session ID from the analyze response.
    /// </summary>
    [Required]
    public Guid AnalysisId { get; set; }

    /// <summary>
    /// Confirmed column mapping to apply.
    /// </summary>
    [Required]
    public Dictionary<string, string> ColumnMapping { get; set; } = new();

    /// <summary>
    /// Date format pattern to use for parsing (e.g., "MM/dd/yyyy").
    /// </summary>
    public string? DateFormat { get; set; }

    /// <summary>
    /// Amount sign convention: "negative_charges" or "positive_charges".
    /// </summary>
    [Required]
    public string AmountSign { get; set; } = "negative_charges";

    /// <summary>
    /// Whether to save this mapping as a user fingerprint for future use.
    /// </summary>
    public bool SaveAsFingerprint { get; set; } = true;

    /// <summary>
    /// Custom name for the saved fingerprint (optional).
    /// </summary>
    public string? FingerprintName { get; set; }
}
