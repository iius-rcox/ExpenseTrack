using ExpenseFlow.Core.Entities;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service interface for expense prediction and pattern learning.
/// Learns from approved expense reports to predict which future transactions
/// are likely business expenses.
/// </summary>
public interface IExpensePredictionService
{
    #region Pattern Learning

    /// <summary>
    /// Learns expense patterns from an approved expense report.
    /// Extracts vendor, amount, and categorization data to build prediction models.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="reportId">Approved expense report ID.</param>
    /// <returns>Number of patterns created or updated.</returns>
    Task<int> LearnFromReportAsync(Guid userId, Guid reportId);

    /// <summary>
    /// Batch learns from multiple approved expense reports.
    /// Used for initial pattern building or catch-up processing.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="reportIds">List of approved expense report IDs.</param>
    /// <returns>Total number of patterns created or updated.</returns>
    Task<int> LearnFromReportsAsync(Guid userId, IEnumerable<Guid> reportIds);

    /// <summary>
    /// Rebuilds all patterns for a user from their historical expense reports.
    /// Includes Draft, Generated, and Submitted reports (all active reports).
    /// Excludes soft-deleted reports to prevent learning from removed/discarded expense data.
    /// Useful for recalculating patterns after configuration changes.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <returns>Total number of patterns rebuilt.</returns>
    Task<int> RebuildPatternsAsync(Guid userId);

    /// <summary>
    /// Imports expense patterns from external data (CSV, historical records).
    /// Creates or updates patterns based on the imported entries.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="request">Import request with expense entries.</param>
    /// <returns>Import result with counts.</returns>
    Task<ImportPatternsResponseDto> ImportPatternsAsync(Guid userId, ImportPatternsRequestDto request);

    /// <summary>
    /// Learns from a user's transaction classification (Business/Personal marking).
    /// Creates or updates a pattern for the vendor based on the classification.
    /// This enables learning from ALL transactions, not just expense reports.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="transactionId">Transaction that was classified.</param>
    /// <param name="isBusiness">True if classified as business, false if personal.</param>
    /// <returns>The pattern that was created or updated.</returns>
    Task<ExpensePattern?> LearnFromTransactionClassificationAsync(Guid userId, Guid transactionId, bool isBusiness);

    /// <summary>
    /// Backfills patterns from all historical transaction classifications.
    /// Scans existing Confirmed/Rejected predictions and learns patterns from them.
    /// Use this once to catch up on historical data after enabling pattern learning from classifications.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <returns>Tuple of (patternsCreated, patternsUpdated, classificationsProcessed).</returns>
    Task<(int PatternsCreated, int PatternsUpdated, int ClassificationsProcessed)> LearnFromHistoricalClassificationsAsync(Guid userId);

    #endregion

    #region Prediction Generation

    /// <summary>
    /// Generates predictions for new transactions based on learned patterns.
    /// Only generates predictions for transactions without existing predictions.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="transactionIds">Transaction IDs to evaluate.</param>
    /// <returns>Number of predictions generated.</returns>
    Task<int> GeneratePredictionsAsync(Guid userId, IEnumerable<Guid> transactionIds);

    /// <summary>
    /// Generates predictions for all unprocessed transactions for a user.
    /// Typically called after statement import.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <returns>Number of predictions generated.</returns>
    Task<int> GenerateAllPendingPredictionsAsync(Guid userId);

    /// <summary>
    /// Backfills transaction types based on learned vendor patterns.
    /// Auto-classifies transactions as Business or Personal based on ActiveClassification
    /// from ExpensePattern feedback history.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <returns>Tuple of (businessCount, personalCount) auto-classified.</returns>
    Task<(int BusinessCount, int PersonalCount)> BackfillTransactionTypesAsync(Guid userId);

    /// <summary>
    /// Gets a single prediction for a transaction if one exists and meets confidence threshold.
    /// </summary>
    /// <param name="transactionId">Transaction ID.</param>
    /// <returns>Prediction summary if available, null otherwise.</returns>
    Task<PredictionSummaryDto?> GetPredictionForTransactionAsync(Guid transactionId);

