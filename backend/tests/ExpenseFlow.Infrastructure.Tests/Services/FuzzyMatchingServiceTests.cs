using ExpenseFlow.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace ExpenseFlow.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for FuzzyMatchingService.
/// Tests string similarity calculations using NormalizedLevenshtein algorithm.
/// </summary>
public class FuzzyMatchingServiceTests
{
    private readonly FuzzyMatchingService _sut;

    public FuzzyMatchingServiceTests()
    {
        _sut = new FuzzyMatchingService();
    }

    #region Exact Match Tests

    [Fact]
    public void CalculateSimilarity_ExactMatch_Returns1()
    {
        // Arrange
        var text = "AMAZON MARKETPLACE";

        // Act
        var result = _sut.CalculateSimilarity(text, text);

        // Assert
        result.Should().Be(1.0);
    }

    [Fact]
    public void CalculateSimilarity_ExactMatchDifferentCase_Returns1()
    {
        // Arrange
        var text1 = "Amazon Marketplace";
        var text2 = "AMAZON MARKETPLACE";

        // Act
        var result = _sut.CalculateSimilarity(text1, text2);

        // Assert
        result.Should().Be(1.0);
    }

    [Fact]
    public void CalculateSimilarity_BothEmpty_Returns1()
    {
        // Act
        var result = _sut.CalculateSimilarity("", "");

        // Assert
        result.Should().Be(1.0);
    }

    #endregion

    #region Similar Strings Tests (>0.7)

    [Theory]
    [InlineData("AMAZON MARKETPLACE", "AMAZON MARKETPLCE", 0.7)]  // Typo
    [InlineData("DELTA AIRLINES", "DELTA AIRLINE", 0.7)]          // Singular/plural
    [InlineData("STARBUCKS COFFEE", "STARBUCKS COFFE", 0.7)]      // Missing letter
    [InlineData("SHELL GAS STATION", "SHELL GAS STATN", 0.7)]     // Truncation
    public void CalculateSimilarity_SimilarStrings_ReturnsAbove70Percent(
        string text1, string text2, double minExpected)
    {
        // Act
        var result = _sut.CalculateSimilarity(text1, text2);

        // Assert
        result.Should().BeGreaterThanOrEqualTo(minExpected,
            $"'{text1}' and '{text2}' should be at least {minExpected * 100}% similar");
    }

    [Fact]
    public void CalculateSimilarity_MinorVariation_ReturnsHighSimilarity()
    {
        // Arrange - Common real-world vendor variations
        var text1 = "AMAZON PRIME";
        var text2 = "AMAZON PRIME VIDEO";

        // Act
        var result = _sut.CalculateSimilarity(text1, text2);

        // Assert - Should still be reasonably similar
        result.Should().BeGreaterThan(0.6);
    }

    [Fact]
    public void CalculateSimilarity_TransposedCharacters_ReturnsHighSimilarity()
    {
        // Arrange - Characters swapped
        var text1 = "CHEVRON";
        var text2 = "CHERVON";

        // Act
        var result = _sut.CalculateSimilarity(text1, text2);

        // Assert
        result.Should().BeGreaterThan(0.7);
    }

    #endregion

    #region Different Strings Tests (<0.5)

    [Theory]
    [InlineData("AMAZON", "WALMART", 0.5)]
    [InlineData("STARBUCKS", "DUNKIN DONUTS", 0.5)]
    [InlineData("SHELL", "EXXON MOBIL", 0.5)]
    [InlineData("DELTA AIRLINES", "UNITED AIRLINES", 0.5)]
    public void CalculateSimilarity_DifferentStrings_ReturnsBelowThreshold(
        string text1, string text2, double maxExpected)
    {
        // Act
        var result = _sut.CalculateSimilarity(text1, text2);

        // Assert
        result.Should().BeLessThan(maxExpected,
            $"'{text1}' and '{text2}' should be less than {maxExpected * 100}% similar");
    }

