using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Summary DTO for expense pattern list views.
/// </summary>
public class PatternSummaryDto
{
    /// <summary>Pattern ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Human-readable vendor name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Most common category for this vendor.</summary>
    public string? Category { get; set; }

    /// <summary>Running average of expense amounts.</summary>
    public decimal AverageAmount { get; set; }

    /// <summary>Number of times this vendor appeared in reports.</summary>
    public int OccurrenceCount { get; set; }

    /// <summary>Most recent occurrence date.</summary>
    public DateTime LastSeenAt { get; set; }

    /// <summary>True if user suppressed predictions for this vendor.</summary>
    public bool IsSuppressed { get; set; }

    /// <summary>When true, predictions only generated for transactions with confirmed receipt matches.</summary>
    public bool RequiresReceiptMatch { get; set; }

    /// <summary>Calculated accuracy from confirm/reject ratio.</summary>
    public decimal AccuracyRate { get; set; }
}

/// <summary>
/// Detail DTO for individual expense pattern views.
/// </summary>
public class PatternDetailDto
{
    /// <summary>Pattern ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Normalized vendor name (system identifier).</summary>
    public string NormalizedVendor { get; set; } = string.Empty;

    /// <summary>Human-readable vendor name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Most common category for this vendor.</summary>
    public string? Category { get; set; }

    /// <summary>Running average of expense amounts.</summary>
    public decimal AverageAmount { get; set; }

    /// <summary>Minimum amount seen.</summary>
    public decimal MinAmount { get; set; }

    /// <summary>Maximum amount seen.</summary>
    public decimal MaxAmount { get; set; }

    /// <summary>Number of times this vendor appeared in reports.</summary>
    public int OccurrenceCount { get; set; }

    /// <summary>Most recent occurrence date.</summary>
    public DateTime LastSeenAt { get; set; }

    /// <summary>Default GL code for this vendor.</summary>
    public string? DefaultGLCode { get; set; }

    /// <summary>Default department for this vendor.</summary>
    public string? DefaultDepartment { get; set; }

    /// <summary>Number of confirmed predictions.</summary>
    public int ConfirmCount { get; set; }

    /// <summary>Number of rejected predictions.</summary>
    public int RejectCount { get; set; }

    /// <summary>True if user suppressed predictions for this vendor.</summary>
    public bool IsSuppressed { get; set; }

    /// <summary>When true, predictions only generated for transactions with confirmed receipt matches.</summary>
    public bool RequiresReceiptMatch { get; set; }

    /// <summary>Accuracy rate (ConfirmCount / (ConfirmCount + RejectCount)).</summary>
    public decimal AccuracyRate { get; set; }

    /// <summary>Pattern creation date.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Last update date.</summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Summary DTO for transaction prediction badge display.
/// </summary>
public class PredictionSummaryDto
{
    /// <summary>Prediction ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Transaction ID this prediction applies to.</summary>
    public Guid TransactionId { get; set; }

    /// <summary>Pattern that generated this prediction.</summary>
    public Guid PatternId { get; set; }

    /// <summary>Vendor display name from pattern.</summary>
    public string VendorName { get; set; } = string.Empty;

    /// <summary>Confidence score (0.00 - 1.00).</summary>
    public decimal ConfidenceScore { get; set; }

    /// <summary>Confidence level (High, Medium).</summary>
    public PredictionConfidence ConfidenceLevel { get; set; }

    /// <summary>Prediction status.</summary>
    public PredictionStatus Status { get; set; }

    /// <summary>Suggested category from pattern.</summary>
    public string? SuggestedCategory { get; set; }

    /// <summary>Suggested GL code from pattern.</summary>
    public string? SuggestedGLCode { get; set; }
}

/// <summary>
/// Detail DTO for full prediction information.
/// </summary>
public class PredictionDetailDto
{
    /// <summary>Prediction ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Transaction ID this prediction applies to.</summary>
    public Guid TransactionId { get; set; }

    /// <summary>Pattern that generated this prediction.</summary>
    public Guid PatternId { get; set; }

