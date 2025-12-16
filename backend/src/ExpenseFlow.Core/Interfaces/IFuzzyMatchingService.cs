namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service for fuzzy string matching using Levenshtein distance.
/// </summary>
public interface IFuzzyMatchingService
{
    /// <summary>
    /// Calculates the similarity between two strings using Normalized Levenshtein distance.
    /// </summary>
    /// <param name="a">First string</param>
    /// <param name="b">Second string</param>
    /// <returns>Similarity score from 0.0 (completely different) to 1.0 (identical)</returns>
    double CalculateSimilarity(string a, string b);
}
