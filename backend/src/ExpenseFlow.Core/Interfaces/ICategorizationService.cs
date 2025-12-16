using ExpenseFlow.Shared.DTOs;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service interface for tiered expense categorization (GL code and department suggestions).
/// </summary>
public interface ICategorizationService
{
    /// <summary>
    /// Gets GL code suggestions for a transaction using tiered approach.
    /// Tier 1: Vendor alias default → Tier 2: Embedding similarity → Tier 3: AI inference.
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <param name="userId">The user ID for logging and filtering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>GL code suggestions with confidence and tier information.</returns>
    Task<GLSuggestionsDto> GetGLSuggestionsAsync(
        Guid transactionId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets department suggestions for a transaction using tiered approach.
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <param name="userId">The user ID for logging and filtering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Department suggestions with confidence and tier information.</returns>
    Task<DepartmentSuggestionsDto> GetDepartmentSuggestionsAsync(
        Guid transactionId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets combined GL and department suggestions for a transaction.
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <param name="userId">The user ID for logging and filtering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Combined categorization suggestions.</returns>
    Task<TransactionCategorizationDto> GetCategorizationAsync(
        Guid transactionId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms user's categorization selection, creating verified embedding and updating vendor alias.
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="glCode">Selected GL code.</param>
    /// <param name="departmentCode">Selected department code.</param>
    /// <param name="acceptedSuggestion">Whether user accepted the AI suggestion.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Confirmation result with learning feedback.</returns>
    Task<CategorizationConfirmationDto> ConfirmCategorizationAsync(
        Guid transactionId,
        Guid userId,
        string glCode,
        string departmentCode,
        bool acceptedSuggestion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Skips AI suggestion for manual categorization (graceful degradation).
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="reason">Skip reason (e.g., 'ai_unavailable', 'user_choice').</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Skip confirmation.</returns>
    Task<CategorizationSkipDto> SkipSuggestionAsync(
        Guid transactionId,
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default);
}
