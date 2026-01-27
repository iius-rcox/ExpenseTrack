using ExpenseFlow.Shared.DTOs;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service for managing thumbnail backfill operations.
/// Generates thumbnails for historical receipts that are missing them.
/// </summary>
public interface IThumbnailBackfillService
{
    /// <summary>
    /// Starts a backfill job to generate thumbnails for receipts without them.
    /// </summary>
    /// <param name="request">Backfill configuration</param>
    /// <returns>Response with job ID and estimated count</returns>
    Task<ThumbnailBackfillResponse> StartBackfillAsync(ThumbnailBackfillRequest request);

    /// <summary>
    /// Gets the current status of the backfill job.
    /// </summary>
    /// <returns>Current status including progress counts</returns>
    Task<ThumbnailBackfillStatus> GetStatusAsync();

    /// <summary>
    /// Regenerates the thumbnail for a specific receipt.
    /// </summary>
    /// <param name="receiptId">Receipt ID</param>
    /// <param name="userId">User ID for authorization</param>
    /// <returns>Response with job ID</returns>
    Task<ThumbnailRegenerationResponse> RegenerateThumbnailAsync(Guid receiptId, Guid userId);
}
