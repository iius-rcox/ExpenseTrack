using ExpenseFlow.Shared.DTOs;
using FluentValidation;

namespace ExpenseFlow.Api.Validators;

/// <summary>
/// Validator for CreateAllowanceRequest.
/// </summary>
public class CreateAllowanceRequestValidator : AbstractValidator<CreateAllowanceRequest>
{
    public CreateAllowanceRequestValidator()
    {
        RuleFor(x => x.VendorName)
            .NotEmpty()
            .WithMessage("Vendor name is required")
            .MaximumLength(100)
            .WithMessage("Vendor name cannot exceed 100 characters");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero")
            .LessThanOrEqualTo(100000)
            .WithMessage("Amount cannot exceed $100,000");

        RuleFor(x => x.Frequency)
            .IsInEnum()
            .WithMessage("Invalid frequency value");

        RuleFor(x => x.GLCode)
            .MaximumLength(20)
            .When(x => x.GLCode != null)
            .WithMessage("GL code cannot exceed 20 characters");

        RuleFor(x => x.DepartmentCode)
            .MaximumLength(20)
            .When(x => x.DepartmentCode != null)
            .WithMessage("Department code cannot exceed 20 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => x.Description != null)
            .WithMessage("Description cannot exceed 500 characters");
    }
}

/// <summary>
/// Validator for UpdateAllowanceRequest.
/// </summary>
public class UpdateAllowanceRequestValidator : AbstractValidator<UpdateAllowanceRequest>
{
    public UpdateAllowanceRequestValidator()
    {
        RuleFor(x => x.VendorName)
            .MaximumLength(100)
            .When(x => x.VendorName != null)
            .WithMessage("Vendor name cannot exceed 100 characters");

        RuleFor(x => x.VendorName)
            .NotEmpty()
            .When(x => x.VendorName != null)
            .WithMessage("Vendor name cannot be empty when provided");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .When(x => x.Amount.HasValue)
            .WithMessage("Amount must be greater than zero");

        RuleFor(x => x.Amount)
            .LessThanOrEqualTo(100000)
            .When(x => x.Amount.HasValue)
            .WithMessage("Amount cannot exceed $100,000");

        RuleFor(x => x.Frequency)
            .IsInEnum()
            .When(x => x.Frequency.HasValue)
            .WithMessage("Invalid frequency value");

        RuleFor(x => x.GLCode)
            .MaximumLength(20)
            .When(x => x.GLCode != null)
            .WithMessage("GL code cannot exceed 20 characters");

        RuleFor(x => x.DepartmentCode)
            .MaximumLength(20)
            .When(x => x.DepartmentCode != null)
            .WithMessage("Department code cannot exceed 20 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => x.Description != null)
            .WithMessage("Description cannot exceed 500 characters");
    }
}
