using ExpenseFlow.Shared.DTOs;
using FluentValidation;

namespace ExpenseFlow.Api.Validators;

/// <summary>
/// Validator for ReceiptUpdateRequestDto.
/// Ensures amounts are non-negative and field values are within expected ranges.
/// </summary>
public class ReceiptUpdateRequestValidator : AbstractValidator<ReceiptUpdateRequestDto>
{
    private static readonly string[] ValidCurrencies =
    {
        "USD", "EUR", "GBP", "CAD", "AUD", "JPY", "CHF", "CNY", "INR", "MXN"
    };

    public ReceiptUpdateRequestValidator()
    {
        RuleFor(x => x.Vendor)
            .MaximumLength(500)
            .When(x => x.Vendor is not null)
            .WithMessage("Vendor name must not exceed 500 characters");

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Amount.HasValue)
            .WithMessage("Amount must be a non-negative number");

        RuleFor(x => x.Tax)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Tax.HasValue)
            .WithMessage("Tax must be a non-negative number");

        RuleFor(x => x.Tax)
            .LessThanOrEqualTo(x => x.Amount)
            .When(x => x.Tax.HasValue && x.Amount.HasValue)
            .WithMessage("Tax cannot exceed the total amount");

        RuleFor(x => x.Currency)
            .Length(3)
            .When(x => x.Currency is not null)
            .WithMessage("Currency must be a 3-character ISO code");

        RuleFor(x => x.Currency)
            .Must(c => ValidCurrencies.Contains(c, StringComparer.OrdinalIgnoreCase))
            .When(x => x.Currency is not null)
            .WithMessage($"Currency must be one of: {string.Join(", ", ValidCurrencies)}");

        RuleFor(x => x.Date)
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .When(x => x.Date.HasValue)
            .WithMessage("Date cannot be in the future");

        RuleForEach(x => x.LineItems)
            .SetValidator(new LineItemValidator())
            .When(x => x.LineItems is not null);

        RuleForEach(x => x.Corrections)
            .SetValidator(new CorrectionMetadataValidator())
            .When(x => x.Corrections is not null);
    }
}

/// <summary>
/// Validator for LineItemDto.
/// </summary>
public class LineItemValidator : AbstractValidator<LineItemDto>
{
    public LineItemValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Line item description is required")
            .MaximumLength(500)
            .WithMessage("Line item description must not exceed 500 characters");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .When(x => x.Quantity.HasValue)
            .WithMessage("Quantity must be greater than zero");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.UnitPrice.HasValue)
            .WithMessage("Unit price must be a non-negative number");

        RuleFor(x => x.TotalPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.TotalPrice.HasValue)
            .WithMessage("Total price must be a non-negative number");

        RuleFor(x => x.Confidence)
            .InclusiveBetween(0, 1)
            .When(x => x.Confidence.HasValue)
            .WithMessage("Confidence must be between 0 and 1");
    }
}

/// <summary>
/// Validator for CorrectionMetadataDto.
/// </summary>
public class CorrectionMetadataValidator : AbstractValidator<CorrectionMetadataDto>
{
    private static readonly string[] ValidFieldNames =
    {
        "vendor", "amount", "date", "tax", "currency", "line_item"
    };

    private static readonly string[] ValidLineItemFields =
    {
        "description", "quantity", "unitPrice", "totalPrice"
    };

    public CorrectionMetadataValidator()
    {
        RuleFor(x => x.FieldName)
            .NotEmpty()
            .WithMessage("Field name is required")
            .Must(f => ValidFieldNames.Contains(f, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Field name must be one of: {string.Join(", ", ValidFieldNames)}");

        RuleFor(x => x.OriginalValue)
            .NotNull()
            .WithMessage("Original value is required for training feedback");

        RuleFor(x => x.LineItemIndex)
            .GreaterThanOrEqualTo(0)
            .When(x => x.LineItemIndex.HasValue)
            .WithMessage("Line item index must be non-negative");

        RuleFor(x => x.LineItemIndex)
            .NotNull()
            .When(x => x.FieldName.Equals("line_item", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Line item index is required when field name is 'line_item'");

        RuleFor(x => x.LineItemField)
            .NotEmpty()
            .When(x => x.FieldName.Equals("line_item", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Line item field is required when field name is 'line_item'");

        RuleFor(x => x.LineItemField)
            .Must(f => ValidLineItemFields.Contains(f!, StringComparer.OrdinalIgnoreCase))
            .When(x => !string.IsNullOrEmpty(x.LineItemField))
            .WithMessage($"Line item field must be one of: {string.Join(", ", ValidLineItemFields)}");
    }
}
