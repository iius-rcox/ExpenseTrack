using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using FluentValidation;

namespace ExpenseFlow.Api.Validators;

/// <summary>
/// Validator for UpdateLineRequest.
/// </summary>
public class UpdateLineRequestValidator : AbstractValidator<UpdateLineRequest>
{
    public UpdateLineRequestValidator()
    {
        RuleFor(x => x.GlCode)
            .MaximumLength(10)
            .WithMessage("GL code cannot exceed 10 characters")
            .Matches(@"^[A-Za-z0-9]*$")
            .When(x => !string.IsNullOrEmpty(x.GlCode))
            .WithMessage("GL code must be alphanumeric");

        RuleFor(x => x.DepartmentCode)
            .MaximumLength(20)
            .WithMessage("Department code cannot exceed 20 characters")
            .Matches(@"^[A-Za-z0-9\-]*$")
            .When(x => !string.IsNullOrEmpty(x.DepartmentCode))
            .WithMessage("Department code must be alphanumeric (hyphens allowed)");

        RuleFor(x => x.MissingReceiptJustification)
            .IsInEnum()
            .When(x => x.MissingReceiptJustification.HasValue)
            .WithMessage("Invalid missing receipt justification value");

        RuleFor(x => x.JustificationNote)
            .MaximumLength(500)
            .WithMessage("Justification note cannot exceed 500 characters");

        RuleFor(x => x.JustificationNote)
            .NotEmpty()
            .When(x => x.MissingReceiptJustification == MissingReceiptJustification.Other)
            .WithMessage("Justification note is required when justification is 'Other'");
    }
}
