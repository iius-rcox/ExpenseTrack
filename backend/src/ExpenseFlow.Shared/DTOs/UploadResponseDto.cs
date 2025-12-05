namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Response DTO for receipt upload operations.
/// </summary>
public class UploadResponseDto
{
    public List<ReceiptSummaryDto> Receipts { get; set; } = new();
    public int TotalUploaded { get; set; }
    public List<UploadFailureDto> Failed { get; set; } = new();
}

/// <summary>
/// DTO for failed upload information.
/// </summary>
public class UploadFailureDto
{
    public string Filename { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}
