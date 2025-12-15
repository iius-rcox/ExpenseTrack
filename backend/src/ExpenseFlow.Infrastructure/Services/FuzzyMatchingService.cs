using ExpenseFlow.Core.Interfaces;
using F23.StringSimilarity;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for fuzzy string matching using Normalized Levenshtein distance.
/// </summary>
public class FuzzyMatchingService : IFuzzyMatchingService
{
    private readonly NormalizedLevenshtein _levenshtein;

    public FuzzyMatchingService()
    {
        _levenshtein = new NormalizedLevenshtein();
    }

    /// <inheritdoc />
    public double CalculateSimilarity(string a, string b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
        {
            return 0.0;
        }

        // Normalize strings for comparison (uppercase, trim)
        var normalizedA = a.Trim().ToUpperInvariant();
        var normalizedB = b.Trim().ToUpperInvariant();

        // NormalizedLevenshtein returns distance (0=identical, 1=completely different)
        // We want similarity (1=identical, 0=completely different)
        var distance = _levenshtein.Distance(normalizedA, normalizedB);
        return 1.0 - distance;
    }
}
