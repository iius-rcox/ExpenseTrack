namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Result of report validation before generation.
/// </summary>
public class ReportValidationResultDto
{
    /// <summary>Whether the report passed all validation rules</summary>
    public bool IsValid => !Errors.Any();

    /// <summary>Validation errors that must be fixed before generating</summary>
    public List<ValidationErrorDto> Errors { get; set; } = new();

    /// <summary>Non-blocking warnings</summary>
    public List<ValidationWarningDto> Warnings { get; set; } = new();
}

/// <summary>
/// A validation error on a specific line or report-level.
/// </summary>
public class ValidationErrorDto
{
    /// <summary>Line ID if error is on a specific line, null for report-level errors</summary>
    public Guid? LineId { get; set; }

    /// <summary>Field name that has the error</summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>Error message</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Error code for programmatic handling</summary>
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// A non-blocking warning.
/// </summary>
public class ValidationWarningDto
{
    /// <summary>Line ID if warning is on a specific line, null for report-level warnings</summary>
    public Guid? LineId { get; set; }

    /// <summary>Field name with the warning</summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>Warning message</summary>
    public string Message { get; set; } = string.Empty;
}
