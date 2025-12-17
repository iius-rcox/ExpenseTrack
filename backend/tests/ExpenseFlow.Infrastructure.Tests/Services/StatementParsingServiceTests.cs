using System.Text;
using CsvHelper;
using ExpenseFlow.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ExpenseFlow.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for StatementParsingService.
/// </summary>
public class StatementParsingServiceTests
{
    private readonly StatementParsingService _sut;
    private readonly Mock<ILogger<StatementParsingService>> _loggerMock;

    public StatementParsingServiceTests()
    {
        _loggerMock = new Mock<ILogger<StatementParsingService>>();
        _sut = new StatementParsingService(_loggerMock.Object);
    }

    #region CSV Parsing Tests

    [Fact]
    public async Task ParseAsync_WithUtf8Csv_ParsesCorrectly()
    {
        // Arrange
        var csvContent = "Date,Description,Amount\n2024-01-15,Coffee Shop,12.50\n2024-01-16,Gas Station,45.00";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _sut.ParseAsync(stream, "test.csv");

        // Assert
        result.Headers.Should().HaveCount(3);
        result.Headers.Should().Contain(new[] { "Date", "Description", "Amount" });
        result.Rows.Should().HaveCount(2);
        result.RowCount.Should().Be(2);
        result.HeaderHash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ParseAsync_WithUtf8BomCsv_ParsesCorrectly()
    {
        // Arrange - UTF-8 with BOM
        var csvContent = "Date,Description,Amount\n2024-01-15,Test,10.00";
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var contentBytes = Encoding.UTF8.GetBytes(csvContent);
        var withBom = bom.Concat(contentBytes).ToArray();
        using var stream = new MemoryStream(withBom);

        // Act
        var result = await _sut.ParseAsync(stream, "test.csv");

        // Assert
        result.Headers.Should().HaveCount(3);
        result.Rows.Should().HaveCount(1);
    }

    [Fact]
    public async Task ParseAsync_WithLatin1EncodedCsv_FallsBackAndParses()
    {
        // Arrange - Latin-1 encoded content with special characters
        // This tests T031a: verify Latin-1 file parses correctly when UTF-8 fails
        var csvContent = "Date,Description,Amount\n2024-01-15,Café Naïve,12.50\n2024-01-16,Résumé Service,25.00";
        using var stream = new MemoryStream(Encoding.Latin1.GetBytes(csvContent));

        // Act
        var result = await _sut.ParseAsync(stream, "test.csv");

        // Assert
        result.Headers.Should().HaveCount(3);
        result.Rows.Should().HaveCount(2);
        // The service should handle the encoding gracefully
        result.Rows[0][1].Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ParseAsync_WithDuplicateHeaders_AppendsNumericSuffix()
    {
        // Arrange
        var csvContent = "Date,Amount,Amount,Notes\n2024-01-15,10.00,5.00,Test";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _sut.ParseAsync(stream, "test.csv");

        // Assert
        result.Headers.Should().HaveCount(4);
        result.Headers.Should().Contain("Amount");
        result.Headers.Should().Contain("Amount_2");
    }

    [Fact]
    public async Task ParseAsync_WithEmptyFile_ThrowsReaderException()
    {
        // Arrange
        using var stream = new MemoryStream(Array.Empty<byte>());

        // Act & Assert
        // CsvHelper throws ReaderException before our code can throw InvalidOperationException
        await Assert.ThrowsAsync<ReaderException>(
            () => _sut.ParseAsync(stream, "empty.csv"));
    }

    [Fact]
    public async Task ParseAsync_WithNoDataRows_ReturnsEmptyRows()
    {
        // Arrange
        var csvContent = "Date,Description,Amount";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _sut.ParseAsync(stream, "test.csv");

        // Assert
        result.Headers.Should().HaveCount(3);
        result.Rows.Should().BeEmpty();
    }

    #endregion

    #region Header Hash Tests

    [Fact]
    public void ComputeHeaderHash_WithSameHeaders_ReturnsSameHash()
    {
        // Arrange
        var headers1 = new[] { "Date", "Description", "Amount" };
        var headers2 = new[] { "Date", "Description", "Amount" };

        // Act
        var hash1 = _sut.ComputeHeaderHash(headers1);
        var hash2 = _sut.ComputeHeaderHash(headers2);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ComputeHeaderHash_WithDifferentOrder_ReturnsSameHash()
    {
        // Arrange - Headers are sorted before hashing
        var headers1 = new[] { "Date", "Description", "Amount" };
        var headers2 = new[] { "Amount", "Date", "Description" };

        // Act
        var hash1 = _sut.ComputeHeaderHash(headers1);
        var hash2 = _sut.ComputeHeaderHash(headers2);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ComputeHeaderHash_WithDifferentCase_ReturnsSameHash()
    {
        // Arrange - Headers are lowercased before hashing
        var headers1 = new[] { "Date", "Description", "Amount" };
        var headers2 = new[] { "DATE", "DESCRIPTION", "AMOUNT" };

        // Act
        var hash1 = _sut.ComputeHeaderHash(headers1);
        var hash2 = _sut.ComputeHeaderHash(headers2);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ComputeHeaderHash_WithDifferentHeaders_ReturnsDifferentHash()
    {
        // Arrange
        var headers1 = new[] { "Date", "Description", "Amount" };
        var headers2 = new[] { "Date", "Memo", "Total" };

        // Act
        var hash1 = _sut.ComputeHeaderHash(headers1);
        var hash2 = _sut.ComputeHeaderHash(headers2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    #endregion

    #region Duplicate Hash Tests

    [Fact]
    public void ComputeDuplicateHash_WithSameValues_ReturnsSameHash()
    {
        // Arrange
        var date = new DateOnly(2024, 1, 15);
        var amount = 12.50m;
        var description = "Coffee Shop";

        // Act
        var hash1 = _sut.ComputeDuplicateHash(date, amount, description);
        var hash2 = _sut.ComputeDuplicateHash(date, amount, description);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ComputeDuplicateHash_WithDifferentCase_ReturnsSameHash()
    {
        // Arrange
        var date = new DateOnly(2024, 1, 15);
        var amount = 12.50m;

        // Act
        var hash1 = _sut.ComputeDuplicateHash(date, amount, "Coffee Shop");
        var hash2 = _sut.ComputeDuplicateHash(date, amount, "COFFEE SHOP");

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ComputeDuplicateHash_WithDifferentAmount_ReturnsDifferentHash()
    {
        // Arrange
        var date = new DateOnly(2024, 1, 15);
        var description = "Coffee Shop";

        // Act
        var hash1 = _sut.ComputeDuplicateHash(date, 12.50m, description);
        var hash2 = _sut.ComputeDuplicateHash(date, 15.00m, description);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    #endregion

    #region Date Parsing Tests

    [Theory]
    [InlineData("2024-01-15", "yyyy-MM-dd", 2024, 1, 15)]
    [InlineData("01/15/2024", "MM/dd/yyyy", 2024, 1, 15)]
    [InlineData("15/01/2024", "dd/MM/yyyy", 2024, 1, 15)]
    public void ParseDate_WithValidFormat_ParsesCorrectly(
        string value, string format, int year, int month, int day)
    {
        // Act
        var result = _sut.ParseDate(value, format);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Year.Should().Be(year);
        result.Value.Month.Should().Be(month);
        result.Value.Day.Should().Be(day);
    }

    [Fact]
    public void ParseDate_WithFallbackFormat_ParsesCorrectly()
    {
        // Arrange - Use a format that doesn't match the preferred format
        var value = "2024-01-15";
        var wrongFormat = "MM/dd/yyyy"; // Doesn't match ISO format

        // Act
        var result = _sut.ParseDate(value, wrongFormat);

        // Assert - Should fallback to common formats and parse successfully
        result.Should().NotBeNull();
        result!.Value.Should().Be(new DateOnly(2024, 1, 15));
    }

    [Fact]
    public void ParseDate_WithInvalidValue_ReturnsNull()
    {
        // Act
        var result = _sut.ParseDate("not-a-date", "yyyy-MM-dd");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ParseDate_WithEmptyValue_ReturnsNull()
    {
        // Act
        var result = _sut.ParseDate("", "yyyy-MM-dd");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Amount Parsing Tests

    [Theory]
    [InlineData("12.50", 12.50)]
    [InlineData("-45.00", -45.00)]
    [InlineData("$100.00", 100.00)]
    [InlineData("€50.00", 50.00)]
    [InlineData("1,234.56", 1234.56)]
    [InlineData("(100.00)", -100.00)]  // Accounting format
    public void ParseAmount_WithValidFormats_ParsesCorrectly(string value, decimal expected)
    {
        // Act
        var result = _sut.ParseAmount(value);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ParseAmount_WithEmptyValue_ReturnsNull()
    {
        // Act
        var result = _sut.ParseAmount("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ParseAmount_WithInvalidValue_ReturnsNull()
    {
        // Act
        var result = _sut.ParseAmount("not-a-number");

        // Assert
        result.Should().BeNull();
    }

    #endregion
}
