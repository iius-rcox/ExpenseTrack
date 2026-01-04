namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Records a user correction to an AI-extracted receipt field.
/// Used as training feedback for model improvement.
/// Retained indefinitely as permanent training corpus.
/// </summary>
public class ExtractionCorrection : BaseEntity
{
    /// <summary>FK to Receipt being corrected</summary>
    public Guid ReceiptId { get; set; }

    /// <summary>FK to User who made the correction</summary>
    public Guid UserId { get; set; }

    /// <summary>Name of the field that was corrected</summary>
    /// <example>vendor, amount, date, tax, currency, line_item</example>
    public string FieldName { get; set; } = null!;

    /// <summary>Original AI-extracted value (JSON-serialized)</summary>
    public string? OriginalValue { get; set; }

    /// <summary>User-corrected value (JSON-serialized)</summary>
    public string? CorrectedValue { get; set; }

    /// <summary>When the correction was submitted</summary>
    public new DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Receipt Receipt { get; set; } = null!;
    public User User { get; set; } = null!;
}
