namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Represents a line item extracted from a receipt.
/// Stored as JSONB within Receipt.LineItems.
/// </summary>
public class ReceiptLineItem
{
    /// <summary>Item description (e.g., "Coffee Large")</summary>
    public string Description { get; set; } = null!;

    /// <summary>Quantity of items</summary>
    public decimal? Quantity { get; set; }

    /// <summary>Price per unit</summary>
    public decimal? UnitPrice { get; set; }

    /// <summary>Total price for this line item</summary>
    public decimal? TotalPrice { get; set; }

    /// <summary>Extraction confidence score (0.0 to 1.0)</summary>
    public double? Confidence { get; set; }
}
