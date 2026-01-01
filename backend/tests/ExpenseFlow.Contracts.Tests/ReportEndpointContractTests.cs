using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ExpenseFlow.Contracts.Tests;

/// <summary>
/// Contract tests for Report API endpoints.
/// Validates that endpoints conform to OpenAPI specification.
/// </summary>
[Trait("Category", "Contract")]
public class ReportEndpointContractTests : ContractTestBase
{
    public ReportEndpointContractTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetReports_Endpoint_ExistsInSpec()
    {
        await ValidateEndpointContractAsync(
            "/api/reports",
            "GET",
            (200, "Success - Returns list of reports"),
            (401, "Unauthorized"));
    }

    [Fact]
    public async Task GetReportById_Endpoint_ExistsInSpec()
    {
        await ValidateEndpointContractAsync(
            "/api/reports/{id}",
            "GET",
            (200, "Success - Returns report details"),
            (404, "Not Found"),
            (401, "Unauthorized"));
    }

    [Fact]
    public async Task PostReport_Endpoint_ExistsInSpec()
    {
        await ValidateEndpointContractAsync(
            "/api/reports",
            "POST",
            (201, "Created - Report created"),
            (400, "Bad Request - Invalid data"),
            (401, "Unauthorized"));
    }

    [Fact]
    public async Task GenerateReport_Endpoint_ExistsInSpec()
    {
        await ValidateEndpointContractAsync(
            "/api/reports/{id}/generate",
            "POST",
            (202, "Accepted - Report generation started"),
            (404, "Not Found"),
            (401, "Unauthorized"));
    }

    [Fact]
    public async Task DownloadReportPdf_Endpoint_ExistsInSpec()
    {
        await ValidateEndpointContractAsync(
            "/api/reports/{id}/pdf",
            "GET",
            (200, "Success - Returns PDF file"),
            (404, "Not Found"),
            (401, "Unauthorized"));
    }

    [Fact]
    public async Task DownloadReportExcel_Endpoint_ExistsInSpec()
    {
        await ValidateEndpointContractAsync(
            "/api/reports/{id}/excel",
            "GET",
            (200, "Success - Returns Excel file"),
            (404, "Not Found"),
            (401, "Unauthorized"));
    }

    [Fact]
    public async Task SubmitReport_Endpoint_ExistsInSpec()
    {
        await ValidateEndpointContractAsync(
            "/api/reports/{id}/submit",
            "POST",
            (200, "Success - Report submitted"),
            (400, "Bad Request - Report not ready for submission"),
            (404, "Not Found"),
            (401, "Unauthorized"));
    }

    [Fact]
    public async Task GetReportItems_Endpoint_ExistsInSpec()
    {
        await ValidateEndpointContractAsync(
            "/api/reports/{id}/items",
            "GET",
            (200, "Success - Returns report line items"),
            (404, "Not Found"),
            (401, "Unauthorized"));
    }

    [Fact]
    public async Task DeleteReport_Endpoint_ExistsInSpec()
    {
        await ValidateEndpointContractAsync(
            "/api/reports/{id}",
            "DELETE",
            (204, "No Content - Report deleted"),
            (404, "Not Found"),
            (401, "Unauthorized"));
    }
}
