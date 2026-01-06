using ExpenseFlow.Shared.DTOs;
using FluentValidation;

namespace ExpenseFlow.Api.Validators;

/// <summary>
/// Validator for UpdateReceiptUrlRequestDto.
/// Ensures the receipt URL is within database column limits and is a valid URL format.
/// </summary>
public class UpdateReceiptUrlRequestValidator : AbstractValidator<UpdateReceiptUrlRequestDto>
{
    /// <summary>
    /// Maximum length for receipt URL (matches database column constraint).
    /// </summary>
    public const int MaxUrlLength = 2048;

    public UpdateReceiptUrlRequestValidator()
    {
        RuleFor(x => x.ReceiptUrl)
            .MaximumLength(MaxUrlLength)
            .When(x => !string.IsNullOrEmpty(x.ReceiptUrl))
            .WithMessage($"Receipt URL must not exceed {MaxUrlLength} characters");

        RuleFor(x => x.ReceiptUrl)
            .Must(BeAValidUrl)
            .When(x => !string.IsNullOrEmpty(x.ReceiptUrl))
            .WithMessage("Receipt URL must be a valid URL format (http:// or https://)");
    }

    /// <summary>
    /// Validates that the string is a properly formatted URL.
    /// </summary>
    private static bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var result)
            && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}

/// <summary>
/// Validator for DismissReceiptRequestDto.
/// Basic validation - the Dismiss property is nullable and has no complex constraints.
/// </summary>
public class DismissReceiptRequestValidator : AbstractValidator<DismissReceiptRequestDto>
{
    public DismissReceiptRequestValidator()
    {
        // No specific validation needed - bool? accepts true, false, or null
        // All are valid states for dismiss/restore functionality
    }
}
