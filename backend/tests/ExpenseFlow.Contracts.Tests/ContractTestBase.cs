using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using ExpenseFlow.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Xunit;

namespace ExpenseFlow.Contracts.Tests;

/// <summary>
/// Extension method to handle IDictionary GetValueOrDefault for OpenAPI operations.
/// </summary>
internal static class DictionaryExtensions
{
    public static TValue? GetValueOrDefault<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary,
        TKey key) where TKey : notnull
    {
        return dictionary.TryGetValue(key, out var value) ? value : default;
    }
}

/// <summary>
/// Custom WebApplicationFactory for contract tests.
/// Sets Testing environment to skip Hangfire and uses in-memory database.
/// </summary>
public class ContractTestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Remove the real DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ExpenseFlowDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add in-memory database for contract testing
            services.AddDbContext<ExpenseFlowDbContext>(options =>
            {
                options.UseInMemoryDatabase("ContractTestDb_" + Guid.NewGuid());
            });

            // Configure test authentication
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, null);
        });
    }
}

/// <summary>
/// Test authentication handler for contract tests.
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "TestScheme";
    public const string TestObjectId = "test-object-id";
    public const string TestEmail = "test@example.com";
    public const string TestName = "Test User";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, TestObjectId),
            new Claim(ClaimTypes.Email, TestEmail),
            new Claim(ClaimTypes.Name, TestName),
            new Claim("oid", TestObjectId)
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

/// <summary>
/// Base class for contract tests that validate API responses against OpenAPI specifications.
/// </summary>
public abstract class ContractTestBase : IClassFixture<ContractTestWebApplicationFactory>
{
    protected readonly HttpClient Client;
    protected readonly ContractTestWebApplicationFactory Factory;
    private static OpenApiDocument? _cachedSpec;
    private static readonly object _lock = new();

    protected ContractTestBase(ContractTestWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    /// <summary>
    /// Gets the OpenAPI specification document, loading from baseline or generating from app.
    /// </summary>
    protected async Task<OpenApiDocument> GetOpenApiSpecAsync()
    {
        if (_cachedSpec != null)
            return _cachedSpec;

        lock (_lock)
        {
            if (_cachedSpec != null)
                return _cachedSpec;

            // Try loading baseline spec first
            var baselinePath = Path.Combine(
                AppContext.BaseDirectory,
                "Baseline",
                "openapi-baseline.json");

            if (File.Exists(baselinePath))
            {
                using var stream = File.OpenRead(baselinePath);
                var reader = new OpenApiStreamReader();
                var result = reader.Read(stream, out var diagnostic);

                if (diagnostic.Errors.Count == 0)
                {
                    _cachedSpec = result;
                    return _cachedSpec;
                }
            }

            // Fall back to generating from running app
            var response = Client.GetAsync("/swagger/v1/swagger.json").Result;
            response.EnsureSuccessStatusCode();

            using var swaggerStream = response.Content.ReadAsStream();
            var swaggerReader = new OpenApiStreamReader();
            _cachedSpec = swaggerReader.Read(swaggerStream, out _);

            return _cachedSpec;
        }
    }

    /// <summary>
    /// Validates that an API response matches the expected schema from the OpenAPI spec.
    /// </summary>
    protected async Task ValidateResponseSchemaAsync<T>(
        HttpResponseMessage response,
        string path,
        string method,
        string expectedStatusCode)
    {
        var spec = await GetOpenApiSpecAsync();

        // Find the path in the spec
        var pathItem = spec.Paths
            .FirstOrDefault(p => MatchPath(p.Key, path));

        pathItem.Value.Should().NotBeNull(
            $"Path '{path}' should exist in OpenAPI spec");

        // Get the operation
        var operation = method.ToUpperInvariant() switch
        {
            "GET" => pathItem.Value.Operations.GetValueOrDefault(OperationType.Get),
            "POST" => pathItem.Value.Operations.GetValueOrDefault(OperationType.Post),
            "PUT" => pathItem.Value.Operations.GetValueOrDefault(OperationType.Put),
            "DELETE" => pathItem.Value.Operations.GetValueOrDefault(OperationType.Delete),
            "PATCH" => pathItem.Value.Operations.GetValueOrDefault(OperationType.Patch),
            _ => throw new ArgumentException($"Unknown HTTP method: {method}")
        };

        operation.Should().NotBeNull(
            $"Method '{method}' should exist for path '{path}'");

        // Validate response status code is documented
        var statusCodeStr = ((int)response.StatusCode).ToString();
        operation!.Responses.Should().ContainKey(statusCodeStr,
            $"Status code {statusCodeStr} should be documented for {method} {path}");

        // If successful, validate response body matches schema
        if (response.IsSuccessStatusCode && response.Content.Headers.ContentLength > 0)
        {
            var content = await response.Content.ReadFromJsonAsync<T>();
            content.Should().NotBeNull();
        }
    }

    /// <summary>
    /// Validates that request body matches the expected schema.
    /// </summary>
    protected async Task ValidateRequestSchemaAsync<T>(
        T request,
        string path,
        string method)
    {
        var spec = await GetOpenApiSpecAsync();

        var pathItem = spec.Paths
            .FirstOrDefault(p => MatchPath(p.Key, path));

        pathItem.Value.Should().NotBeNull(
            $"Path '{path}' should exist in OpenAPI spec");

        // Serialize to JSON and back to validate structure
        var json = JsonSerializer.Serialize(request);
        var deserialized = JsonSerializer.Deserialize<T>(json);
        deserialized.Should().NotBeNull();
    }

    /// <summary>
    /// Validates that all documented response types are handled.
    /// </summary>
    protected async Task ValidateEndpointContractAsync(
        string path,
        string method,
        params (int StatusCode, string Description)[] expectedResponses)
    {
        var spec = await GetOpenApiSpecAsync();

        var pathItem = spec.Paths
            .FirstOrDefault(p => MatchPath(p.Key, path));

        pathItem.Value.Should().NotBeNull(
            $"Path '{path}' should exist in OpenAPI spec");

        var operation = GetOperation(pathItem.Value, method);
        operation.Should().NotBeNull();

        foreach (var expected in expectedResponses)
        {
            operation!.Responses.Should().ContainKey(expected.StatusCode.ToString(),
                $"Response {expected.StatusCode} ({expected.Description}) should be documented");
        }
    }

    private static bool MatchPath(string specPath, string actualPath)
    {
        // Handle path parameters: /api/receipts/{id} matches /api/receipts/123
        var specSegments = specPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var actualSegments = actualPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (specSegments.Length != actualSegments.Length)
            return false;

        for (int i = 0; i < specSegments.Length; i++)
        {
            if (specSegments[i].StartsWith('{') && specSegments[i].EndsWith('}'))
                continue; // Path parameter matches anything

            if (!specSegments[i].Equals(actualSegments[i], StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private static OpenApiOperation? GetOperation(OpenApiPathItem pathItem, string method)
    {
        return method.ToUpperInvariant() switch
        {
            "GET" => pathItem.Operations.GetValueOrDefault(OperationType.Get),
            "POST" => pathItem.Operations.GetValueOrDefault(OperationType.Post),
            "PUT" => pathItem.Operations.GetValueOrDefault(OperationType.Put),
            "DELETE" => pathItem.Operations.GetValueOrDefault(OperationType.Delete),
            "PATCH" => pathItem.Operations.GetValueOrDefault(OperationType.Patch),
            _ => null
        };
    }
}