    /// <summary>
    /// Gets predictions for multiple transactions.
    /// </summary>
    /// <param name="transactionIds">Transaction IDs.</param>
    /// <returns>Dictionary mapping transaction ID to prediction summary.</returns>
    Task<Dictionary<Guid, PredictionSummaryDto>> GetPredictionsForTransactionsAsync(IEnumerable<Guid> transactionIds);

    #endregion

    #region Prediction Actions

    /// <summary>
    /// Confirms a prediction as correct.
    /// Updates prediction status and reinforces the pattern.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="request">Confirmation request with optional overrides.</param>
    /// <returns>Action result.</returns>
    Task<PredictionActionResponseDto> ConfirmPredictionAsync(Guid userId, ConfirmPredictionRequestDto request);

    /// <summary>
    /// Rejects a prediction as incorrect.
    /// Updates prediction status and weakens the pattern confidence.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="request">Rejection request.</param>
    /// <returns>Action result.</returns>
    Task<PredictionActionResponseDto> RejectPredictionAsync(Guid userId, RejectPredictionRequestDto request);

    /// <summary>
    /// Performs bulk confirmation or rejection of predictions.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="request">Bulk action request.</param>
    /// <returns>Bulk action result.</returns>
    Task<BulkPredictionActionResponseDto> BulkActionAsync(Guid userId, BulkPredictionActionRequestDto request);

    #endregion

    #region Manual Transaction Marking

    /// <summary>
    /// Manually marks a transaction as reimbursable.
    /// Creates a manual override prediction if no prediction exists,
    /// or updates existing prediction to Confirmed status.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="transactionId">Transaction ID to mark.</param>
    /// <returns>Action result with new status.</returns>
    Task<PredictionActionResponseDto> MarkTransactionReimbursableAsync(Guid userId, Guid transactionId);

    /// <summary>
    /// Manually marks a transaction as not reimbursable.
    /// Creates a manual override prediction if no prediction exists,
    /// or updates existing prediction to Rejected status.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="transactionId">Transaction ID to mark.</param>
    /// <returns>Action result with new status.</returns>
    Task<PredictionActionResponseDto> MarkTransactionNotReimbursableAsync(Guid userId, Guid transactionId);

    /// <summary>
    /// Clears manual reimbursability override for a transaction.
    /// Removes manual prediction, allowing auto-prediction on next generation cycle.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="transactionId">Transaction ID to clear override.</param>
    /// <returns>Action result.</returns>
    Task<PredictionActionResponseDto> ClearManualOverrideAsync(Guid userId, Guid transactionId);

    /// <summary>
    /// Bulk marks multiple transactions as reimbursable or not reimbursable.
    /// Creates manual override predictions for transactions without predictions,
    /// or updates existing predictions.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="request">Bulk reimbursability request with transaction IDs and action.</param>
    /// <returns>Bulk action result with success/failure counts.</returns>
    Task<BulkTransactionReimbursabilityResponseDto> BulkMarkTransactionsAsync(
        Guid userId,
        BulkTransactionReimbursabilityRequestDto request);

    #endregion

    #region Pattern Management

    /// <summary>
    /// Gets paginated list of patterns for a user with optional filtering and sorting.
    /// </summary>
    /// <param name="userId">User ID for row-level security.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of patterns per page.</param>
    /// <param name="includeSuppressed">Whether to include suppressed patterns.</param>
    /// <param name="suppressedOnly">Whether to show only suppressed patterns.</param>
    /// <param name="category">Optional category filter.</param>
    /// <param name="search">Optional search term for vendor name.</param>
    /// <param name="sortBy">Sort field: displayName, averageAmount, accuracyRate, occurrenceCount.</param>
    /// <param name="sortOrder">Sort direction: asc or desc.</param>
    Task<PatternListResponseDto> GetPatternsAsync(
        Guid userId,
        int page,
        int pageSize,
        bool includeSuppressed = false,
        bool suppressedOnly = false,
        string? category = null,
        string? search = null,
        string sortBy = "accuracyRate",
        string sortOrder = "desc");

