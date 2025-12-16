namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Combined GL and department suggestions for a transaction.
/// </summary>
public class TransactionCategorizationDto
{
    /// <summary>
    /// Transaction ID.
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Normalized description text.
    /// </summary>
    public string NormalizedDescription { get; set; } = string.Empty;

    /// <summary>
    /// Extracted vendor name.
    /// </summary>
    public string Vendor { get; set; } = string.Empty;

    /// <summary>
    /// GL code suggestions.
    /// </summary>
    public GLCategorizationSection GL { get; set; } = new();

    /// <summary>
    /// Department suggestions.
    /// </summary>
    public DepartmentCategorizationSection Department { get; set; } = new();
}

/// <summary>
/// GL categorization section with top suggestion and alternatives.
/// </summary>
public class GLCategorizationSection
{
    /// <summary>
    /// Top GL suggestion (highest confidence).
    /// </summary>
    public CategorizationSuggestionDto? TopSuggestion { get; set; }

    /// <summary>
    /// Alternative GL suggestions.
    /// </summary>
    public List<CategorizationSuggestionDto> Alternatives { get; set; } = new();
}

/// <summary>
/// Department categorization section with top suggestion and alternatives.
/// </summary>
public class DepartmentCategorizationSection
{
    /// <summary>
    /// Top department suggestion (highest confidence).
    /// </summary>
    public CategorizationSuggestionDto? TopSuggestion { get; set; }

    /// <summary>
    /// Alternative department suggestions.
    /// </summary>
    public List<CategorizationSuggestionDto> Alternatives { get; set; } = new();
}

/// <summary>
/// Confirmation response after user accepts/modifies categorization.
/// </summary>
public class CategorizationConfirmationDto
{
    /// <summary>
    /// Transaction ID.
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Confirmed GL code.
    /// </summary>
    public string GLCode { get; set; } = string.Empty;

    /// <summary>
    /// Confirmed department code.
    /// </summary>
    public string DepartmentCode { get; set; } = string.Empty;

    /// <summary>
    /// Whether a verified embedding was created.
    /// </summary>
    public bool EmbeddingCreated { get; set; }

    /// <summary>
    /// Whether vendor alias default was updated.
    /// </summary>
    public bool VendorAliasUpdated { get; set; }

    /// <summary>
    /// Message about vendor alias update (if applicable).
    /// </summary>
    public string? VendorAliasMessage { get; set; }

    /// <summary>
    /// Confirmation message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response when user skips AI suggestion for manual categorization.
/// </summary>
public class CategorizationSkipDto
{
    /// <summary>
    /// Transaction ID.
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Whether skip was successful.
    /// </summary>
    public bool Skipped { get; set; }

    /// <summary>
    /// Message to user.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Request to confirm categorization.
/// </summary>
public class CategorizationConfirmRequest
{
    /// <summary>
    /// Selected GL code.
    /// </summary>
    public string GLCode { get; set; } = string.Empty;

    /// <summary>
    /// Selected department code.
    /// </summary>
    public string DepartmentCode { get; set; } = string.Empty;

    /// <summary>
    /// Whether user accepted the AI suggestion.
    /// </summary>
    public bool AcceptedSuggestion { get; set; }
}

/// <summary>
/// Request to skip AI suggestion.
/// </summary>
public class CategorizationSkipRequest
{
    /// <summary>
    /// Reason for skipping ('ai_unavailable', 'user_choice').
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
