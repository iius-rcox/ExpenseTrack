using ExpenseFlow.Shared.DTOs;
using FluentValidation;

namespace ExpenseFlow.Api.Validators;

/// <summary>
/// Validator for AddLineRequest.
/// </summary>
public class AddLineRequestValidator : AbstractValidator<AddLineRequest>
{
    public AddLineRequestValidator()
    {
        RuleFor(x => x.TransactionId)
            .NotEmpty()
            .WithMessage("Transaction ID is required");

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
    }
}