    /// <summary>
    /// Gets pattern details by ID.
    /// </summary>
    Task<PatternDetailDto?> GetPatternAsync(Guid userId, Guid patternId);

    /// <summary>
    /// Updates pattern suppression status.
    /// Suppressed patterns do not generate new predictions.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="request">Suppression update request.</param>
    /// <returns>True if updated successfully, false if pattern not found.</returns>
    Task<bool> UpdatePatternSuppressionAsync(Guid userId, UpdatePatternSuppressionRequestDto request);

    /// <summary>
    /// Updates whether a pattern requires a receipt match for predictions.
    /// When enabled, predictions are only generated for transactions with confirmed receipt matches.
    /// Useful for mixed-use vendors like Amazon where most purchases are personal.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="request">Receipt match requirement update request.</param>
    /// <returns>True if updated successfully, false if pattern not found.</returns>
    Task<bool> UpdatePatternReceiptMatchAsync(Guid userId, UpdatePatternReceiptMatchRequestDto request);

    /// <summary>
    /// Deletes a pattern and all associated predictions.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="patternId">Pattern ID to delete.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeletePatternAsync(Guid userId, Guid patternId);

    /// <summary>
    /// Performs bulk actions on patterns (suppress, enable, delete).
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="request">Bulk action request with pattern IDs and action type.</param>
    /// <returns>Result with success/failure counts.</returns>
    Task<BulkPatternActionResponseDto> BulkPatternActionAsync(Guid userId, BulkPatternActionRequestDto request);

    #endregion

    #region Prediction Queries

    /// <summary>
    /// Gets paginated list of predictions for a user.
    /// </summary>
    Task<PredictionListResponseDto> GetPredictionsAsync(
        Guid userId,
        int page,
        int pageSize,
        PredictionStatus? status = null,
        PredictionConfidence? minConfidence = null);

    /// <summary>
    /// Gets prediction details by ID.
    /// </summary>
    Task<PredictionDetailDto?> GetPredictionAsync(Guid userId, Guid predictionId);

    /// <summary>
    /// Gets dashboard summary for expense predictions.
    /// </summary>
    Task<PredictionDashboardDto> GetDashboardAsync(Guid userId);

    /// <summary>
    /// Gets accuracy statistics for predictions.
    /// </summary>
    Task<PredictionAccuracyStatsDto> GetAccuracyStatsAsync(Guid userId);

    /// <summary>
    /// Checks if predictions are available for a user (has learned patterns).
    /// </summary>
    Task<PredictionAvailabilityDto> CheckAvailabilityAsync(Guid userId);

    #endregion

    #region Transaction Enrichment

    /// <summary>
    /// Enriches transactions with prediction data for display.
    /// Used to add prediction badges to transaction lists.
    /// </summary>
    /// <param name="transactions">Transaction summaries to enrich.</param>
    /// <returns>Transactions with prediction data attached.</returns>
    Task<List<PredictionTransactionDto>> EnrichWithPredictionsAsync(IEnumerable<TransactionSummaryDto> transactions);

    #endregion

    #region Draft Pre-Population (User Story 2)

    /// <summary>
    /// Gets transactions with high-confidence predictions for a date range.
    /// Used to pre-populate expense report drafts with likely business expenses.
    /// Only returns transactions with High confidence predictions in Pending status.
    /// </summary>
    /// <param name="userId">User ID for row-level security.</param>
    /// <param name="startDate">Start of date range (inclusive).</param>
    /// <param name="endDate">End of date range (inclusive).</param>
    /// <returns>List of transaction IDs with their prediction details, sorted by confidence descending.</returns>
    Task<List<PredictedTransactionDto>> GetPredictedTransactionsForPeriodAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate);

    #endregion
}
