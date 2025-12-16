namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Summary DTO for vendor alias information.
/// </summary>
public class VendorAliasSummaryDto
{
    /// <summary>
    /// Vendor alias ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Canonical vendor name.
    /// </summary>
    public string CanonicalName { get; set; } = string.Empty;

    /// <summary>
    /// Display name for UI.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Pattern to match in transaction descriptions.
    /// </summary>
    public string AliasPattern { get; set; } = string.Empty;

    /// <summary>
    /// Default GL code for this vendor.
    /// </summary>
    public string? DefaultGLCode { get; set; }

    /// <summary>
    /// Default department for this vendor.
    /// </summary>
    public string? DefaultDepartment { get; set; }

    /// <summary>
    /// Number of times this alias has matched.
    /// </summary>
    public int MatchCount { get; set; }
}
