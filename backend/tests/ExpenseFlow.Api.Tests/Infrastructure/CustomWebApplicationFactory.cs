using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace ExpenseFlow.Api.Tests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for integration testing.
/// Uses in-memory database and test authentication.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "TestDb_" + Guid.NewGuid().ToString();
    public Guid TestUserId { get; private set; }
    public string TestUserEmail => TestAuthHandler.TestEmail;
    public string TestUserName => TestAuthHandler.TestName;

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

            // Add in-memory database for testing (without vector support)
            services.AddDbContext<ExpenseFlowDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
                options.EnableSensitiveDataLogging();
            });

            // Mock external data source to avoid SQL Server connection
            var mockExternalDataSource = new Mock<IExternalDataSource>();
            mockExternalDataSource.Setup(x => x.GetDepartmentsAsync())
                .ReturnsAsync(new List<ExpenseFlow.Core.Entities.Department>());
            mockExternalDataSource.Setup(x => x.GetProjectsAsync())
                .ReturnsAsync(new List<ExpenseFlow.Core.Entities.Project>());
            mockExternalDataSource.Setup(x => x.GetGLAccountsAsync())
                .ReturnsAsync(new List<ExpenseFlow.Core.Entities.GLAccount>());
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
                .ReturnsAsync(new Pgvector.Vector(new float[1536])); // Return zero vector for tests
            mockEmbeddingService.Setup(x => x.FindSimilarAsync(
                    It.IsAny<Pgvector.Vector>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ExpenseFlow.Core.Entities.ExpenseEmbedding>());
            services.RemoveAll<IEmbeddingService>();
            services.AddSingleton(mockEmbeddingService.Object);

            // Mock Description Normalization Service to avoid Azure OpenAI connection
            var mockDescNormService = new Mock<IDescriptionNormalizationService>();
            mockDescNormService.Setup(x => x.NormalizeAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string desc, Guid _, CancellationToken _) => new ExpenseFlow.Shared.DTOs.NormalizationResultDto
                {
                    RawDescription = desc,
                    NormalizedDescription = desc, // Pass through for tests
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

        builder.UseEnvironment("Testing");
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
            var user = new ExpenseFlow.Core.Entities.User
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
