namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Interface for the receipt processing background job.
/// Used by Hangfire to queue and execute receipt processing.
/// </summary>
public interface IReceiptProcessingJob
{
    /// <summary>
    /// Processes a receipt: downloads from blob, extracts data via Document Intelligence,
    /// and updates the receipt entity with results.
    /// </summary>
    /// <param name="receiptId">The ID of the receipt to process</param>
    Task ProcessAsync(Guid receiptId);
}
