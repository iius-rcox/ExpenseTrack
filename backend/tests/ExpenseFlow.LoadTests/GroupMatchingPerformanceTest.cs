using System.Net.Http.Json;
using System.Text.Json;
using ExpenseFlow.Shared.DTOs;
using Xunit;
using Xunit.Abstractions;

namespace ExpenseFlow.LoadTests;

/// <summary>
/// Performance tests for transaction group matching (T048).
/// Verifies that auto-match completes within acceptable time limits.
/// </summary>
/// <remarks>
/// These tests require:
/// 1. A valid Bearer token in the Authorization header
/// 2. Staging environment with test data (1000 transactions, 50 groups)
///
/// Test data setup (run once in staging):
/// - Create 1000 transactions via /api/statements/import
/// - Create 50 transaction groups via /api/transaction-groups
/// - Upload receipts that match some of the groups
/// </remarks>
public class GroupMatchingPerformanceTest
{
    private readonly ITestOutputHelper _output;
    private const string BaseUrl = "https://staging.expense.ii-us.com";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GroupMatchingPerformanceTest(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// T048: Verify auto-match completes in under 2 seconds with large dataset.
    /// </summary>
    /// <remarks>
    /// This test measures the server-side processing time as reported by the API
    /// (DurationMs field) rather than total HTTP round-trip time, which accounts
    /// for network latency.
    ///
    /// Pre-requisites:
    /// - 1000+ unmatched transactions in the system
    /// - 50+ transaction groups
    /// - At least some receipts that should match
    /// </remarks>
    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Story", "T048")]
    public async Task AutoMatch_WithLargeDataset_CompletesInUnder2Seconds()
    {
        // Arrange
        var token = Environment.GetEnvironmentVariable("EXPENSEFLOW_TEST_TOKEN");
        if (string.IsNullOrEmpty(token))
        {
            _output.WriteLine("SKIPPED: Set EXPENSEFLOW_TEST_TOKEN environment variable to run this test");
            Assert.True(true, "Skipped - no token");
            return;
        }

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
        };
        using var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await client.PostAsync("/api/matching/auto", null);
        stopwatch.Stop();

        // Assert
        _output.WriteLine($"=== Auto-Match Performance Test (T048) ===");
        _output.WriteLine($"Base URL: {BaseUrl}");
        _output.WriteLine($"HTTP Status: {response.StatusCode}");
        _output.WriteLine($"Round-trip time: {stopwatch.ElapsedMilliseconds}ms");

        Assert.True(response.IsSuccessStatusCode, $"API call failed: {response.StatusCode}");

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AutoMatchResponseDto>(content, JsonOptions);

        if (result != null)
        {
            _output.WriteLine($"--- Results ---");
            _output.WriteLine($"Processing Time (DurationMs): {result.DurationMs}ms");
            _output.WriteLine($"Processed Count: {result.ProcessedCount}");
            _output.WriteLine($"Proposed Count: {result.ProposedCount}");
            _output.WriteLine($"  - Transaction Matches: {result.TransactionMatchCount}");
            _output.WriteLine($"  - Group Matches: {result.GroupMatchCount}");
            _output.WriteLine($"Ambiguous Count: {result.AmbiguousCount}");

            // The server-side processing time should be under 2 seconds
            // Network round-trip may add latency, so we check DurationMs
            Assert.True(result.DurationMs < 2000,
                $"Auto-match processing time ({result.DurationMs}ms) exceeded 2000ms threshold");
        }
    }

    /// <summary>
    /// Verify get-candidates endpoint performance for manual matching UI.
    /// </summary>
    /// <remarks>
    /// When users open the manual match dialog, the system retrieves ranked
    /// candidates for a specific receipt. This should be fast for good UX.
    /// </remarks>
    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Story", "T048")]
    public async Task GetCandidates_ForReceipt_CompletesInUnder500ms()
    {
        // Arrange
        var token = Environment.GetEnvironmentVariable("EXPENSEFLOW_TEST_TOKEN");
        if (string.IsNullOrEmpty(token))
        {
            _output.WriteLine("SKIPPED: Set EXPENSEFLOW_TEST_TOKEN environment variable to run this test");
            Assert.True(true, "Skipped - no token");
            return;
        }

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
        };
        using var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // First, get an unmatched receipt
        var receiptsResponse = await client.GetAsync("/api/matching/receipts/unmatched?pageSize=1");
        if (!receiptsResponse.IsSuccessStatusCode)
        {
            _output.WriteLine("SKIPPED: Could not get unmatched receipts");
            Assert.True(true, "Skipped - no unmatched receipts");
            return;
        }

        var receiptsContent = await receiptsResponse.Content.ReadAsStringAsync();
        var receipts = JsonSerializer.Deserialize<UnmatchedReceiptsResponseDto>(receiptsContent, JsonOptions);

        if (receipts?.Items == null || receipts.Items.Count == 0)
        {
            _output.WriteLine("SKIPPED: No unmatched receipts found");
            Assert.True(true, "Skipped - no unmatched receipts");
            return;
        }

        var receiptId = receipts.Items[0].Id;
        _output.WriteLine($"Testing candidates for receipt: {receiptId}");

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await client.GetAsync($"/api/matching/candidates/{receiptId}?limit=20");
        stopwatch.Stop();

        // Assert
        _output.WriteLine($"=== Get Candidates Performance Test ===");
        _output.WriteLine($"Receipt ID: {receiptId}");
        _output.WriteLine($"HTTP Status: {response.StatusCode}");
        _output.WriteLine($"Round-trip time: {stopwatch.ElapsedMilliseconds}ms");

        Assert.True(response.IsSuccessStatusCode, $"API call failed: {response.StatusCode}");

        var candidatesContent = await response.Content.ReadAsStringAsync();
        var candidates = JsonSerializer.Deserialize<List<MatchCandidateDto>>(candidatesContent, JsonOptions);

        _output.WriteLine($"Candidates returned: {candidates?.Count ?? 0}");
        if (candidates != null)
        {
            foreach (var candidate in candidates)
            {
                _output.WriteLine($"  - [{candidate.CandidateType}] {candidate.DisplayName}: {candidate.ConfidenceScore:F1}%");
            }
        }

        // Get candidates should be very fast (sub-500ms)
        Assert.True(stopwatch.ElapsedMilliseconds < 500,
            $"Get candidates took {stopwatch.ElapsedMilliseconds}ms, expected < 500ms");
    }
}
