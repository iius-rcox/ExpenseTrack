using ExpenseFlow.Core.Entities;
using ExpenseFlow.Shared.Enums;
using FluentAssertions;
using Xunit;

namespace ExpenseFlow.Core.Tests.Entities;

/// <summary>
/// Unit tests for VendorAlias entity.
/// Tests business rules around vendor aliasing, GL confirmation, and confidence decay.
/// </summary>
[Trait("Category", "Unit")]
public class VendorAliasTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var vendorAlias = new VendorAlias();

        // Assert
        vendorAlias.CanonicalName.Should().BeEmpty();
        vendorAlias.AliasPattern.Should().BeEmpty();
        vendorAlias.DisplayName.Should().BeEmpty();
        vendorAlias.DefaultGLCode.Should().BeNull();
        vendorAlias.DefaultDepartment.Should().BeNull();
        vendorAlias.GLConfirmCount.Should().Be(0);
        vendorAlias.DeptConfirmCount.Should().Be(0);
        vendorAlias.MatchCount.Should().Be(0);
        vendorAlias.LastMatchedAt.Should().BeNull();
        vendorAlias.Confidence.Should().Be(1.00m);
        vendorAlias.Category.Should().Be(VendorCategory.Standard);
        vendorAlias.SplitPatterns.Should().BeEmpty();
    }

    [Theory]
    [InlineData("delta_airlines", "DELTA*", "Delta Airlines", VendorCategory.Airline)]
    [InlineData("marriott_hotels", "MARRIOTT*", "Marriott Hotels", VendorCategory.Hotel)]
    [InlineData("openai", "OPENAI*", "OpenAI", VendorCategory.Subscription)]
    public void Properties_CanBeSetCorrectly(string canonical, string pattern, string display, VendorCategory category)
    {
        // Arrange & Act
        var vendorAlias = new VendorAlias
        {
            CanonicalName = canonical,
            AliasPattern = pattern,
            DisplayName = display,
            Category = category,
            DefaultGLCode = "63300",
            DefaultDepartment = "07"
        };

        // Assert
        vendorAlias.CanonicalName.Should().Be(canonical);
        vendorAlias.AliasPattern.Should().Be(pattern);
        vendorAlias.DisplayName.Should().Be(display);
        vendorAlias.Category.Should().Be(category);
        vendorAlias.DefaultGLCode.Should().Be("63300");
        vendorAlias.DefaultDepartment.Should().Be("07");
    }

    [Fact]
    public void Confidence_CanBeSetWithinValidRange()
    {
        // Arrange
        var vendorAlias = new VendorAlias();

        // Act
        vendorAlias.Confidence = 0.75m;

        // Assert
        vendorAlias.Confidence.Should().Be(0.75m);
    }

    [Fact]
    public void GLConfirmCount_CanBeIncremented()
    {
        // Arrange
        var vendorAlias = new VendorAlias { GLConfirmCount = 2 };

        // Act
        vendorAlias.GLConfirmCount++;

        // Assert - at 3+ confirmations, the GL code is considered auto-updateable
        vendorAlias.GLConfirmCount.Should().Be(3);
    }

    [Fact]
    public void MatchCount_AndLastMatchedAt_TrackUsage()
    {
        // Arrange
        var vendorAlias = new VendorAlias();
        var matchTime = DateTime.UtcNow;

        // Act
        vendorAlias.MatchCount = 5;
        vendorAlias.LastMatchedAt = matchTime;

        // Assert
        vendorAlias.MatchCount.Should().Be(5);
        vendorAlias.LastMatchedAt.Should().Be(matchTime);
    }

    [Fact]
    public void SplitPatterns_CanHaveMultiplePatterns()
    {
        // Arrange
        var vendorAlias = new VendorAlias();
        var pattern1 = new SplitPattern { Id = Guid.NewGuid() };
        var pattern2 = new SplitPattern { Id = Guid.NewGuid() };

        // Act
        vendorAlias.SplitPatterns.Add(pattern1);
        vendorAlias.SplitPatterns.Add(pattern2);

        // Assert
        vendorAlias.SplitPatterns.Should().HaveCount(2);
        vendorAlias.SplitPatterns.Should().Contain(pattern1);
        vendorAlias.SplitPatterns.Should().Contain(pattern2);
    }
}
