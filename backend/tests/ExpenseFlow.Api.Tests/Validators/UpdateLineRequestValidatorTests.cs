using ExpenseFlow.Api.Validators;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using FluentValidation.TestHelper;
using Xunit;

namespace ExpenseFlow.Api.Tests.Validators;

/// <summary>
/// Unit tests for UpdateLineRequestValidator.
/// </summary>
public class UpdateLineRequestValidatorTests
{
    private readonly UpdateLineRequestValidator _validator = new();

    #region GlCode Tests

    [Theory]
    [InlineData("65000")]
    [InlineData("ABC123")]
    [InlineData("1234567890")]  // Max length 10
    [InlineData(null)]  // Optional
    public void GlCode_WithValidValue_ShouldNotHaveValidationError(string? glCode)
    {
        // Arrange
        var request = new UpdateLineRequest { GlCode = glCode };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.GlCode);
    }

    [Fact]
    public void GlCode_ExceedingMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        var request = new UpdateLineRequest { GlCode = "12345678901" }; // 11 chars

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.GlCode)
            .WithErrorMessage("GL code cannot exceed 10 characters");
    }

    [Theory]
    [InlineData("65-000")]
    [InlineData("GL_CODE")]
    [InlineData("GL.CODE")]
    public void GlCode_WithNonAlphanumeric_ShouldHaveValidationError(string glCode)
    {
        // Arrange
        var request = new UpdateLineRequest { GlCode = glCode };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.GlCode)
            .WithErrorMessage("GL code must be alphanumeric");
    }

    #endregion

    #region DepartmentCode Tests

    [Theory]
    [InlineData("IT")]
    [InlineData("HR-DEPT")]
    [InlineData("DEPT-123")]
    [InlineData(null)]  // Optional
    public void DepartmentCode_WithValidValue_ShouldNotHaveValidationError(string? deptCode)
    {
        // Arrange
        var request = new UpdateLineRequest { DepartmentCode = deptCode };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DepartmentCode);
    }

    [Fact]
    public void DepartmentCode_ExceedingMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        var request = new UpdateLineRequest { DepartmentCode = new string('A', 21) };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DepartmentCode)
            .WithErrorMessage("Department code cannot exceed 20 characters");
    }

    [Theory]
    [InlineData("DEPT_CODE")]
    [InlineData("DEPT.CODE")]
    public void DepartmentCode_WithInvalidCharacters_ShouldHaveValidationError(string deptCode)
    {
        // Arrange
        var request = new UpdateLineRequest { DepartmentCode = deptCode };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DepartmentCode);
    }

    #endregion

    #region MissingReceiptJustification Tests

    [Theory]
    [InlineData(MissingReceiptJustification.NotProvided)]
    [InlineData(MissingReceiptJustification.Lost)]
    [InlineData(MissingReceiptJustification.DigitalSubscription)]
    [InlineData(MissingReceiptJustification.UnderThreshold)]
    [InlineData(null)]
    public void MissingReceiptJustification_WithValidValue_ShouldNotHaveValidationError(
        MissingReceiptJustification? justification)
    {
        // Arrange
        var request = new UpdateLineRequest { MissingReceiptJustification = justification };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MissingReceiptJustification);
    }

    [Fact]
    public void MissingReceiptJustification_Other_WithNote_ShouldNotHaveValidationError()
    {
        // Arrange
        var request = new UpdateLineRequest
        {
            MissingReceiptJustification = MissingReceiptJustification.Other,
            JustificationNote = "Receipt was emailed"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void MissingReceiptJustification_Other_WithoutNote_ShouldHaveValidationError()
    {
        // Arrange
        var request = new UpdateLineRequest
        {
            MissingReceiptJustification = MissingReceiptJustification.Other,
            JustificationNote = null
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.JustificationNote)
            .WithErrorMessage("Justification note is required when justification is 'Other'");
    }

    [Fact]
    public void MissingReceiptJustification_Other_WithEmptyNote_ShouldHaveValidationError()
    {
        // Arrange
        var request = new UpdateLineRequest
        {
            MissingReceiptJustification = MissingReceiptJustification.Other,
            JustificationNote = ""
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.JustificationNote);
    }

    #endregion

    #region JustificationNote Tests

    [Fact]
    public void JustificationNote_ExceedingMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        var request = new UpdateLineRequest
        {
            JustificationNote = new string('A', 501)
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.JustificationNote)
            .WithErrorMessage("Justification note cannot exceed 500 characters");
    }

    [Fact]
    public void JustificationNote_AtMaxLength_ShouldNotHaveValidationError()
    {
        // Arrange
        var request = new UpdateLineRequest
        {
            JustificationNote = new string('A', 500)
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.JustificationNote);
    }

    #endregion
}
