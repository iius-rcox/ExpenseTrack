using ExpenseFlow.Shared.DTOs;
using FluentValidation;

namespace ExpenseFlow.Api.Validators;

/// <summary>
/// Validator for GenerateDraftRequest.
/// </summary>
public class GenerateDraftRequestValidator : AbstractValidator<GenerateDraftRequest>
{
    public GenerateDraftRequestValidator()
    {
        RuleFor(x => x.Period)
            .NotEmpty()
            .WithMessage("Period is required")
            .Matches(@"^\d{4}-(0[1-9]|1[0-2])$")
            .WithMessage("Period must be in YYYY-MM format (e.g., 2024-01)")
            .Must(BeValidPeriod)
            .WithMessage("Period cannot be in the future");
    }

    private static bool BeValidPeriod(string period)
    {
        if (string.IsNullOrEmpty(period) || period.Length != 7)
            return true; // Let other validators handle format issues

        if (!int.TryParse(period[..4], out var year) ||
            !int.TryParse(period[5..], out var month))
            return true;

        // Validate month is in valid range
        if (month < 1 || month > 12 || year < 1)
            return true; // Let the Matches validator handle format issues

        var periodDate = new DateOnly(year, month, 1);
        var currentPeriod = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        return periodDate <= currentPeriod;
    }
}
