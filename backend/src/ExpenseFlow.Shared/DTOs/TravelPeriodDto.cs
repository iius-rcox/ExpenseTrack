using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Summary view of a travel period for list displays.
/// </summary>
public class TravelPeriodSummaryDto
{
    /// <summary>
    /// Unique travel period identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Start date of travel.
    /// </summary>
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// End date of travel.
    /// </summary>
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Travel destination (city, airport code, etc.).
    /// </summary>
    public string? Destination { get; set; }

    /// <summary>
    /// How the period was created (Flight, Hotel, Manual).
    /// </summary>
    public TravelPeriodSource Source { get; set; }

    /// <summary>
    /// Whether this period requires AI review.
    /// </summary>
    public bool RequiresAiReview { get; set; }

    /// <summary>
    /// Count of linked expenses.
    /// </summary>
    public int ExpenseCount { get; set; }
}

/// <summary>
/// Detailed view of a travel period.
/// </summary>
public class TravelPeriodDetailDto
{
    /// <summary>
    /// Unique travel period identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Start date of travel.
    /// </summary>
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// End date of travel.
    /// </summary>
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Travel destination (city, airport code, etc.).
    /// </summary>
    public string? Destination { get; set; }

    /// <summary>
    /// How the period was created (Flight, Hotel, Manual).
    /// </summary>
    public TravelPeriodSource Source { get; set; }

    /// <summary>
    /// ID of the source receipt that triggered detection.
    /// </summary>
    public Guid? SourceReceiptId { get; set; }

    /// <summary>
    /// Whether this period requires AI review.
    /// </summary>
    public bool RequiresAiReview { get; set; }

    /// <summary>
    /// When the period was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the period was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Request to create a new travel period manually.
/// </summary>
public class CreateTravelPeriodRequestDto
{
    /// <summary>
    /// Start date of travel.
    /// </summary>
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// End date of travel.
    /// </summary>
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Travel destination (city, airport code, etc.).
    /// </summary>
    public string? Destination { get; set; }
}

/// <summary>
/// Request to update an existing travel period.
/// </summary>
public class UpdateTravelPeriodRequestDto
{
    /// <summary>
    /// Start date of travel.
    /// </summary>
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// End date of travel.
    /// </summary>
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Travel destination (city, airport code, etc.).
    /// </summary>
    public string? Destination { get; set; }
}

/// <summary>
/// Paginated list of travel periods.
/// </summary>
public class TravelPeriodListResponseDto
{
    /// <summary>
    /// List of travel periods.
    /// </summary>
    public List<TravelPeriodSummaryDto> TravelPeriods { get; set; } = new();

    /// <summary>
    /// Total count of travel periods matching filters.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size.
    /// </summary>
    public int PageSize { get; set; }
}
