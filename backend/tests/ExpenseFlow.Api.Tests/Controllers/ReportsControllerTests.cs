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

using ExistingDraftResponse = ExpenseFlow.Api.Controllers.ExistingDraftResponse;

namespace ExpenseFlow.Api.Tests.Controllers;

/// <summary>
/// Unit tests for ReportsController.
/// </summary>
public class ReportsControllerTests
{
    private readonly Mock<IReportService> _reportServiceMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<IExcelExportService> _excelExportServiceMock;
    private readonly Mock<IPdfGenerationService> _pdfGenerationServiceMock;
    private readonly Mock<ILogger<ReportsController>> _loggerMock;
    private readonly ReportsController _controller;
    private readonly User _testUser;

    public ReportsControllerTests()
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

    #region GenerateDraft Tests

    [Fact]
    public async Task GenerateDraft_WithValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var request = new GenerateDraftRequest { Period = "2024-01" };
        var expectedReport = new ExpenseReportDto
        {
            Id = Guid.NewGuid(),
            Period = "2024-01",
            Status = ReportStatus.Draft,
            TotalAmount = 0,
            LineCount = 0,
            Lines = new List<ExpenseLineDto>()
        };

        _reportServiceMock
            .Setup(s => s.GenerateDraftAsync(_testUser.Id, "2024-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        // Act
        var result = await _controller.GenerateDraft(request, CancellationToken.None);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var report = createdResult.Value.Should().BeOfType<ExpenseReportDto>().Subject;
        report.Id.Should().Be(expectedReport.Id);
        report.Period.Should().Be("2024-01");
    }

    [Fact]
    public async Task GenerateDraft_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var request = new GenerateDraftRequest { Period = "2024-06" };
        _reportServiceMock
            .Setup(s => s.GenerateDraftAsync(_testUser.Id, "2024-06", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExpenseReportDto { Id = Guid.NewGuid(), Period = "2024-06" });

        // Act
        await _controller.GenerateDraft(request, CancellationToken.None);

        // Assert
        _reportServiceMock.Verify(
            s => s.GenerateDraftAsync(_testUser.Id, "2024-06", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region CheckExistingDraft Tests

    [Fact]
    public async Task CheckExistingDraft_WhenDraftExists_ReturnsExistsTrue()
    {
        // Arrange
        var existingId = Guid.NewGuid();
        _reportServiceMock
            .Setup(s => s.GetExistingDraftIdAsync(_testUser.Id, "2024-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingId);

        // Act
        var result = await _controller.CheckExistingDraft("2024-01", CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ExistingDraftResponse>().Subject;
        response.Exists.Should().BeTrue();
        response.ReportId.Should().Be(existingId);
    }

    [Fact]
    public async Task CheckExistingDraft_WhenNoDraftExists_ReturnsExistsFalse()
    {
        // Arrange
        _reportServiceMock
            .Setup(s => s.GetExistingDraftIdAsync(_testUser.Id, "2024-02", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        // Act
        var result = await _controller.CheckExistingDraft("2024-02", CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ExistingDraftResponse>().Subject;
        response.Exists.Should().BeFalse();
        response.ReportId.Should().BeNull();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithExistingReport_ReturnsOk()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var expectedReport = new ExpenseReportDto
        {
            Id = reportId,
            Period = "2024-01",
            Status = ReportStatus.Draft
        };

        _reportServiceMock
            .Setup(s => s.GetByIdAsync(_testUser.Id, reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        // Act
        var result = await _controller.GetById(reportId, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var report = okResult.Value.Should().BeOfType<ExpenseReportDto>().Subject;
        report.Id.Should().Be(reportId);
    }

    [Fact]
    public async Task GetById_WithNonExistingReport_ReturnsNotFound()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        _reportServiceMock
            .Setup(s => s.GetByIdAsync(_testUser.Id, reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExpenseReportDto?)null);

        // Act
        var result = await _controller.GetById(reportId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetList Tests

    [Fact]
    public async Task GetList_ReturnsPagedResults()
    {
        // Arrange
        var expectedResponse = new ReportListResponse
        {
            Items = new List<ReportSummaryDto>
            {
                new() { Id = Guid.NewGuid(), Period = "2024-01", Status = ReportStatus.Draft }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };

        _reportServiceMock
            .Setup(s => s.GetListAsync(_testUser.Id, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetList(null, null, 1, 20, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ReportListResponse>().Subject;
        response.Items.Should().HaveCount(1);
        response.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetList_WithStatusFilter_PassesFilterToService()
    {
        // Arrange
        _reportServiceMock
            .Setup(s => s.GetListAsync(_testUser.Id, ReportStatus.Draft, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReportListResponse());

        // Act
        await _controller.GetList(ReportStatus.Draft, null, 1, 20, CancellationToken.None);

        // Assert
        _reportServiceMock.Verify(
            s => s.GetListAsync(_testUser.Id, ReportStatus.Draft, null, 1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetList_WithInvalidPage_DefaultsToOne()
    {
        // Arrange
        _reportServiceMock
            .Setup(s => s.GetListAsync(_testUser.Id, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReportListResponse());

        // Act - pass negative page
        await _controller.GetList(null, null, -1, 20, CancellationToken.None);

        // Assert - should use page 1
        _reportServiceMock.Verify(
            s => s.GetListAsync(_testUser.Id, null, null, 1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetList_WithLargePageSize_CapsAt100()
    {
        // Arrange
        _reportServiceMock
            .Setup(s => s.GetListAsync(_testUser.Id, null, null, 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReportListResponse());

        // Act - pass page size > 100
        await _controller.GetList(null, null, 1, 500, CancellationToken.None);

        // Assert - should cap at 100
        _reportServiceMock.Verify(
            s => s.GetListAsync(_testUser.Id, null, null, 1, 100, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region UpdateLine Tests

    [Fact]
    public async Task UpdateLine_WithValidRequest_ReturnsUpdatedLine()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var request = new UpdateLineRequest { GlCode = "65000" };
        var expectedLine = new ExpenseLineDto
        {
            Id = lineId,
            GlCode = "65000",
            IsUserEdited = true
        };

        _reportServiceMock
            .Setup(s => s.UpdateLineAsync(_testUser.Id, reportId, lineId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedLine);

        // Act
        var result = await _controller.UpdateLine(reportId, lineId, request, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var line = okResult.Value.Should().BeOfType<ExpenseLineDto>().Subject;
        line.GlCode.Should().Be("65000");
        line.IsUserEdited.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateLine_WithNonExistingReport_ReturnsNotFound()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var request = new UpdateLineRequest { GlCode = "65000" };

        _reportServiceMock
            .Setup(s => s.UpdateLineAsync(_testUser.Id, reportId, lineId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExpenseLineDto?)null);

        // Act
        var result = await _controller.UpdateLine(reportId, lineId, request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithExistingReport_ReturnsNoContent()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        _reportServiceMock
            .Setup(s => s.DeleteAsync(_testUser.Id, reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(reportId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WithNonExistingReport_ReturnsNotFound()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        _reportServiceMock
            .Setup(s => s.DeleteAsync(_testUser.Id, reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(reportId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Generate (Finalize) Tests

    [Fact]
    public async Task Generate_WithValidDraftReport_ReturnsOkWithGeneratedStatus()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var expectedResponse = new GenerateReportResponseDto
        {
            ReportId = reportId,
            Status = "Generated",
            GeneratedAt = DateTimeOffset.UtcNow,
            LineCount = 5,
            TotalAmount = 500.00m
        };

        _reportServiceMock
            .Setup(s => s.GenerateAsync(_testUser.Id, reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Generate(reportId, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<GenerateReportResponseDto>().Subject;
        response.Status.Should().Be("Generated");
        response.ReportId.Should().Be(reportId);
        response.LineCount.Should().Be(5);
    }

    [Fact]
    public async Task Generate_WithNonExistingReport_ReturnsNotFound()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        _reportServiceMock
            .Setup(s => s.GenerateAsync(_testUser.Id, reportId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException($"Report with ID {reportId} was not found"));

        // Act
        var result = await _controller.Generate(reportId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Generate_WithValidationFailure_ReturnsBadRequest()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        _reportServiceMock
            .Setup(s => s.GenerateAsync(_testUser.Id, reportId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Report validation failed: Expense line must have a category (GL code) assigned"));

        // Act
        var result = await _controller.Generate(reportId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Generate_WithAlreadyGeneratedReport_ReturnsConflict()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        _reportServiceMock
            .Setup(s => s.GenerateAsync(_testUser.Id, reportId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Report has already been finalized"));

        // Act
        var result = await _controller.Generate(reportId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Generate_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        _reportServiceMock
            .Setup(s => s.GenerateAsync(_testUser.Id, reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GenerateReportResponseDto
            {
                ReportId = reportId,
                Status = "Generated",
                GeneratedAt = DateTimeOffset.UtcNow
            });

        // Act
        await _controller.Generate(reportId, CancellationToken.None);

        // Assert
        _reportServiceMock.Verify(
            s => s.GenerateAsync(_testUser.Id, reportId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Submit Tests

    [Fact]
    public async Task Submit_WithGeneratedReport_ReturnsOkWithSubmittedStatus()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var generatedAt = DateTimeOffset.UtcNow.AddHours(-1);
        var expectedResponse = new SubmitReportResponseDto
        {
            ReportId = reportId,
            Status = "Submitted",
            GeneratedAt = generatedAt,
            SubmittedAt = DateTimeOffset.UtcNow
        };

        _reportServiceMock
            .Setup(s => s.SubmitAsync(_testUser.Id, reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Submit(reportId, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<SubmitReportResponseDto>().Subject;
        response.Status.Should().Be("Submitted");
        response.ReportId.Should().Be(reportId);
        response.GeneratedAt.Should().Be(generatedAt);
    }

    [Fact]
    public async Task Submit_WithNonExistingReport_ReturnsNotFound()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        _reportServiceMock
            .Setup(s => s.SubmitAsync(_testUser.Id, reportId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException($"Report with ID {reportId} was not found"));

        // Act
        var result = await _controller.Submit(reportId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Submit_WithDraftReport_ReturnsBadRequest()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        _reportServiceMock
            .Setup(s => s.SubmitAsync(_testUser.Id, reportId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Report must be in Generated status before submitting"));

        // Act
        var result = await _controller.Submit(reportId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Submit_WithAlreadySubmittedReport_ReturnsConflict()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        _reportServiceMock
            .Setup(s => s.SubmitAsync(_testUser.Id, reportId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Report has already been submitted"));

        // Act
        var result = await _controller.Submit(reportId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Submit_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        _reportServiceMock
            .Setup(s => s.SubmitAsync(_testUser.Id, reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubmitReportResponseDto
            {
                ReportId = reportId,
                Status = "Submitted",
                GeneratedAt = DateTimeOffset.UtcNow.AddHours(-1),
                SubmittedAt = DateTimeOffset.UtcNow
            });

        // Act
        await _controller.Submit(reportId, CancellationToken.None);

        // Assert
        _reportServiceMock.Verify(
            s => s.SubmitAsync(_testUser.Id, reportId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
