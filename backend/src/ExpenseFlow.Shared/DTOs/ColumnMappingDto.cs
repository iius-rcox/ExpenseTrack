namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Represents a column mapping from statement header to field type.
/// </summary>
public class ColumnMappingDto
{
    /// <summary>
    /// Maps column header names to their field types.
    /// Key: Original column header from statement (e.g., "Transaction Date")
    /// Value: Field type (date, post_date, description, amount, category, memo, reference, ignore)
    /// </summary>
    public Dictionary<string, string> Mapping { get; set; } = new();
}

/// <summary>
/// Valid field types for column mapping.
/// </summary>
public static class ColumnFieldTypes
{
    public const string Date = "date";
    public const string PostDate = "post_date";
    public const string Description = "description";
    public const string Amount = "amount";
    public const string Category = "category";
    public const string Memo = "memo";
    public const string Reference = "reference";
    public const string Ignore = "ignore";

    public static readonly HashSet<string> ValidTypes = new()
    {
        Date, PostDate, Description, Amount, Category, Memo, Reference, Ignore
    };

    public static bool IsValid(string type) => ValidTypes.Contains(type);
}
