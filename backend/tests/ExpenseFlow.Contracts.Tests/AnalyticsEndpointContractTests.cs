using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ExpenseFlow.Contracts.Tests;

/// <summary>
/// Contract tests for Analytics API endpoints.
/// Validates that endpoints conform to OpenAPI specification.
/// </summary>
[Trait("Category", "Contract")]
public class AnalyticsEndpointContractTests : ContractTestBase
{
    public AnalyticsEndpointContractTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetSpendingSummary_Endpoint_ExistsInSpec()
    {
        await ValidateEndpointContractAsync(
            "/api/analytics/spending-summary",
            "GET",
            (200, "Success - Returns spending summary"),
            (401, "Unauthorized"));
    }

    [Fact]
    public async Task GetCategoryBreakdown_Endpoint_ExistsInSpec()
    {
        await ValidateEndpointContractAsync(
            "/api/analytics/category-breakdown",
            "GET",
            (200, "Success - Returns category breakdown"),
            (401, "Unauthorized"));
    }

    [Fact]
    public async Task GetTrendAnalysis_Endpoint_ExistsInSpec()
    {
        await ValidateEndpointContractAsync(
            "/api/analytics/trends",
            "GET",
            (200, "Success - Returns trend data"),
            (401, "Unauthorized"));
    }

    [Fact]
    public async Task GetVendorInsights_Endpoint_ExistsInSpec()
    {
        await ValidateEndpointContractAsync(
            "/api/analytics/vendor-insights",
            "GET",
            (200, "Success - Returns vendor insights"),
            (401, "Unauthorized"));
    }

    [Fact]
    public async Task GetBudgetComparison_Endpoint_ExistsInSpec()
    {
        await ValidateEndpointContractAsync(
            "/api/analytics/budget-comparison",
            "GET",
            (200, "Success - Returns budget comparison"),
            (401, "Unauthorized"));
    }

    [Fact]
    public async Task ExportAnalytics_Endpoint_ExistsInSpec()
    {
        await ValidateEndpointContractAsync(
            "/api/analytics/export",
            "GET",
            (200, "Success - Returns export data"),
            (401, "Unauthorized"));
    }

    [Fact]
    public async Task GetAnalyticsSummary_ReturnsValidResponse()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/api/analytics/spending-summary");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAnalytics_WithDateRange_ReturnsValidResponse()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // Act
        var response = await Client.GetAsync(
            $"/api/analytics/spending-summary?startDate={startDate}&endDate={endDate}");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden,
            HttpStatusCode.BadRequest);
    }
}