    /// <summary>Vendor display name from pattern.</summary>
    public string VendorName { get; set; } = string.Empty;

    /// <summary>Confidence score (0.00 - 1.00).</summary>
    public decimal ConfidenceScore { get; set; }

    /// <summary>Confidence level (High, Medium).</summary>
    public PredictionConfidence ConfidenceLevel { get; set; }

    /// <summary>Prediction status.</summary>
    public PredictionStatus Status { get; set; }

    /// <summary>Suggested category from pattern.</summary>
    public string? SuggestedCategory { get; set; }

    /// <summary>Suggested GL code from pattern.</summary>
    public string? SuggestedGLCode { get; set; }

    /// <summary>Suggested department from pattern.</summary>
    public string? SuggestedDepartment { get; set; }

    /// <summary>Pattern's average amount for comparison.</summary>
    public decimal PatternAverageAmount { get; set; }

    /// <summary>Number of occurrences supporting this pattern.</summary>
    public int PatternOccurrenceCount { get; set; }

    /// <summary>When the prediction was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the user acted on the prediction (nullable).</summary>
    public DateTime? ResolvedAt { get; set; }
}

/// <summary>
/// Minimal transaction summary for prediction context.
/// </summary>
public class PredictionTransactionDto
{
    /// <summary>Transaction ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Transaction date.</summary>
    public DateOnly TransactionDate { get; set; }

    /// <summary>Transaction description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Transaction amount.</summary>
    public decimal Amount { get; set; }

    /// <summary>Whether transaction has a matched receipt.</summary>
    public bool HasMatchedReceipt { get; set; }

    /// <summary>Prediction summary if available.</summary>
    public PredictionSummaryDto? Prediction { get; set; }
}

/// <summary>
/// Request DTO for confirming a prediction.
/// </summary>
public class ConfirmPredictionRequestDto
{
    /// <summary>Prediction ID to confirm.</summary>
    public Guid PredictionId { get; set; }

    /// <summary>Optional override for GL code (uses pattern default if null).</summary>
    public string? GLCodeOverride { get; set; }

    /// <summary>Optional override for department (uses pattern default if null).</summary>
    public string? DepartmentOverride { get; set; }
}

/// <summary>
/// Request DTO for rejecting a prediction.
/// </summary>
public class RejectPredictionRequestDto
{
    /// <summary>Prediction ID to reject.</summary>
    public Guid PredictionId { get; set; }
}

/// <summary>
/// Request DTO for bulk prediction actions.
/// </summary>
public class BulkPredictionActionRequestDto
{
    /// <summary>List of prediction IDs to act on.</summary>
    public List<Guid> PredictionIds { get; set; } = new();

    /// <summary>Action to perform: Confirm or Reject.</summary>
    public FeedbackType Action { get; set; }
}

/// <summary>
/// Request DTO for updating pattern suppression.
/// </summary>
public class UpdatePatternSuppressionRequestDto
{
    /// <summary>Pattern ID to update.</summary>
    public Guid PatternId { get; set; }

    /// <summary>Whether to suppress predictions for this pattern.</summary>
    public bool IsSuppressed { get; set; }
}

/// <summary>
/// Request DTO for updating pattern receipt match requirement.
/// </summary>
public class UpdatePatternReceiptMatchRequestDto
{
    /// <summary>Pattern ID to update.</summary>
    public Guid PatternId { get; set; }

    /// <summary>Whether to require receipt matches for predictions from this pattern.</summary>
    public bool RequiresReceiptMatch { get; set; }
}

/// <summary>
/// Request DTO for bulk pattern actions (suppress, enable, delete).
/// </summary>
public class BulkPatternActionRequestDto
{
    /// <summary>List of pattern IDs to act on.</summary>
    public List<Guid> PatternIds { get; set; } = new();

