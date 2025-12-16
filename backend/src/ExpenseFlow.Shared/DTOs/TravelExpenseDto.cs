namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// An expense that falls within a travel period.
/// </summary>
public class TravelExpenseDto
{
    /// <summary>
    /// Unique expense/transaction identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Date of the expense.
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// Expense description/vendor name.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Expense amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Associated receipt ID (nullable).
    /// </summary>
    public Guid? ReceiptId { get; set; }

    /// <summary>
    /// Suggested GL code (66300 for travel).
    /// </summary>
    public string SuggestedGLCode { get; set; } = string.Empty;

    /// <summary>
    /// Whether the expense is linked to the travel period.
    /// </summary>
    public bool IsLinked { get; set; }

    /// <summary>
    /// Source of the expense (Receipt, Transaction).
    /// </summary>
    public TravelExpenseSource Source { get; set; }
}

/// <summary>
/// Source of a travel expense.
/// </summary>
public enum TravelExpenseSource
{
    /// <summary>Expense from uploaded receipt.</summary>
    Receipt = 0,

    /// <summary>Expense from imported statement transaction.</summary>
    Transaction = 1
}

/// <summary>
/// Response for travel period expenses.
/// </summary>
public class TravelExpenseListResponseDto
{
    /// <summary>
    /// List of expenses within the travel period.
    /// </summary>
    public List<TravelExpenseDto> Expenses { get; set; } = new();

    /// <summary>
    /// Total count of expenses.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total amount of all expenses.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Count of linked expenses.
    /// </summary>
    public int LinkedCount { get; set; }

    /// <summary>
    /// Count of unlinked expenses (for review).
    /// </summary>
    public int UnlinkedCount { get; set; }
}
