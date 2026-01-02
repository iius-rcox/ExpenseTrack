using System.ComponentModel.DataAnnotations;

namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Request parameters for analytics data export.
/// </summary>
public class AnalyticsExportRequestDto
{
    /// <summary>Start date for export range (ISO format YYYY-MM-DD)</summary>
    [Required]
    public string StartDate { get; set; } = string.Empty;

    /// <summary>End date for export range (ISO format YYYY-MM-DD)</summary>
    [Required]
    public string EndDate { get; set; } = string.Empty;

    /// <summary>Export format: "csv" or "xlsx"</summary>
    [Required]
    [RegularExpression("^(csv|xlsx)$", ErrorMessage = "Format must be 'csv' or 'xlsx'")]
    public string Format { get; set; } = "csv";

    /// <summary>
    /// Sections to include (comma-separated): trends, categories, vendors, transactions.
    /// If empty, defaults to all aggregated summaries (trends, categories, vendors).
    /// </summary>
    public string? Sections { get; set; }

    /// <summary>
    /// Parses the Sections string into a list of section names.
    /// Returns default sections if none specified.
    /// </summary>
    public List<string> GetSectionsList()
    {
        if (string.IsNullOrWhiteSpace(Sections))
        {
            return new List<string> { "trends", "categories", "vendors" };
        }

        return Sections
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => s.ToLowerInvariant())
            .Where(s => IsValidSection(s))
            .ToList();
    }

    private static bool IsValidSection(string section)
    {
        return section is "trends" or "categories" or "vendors" or "transactions";
    }
}
