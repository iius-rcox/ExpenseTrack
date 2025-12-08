namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Represents parsed statement data.
/// </summary>
public class ParsedStatementData
{
    /// <summary>
    /// Column headers from the statement.
    /// </summary>
    public List<string> Headers { get; set; } = new();

    /// <summary>
    /// All data rows from the statement.
    /// </summary>
    public List<List<string>> Rows { get; set; } = new();

    /// <summary>
    /// Number of data rows (excluding header).
    /// </summary>
    public int RowCount => Rows.Count;

    /// <summary>
    /// SHA-256 hash of normalized, sorted headers for fingerprint matching.
    /// </summary>
    public string HeaderHash { get; set; } = string.Empty;
}

/// <summary>
/// Service interface for parsing CSV and Excel statement files.
/// </summary>
public interface IStatementParsingService
{
    /// <summary>
    /// Parses a statement file (CSV or Excel) and returns structured data.
    /// </summary>
    /// <param name="fileStream">The file stream to parse.</param>
    /// <param name="fileName">Original filename for format detection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Parsed statement data with headers, rows, and header hash.</returns>
    Task<ParsedStatementData> ParseAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes SHA-256 hash of normalized, sorted headers.
    /// </summary>
    /// <param name="headers">Column headers to hash.</param>
    /// <returns>Hex-encoded SHA-256 hash.</returns>
    string ComputeHeaderHash(IEnumerable<string> headers);

    /// <summary>
    /// Computes duplicate detection hash for a transaction.
    /// </summary>
    /// <param name="date">Transaction date.</param>
    /// <param name="amount">Transaction amount.</param>
    /// <param name="description">Transaction description.</param>
    /// <returns>Hex-encoded SHA-256 hash.</returns>
    string ComputeDuplicateHash(DateOnly date, decimal amount, string description);
}
