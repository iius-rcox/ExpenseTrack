using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using Xunit;

namespace ExpenseFlow.LoadTests.Scenarios;

/// <summary>
/// Load tests for batch receipt processing.
/// SC-005: Batch processing of 50 receipts completes within 5 minutes.
/// </summary>
public class BatchReceiptProcessingTests : ScenarioBase
{
    private const int BatchSize = 50;
    private static readonly TimeSpan MaxProcessingTime = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Test uploading 50 receipts and verifying total processing time.
    /// Target: All 50 receipts processed within 5 minutes (300 seconds).
    /// </summary>
    [Fact(Skip = "Load test - run manually against staging")]
    public async Task BatchReceiptUpload_50Receipts_CompletesWithin5Minutes()
    {
        var uploadedReceiptIds = new List<Guid>();
        var startTime = DateTime.UtcNow;

        using var client = CreateHttpClient();

        // Upload phase: Upload 50 receipts sequentially (simulating real-world upload)
        var scenario = Scenario.Create("batch_receipt_upload", async context =>
        {
            // Create a synthetic test receipt
            var receiptContent = CreateTestReceiptContent(context.ScenarioInfo.ThreadNumber);

            using var formContent = new MultipartFormDataContent();
            formContent.Add(
                new ByteArrayContent(receiptContent),
                "file",
                $"test-receipt-{context.ScenarioInfo.ThreadNumber}.jpg");

            var request = Http.CreateRequest("POST", "/api/receipts/upload")
                .WithHeader("Authorization", $"Bearer {AuthToken}")
                .WithBody(formContent);

            var response = await Http.Send(client, request);

            if (response.IsError)
            {
                return Response.Fail(statusCode: response.StatusCode.ToString());
            }

            // Parse receipt ID from response
            var responseBody = await response.Payload.Value.Content.ReadAsStringAsync();
            // Assuming response contains { "id": "guid" }
            if (TryParseReceiptId(responseBody, out var receiptId))
            {
                lock (uploadedReceiptIds)
                {
                    uploadedReceiptIds.Add(receiptId);
                }
            }

            return Response.Ok(statusCode: response.StatusCode.ToString());
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 5, during: TimeSpan.FromMinutes(2))); // 5 parallel uploads

        // Run upload scenario
        var uploadResult = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder(Path.Combine("Reports", "BatchReceipt", DateTime.Now.ToString("yyyy-MM-dd_HH-mm")))
            .Run();

        // Verification phase: Poll for processing completion
        var processingComplete = await WaitForProcessingCompletion(
            client,
            uploadedReceiptIds,
            MaxProcessingTime);

        var totalTime = DateTime.UtcNow - startTime;

        // Assertions
        Assert.True(processingComplete, "Not all receipts completed processing within timeout");
        Assert.True(totalTime < MaxProcessingTime,
            $"Total processing time {totalTime.TotalSeconds}s exceeded limit of {MaxProcessingTime.TotalSeconds}s");
        Assert.True(uploadedReceiptIds.Count >= BatchSize,
            $"Expected {BatchSize} receipts uploaded, but only {uploadedReceiptIds.Count} succeeded");
    }

    /// <summary>
    /// Stress test: Upload receipts at high rate to find system limits.
    /// </summary>
    [Fact(Skip = "Load test - run manually against staging")]
    public void BatchReceiptUpload_StressTest_FindsLimit()
    {
        using var client = CreateHttpClient();

        var scenario = Scenario.Create("receipt_stress_test", async context =>
        {
            var receiptContent = CreateTestReceiptContent(context.InvocationNumber);

            using var formContent = new MultipartFormDataContent();
            formContent.Add(
                new ByteArrayContent(receiptContent),
                "file",
                $"stress-receipt-{context.InvocationNumber}.jpg");

            var request = Http.CreateRequest("POST", "/api/receipts/upload")
                .WithHeader("Authorization", $"Bearer {AuthToken}")
                .WithBody(formContent);

            var response = await Http.Send(client, request);

            return response.IsError
                ? Response.Fail(statusCode: response.StatusCode.ToString())
                : Response.Ok(statusCode: response.StatusCode.ToString());
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            // Ramp up to 10 concurrent uploads
            Simulation.RampingInject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
            // Hold at peak
            Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(2)));

        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder(Path.Combine("Reports", "ReceiptStress", DateTime.Now.ToString("yyyy-MM-dd_HH-mm")))
            .Run();

        // Log results for analysis
        var stats = result.ScenarioStats[0];
        Console.WriteLine($"Total Requests: {stats.AllRequestCount}");
        Console.WriteLine($"Success Rate: {stats.Ok.Request.Percent}%");
        Console.WriteLine($"Mean Latency: {stats.Ok.Latency.MeanMs}ms");
        Console.WriteLine($"P95 Latency: {stats.Ok.Latency.Percent95}ms");
    }

    private static byte[] CreateTestReceiptContent(int index)
    {
        // Create a minimal valid JPEG header (synthetic image for testing)
        // In production, use actual test image files
        var header = new byte[]
        {
            0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
            0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00
        };

        // Add some unique content based on index
        var content = new byte[1024];
        Buffer.BlockCopy(header, 0, content, 0, header.Length);
        BitConverter.GetBytes(index).CopyTo(content, header.Length);

        // JPEG end marker
        content[^2] = 0xFF;
        content[^1] = 0xD9;

        return content;
    }

    private static bool TryParseReceiptId(string json, out Guid receiptId)
    {
        receiptId = Guid.Empty;
        try
        {
            // Simple parsing - in production use proper JSON deserialization
            var idStart = json.IndexOf("\"id\":", StringComparison.OrdinalIgnoreCase);
            if (idStart < 0) return false;

            var guidStart = json.IndexOf('"', idStart + 5) + 1;
            var guidEnd = json.IndexOf('"', guidStart);
            var guidStr = json.Substring(guidStart, guidEnd - guidStart);

            return Guid.TryParse(guidStr, out receiptId);
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> WaitForProcessingCompletion(
        HttpClient client,
        List<Guid> receiptIds,
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        var pendingReceipts = new HashSet<Guid>(receiptIds);

        while (DateTime.UtcNow < deadline && pendingReceipts.Count > 0)
        {
            foreach (var receiptId in pendingReceipts.ToList())
            {
                try
                {
                    var response = await client.GetAsync($"/api/receipts/{receiptId}");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        // Check if status is "Completed" or similar
                        if (content.Contains("\"status\":\"Completed\"", StringComparison.OrdinalIgnoreCase) ||
                            content.Contains("\"status\":\"Processed\"", StringComparison.OrdinalIgnoreCase))
                        {
                            pendingReceipts.Remove(receiptId);
                        }
                    }
                }
                catch
                {
                    // Continue polling
                }
            }

            if (pendingReceipts.Count > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }

        return pendingReceipts.Count == 0;
    }
}
