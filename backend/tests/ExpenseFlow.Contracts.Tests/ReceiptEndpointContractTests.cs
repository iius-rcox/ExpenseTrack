using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ExpenseFlow.Contracts.Tests;

/// <summary>
/// Contract tests for Receipt API endpoints.
/// Validates that endpoints conform to OpenAPI specification.
/// </summary>
[Trait("Category", "Contract")]
public class ReceiptEndpointContractTests : ContractTestBase
{
    public ReceiptEndpointContractTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetReceipts_Endpoint_ExistsInSpec()
    {
        // Arrange & Act
        await ValidateEndpointContractAsync(
            "/api/receipts",
            "GET",
            (200, "Success - Returns list of receipts"),
            (401, "Unauthorized"));
    }

    [Fact]
    public async Task GetReceiptById_Endpoint_ExistsInSpec()
    {
        // Arrange & Act
        await ValidateEndpointContractAsync(
            "/api/receipts/{id}",
            "GET",
            (200, "Success - Returns receipt details"),
            (404, "Not Found"),
            (401, "Unauthorized"));
    }

    [Fact]
    public async Task PostReceipt_Endpoint_ExistsInSpec()
    {
        // Arrange & Act
        await ValidateEndpointContractAsync(
            "/api/receipts",
            "POST",
            (201, "Created - Receipt uploaded successfully"),
            (400, "Bad Request - Invalid file or data"),
            (401, "Unauthorized"));
    }

    [Fact]
    public async Task DeleteReceipt_Endpoint_ExistsInSpec()
    {
        // Arrange & Act
        await ValidateEndpointContractAsync(
            "/api/receipts/{id}",
            "DELETE",
            (204, "No Content - Receipt deleted"),
            (404, "Not Found"),
            (401, "Unauthorized"));
    }

    [Fact]
    public async Task GetReceiptsUnmatchedEndpoint_ReturnsValidResponse()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/api/receipts?matched=false");

        // Assert - Should return valid JSON array or 401
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task GetReceiptImage_Endpoint_ExistsInSpec()
    {
        // Arrange & Act
        await ValidateEndpointContractAsync(
            "/api/receipts/{id}/image",
            "GET",
            (200, "Success - Returns image data"),
            (404, "Not Found"),
            (401, "Unauthorized"));
    }

    [Fact]
    public async Task ReprocessReceipt_Endpoint_ExistsInSpec()
    {
        // Arrange & Act
        await ValidateEndpointContractAsync(
            "/api/receipts/{id}/reprocess",
            "POST",
            (202, "Accepted - Reprocessing started"),
            (404, "Not Found"),
            (401, "Unauthorized"));
    }
}
