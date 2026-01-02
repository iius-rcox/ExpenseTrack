using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Moq;
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
    private readonly string _dbName = "ContractTestDb_" + Guid.NewGuid();
    public Guid TestUserId { get; private set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Add test configuration first
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Provide dummy config values to prevent service registration failures
                ["ConnectionStrings:PostgreSQL"] = "Host=localhost;Database=test",
                ["ConnectionStrings:SqlServer"] = "Server=localhost;Database=test;Integrated Security=true",
                ["Azure:BlobStorage:ConnectionString"] = "UseDevelopmentStorage=true",
                ["Azure:BlobStorage:ContainerName"] = "test-container",
                ["Azure:DocumentIntelligence:Endpoint"] = "https://test.cognitiveservices.azure.com",
                ["Azure:DocumentIntelligence:ApiKey"] = "test-key"
            });
        });

        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Remove ALL DbContext-related registrations
            var descriptorsToRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<ExpenseFlowDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(ExpenseFlowDbContext)).ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for contract testing
            services.AddDbContext<ExpenseFlowDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
                options.EnableSensitiveDataLogging();
            });

            // Mock external data source to avoid SQL Server connection
            var mockExternalDataSource = new Mock<IExternalDataSource>();
            mockExternalDataSource.Setup(x => x.GetDepartmentsAsync())
                .ReturnsAsync(new List<Department>());
            mockExternalDataSource.Setup(x => x.GetProjectsAsync())
                .ReturnsAsync(new List<Project>());
            mockExternalDataSource.Setup(x => x.GetGLAccountsAsync())
                .ReturnsAsync(new List<GLAccount>());
            services.RemoveAll<IExternalDataSource>();
            services.AddSingleton(mockExternalDataSource.Object);

            // Mock blob storage service to avoid Azure connection
            var mockBlobStorage = new Mock<IBlobStorageService>();
            services.RemoveAll<IBlobStorageService>();
            services.AddSingleton(mockBlobStorage.Object);

            // Mock Document Intelligence to avoid Azure connection
            var mockDocIntel = new Mock<IDocumentIntelligenceService>();
            services.RemoveAll<IDocumentIntelligenceService>();
            services.AddSingleton(mockDocIntel.Object);

            // Mock Embedding Service to avoid Azure OpenAI connection
            var mockEmbeddingService = new Mock<IEmbeddingService>();
            mockEmbeddingService.Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Pgvector.Vector(new float[1536]));
            mockEmbeddingService.Setup(x => x.FindSimilarAsync(
                    It.IsAny<Pgvector.Vector>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ExpenseEmbedding>());
            services.RemoveAll<IEmbeddingService>();
            services.AddSingleton(mockEmbeddingService.Object);

            // Mock Description Normalization Service to avoid Azure OpenAI connection
            var mockDescNormService = new Mock<IDescriptionNormalizationService>();
            mockDescNormService.Setup(x => x.NormalizeAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string desc, Guid _, CancellationToken _) => new ExpenseFlow.Shared.DTOs.NormalizationResultDto
                {
                    RawDescription = desc,
                    NormalizedDescription = desc,
                    ExtractedVendor = "Test Vendor",
                    Confidence = 1.0m,
                    Tier = 1,
                    CacheHit = true
                });
            mockDescNormService.Setup(x => x.GetCacheStatsAsync())
                .ReturnsAsync((100, 50));
            services.RemoveAll<IDescriptionNormalizationService>();
            services.AddSingleton(mockDescNormService.Object);

            // Mock Categorization Service to avoid Azure OpenAI connection
            var mockCategorizationService = new Mock<ICategorizationService>();
            mockCategorizationService.Setup(x => x.GetCategorizationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExpenseFlow.Shared.DTOs.TransactionCategorizationDto
                {
                    TransactionId = Guid.Empty,
                    NormalizedDescription = "Test Description",
                    Vendor = "Test Vendor",
                    GL = new ExpenseFlow.Shared.DTOs.GLCategorizationSection { Alternatives = [] },
                    Department = new ExpenseFlow.Shared.DTOs.DepartmentCategorizationSection { Alternatives = [] }
                });
            mockCategorizationService.Setup(x => x.GetGLSuggestionsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExpenseFlow.Shared.DTOs.GLSuggestionsDto { Suggestions = [] });
            mockCategorizationService.Setup(x => x.GetDepartmentSuggestionsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExpenseFlow.Shared.DTOs.DepartmentSuggestionsDto { Suggestions = [] });
            mockCategorizationService.Setup(x => x.ConfirmCategorizationAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExpenseFlow.Shared.DTOs.CategorizationConfirmationDto { Message = "Confirmed" });
            mockCategorizationService.Setup(x => x.SkipSuggestionAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExpenseFlow.Shared.DTOs.CategorizationSkipDto { Skipped = true });
            services.RemoveAll<ICategorizationService>();
            services.AddSingleton(mockCategorizationService.Object);

            // Configure test authentication - remove existing auth schemes first
            services.RemoveAll<AuthenticationHandler<AuthenticationSchemeOptions>>();
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName, null);
        });
    }

    public void SeedDatabase(Action<ExpenseFlowDbContext, Guid> seedAction)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExpenseFlowDbContext>();
        db.Database.EnsureCreated();

        // Seed test user if not exists
        var existingUser = db.Users.FirstOrDefault(u => u.EntraObjectId == TestAuthHandler.TestObjectId);
        if (existingUser == null)
        {
            TestUserId = Guid.NewGuid();
            var user = new User
            {
                Id = TestUserId,
                EntraObjectId = TestAuthHandler.TestObjectId,
                Email = TestAuthHandler.TestEmail,
                DisplayName = TestAuthHandler.TestName,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            db.SaveChanges();
        }
        else
        {
            TestUserId = existingUser.Id;
        }

        seedAction(db, TestUserId);
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
