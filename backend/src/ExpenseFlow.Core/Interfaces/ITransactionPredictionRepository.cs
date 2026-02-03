using ExpenseFlow.Core.Entities;
using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Repository interface for TransactionPrediction entity operations.
/// </summary>
public interface ITransactionPredictionRepository
{
    /// <summary>
    /// Gets a prediction by ID for a specific user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="predictionId">Prediction ID.</param>
    /// <returns>Prediction if found, null otherwise.</returns>
    Task<TransactionPrediction?> GetByIdAsync(Guid userId, Guid predictionId);

    /// <summary>
    /// Gets a prediction by transaction ID for a specific user.
    /// </summary>
    /// <param name="userId">User ID for authorization.</param>
    /// <param name="transactionId">Transaction ID.</param>
    /// <returns>Prediction if found and owned by user, null otherwise.</returns>
    Task<TransactionPrediction?> GetByTransactionIdAsync(Guid userId, Guid transactionId);

    /// <summary>
    /// Gets paginated predictions for a user with optional filters.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="minConfidence">Optional minimum confidence level filter.</param>
    /// <returns>Tuple of predictions list and total count.</returns>
    Task<(List<TransactionPrediction> Predictions, int TotalCount)> GetPagedAsync(
        Guid userId,
        int page,
        int pageSize,
        PredictionStatus? status = null,
        PredictionConfidence? minConfidence = null);

    /// <summary>
    /// Gets pending predictions for a user, optionally filtered by confidence level.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="minConfidence">Minimum confidence level to include.</param>
    /// <returns>List of pending predictions.</returns>
    Task<List<TransactionPrediction>> GetPendingAsync(Guid userId, PredictionConfidence minConfidence = PredictionConfidence.Medium);

    /// <summary>
    /// Gets predictions by pattern ID.
    /// </summary>
    /// <param name="patternId">Pattern ID.</param>
    /// <returns>List of predictions for the pattern.</returns>
    Task<List<TransactionPrediction>> GetByPatternIdAsync(Guid patternId);

    /// <summary>
    /// Gets predictions for multiple transaction IDs for a specific user.
    /// </summary>
    /// <param name="userId">User ID for authorization.</param>
    /// <param name="transactionIds">Transaction IDs.</param>
    /// <returns>Dictionary mapping transaction ID to prediction (only those owned by user).</returns>
    Task<Dictionary<Guid, TransactionPrediction>> GetByTransactionIdsAsync(Guid userId, IEnumerable<Guid> transactionIds);

    /// <summary>
    /// Gets count of predictions by status for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <returns>Dictionary mapping status to count.</returns>
    Task<Dictionary<PredictionStatus, int>> GetStatusCountsAsync(Guid userId);

    /// <summary>
    /// Gets count of pending predictions by confidence level for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <returns>Dictionary mapping confidence level to count.</returns>
    Task<Dictionary<PredictionConfidence, int>> GetPendingConfidenceCountsAsync(Guid userId);

    /// <summary>
    /// Checks if a prediction exists for a transaction.
    /// </summary>
    /// <param name="transactionId">Transaction ID.</param>
    /// <returns>True if prediction exists, false otherwise.</returns>
    Task<bool> ExistsForTransactionAsync(Guid transactionId);

    /// <summary>
    /// Adds a new prediction.
    /// </summary>
    /// <param name="prediction">Prediction to add.</param>
    Task AddAsync(TransactionPrediction prediction);

    /// <summary>
    /// Adds multiple predictions.
    /// </summary>
    /// <param name="predictions">Predictions to add.</param>
    Task AddRangeAsync(IEnumerable<TransactionPrediction> predictions);

    /// <summary>
    /// Updates an existing prediction.
    /// </summary>
    /// <param name="prediction">Prediction to update.</param>
    Task UpdateAsync(TransactionPrediction prediction);

    /// <summary>
    /// Updates multiple predictions.
    /// </summary>
    /// <param name="predictions">Predictions to update.</param>
    Task UpdateRangeAsync(IEnumerable<TransactionPrediction> predictions);

    /// <summary>
    /// Deletes a prediction.
    /// </summary>
    /// <param name="prediction">Prediction to delete.</param>
    Task DeleteAsync(TransactionPrediction prediction);

    /// <summary>
    /// Saves all pending changes.
    /// </summary>
    Task SaveChangesAsync();
}
