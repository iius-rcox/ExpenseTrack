using System.Text.Json;
using WireMock.Server;
using WireMock.Settings;
using Xunit;

namespace ExpenseFlow.Scenarios.Tests.Infrastructure;

/// <summary>
/// xUnit fixture that provides a WireMock server for mocking external HTTP services.
/// Used to mock Azure Document Intelligence, OpenAI, and Vista ERP endpoints.
/// </summary>
public class WireMockFixture : IAsyncLifetime
{
    private WireMockServer? _server;

    /// <summary>
    /// Gets the base URL of the WireMock server.
    /// </summary>
    public string BaseUrl => _server?.Url
        ?? throw new InvalidOperationException("WireMock server not initialized");

    /// <summary>
    /// Gets the WireMock server instance for advanced configuration.
    /// </summary>
    public WireMockServer Server => _server
        ?? throw new InvalidOperationException("WireMock server not initialized");

    /// <summary>
    /// Starts the WireMock server.
    /// </summary>
    public Task InitializeAsync()
    {
        _server = WireMockServer.Start(new WireMockServerSettings
        {
            Port = null, // Auto-assign port
            UseSSL = false,
            StartAdminInterface = true
        });

        // Load default stubs
        LoadDefaultStubs();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the WireMock server.
    /// </summary>
    public Task DisposeAsync()
    {
        _server?.Stop();
        _server?.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Resets all mappings to defaults.
    /// </summary>
    public void Reset()
    {
        _server?.Reset();
        LoadDefaultStubs();
    }

    /// <summary>
    /// Loads stub mappings from a JSON file.
    /// </summary>
    public void LoadStubsFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        var json = File.ReadAllText(filePath);
        _server?.WithMapping(json);
    }

    /// <summary>
    /// Configures a successful OCR response for Document Intelligence.
    /// </summary>
    public void SetupOcrSuccess(string receiptId, decimal amount, string vendor, DateTime date)
    {
        _server?.Given(
            WireMock.RequestBuilders.Request.Create()
                .WithPath("/formrecognizer/documentModels/prebuilt-receipt:analyze")
                .UsingPost())
            .RespondWith(
                WireMock.ResponseBuilders.Response.Create()
                    .WithStatusCode(202)
                    .WithHeader("Operation-Location", $"{BaseUrl}/formrecognizer/operations/{receiptId}")
            );

        _server?.Given(
            WireMock.RequestBuilders.Request.Create()
                .WithPath($"/formrecognizer/operations/{receiptId}")
                .UsingGet())
            .RespondWith(
                WireMock.ResponseBuilders.Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(new
                    {
                        status = "succeeded",
                        analyzeResult = new
                        {
                            documents = new[]
                            {
                                new
                                {
                                    fields = new
                                    {
                                        Total = new { value = amount },
                                        MerchantName = new { value = vendor },
                                        TransactionDate = new { value = date.ToString("yyyy-MM-dd") }
                                    }
                                }
                            }
                        }
                    }))
            );
    }

    /// <summary>
    /// Configures a failed OCR response.
    /// </summary>
    public void SetupOcrFailure(int statusCode = 500, string error = "Internal Server Error")
    {
        _server?.Given(
            WireMock.RequestBuilders.Request.Create()
                .WithPath("/formrecognizer/documentModels/prebuilt-receipt:analyze")
                .UsingPost())
            .RespondWith(
                WireMock.ResponseBuilders.Response.Create()
                    .WithStatusCode(statusCode)
                    .WithBody(JsonSerializer.Serialize(new { error = new { message = error } }))
            );
    }

    /// <summary>
    /// Configures a successful OpenAI categorization response.
    /// </summary>
    public void SetupOpenAiSuccess(string category, double confidence = 0.95)
    {
        _server?.Given(
            WireMock.RequestBuilders.Request.Create()
                .WithPath("/openai/deployments/*/chat/completions")
                .UsingPost())
            .RespondWith(
                WireMock.ResponseBuilders.Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(new
                    {
                        choices = new[]
                        {
                            new
                            {
                                message = new
                                {
                                    content = JsonSerializer.Serialize(new
                                    {
                                        category,
                                        confidence,
                                        reasoning = "Test categorization"
                                    })
                                }
                            }
                        }
                    }))
            );
    }

    /// <summary>
    /// Configures OpenAI rate limiting (429 response).
    /// </summary>
    public void SetupOpenAiRateLimit()
    {
        _server?.Given(
            WireMock.RequestBuilders.Request.Create()
                .WithPath("/openai/deployments/*/chat/completions")
                .UsingPost())
            .RespondWith(
                WireMock.ResponseBuilders.Response.Create()
                    .WithStatusCode(429)
                    .WithHeader("Retry-After", "60")
                    .WithBody("{\"error\":{\"code\":\"rate_limit_exceeded\"}}")
            );
    }

    private void LoadDefaultStubs()
    {
        // Default health check endpoint
        _server?.Given(
            WireMock.RequestBuilders.Request.Create()
                .WithPath("/health")
                .UsingGet())
            .RespondWith(
                WireMock.ResponseBuilders.Response.Create()
                    .WithStatusCode(200)
                    .WithBody("{\"status\":\"healthy\"}")
            );
    }
}

/// <summary>
/// Collection definition for sharing WireMock server across tests.
/// </summary>
[CollectionDefinition("WireMock")]
public class WireMockCollectionDefinition : ICollectionFixture<WireMockFixture>
{
}
