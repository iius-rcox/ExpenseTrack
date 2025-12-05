namespace ExpenseFlow.Shared.Enums;

/// <summary>
/// Processing status for uploaded receipts.
/// </summary>
public enum ReceiptStatus
{
    /// <summary>File uploaded, awaiting processing</summary>
    Uploaded,

    /// <summary>Document Intelligence extraction in progress</summary>
    Processing,

    /// <summary>Extraction complete, confidence >= 60%</summary>
    Ready,

    /// <summary>Extraction complete, confidence &lt; 60%</summary>
    ReviewRequired,

    /// <summary>Extraction failed after retries</summary>
    Error,

    /// <summary>Ready but not yet matched to transaction (Sprint 5)</summary>
    Unmatched,

    /// <summary>Matched to transaction (Sprint 5)</summary>
    Matched
}
