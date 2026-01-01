using System.Net;
using FluentAssertions;
using Xunit;

namespace ExpenseFlow.Contracts.Tests;

/// <summary>
/// Contract tests for Transaction API endpoints.
/// Validates that endpoints conform to OpenAPI specification.
/// </summary>
[Trait("Category", "Contract")]
public class TransactionEndpointContractTests : ContractTestBase
{
    public TransactionEndpointContractTests(ContractTestWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetTransactions_Endpoint_ExistsInSpec()
    {
        await ValidateEndpointContractAsync(
            "/api/transactions",
            "GET",
            (200, "Success - Returns list of transactions"),
            (401, "Unauthorized"));
    }

    [Fact]
    public async Task GetTransactionById_Endpoint_ExistsInSpec()
    {
        await ValidateEndpointContractAsync(
            "/api/transactions/{id}",
            "GET",
            (200, "Success - Returns transaction details"),
            (404, "Not Found"),
            (401, "Unauthorized"));
    }

    [Fact]
    public async Task PostTransaction_Endpoint_ExistsInSpec()
    {
        await ValidateEndpointContractAsync(
            "/api/transactions",
            "POST",
            (201, "Created - Transaction created"),
            (400, "Bad Request - Invalid data"),
            (401, "Unauthorized"));
    }

    [Fact]
    public async Task PutTransaction_Endpoint_ExistsInSpec()
    {
        await ValidateEndpointContractAsync(
            "/api/transactions/{id}",
            "PUT",
            (200, "Success - Transaction updated"),
            (400, "Bad Request - Invalid data"),
            (404, "Not Found"),
            (401, "Unauthorized"));
    }

    [Fact]
    public async Task DeleteTransaction_Endpoint_ExistsInSpec()
    {
        await ValidateEndpointContractAsync(
            "/api/transactions/{id}",
            "DELETE",
            (204, "No Content - Transaction deleted"),
            (404, "Not Found"),
            (401, "Unauthorized"));
    }

    [Fact]
    public async Task GetUnmatchedTransactions_ReturnsValidResponse()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/api/transactions?matched=false");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetTransactionsByCategory_ReturnsValidResponse()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/api/transactions?categoryId=1");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CategorizeTransaction_Endpoint_ExistsInSpec()
    {
        await ValidateEndpointContractAsync(
            "/api/transactions/{id}/categorize",
            "POST",
            (200, "Success - Transaction categorized"),
            (404, "Not Found"),
            (401, "Unauthorized"));
    }
}
