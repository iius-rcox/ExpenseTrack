using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using ExpenseFlow.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for parsing CSV and Excel statement files.
/// </summary>
public class StatementParsingService : IStatementParsingService
{
    private readonly ILogger<StatementParsingService> _logger;

    /// <summary>
    /// Common date formats to try during fallback parsing.
    /// </summary>
    private static readonly string[] DateFormats =
    {
        "yyyy-MM-dd",    // ISO 8601
        "MM/dd/yyyy",    // US format
        "dd/MM/yyyy",    // EU format
        "MM-dd-yyyy",    // US with dashes
        "dd-MM-yyyy",    // EU with dashes
        "M/d/yyyy",      // US short
        "d/M/yyyy",      // EU short
        "yyyy/MM/dd",    // ISO variant
        "MM/dd/yy",      // US 2-digit year
        "dd/MM/yy"       // EU 2-digit year
    };

    public StatementParsingService(ILogger<StatementParsingService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ParsedStatementData> ParseAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".csv" => await ParseCsvAsync(fileStream, cancellationToken),
            ".xlsx" or ".xls" => await ParseExcelAsync(fileStream, cancellationToken),
            _ => throw new ArgumentException($"Unsupported file format: {extension}. Supported formats: .csv, .xlsx, .xls")
        };
    }

    /// <inheritdoc />
    public string ComputeHeaderHash(IEnumerable<string> headers)
    {
        // Normalize: lowercase, trim whitespace, sort alphabetically
        var normalizedHeaders = headers
            .Select(h => h.Trim().ToLowerInvariant())
            .Where(h => !string.IsNullOrEmpty(h))
            .OrderBy(h => h)
            .ToList();

        var combined = string.Join(",", normalizedHeaders);

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <inheritdoc />
    public string ComputeDuplicateHash(DateOnly date, decimal amount, string description)
    {
        // Normalize description: lowercase, trim, remove extra spaces
        var normalizedDescription = string.Join(" ",
            description.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

        var combined = $"{date:yyyy-MM-dd}|{amount:F2}|{normalizedDescription}";

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Parses a date string using the preferred format with fallback to common formats.
    /// </summary>
    /// <param name="value">Date string to parse.</param>
    /// <param name="preferredFormat">Preferred date format from fingerprint.</param>
    /// <returns>Parsed DateOnly or null if parsing fails.</returns>
    public DateOnly? ParseDate(string value, string? preferredFormat)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = value.Trim();

        // Try preferred format first
        if (!string.IsNullOrEmpty(preferredFormat) &&
            DateOnly.TryParseExact(value, preferredFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        // Fallback to common formats
        foreach (var format in DateFormats)
        {
            if (DateOnly.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                _logger.LogDebug("Parsed date '{Value}' using fallback format '{Format}'", value, format);
                return date;
            }
        }

        // Last resort: try standard parsing
        if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, out date))
        {
            return date;
        }

        _logger.LogWarning("Failed to parse date value: '{Value}'", value);
        return null;
    }

    /// <summary>
    /// Parses an amount string to decimal.
    /// </summary>
    /// <param name="value">Amount string to parse.</param>
    /// <returns>Parsed decimal or null if parsing fails.</returns>
    public decimal? ParseAmount(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // Remove currency symbols and whitespace
        value = value.Trim()
            .Replace("$", "")
            .Replace("€", "")
            .Replace("£", "")
            .Replace(",", "") // Remove thousands separator
            .Trim();

        // Handle parentheses for negative numbers (accounting format)
        if (value.StartsWith("(") && value.EndsWith(")"))
        {
            value = "-" + value.Trim('(', ')');
        }

        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
        {
            return amount;
        }

        _logger.LogWarning("Failed to parse amount value: '{Value}'", value);
        return null;
    }

    private async Task<ParsedStatementData> ParseCsvAsync(Stream fileStream, CancellationToken cancellationToken)
    {
        // Try UTF-8 first with BOM detection, fall back to Latin-1
        var encoding = DetectEncoding(fileStream);
        fileStream.Position = 0;

        using var reader = new StreamReader(fileStream, encoding, detectEncodingFromByteOrderMarks: true);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null, // Don't throw on missing fields
            BadDataFound = null, // Don't throw on bad data
            TrimOptions = TrimOptions.Trim
        };

        using var csv = new CsvReader(reader, config);

        // Read header
        await csv.ReadAsync();
        csv.ReadHeader();

        var headers = csv.HeaderRecord?.ToList() ?? throw new InvalidOperationException("No header row found in CSV file");

        // Handle duplicate column names by appending numeric suffix
        headers = HandleDuplicateHeaders(headers);

        var rows = new List<List<string>>();

        // Read all data rows
        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var row = new List<string>();
            for (int i = 0; i < headers.Count; i++)
            {
                row.Add(csv.GetField(i) ?? string.Empty);
            }
            rows.Add(row);
        }

        _logger.LogDebug("Parsed CSV file with {HeaderCount} columns and {RowCount} data rows",
            headers.Count, rows.Count);

        return new ParsedStatementData
        {
            Headers = headers,
            Rows = rows,
            HeaderHash = ComputeHeaderHash(headers)
        };
    }

    private async Task<ParsedStatementData> ParseExcelAsync(Stream fileStream, CancellationToken cancellationToken)
    {
        // Copy stream to MemoryStream for ClosedXML (it needs seekable stream)
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        using var workbook = new XLWorkbook(memoryStream);
        var worksheet = workbook.Worksheets.First();

        var usedRange = worksheet.RangeUsed();
        if (usedRange == null)
        {
            throw new InvalidOperationException("Excel file contains no data");
        }

        var firstRow = usedRange.FirstRow();
        var headers = firstRow.Cells()
            .Select(c => c.GetString())
            .ToList();

        if (headers.All(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("No header row found in Excel file");
        }

        // Handle duplicate column names
        headers = HandleDuplicateHeaders(headers);

        var rows = new List<List<string>>();
        var dataRows = usedRange.Rows().Skip(1); // Skip header row

        foreach (var row in dataRows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var rowData = new List<string>();
            for (int i = 1; i <= headers.Count; i++)
            {
                var cell = row.Cell(i);
                rowData.Add(cell.GetString());
            }
            rows.Add(rowData);
        }

        _logger.LogDebug("Parsed Excel file with {HeaderCount} columns and {RowCount} data rows",
            headers.Count, rows.Count);

        return new ParsedStatementData
        {
            Headers = headers,
            Rows = rows,
            HeaderHash = ComputeHeaderHash(headers)
        };
    }

    private static Encoding DetectEncoding(Stream stream)
    {
        // Check for BOM
        var buffer = new byte[4];
        var bytesRead = stream.Read(buffer, 0, 4);
        stream.Position = 0;

        if (bytesRead >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
        {
            return Encoding.UTF8;
        }

        if (bytesRead >= 2 && buffer[0] == 0xFF && buffer[1] == 0xFE)
        {
            return Encoding.Unicode; // UTF-16 LE
        }

        if (bytesRead >= 2 && buffer[0] == 0xFE && buffer[1] == 0xFF)
        {
            return Encoding.BigEndianUnicode; // UTF-16 BE
        }

        // Default to UTF-8, will fall back to Latin-1 if decoding fails
        return Encoding.UTF8;
    }

    private static List<string> HandleDuplicateHeaders(List<string> headers)
    {
        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        foreach (var header in headers)
        {
            var normalizedHeader = header.Trim();
            if (string.IsNullOrEmpty(normalizedHeader))
            {
                normalizedHeader = "Column";
            }

            if (seen.TryGetValue(normalizedHeader, out var count))
            {
                seen[normalizedHeader] = count + 1;
                result.Add($"{normalizedHeader}_{count + 1}");
            }
            else
            {
                seen[normalizedHeader] = 1;
                result.Add(normalizedHeader);
            }
        }

        return result;
    }
}
