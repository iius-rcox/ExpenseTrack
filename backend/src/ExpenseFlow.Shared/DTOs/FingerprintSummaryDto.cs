namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Summary view of a statement fingerprint.
/// </summary>
public class FingerprintSummaryDto
{
    /// <summary>
    /// Unique fingerprint identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Friendly name for the statement source (e.g., "Chase Business Card").
    /// </summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is a system-wide fingerprint (UserId is null).
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// Number of times this fingerprint was successfully used.
    /// </summary>
    public int HitCount { get; set; }

    /// <summary>
    /// Last time this fingerprint was used (nullable).
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// When the fingerprint was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response containing list of fingerprints.
/// </summary>
public class FingerprintListResponse
{
    /// <summary>
    /// List of available fingerprints.
    /// </summary>
    public List<FingerprintSummaryDto> Fingerprints { get; set; } = new();
}
