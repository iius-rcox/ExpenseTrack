namespace ExpenseFlow.Api.Validators;

/// <summary>
/// Validation helpers for analytics endpoints.
/// </summary>
public static class AnalyticsValidation
{
    /// <summary>
    /// Maximum allowed date range in days (5 years = 1,826 days).
    /// </summary>
    public const int MaxDateRangeDays = 1826;

    /// <summary>
    /// Valid granularity values for spending trends.
    /// </summary>
    public static readonly string[] ValidGranularities = { "day", "week", "month" };

    /// <summary>
    /// Valid confidence levels for subscription filtering.
    /// </summary>
    public static readonly string[] ValidConfidenceLevels = { "high", "medium", "low" };

    /// <summary>
    /// Valid frequency values for subscription filtering.
    /// </summary>
    public static readonly string[] ValidFrequencies =
        { "weekly", "biweekly", "monthly", "quarterly", "annual" };

    /// <summary>
    /// Validates a date range for analytics queries.
    /// </summary>
    /// <param name="startDateStr">Start date string in YYYY-MM-DD format.</param>
    /// <param name="endDateStr">End date string in YYYY-MM-DD format.</param>
    /// <param name="startDate">Parsed start date if valid.</param>
    /// <param name="endDate">Parsed end date if valid.</param>
    /// <param name="error">Error message if validation fails.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public static bool ValidateDateRange(
        string? startDateStr,
        string? endDateStr,
        out DateOnly startDate,
        out DateOnly endDate,
        out string? error)
    {
        startDate = default;
        endDate = default;
        error = null;

        // Check for required parameters
        if (string.IsNullOrWhiteSpace(startDateStr))
        {
            error = "startDate is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(endDateStr))
        {
            error = "endDate is required.";
            return false;
        }

        // Parse start date
        if (!DateOnly.TryParse(startDateStr, out startDate))
        {
            error = $"Invalid startDate format. Expected YYYY-MM-DD, got '{startDateStr}'.";
            return false;
        }

        // Parse end date
        if (!DateOnly.TryParse(endDateStr, out endDate))
        {
            error = $"Invalid endDate format. Expected YYYY-MM-DD, got '{endDateStr}'.";
            return false;
        }

        // Validate start <= end
        if (startDate > endDate)
        {
            error = "startDate must be less than or equal to endDate.";
            return false;
        }

        // Validate range doesn't exceed 5 years
        var daysDiff = endDate.DayNumber - startDate.DayNumber;
        if (daysDiff > MaxDateRangeDays)
        {
            error = $"Date range exceeds maximum of 5 years ({MaxDateRangeDays} days). Requested range: {daysDiff} days.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates the granularity parameter for spending trends.
    /// </summary>
    /// <param name="granularity">The granularity value to validate.</param>
    /// <param name="error">Error message if validation fails.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public static bool ValidateGranularity(string? granularity, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(granularity))
        {
            // Default is fine, no error
            return true;
        }

        if (!ValidGranularities.Contains(granularity.ToLowerInvariant()))
        {
            error = $"Invalid granularity value. Expected one of: {string.Join(", ", ValidGranularities)}.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates the topCount parameter for merchant analytics.
    /// </summary>
    /// <param name="topCount">The topCount value to validate.</param>
    /// <param name="error">Error message if validation fails.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public static bool ValidateTopCount(int topCount, out string? error)
    {
        error = null;

        if (topCount < 1 || topCount > 100)
        {
            error = "topCount must be between 1 and 100.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates the minConfidence parameter for subscription filtering.
    /// </summary>
    /// <param name="minConfidence">The confidence level to validate.</param>
    /// <param name="error">Error message if validation fails.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public static bool ValidateConfidenceLevel(string? minConfidence, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(minConfidence))
        {
            // No filter is fine
            return true;
        }

        if (!ValidConfidenceLevels.Contains(minConfidence.ToLowerInvariant()))
        {
            error = $"Invalid minConfidence value. Expected one of: {string.Join(", ", ValidConfidenceLevels)}.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates the frequency parameter for subscription filtering.
    /// </summary>
    /// <param name="frequencies">The frequency values to validate.</param>
    /// <param name="error">Error message if validation fails.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public static bool ValidateFrequencies(List<string>? frequencies, out string? error)
    {
        error = null;

        if (frequencies == null || frequencies.Count == 0)
        {
            // No filter is fine
            return true;
        }

        foreach (var freq in frequencies)
        {
            if (!ValidFrequencies.Contains(freq.ToLowerInvariant()))
            {
                error = $"Invalid frequency value '{freq}'. Expected one of: {string.Join(", ", ValidFrequencies)}.";
                return false;
            }
        }

        return true;
    }
}
