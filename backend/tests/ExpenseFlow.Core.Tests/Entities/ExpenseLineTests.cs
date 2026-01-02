using ExpenseFlow.Core.Entities;
using ExpenseFlow.Shared.Enums;
using FluentAssertions;
using Xunit;

namespace ExpenseFlow.Core.Tests.Entities;

/// <summary>
/// Unit tests for ExpenseLine entity.
/// Tests expense line creation, categorization tier tracking, and receipt accountability.
/// </summary>
[Trait("Category", "Unit")]
public class ExpenseLineTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var expenseLine = new ExpenseLine();

        // Assert
        expenseLine.ReportId.Should().Be(Guid.Empty);
        expenseLine.ReceiptId.Should().BeNull();
        expenseLine.TransactionId.Should().BeNull();
        expenseLine.LineOrder.Should().Be(0);
        expenseLine.Amount.Should().Be(0);
        expenseLine.OriginalDescription.Should().BeEmpty();
        expenseLine.NormalizedDescription.Should().BeEmpty();
        expenseLine.VendorName.Should().BeNull();
        expenseLine.GLCode.Should().BeNull();
        expenseLine.GLCodeSuggested.Should().BeNull();
        expenseLine.GLCodeTier.Should().BeNull();
        expenseLine.GLCodeSource.Should().BeNull();
        expenseLine.DepartmentCode.Should().BeNull();
        expenseLine.HasReceipt.Should().BeFalse();
        expenseLine.MissingReceiptJustification.Should().BeNull();
        expenseLine.IsUserEdited.Should().BeFalse();
    }

    [Fact]
    public void CanCreate_ExpenseLineWithAllFields()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var expenseDate = new DateOnly(2025, 12, 15);

        // Act
        var expenseLine = new ExpenseLine
        {
            ReportId = reportId,
            ReceiptId = receiptId,
            TransactionId = transactionId,
            LineOrder = 1,
            ExpenseDate = expenseDate,
            Amount = 125.50m,
            OriginalDescription = "DELTA AIR 0062363598531",
            NormalizedDescription = "Delta Airlines Flight",
            VendorName = "Delta Airlines",
            GLCode = "66300",
            GLCodeSuggested = "66300",
            GLCodeTier = 1,
            GLCodeSource = "VendorAlias",
            DepartmentCode = "07",
            DepartmentSuggested = "07",
            DepartmentTier = 1,
            DepartmentSource = "VendorAlias",
            HasReceipt = true
        };

        // Assert
        expenseLine.ReportId.Should().Be(reportId);
        expenseLine.ReceiptId.Should().Be(receiptId);
        expenseLine.TransactionId.Should().Be(transactionId);
        expenseLine.LineOrder.Should().Be(1);
        expenseLine.ExpenseDate.Should().Be(expenseDate);
        expenseLine.Amount.Should().Be(125.50m);
        expenseLine.OriginalDescription.Should().Be("DELTA AIR 0062363598531");
        expenseLine.NormalizedDescription.Should().Be("Delta Airlines Flight");
        expenseLine.GLCode.Should().Be("66300");
        expenseLine.GLCodeTier.Should().Be(1);
        expenseLine.GLCodeSource.Should().Be("VendorAlias");
        expenseLine.HasReceipt.Should().BeTrue();
    }

    [Theory]
    [InlineData(1, "VendorAlias")]       // Tier 1: Cache lookup
    [InlineData(2, "EmbeddingSimilarity")] // Tier 2: Vector similarity
    [InlineData(3, "AIInference")]       // Tier 3: GPT-4o-mini
    public void GLCodeTier_TracksCategorizationSource(int tier, string source)
    {
        // Arrange & Act
        var expenseLine = new ExpenseLine
        {
            GLCodeTier = tier,
            GLCodeSource = source,
            GLCodeSuggested = "63300"
        };

        // Assert
        expenseLine.GLCodeTier.Should().Be(tier);
        expenseLine.GLCodeSource.Should().Be(source);
    }

    [Theory]
    [InlineData(MissingReceiptJustification.NotProvided)]
    [InlineData(MissingReceiptJustification.Lost)]
    [InlineData(MissingReceiptJustification.DigitalSubscription)]
    [InlineData(MissingReceiptJustification.Other)]
    public void MissingReceiptJustification_CanBeSet(MissingReceiptJustification justification)
    {
        // Arrange & Act
        var expenseLine = new ExpenseLine
        {
            HasReceipt = false,
            MissingReceiptJustification = justification,
            JustificationNote = justification == MissingReceiptJustification.Other ? "Custom note" : null
        };

        // Assert
        expenseLine.HasReceipt.Should().BeFalse();
        expenseLine.MissingReceiptJustification.Should().Be(justification);
    }

    [Fact]
    public void IsUserEdited_TracksManualModifications()
    {
        // Arrange
        var expenseLine = new ExpenseLine
        {
            GLCode = "63300",
            GLCodeSuggested = "66300",
            IsUserEdited = false
        };

        // Act - Simulate user changing the GL code
        expenseLine.GLCode = "66800";
        expenseLine.IsUserEdited = true;
        expenseLine.UpdatedAt = DateTime.UtcNow;

        // Assert
        expenseLine.GLCode.Should().Be("66800");
        expenseLine.IsUserEdited.Should().BeTrue();
        expenseLine.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Amount_CanHandleNegativeValues_ForRefunds()
    {
        // Arrange & Act
        var expenseLine = new ExpenseLine
        {
            Amount = -50.00m,
            NormalizedDescription = "Refund - Delta Airlines"
        };

        // Assert
        expenseLine.Amount.Should().BeNegative();
    }

    [Fact]
    public void Amount_CanHandleLargeValues()
    {
        // Arrange & Act
        var expenseLine = new ExpenseLine
        {
            Amount = 9999999.99m
        };

        // Assert
        expenseLine.Amount.Should().Be(9999999.99m);
    }
}
