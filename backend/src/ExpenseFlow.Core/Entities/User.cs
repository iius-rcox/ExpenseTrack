namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Represents an authenticated employee in the system.
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    /// Azure AD object ID from JWT 'oid' claim.
    /// </summary>
    public string EntraObjectId { get; set; } = string.Empty;

    /// <summary>
    /// Email from JWT 'preferred_username' claim.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Display name from JWT 'name' claim.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Department from JWT custom claim or manual entry.
    /// </summary>
    public string? Department { get; set; }

    /// <summary>
    /// Most recent authentication timestamp.
    /// </summary>
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<StatementFingerprint> StatementFingerprints { get; set; } = new List<StatementFingerprint>();

    /// <summary>
    /// User's application preferences (1:1 relationship, may be null if not yet created).
    /// </summary>
    public UserPreferences? Preferences { get; set; }
}