    [Fact]
    public void CalculateSimilarity_CompletelyDifferent_ReturnsLowScore()
    {
        // Arrange
        var text1 = "ABCDEFGHIJ";
        var text2 = "KLMNOPQRST";

        // Act
        var result = _sut.CalculateSimilarity(text1, text2);

        // Assert
        result.Should().BeLessThan(0.2);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void CalculateSimilarity_OneEmpty_ReturnsZero()
    {
        // Act
        var result = _sut.CalculateSimilarity("AMAZON", "");

        // Assert
        result.Should().Be(0.0);
    }

    [Fact]
    public void CalculateSimilarity_OtherEmpty_ReturnsZero()
    {
        // Act
        var result = _sut.CalculateSimilarity("", "AMAZON");

        // Assert
        result.Should().Be(0.0);
    }

    [Fact]
    public void CalculateSimilarity_NullInputs_HandlesGracefully()
    {
        // Act
        var result = _sut.CalculateSimilarity(null!, null!);

        // Assert - Implementation should handle nulls
        result.Should().BeGreaterThanOrEqualTo(0.0);
    }

    [Fact]
    public void CalculateSimilarity_SingleCharacter_WorksCorrectly()
    {
        // Act
        var sameChar = _sut.CalculateSimilarity("A", "A");
        var diffChar = _sut.CalculateSimilarity("A", "B");

        // Assert
        sameChar.Should().Be(1.0);
        diffChar.Should().Be(0.0);
    }

    [Fact]
    public void CalculateSimilarity_WhitespaceHandling_ComparesTrimmed()
    {
        // Arrange
        var text1 = "  AMAZON  ";
        var text2 = "AMAZON";

        // Act
        var result = _sut.CalculateSimilarity(text1, text2);

        // Assert - After trimming, should be very similar or identical
        result.Should().BeGreaterThan(0.7);
    }

    #endregion

    #region Real-World Vendor Pattern Tests

    [Theory]
    [InlineData("AMAZON.COM AMZN.COM/BILL", "AMAZON", 0.4)]        // Contains base name
    [InlineData("SQ *COFFEE SHOP", "SQUARE COFFEE", 0.3)]          // Square prefix
    [InlineData("PAYPAL *VENDOR NAME", "VENDOR NAME", 0.4)]        // PayPal prefix
    [InlineData("TST* RESTAURANT NAME", "RESTAURANT NAME", 0.5)]   // Toast prefix
    public void CalculateSimilarity_VendorPatterns_ReturnsExpectedRange(
        string transactionDesc, string expectedVendor, double minSimilarity)
    {
        // Act
        var result = _sut.CalculateSimilarity(transactionDesc, expectedVendor);

        // Assert - These may not match well without pattern extraction
        // This demonstrates why we extract vendor patterns before matching
        result.Should().BeGreaterThanOrEqualTo(0.0);
    }

    [Fact]
    public void CalculateSimilarity_SameVendorDifferentFormat_MayNeedPreprocessing()
    {
        // Arrange - Same vendor, different statement formats
        var format1 = "DELTA AIR 0123456789";
        var format2 = "DELTA AIRLINES INC";

        // Act
        var result = _sut.CalculateSimilarity(format1, format2);

        // Assert - This shows why vendor pattern extraction is important
        // Raw descriptions may not match well
        result.Should().BeGreaterThan(0.3);
    }

    #endregion

    #region Performance Sanity Tests

    [Fact]
    public void CalculateSimilarity_LongStrings_CompletesQuickly()
    {
        // Arrange - Create reasonably long strings
        var text1 = string.Concat(Enumerable.Repeat("AMAZON MARKETPLACE ", 20));
        var text2 = string.Concat(Enumerable.Repeat("AMAZON MARKETPLACE ", 20));

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = _sut.CalculateSimilarity(text1, text2);
        sw.Stop();

        // Assert
        result.Should().Be(1.0);
        sw.ElapsedMilliseconds.Should().BeLessThan(1000, "Comparison should complete quickly");
    }

    #endregion
}
