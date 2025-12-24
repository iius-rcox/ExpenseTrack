using ExpenseFlow.Shared.DTOs;
using FluentValidation;

namespace ExpenseFlow.Api.Validators;

/// <summary>
/// Validator for UpdatePreferencesRequest.
/// </summary>
public class UpdatePreferencesRequestValidator : AbstractValidator<UpdatePreferencesRequest>
{
    private static readonly string[] ValidThemes = { "light", "dark", "system" };

    public UpdatePreferencesRequestValidator()
    {
        RuleFor(x => x.Theme)
            .Must(theme => ValidThemes.Contains(theme, StringComparer.OrdinalIgnoreCase))
            .When(x => x.Theme is not null)
            .WithMessage("Theme must be one of: light, dark, system");
    }
}