    /// <summary>Action to perform: suppress, enable, or delete.</summary>
    public string Action { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for bulk pattern actions.
/// </summary>
public class BulkPatternActionResponseDto
{
    /// <summary>Number of patterns successfully updated.</summary>
    public int SuccessCount { get; set; }

    /// <summary>Number of patterns that failed to update.</summary>
    public int FailedCount { get; set; }

    /// <summary>IDs of patterns that failed (if any).</summary>
    public List<Guid> FailedIds { get; set; } = new();

    /// <summary>Summary message.</summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Paginated response for predictions list.
/// </summary>
public class PredictionListResponseDto
{
    /// <summary>List of predictions.</summary>
    public List<PredictionSummaryDto> Predictions { get; set; } = new();

    /// <summary>Total count matching filters.</summary>
    public int TotalCount { get; set; }

    /// <summary>Current page number.</summary>
    public int Page { get; set; }

    /// <summary>Page size.</summary>
    public int PageSize { get; set; }

    /// <summary>Count of pending predictions.</summary>
    public int PendingCount { get; set; }

    /// <summary>Count of high confidence predictions.</summary>
    public int HighConfidenceCount { get; set; }
}

/// <summary>
/// Paginated response for patterns list.
/// </summary>
public class PatternListResponseDto
{
    /// <summary>List of patterns.</summary>
    public List<PatternSummaryDto> Patterns { get; set; } = new();

    /// <summary>Total count matching filters.</summary>
    public int TotalCount { get; set; }

    /// <summary>Current page number.</summary>
    public int Page { get; set; }

    /// <summary>Page size.</summary>
    public int PageSize { get; set; }

    /// <summary>Count of active (non-suppressed) patterns.</summary>
    public int ActiveCount { get; set; }

    /// <summary>Count of suppressed patterns.</summary>
    public int SuppressedCount { get; set; }
}

/// <summary>
/// Response DTO for prediction action results.
/// </summary>
public class PredictionActionResponseDto
{
    /// <summary>Whether the action succeeded.</summary>
    public bool Success { get; set; }

    /// <summary>Updated prediction status.</summary>
    public PredictionStatus NewStatus { get; set; }

    /// <summary>Message describing the result.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// True if the pattern was auto-suppressed due to low accuracy.
    /// Only set on reject actions when pattern hits threshold (>3 rejects, &lt;30% confirm rate).
    /// </summary>
    public bool PatternSuppressed { get; set; }
}

/// <summary>
/// Response DTO for bulk prediction actions.
/// </summary>
public class BulkPredictionActionResponseDto
{
    /// <summary>Number of predictions successfully updated.</summary>
    public int SuccessCount { get; set; }

    /// <summary>Number of predictions that failed to update.</summary>
    public int FailedCount { get; set; }

    /// <summary>IDs of predictions that failed (if any).</summary>
    public List<Guid> FailedIds { get; set; } = new();

    /// <summary>Summary message.</summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Dashboard summary for expense predictions.
/// </summary>
public class PredictionDashboardDto
{
    /// <summary>Total pending predictions (Medium + High confidence only).</summary>
    public int PendingCount { get; set; }

    /// <summary>High confidence pending predictions.</summary>
    public int HighConfidenceCount { get; set; }

    /// <summary>Medium confidence pending predictions.</summary>
    public int MediumConfidenceCount { get; set; }

    /// <summary>Total active patterns.</summary>
    public int ActivePatternCount { get; set; }

    /// <summary>Overall prediction accuracy (confirm rate).</summary>
    public decimal OverallAccuracyRate { get; set; }

    /// <summary>Top predicted transactions for quick action.</summary>
    public List<PredictionTransactionDto> TopPredictions { get; set; } = new();
}

/// <summary>
/// Prediction accuracy statistics.
/// </summary>
public class PredictionAccuracyStatsDto
{
    /// <summary>Total predictions made.</summary>
    public int TotalPredictions { get; set; }

    /// <summary>Total confirmed predictions.</summary>
    public int ConfirmedCount { get; set; }

    /// <summary>Total rejected predictions.</summary>
    public int RejectedCount { get; set; }

    /// <summary>Total ignored predictions.</summary>
    public int IgnoredCount { get; set; }

    /// <summary>Overall accuracy rate (confirmed / (confirmed + rejected)).</summary>
    public decimal AccuracyRate { get; set; }

    /// <summary>High confidence accuracy rate.</summary>
    public decimal HighConfidenceAccuracyRate { get; set; }

    /// <summary>Medium confidence accuracy rate.</summary>
    public decimal MediumConfidenceAccuracyRate { get; set; }
}

/// <summary>
/// Response DTO for prediction availability check.
/// </summary>
public class PredictionAvailabilityDto
{
    /// <summary>True if user has at least one expense pattern.</summary>
    public bool IsAvailable { get; set; }

    /// <summary>Number of learned patterns.</summary>
    public int PatternCount { get; set; }

    /// <summary>User-friendly message explaining availability status.</summary>
    public string Message { get; set; } = string.Empty;
}

#region Pattern Import

/// <summary>
/// Request DTO for importing a single expense pattern.
/// </summary>
public class ImportPatternDto
{
    /// <summary>Vendor name (raw, will be normalized).</summary>
    public string Vendor { get; set; } = string.Empty;

    /// <summary>Human-readable display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Category for this vendor's expenses.</summary>
    public string? Category { get; set; }

    /// <summary>Expense amount for this occurrence.</summary>
    public decimal Amount { get; set; }

    /// <summary>GL code for this expense.</summary>
    public string? GLCode { get; set; }

    /// <summary>Department for this expense.</summary>
    public string? Department { get; set; }

    /// <summary>Date of this expense occurrence.</summary>
    public DateTime Date { get; set; }
}

/// <summary>
/// Request DTO for bulk importing expense patterns.
/// </summary>
public class ImportPatternsRequestDto
{
    /// <summary>List of expense entries to learn patterns from.</summary>
    public List<ImportPatternDto> Entries { get; set; } = new();
}

/// <summary>
/// Response DTO for pattern import results.
/// </summary>
public class ImportPatternsResponseDto
{
    /// <summary>Number of patterns created.</summary>
    public int CreatedCount { get; set; }

    /// <summary>Number of patterns updated.</summary>
    public int UpdatedCount { get; set; }

    /// <summary>Total entries processed.</summary>
    public int TotalProcessed { get; set; }

    /// <summary>Summary message.</summary>
    public string Message { get; set; } = string.Empty;
}

#endregion

#region User Story 2 - Draft Pre-Population

/// <summary>
/// Transaction with prediction data for draft pre-population.
/// Used by GetPredictedTransactionsForPeriodAsync to identify
/// transactions that should be auto-selected when creating a draft report.
/// </summary>
public class PredictedTransactionDto
{
    /// <summary>Transaction ID.</summary>
    public Guid TransactionId { get; set; }

    /// <summary>Transaction date.</summary>
    public DateOnly TransactionDate { get; set; }

    /// <summary>Transaction description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Transaction amount.</summary>
    public decimal Amount { get; set; }

    /// <summary>Prediction ID.</summary>
    public Guid PredictionId { get; set; }

    /// <summary>Pattern ID that generated this prediction.</summary>
    public Guid PatternId { get; set; }

    /// <summary>Vendor display name from pattern.</summary>
    public string VendorName { get; set; } = string.Empty;

    /// <summary>Confidence score (0.00 - 1.00).</summary>
    public decimal ConfidenceScore { get; set; }

    /// <summary>Confidence level (High, Medium).</summary>
    public PredictionConfidence ConfidenceLevel { get; set; }

    /// <summary>Suggested category from pattern.</summary>
    public string? SuggestedCategory { get; set; }

    /// <summary>Suggested GL code from pattern.</summary>
    public string? SuggestedGLCode { get; set; }

    /// <summary>Suggested department from pattern.</summary>
    public string? SuggestedDepartment { get; set; }

    /// <summary>Whether transaction has a matched receipt.</summary>
    public bool HasMatchedReceipt { get; set; }
}

#endregion
