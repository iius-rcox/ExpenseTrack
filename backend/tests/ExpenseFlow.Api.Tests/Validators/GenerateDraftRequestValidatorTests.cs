using ExpenseFlow.Api.Validators;
using ExpenseFlow.Shared.DTOs;
using FluentValidation.TestHelper;
using Xunit;

namespace ExpenseFlow.Api.Tests.Validators;

/// <summary>
/// Unit tests for GenerateDraftRequestValidator.
/// </summary>
public class GenerateDraftRequestValidatorTests
{
    private readonly GenerateDraftRequestValidator _validator = new();

    [Theory]
    [InlineData("2024-01")]
    [InlineData("2024-12")]
    [InlineData("2023-06")]
    [InlineData("2020-01")]
    public void Period_WithValidFormat_ShouldNotHaveValidationError(string period)
    {
        // Arrange
        var request = new GenerateDraftRequest { Period = period };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Period);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Period_WhenEmpty_ShouldHaveValidationError(string? period)
    {
        // Arrange
        var request = new GenerateDraftRequest { Period = period ?? string.Empty };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Period)
            .WithErrorMessage("Period is required");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("2024")]
    [InlineData("2024-1")]
    [InlineData("2024-13")]
    [InlineData("2024-00")]
    [InlineData("24-01")]
    [InlineData("2024/01")]
    [InlineData("01-2024")]
    public void Period_WithInvalidFormat_ShouldHaveValidationError(string period)
    {
        // Arrange
        var request = new GenerateDraftRequest { Period = period };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Period);
    }

    [Fact]
    public void Period_InTheFuture_ShouldHaveValidationError()
    {
        // Arrange
        var futurePeriod = DateTime.UtcNow.AddMonths(2).ToString("yyyy-MM");
        var request = new GenerateDraftRequest { Period = futurePeriod };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Period)
            .WithErrorMessage("Period cannot be in the future");
    }

    [Fact]
    public void Period_CurrentMonth_ShouldNotHaveValidationError()
    {
        // Arrange
        var currentPeriod = DateTime.UtcNow.ToString("yyyy-MM");
        var request = new GenerateDraftRequest { Period = currentPeriod };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Period);
    }
}
