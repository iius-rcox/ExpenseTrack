using System.Security.Claims;
using ExpenseFlow.Api.Controllers;
using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ExpenseFlow.Api.Tests.Controllers;

/// <summary>
/// Unit tests for the Save Report (batch update lines) functionality.
/// Tests the POST /reports/{reportId}/save endpoint.
/// </summary>
public class SaveReportControllerTests
{
    private readonly Mock<IReportService> _reportServiceMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<IExcelExportService> _excelExportServiceMock;
    private readonly Mock<IPdfGenerationService> _pdfGenerationServiceMock;
    private readonly Mock<ILogger<ReportsController>> _loggerMock;
    private readonly ReportsController _controller;
    private readonly User _testUser;

    public SaveReportControllerTests()
    {
        _reportServiceMock = new Mock<IReportService>();
        _userServiceMock = new Mock<IUserService>();
        _excelExportServiceMock = new Mock<IExcelExportService>();
        _pdfGenerationServiceMock = new Mock<IPdfGenerationService>();
        _loggerMock = new Mock<ILogger<ReportsController>>();

        _testUser = new User
        {
            Id = Guid.NewGuid(),
            EntraObjectId = "test-object-id",
            Email = "test@example.com",
            DisplayName = "Test User",
            CreatedAt = DateTime.UtcNow
        };

        _userServiceMock
            .Setup(s => s.GetOrCreateUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(_testUser);

        _controller = new ReportsController(
            _reportServiceMock.Object,
            _userServiceMock.Object,
            _excelExportServiceMock.Object,
            _pdfGenerationServiceMock.Object,
            _loggerMock.Object);

        // Set up HttpContext with a user
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "test-object-id"),
            new("oid", "test-object-id"),
            new(ClaimTypes.Email, "test@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    #region SaveReport Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SaveReport_WithDirtyLines_ReturnsSuccessAndUpdatesLines()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var lineId1 = Guid.NewGuid();
        var lineId2 = Guid.NewGuid();

        var request = new BatchUpdateLinesRequest
        {
            Lines = new List<BatchLineUpdate>
            {
                new() { LineId = lineId1, GlCode = "65000", DepartmentCode = "ADMIN" },
                new() { LineId = lineId2, GlCode = "62000", DepartmentCode = "IT" }
            }
        };

        var expectedResponse = new BatchUpdateLinesResponse
        {
            ReportId = reportId,
            UpdatedCount = 2,
            FailedCount = 0,
            UpdatedAt = DateTimeOffset.UtcNow,
            FailedLines = new List<FailedLineUpdate>()
        };

        _reportServiceMock
            .Setup(s => s.BatchUpdateLinesAsync(_testUser.Id, reportId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.SaveReport(reportId, request, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<BatchUpdateLinesResponse>().Subject;
        response.ReportId.Should().Be(reportId);
        response.UpdatedCount.Should().Be(2);
        response.FailedCount.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SaveReport_WithNonDraftReport_ReturnsBadRequest()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var request = new BatchUpdateLinesRequest
        {
            Lines = new List<BatchLineUpdate>
            {
                new() { LineId = Guid.NewGuid(), GlCode = "65000" }
            }
        };

        _reportServiceMock
            .Setup(s => s.BatchUpdateLinesAsync(_testUser.Id, reportId, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot modify expense lines: report is in Generated status and is locked for editing"));

        // Act
        var result = await _controller.SaveReport(reportId, request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SaveReport_PreservesReportDraftStatus()
    {
        // Arrange - This is the CRITICAL test
        var reportId = Guid.NewGuid();
        var request = new BatchUpdateLinesRequest
        {
            Lines = new List<BatchLineUpdate>
            {
                new() { LineId = Guid.NewGuid(), GlCode = "65000" }
            }
        };

        var expectedResponse = new BatchUpdateLinesResponse
        {
            ReportId = reportId,
            UpdatedCount = 1,
            FailedCount = 0,
            UpdatedAt = DateTimeOffset.UtcNow,
            FailedLines = new List<FailedLineUpdate>(),
            // CRITICAL: Status should remain Draft
            ReportStatus = ReportStatus.Draft.ToString()
        };

        _reportServiceMock
            .Setup(s => s.BatchUpdateLinesAsync(_testUser.Id, reportId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.SaveReport(reportId, request, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<BatchUpdateLinesResponse>().Subject;

        // CRITICAL ASSERTION: Report status must remain Draft after save
        response.ReportStatus.Should().Be("Draft", "Save should preserve Draft status, not lock the report");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SaveReport_WithInvalidReportId_ReturnsNotFound()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var request = new BatchUpdateLinesRequest
        {
            Lines = new List<BatchLineUpdate>
            {
                new() { LineId = Guid.NewGuid(), GlCode = "65000" }
            }
        };

        _reportServiceMock
            .Setup(s => s.BatchUpdateLinesAsync(_testUser.Id, reportId, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException($"Report with ID {reportId} was not found"));

        // Act
        var result = await _controller.SaveReport(reportId, request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SaveReport_UpdatesReportTimestamp()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var request = new BatchUpdateLinesRequest
        {
            Lines = new List<BatchLineUpdate>
            {
                new() { LineId = Guid.NewGuid(), GlCode = "65000" }
            }
        };

        var beforeSave = DateTimeOffset.UtcNow;

        var expectedResponse = new BatchUpdateLinesResponse
        {
            ReportId = reportId,
            UpdatedCount = 1,
            FailedCount = 0,
            UpdatedAt = DateTimeOffset.UtcNow,
            FailedLines = new List<FailedLineUpdate>()
        };

        _reportServiceMock
            .Setup(s => s.BatchUpdateLinesAsync(_testUser.Id, reportId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.SaveReport(reportId, request, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<BatchUpdateLinesResponse>().Subject;
        response.UpdatedAt.Should().BeOnOrAfter(beforeSave);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SaveReport_WithEmptyLines_ReturnsOkWithZeroUpdates()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var request = new BatchUpdateLinesRequest
        {
            Lines = new List<BatchLineUpdate>()
        };

        var expectedResponse = new BatchUpdateLinesResponse
        {
            ReportId = reportId,
            UpdatedCount = 0,
            FailedCount = 0,
            UpdatedAt = DateTimeOffset.UtcNow,
            FailedLines = new List<FailedLineUpdate>()
        };

        _reportServiceMock
            .Setup(s => s.BatchUpdateLinesAsync(_testUser.Id, reportId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.SaveReport(reportId, request, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<BatchUpdateLinesResponse>().Subject;
        response.UpdatedCount.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SaveReport_WithPartialFailures_ReturnsOkWithFailureDetails()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var validLineId = Guid.NewGuid();
        var invalidLineId = Guid.NewGuid();

        var request = new BatchUpdateLinesRequest
        {
            Lines = new List<BatchLineUpdate>
            {
                new() { LineId = validLineId, GlCode = "65000" },
                new() { LineId = invalidLineId, GlCode = "65000" }
            }
        };

        var expectedResponse = new BatchUpdateLinesResponse
        {
            ReportId = reportId,
            UpdatedCount = 1,
            FailedCount = 1,
            UpdatedAt = DateTimeOffset.UtcNow,
            FailedLines = new List<FailedLineUpdate>
            {
                new() { LineId = invalidLineId, Error = "Line not found" }
            }
        };

        _reportServiceMock
            .Setup(s => s.BatchUpdateLinesAsync(_testUser.Id, reportId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.SaveReport(reportId, request, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<BatchUpdateLinesResponse>().Subject;
        response.UpdatedCount.Should().Be(1);
        response.FailedCount.Should().Be(1);
        response.FailedLines.Should().HaveCount(1);
        response.FailedLines[0].LineId.Should().Be(invalidLineId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SaveReport_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var request = new BatchUpdateLinesRequest
        {
            Lines = new List<BatchLineUpdate>
            {
                new() { LineId = Guid.NewGuid(), GlCode = "65000", DepartmentCode = "ADMIN" }
            }
        };

        _reportServiceMock
            .Setup(s => s.BatchUpdateLinesAsync(_testUser.Id, reportId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BatchUpdateLinesResponse { ReportId = reportId, UpdatedCount = 1, UpdatedAt = DateTimeOffset.UtcNow });

        // Act
        await _controller.SaveReport(reportId, request, CancellationToken.None);

        // Assert
        _reportServiceMock.Verify(
            s => s.BatchUpdateLinesAsync(_testUser.Id, reportId, request, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SaveReport_WithMissingReceiptJustification_UpdatesJustification()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var lineId = Guid.NewGuid();

        var request = new BatchUpdateLinesRequest
        {
            Lines = new List<BatchLineUpdate>
            {
                new()
                {
                    LineId = lineId,
                    GlCode = "65000",
                    MissingReceiptJustification = MissingReceiptJustification.UnderThreshold,
                    JustificationNote = "Small purchase"
                }
            }
        };

        var expectedResponse = new BatchUpdateLinesResponse
        {
            ReportId = reportId,
            UpdatedCount = 1,
            FailedCount = 0,
            UpdatedAt = DateTimeOffset.UtcNow,
            FailedLines = new List<FailedLineUpdate>()
        };

        _reportServiceMock
            .Setup(s => s.BatchUpdateLinesAsync(_testUser.Id, reportId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.SaveReport(reportId, request, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<BatchUpdateLinesResponse>().Subject;
        response.UpdatedCount.Should().Be(1);
    }

    #endregion
}
