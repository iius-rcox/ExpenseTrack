using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExpenseFlow.Api.Controllers;
using ExpenseFlow.Api.Tests.Infrastructure;
using ExpenseFlow.Core.Entities;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ExpenseFlow.Api.Tests.Controllers;

/// <summary>
/// Integration tests for ReportsController endpoints.
/// </summary>
public class ReportsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    // Match API JSON serialization options (especially JsonStringEnumConverter)
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public ReportsControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync()
    {
        // Seed test user on first test
        _factory.SeedDatabase((db, userId) => { });
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    #region GenerateDraft Tests

    [Fact]
    public async Task GenerateDraft_WithValidPeriod_ReturnsCreated()
    {
        // Arrange
        var request = new GenerateDraftRequest { Period = "2024-01" };

        // Seed test data
        await SeedTransactionForPeriodAsync("2024-01");

        // Act
        var response = await _client.PostAsJsonAsync("/api/reports/draft", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var report = await response.Content.ReadFromJsonAsync<ExpenseReportDto>(JsonOptions);
        report.Should().NotBeNull();
        report!.Period.Should().Be("2024-01");
        report.Status.Should().Be(ReportStatus.Draft);
    }

    [Fact]
    public async Task GenerateDraft_WithInvalidPeriodFormat_ReturnsBadRequest()
    {
        // Arrange
        var request = new GenerateDraftRequest { Period = "invalid" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reports/draft", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GenerateDraft_WithFuturePeriod_ReturnsBadRequest()
    {
        // Arrange
        var futurePeriod = DateTime.UtcNow.AddMonths(2).ToString("yyyy-MM");
        var request = new GenerateDraftRequest { Period = futurePeriod };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reports/draft", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GenerateDraft_WithEmptyPeriod_ReturnsBadRequest()
    {
        // Arrange
        var request = new GenerateDraftRequest { Period = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reports/draft", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region CheckExistingDraft Tests

    [Fact]
    public async Task CheckExistingDraft_WhenNoDraftExists_ReturnsNotExists()
    {
        // Act
        var response = await _client.GetAsync("/api/reports/draft/exists?period=2024-02");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ExistingDraftResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Exists.Should().BeFalse();
        result.ReportId.Should().BeNull();
    }

    [Fact]
    public async Task CheckExistingDraft_WhenDraftExists_ReturnsExists()
    {
        // Arrange - create a draft first
        await SeedTransactionForPeriodAsync("2024-03");
        var createRequest = new GenerateDraftRequest { Period = "2024-03" };
        var createResponse = await _client.PostAsJsonAsync("/api/reports/draft", createRequest);
        var createdReport = await createResponse.Content.ReadFromJsonAsync<ExpenseReportDto>(JsonOptions);

        // Act
        var response = await _client.GetAsync("/api/reports/draft/exists?period=2024-03");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ExistingDraftResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Exists.Should().BeTrue();
        result.ReportId.Should().Be(createdReport!.Id);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithValidId_ReturnsReport()
    {
        // Arrange - create a draft first
        await SeedTransactionForPeriodAsync("2024-04");
        var createRequest = new GenerateDraftRequest { Period = "2024-04" };
        var createResponse = await _client.PostAsJsonAsync("/api/reports/draft", createRequest);
        var createdReport = await createResponse.Content.ReadFromJsonAsync<ExpenseReportDto>(JsonOptions);

        // Act
        var response = await _client.GetAsync($"/api/reports/{createdReport!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var report = await response.Content.ReadFromJsonAsync<ExpenseReportDto>(JsonOptions);
        report.Should().NotBeNull();
        report!.Id.Should().Be(createdReport.Id);
        report.Period.Should().Be("2024-04");
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/reports/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetList Tests

    [Fact]
    public async Task GetList_ReturnsPagedResults()
    {
        // Arrange - create a draft
        await SeedTransactionForPeriodAsync("2024-05");
        await _client.PostAsJsonAsync("/api/reports/draft", new GenerateDraftRequest { Period = "2024-05" });

        // Act
        var response = await _client.GetAsync("/api/reports?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ReportListResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetList_WithStatusFilter_ReturnsFilteredResults()
    {
        // Arrange - create a draft
        await SeedTransactionForPeriodAsync("2024-06");
        await _client.PostAsJsonAsync("/api/reports/draft", new GenerateDraftRequest { Period = "2024-06" });

        // Act
        var response = await _client.GetAsync("/api/reports?status=0"); // Draft = 0

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ReportListResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Items.Should().OnlyContain(r => r.Status == ReportStatus.Draft);
    }

    #endregion

    #region UpdateLine Tests

    [Fact]
    public async Task UpdateLine_WithValidRequest_ReturnsUpdatedLine()
    {
        // Arrange - create a draft with a line
        await SeedTransactionForPeriodAsync("2024-07");
        var createResponse = await _client.PostAsJsonAsync("/api/reports/draft",
            new GenerateDraftRequest { Period = "2024-07" });
        var report = await createResponse.Content.ReadFromJsonAsync<ExpenseReportDto>(JsonOptions);

        var lineId = report!.Lines.First().Id;
        var updateRequest = new UpdateLineRequest
        {
            GlCode = "65000",
            DepartmentCode = "IT"
        };

        // Act
        var response = await _client.PatchAsJsonAsync(
            $"/api/reports/{report.Id}/lines/{lineId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedLine = await response.Content.ReadFromJsonAsync<ExpenseLineDto>(JsonOptions);
        updatedLine.Should().NotBeNull();
        updatedLine!.GlCode.Should().Be("65000");
        updatedLine.DepartmentCode.Should().Be("IT");
        updatedLine.IsUserEdited.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateLine_WithInvalidReportId_ReturnsNotFound()
    {
        // Arrange
        var updateRequest = new UpdateLineRequest { GlCode = "65000" };

        // Act
        var response = await _client.PatchAsJsonAsync(
            $"/api/reports/{Guid.NewGuid()}/lines/{Guid.NewGuid()}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateLine_WithMissingReceiptJustificationOther_RequiresNote()
    {
        // Arrange
        await SeedTransactionForPeriodAsync("2024-08");
        var createResponse = await _client.PostAsJsonAsync("/api/reports/draft",
            new GenerateDraftRequest { Period = "2024-08" });
        var report = await createResponse.Content.ReadFromJsonAsync<ExpenseReportDto>(JsonOptions);

        var lineId = report!.Lines.First().Id;
        var updateRequest = new UpdateLineRequest
        {
            MissingReceiptJustification = MissingReceiptJustification.Other
            // Missing JustificationNote - should fail validation
        };

        // Act
        var response = await _client.PatchAsJsonAsync(
            $"/api/reports/{report.Id}/lines/{lineId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateLine_WithMissingReceiptJustificationOtherAndNote_Succeeds()
    {
        // Arrange
        await SeedTransactionForPeriodAsync("2024-09");
        var createResponse = await _client.PostAsJsonAsync("/api/reports/draft",
            new GenerateDraftRequest { Period = "2024-09" });
        var report = await createResponse.Content.ReadFromJsonAsync<ExpenseReportDto>(JsonOptions);

        var lineId = report!.Lines.First().Id;
        var updateRequest = new UpdateLineRequest
        {
            MissingReceiptJustification = MissingReceiptJustification.Other,
            JustificationNote = "Receipt was emailed but not saved"
        };

        // Act
        var response = await _client.PatchAsJsonAsync(
            $"/api/reports/{report.Id}/lines/{lineId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedLine = await response.Content.ReadFromJsonAsync<ExpenseLineDto>(JsonOptions);
        updatedLine!.MissingReceiptJustification.Should().Be(MissingReceiptJustification.Other);
        updatedLine.JustificationNote.Should().Be("Receipt was emailed but not saved");
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithValidId_ReturnsNoContent()
    {
        // Arrange - create a draft first
        await SeedTransactionForPeriodAsync("2024-10");
        var createResponse = await _client.PostAsJsonAsync("/api/reports/draft",
            new GenerateDraftRequest { Period = "2024-10" });
        var report = await createResponse.Content.ReadFromJsonAsync<ExpenseReportDto>(JsonOptions);

        // Act
        var response = await _client.DeleteAsync($"/api/reports/{report!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's actually deleted (soft delete)
        var getResponse = await _client.GetAsync($"/api/reports/{report.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.DeleteAsync($"/api/reports/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Helper Methods

    private Task SeedTransactionForPeriodAsync(string period)
    {
        _factory.SeedDatabase((db, userId) =>
        {
            // Parse period to create proper date
            var year = int.Parse(period[..4]);
            var month = int.Parse(period[5..7]);
            var transactionDate = new DateOnly(year, month, 15);

            // Check if transaction already exists for this period
            var existingTransaction = db.Transactions
                .FirstOrDefault(t => t.UserId == userId && t.TransactionDate == transactionDate);
            if (existingTransaction != null)
                return;

            // Create a statement import first
            var import = new StatementImport
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FileName = $"test-statement-{period}.csv",
                FileSize = 1024,
                TierUsed = 1,
                TransactionCount = 1,
                CreatedAt = DateTime.UtcNow
            };
            db.StatementImports.Add(import);

            // Create transaction
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ImportId = import.Id,
                TransactionDate = transactionDate,
                PostDate = transactionDate,
                Description = "Test Transaction",
                OriginalDescription = "TEST TRANSACTION",
                Amount = 99.99m,
                DuplicateHash = Guid.NewGuid().ToString("N"),
                MatchStatus = MatchStatus.Unmatched,
                CreatedAt = DateTime.UtcNow
            };
            db.Transactions.Add(transaction);
            db.SaveChanges();
        });
        return Task.CompletedTask;
    }

    #endregion
}
