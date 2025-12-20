using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseFlow.Api.Controllers;

/// <summary>
/// Controller for receipt management operations.
/// </summary>
[Authorize]
public class ReceiptsController : ApiControllerBase
{
    private readonly IReceiptService _receiptService;
    private readonly IUserService _userService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<ReceiptsController> _logger;
    private readonly int _maxBatchSize;
    private readonly int _maxFileSizeBytes;
    private static readonly TimeSpan DefaultSasExpiry = TimeSpan.FromHours(1);

    public ReceiptsController(
        IReceiptService receiptService,
        IUserService userService,
        IBlobStorageService blobStorageService,
        IConfiguration configuration,
        ILogger<ReceiptsController> logger)
    {
        _receiptService = receiptService;
        _userService = userService;
        _blobStorageService = blobStorageService;
        _logger = logger;

        _maxBatchSize = configuration.GetValue<int>("ReceiptProcessing:MaxBatchSize", 20);
        var maxSizeMb = configuration.GetValue<int>("ReceiptProcessing:MaxFileSizeMB", 25);
        _maxFileSizeBytes = maxSizeMb * 1024 * 1024;
    }

    /// <summary>
    /// Uploads one or more receipt files.
    /// </summary>
    /// <param name="files">Receipt image files (JPEG, PNG, HEIC, PDF)</param>
    /// <returns>Upload results including successful and failed uploads</returns>
    [HttpPost]
    [RequestSizeLimit(524_288_000)] // 500MB total for batch uploads
    [ProducesResponseType(typeof(UploadResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<UploadResponseDto>> Upload(IFormFileCollection files)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "No files provided",
                Detail = "At least one file must be provided for upload"
            });
        }

        if (files.Count > _maxBatchSize)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Too many files",
                Detail = $"Maximum {_maxBatchSize} files can be uploaded at once"
            });
        }

        var user = await _userService.GetOrCreateUserAsync(User);
        var response = new UploadResponseDto();

        // Process files sequentially to ensure DbContext thread-safety
        // DbContext is NOT thread-safe; parallel processing caused receipts to get stuck in 'Uploaded' status
        // when the Hangfire job enqueue failed due to concurrent DbContext access (BUG-001 fix)
        foreach (var file in files)
        {
            try
            {
                if (file.Length > _maxFileSizeBytes)
                {
                    response.Failed.Add(new UploadFailureDto
                    {
                        Filename = file.FileName,
                        Error = $"File exceeds maximum size of {_maxFileSizeBytes / 1024 / 1024}MB"
                    });
                    continue;
                }

                if (file.Length == 0)
                {
                    response.Failed.Add(new UploadFailureDto
                    {
                        Filename = file.FileName,
                        Error = "File is empty"
                    });
                    continue;
                }

                using var stream = file.OpenReadStream();
                var receipt = await _receiptService.UploadReceiptAsync(
                    stream,
                    file.FileName,
                    file.ContentType,
                    user.Id);

                response.Receipts.Add(MapToSummaryDto(receipt));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error for file {Filename}", file.FileName);
                response.Failed.Add(new UploadFailureDto
                {
                    Filename = file.FileName,
                    Error = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {Filename}", file.FileName);
                response.Failed.Add(new UploadFailureDto
                {
                    Filename = file.FileName,
                    Error = "An error occurred during upload"
                });
            }
        }

        response.TotalUploaded = response.Receipts.Count;

        // Generate SAS URLs for thumbnail access (storage account has public access disabled)
        await PopulateThumbnailSasUrlsAsync(response.Receipts);

        _logger.LogInformation(
            "Upload completed for user {UserId}: {Uploaded} successful, {Failed} failed",
            user.Id,
            response.TotalUploaded,
            response.Failed.Count);

        return CreatedAtAction(nameof(GetReceipts), response);
    }

    /// <summary>
    /// Gets a paginated list of receipts for the current user.
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Items per page (max 100)</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <returns>Paginated list of receipts</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ReceiptListResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReceiptListResponseDto>> GetReceipts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] ReceiptStatus? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var user = await _userService.GetOrCreateUserAsync(User);
        var (items, totalCount) = await _receiptService.GetReceiptsAsync(
            user.Id, pageNumber, pageSize, status, fromDate, toDate);

        var response = new ReceiptListResponseDto
        {
            Items = items.Select(MapToSummaryDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        // Generate SAS URLs for thumbnail access (storage account has public access disabled)
        await PopulateThumbnailSasUrlsAsync(response.Items);

        return Ok(response);
    }

    /// <summary>
    /// Gets a specific receipt by ID.
    /// </summary>
    /// <param name="id">Receipt ID</param>
    /// <returns>Receipt details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReceiptDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReceiptDetailDto>> GetReceipt(Guid id)
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var receipt = await _receiptService.GetReceiptAsync(id, user.Id);

        if (receipt == null)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Receipt not found",
                Detail = $"Receipt with ID {id} was not found"
            });
        }

        var dto = MapToDetailDto(receipt);

        // Generate SAS URLs for blob access (storage account has public access disabled)
        if (!string.IsNullOrEmpty(receipt.BlobUrl))
        {
            dto.BlobUrl = await _blobStorageService.GenerateSasUrlAsync(receipt.BlobUrl, DefaultSasExpiry);
        }
        if (!string.IsNullOrEmpty(receipt.ThumbnailUrl))
        {
            dto.ThumbnailUrl = await _blobStorageService.GenerateSasUrlAsync(receipt.ThumbnailUrl, DefaultSasExpiry);
        }

        return Ok(dto);
    }

    /// <summary>
    /// Deletes a receipt.
    /// </summary>
    /// <param name="id">Receipt ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteReceipt(Guid id)
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var deleted = await _receiptService.DeleteReceiptAsync(id, user.Id);

        if (!deleted)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Receipt not found",
                Detail = $"Receipt with ID {id} was not found"
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Gets a temporary download URL for a receipt.
    /// </summary>
    /// <param name="id">Receipt ID</param>
    /// <returns>Temporary SAS URL for downloading the receipt</returns>
    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDownloadUrl(Guid id)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        try
        {
            var url = await _receiptService.GetReceiptUrlAsync(id, user.Id, TimeSpan.FromHours(1));
            return Ok(new { url });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Receipt not found",
                Detail = $"Receipt with ID {id} was not found"
            });
        }
    }

    /// <summary>
    /// Gets receipt status counts for the current user.
    /// </summary>
    /// <returns>Count of receipts by status</returns>
    [HttpGet("counts")]
    [ProducesResponseType(typeof(ReceiptStatusCountsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReceiptStatusCountsDto>> GetStatusCounts()
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var counts = await _receiptService.GetStatusCountsAsync(user.Id);

        var response = new ReceiptStatusCountsDto
        {
            Counts = counts.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value),
            Total = counts.Values.Sum()
        };

        return Ok(response);
    }

    /// <summary>
    /// Gets a paginated list of unmatched receipts for the current user.
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Items per page (max 100)</param>
    /// <returns>Paginated list of unmatched receipts</returns>
    [HttpGet("unmatched")]
    [ProducesResponseType(typeof(ReceiptListResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReceiptListResponseDto>> GetUnmatchedReceipts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var user = await _userService.GetOrCreateUserAsync(User);
        var (items, totalCount) = await _receiptService.GetReceiptsAsync(
            user.Id, pageNumber, pageSize, ReceiptStatus.Unmatched);

        var response = new ReceiptListResponseDto
        {
            Items = items.Select(MapToSummaryDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        // Generate SAS URLs for thumbnail access (storage account has public access disabled)
        await PopulateThumbnailSasUrlsAsync(response.Items);

        return Ok(response);
    }

    /// <summary>
    /// Retries processing for a failed receipt.
    /// </summary>
    /// <param name="id">Receipt ID</param>
    /// <returns>Updated receipt</returns>
    [HttpPost("{id:guid}/retry")]
    [ProducesResponseType(typeof(ReceiptSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReceiptSummaryDto>> RetryReceipt(Guid id)
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var receipt = await _receiptService.GetReceiptAsync(id, user.Id);

        if (receipt == null)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Receipt not found",
                Detail = $"Receipt with ID {id} was not found"
            });
        }

        if (receipt.Status != ReceiptStatus.Error)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid status",
                Detail = "Only receipts with Error status can be retried"
            });
        }

        if (receipt.RetryCount >= 3)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Max retries exceeded",
                Detail = "Receipt has reached maximum retry attempts (3)"
            });
        }

        var retried = await _receiptService.RetryReceiptAsync(id, user.Id);
        if (retried == null)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Retry failed",
                Detail = "Unable to retry receipt processing"
            });
        }

        var dto = MapToSummaryDto(retried);
        await PopulateThumbnailSasUrlAsync(dto);

        return Ok(dto);
    }

    /// <summary>
    /// Updates receipt data (for manual corrections).
    /// </summary>
    /// <param name="id">Receipt ID</param>
    /// <param name="request">Updated receipt data</param>
    /// <returns>Updated receipt</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ReceiptDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReceiptDetailDto>> UpdateReceipt(Guid id, [FromBody] ReceiptUpdateRequestDto request)
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var receipt = await _receiptService.GetReceiptAsync(id, user.Id);

        if (receipt == null)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Receipt not found",
                Detail = $"Receipt with ID {id} was not found"
            });
        }

        var updated = await _receiptService.UpdateReceiptAsync(id, user.Id, request);
        if (updated == null)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Update failed",
                Detail = "Unable to update receipt"
            });
        }

        var dto = MapToDetailDto(updated);

        // Generate SAS URLs for blob access (storage account has public access disabled)
        if (!string.IsNullOrEmpty(updated.BlobUrl))
        {
            dto.BlobUrl = await _blobStorageService.GenerateSasUrlAsync(updated.BlobUrl, DefaultSasExpiry);
        }
        if (!string.IsNullOrEmpty(updated.ThumbnailUrl))
        {
            dto.ThumbnailUrl = await _blobStorageService.GenerateSasUrlAsync(updated.ThumbnailUrl, DefaultSasExpiry);
        }

        return Ok(dto);
    }

    /// <summary>
    /// Triggers processing for a receipt with Uploaded status.
    /// </summary>
    /// <param name="id">Receipt ID</param>
    /// <returns>Updated receipt</returns>
    [HttpPost("{id:guid}/process")]
    [ProducesResponseType(typeof(ReceiptSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReceiptSummaryDto>> ProcessReceipt(Guid id)
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var receipt = await _receiptService.GetReceiptAsync(id, user.Id);

        if (receipt == null)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Receipt not found",
                Detail = $"Receipt with ID {id} was not found"
            });
        }

        if (receipt.Status != ReceiptStatus.Uploaded)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid status",
                Detail = $"Receipt has status {receipt.Status}. Only Uploaded receipts can be processed."
            });
        }

        var processed = await _receiptService.TriggerProcessingAsync(id, user.Id);
        if (processed == null)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Processing failed",
                Detail = "Unable to trigger receipt processing"
            });
        }

        var dto = MapToSummaryDto(processed);
        await PopulateThumbnailSasUrlAsync(dto);

        return Ok(dto);
    }

    private static ReceiptSummaryDto MapToSummaryDto(Receipt receipt)
    {
        return new ReceiptSummaryDto
        {
            Id = receipt.Id,
            ThumbnailUrl = receipt.ThumbnailUrl,
            OriginalFilename = receipt.OriginalFilename,
            Status = receipt.Status,
            Vendor = receipt.VendorExtracted,
            Date = receipt.DateExtracted?.ToDateTime(TimeOnly.MinValue),
            Amount = receipt.AmountExtracted,
            Currency = receipt.Currency ?? "USD",
            CreatedAt = receipt.CreatedAt
        };
    }

    private static ReceiptDetailDto MapToDetailDto(Receipt receipt)
    {
        return new ReceiptDetailDto
        {
            Id = receipt.Id,
            ThumbnailUrl = receipt.ThumbnailUrl,
            OriginalFilename = receipt.OriginalFilename,
            Status = receipt.Status,
            Vendor = receipt.VendorExtracted,
            Date = receipt.DateExtracted?.ToDateTime(TimeOnly.MinValue),
            Amount = receipt.AmountExtracted,
            Currency = receipt.Currency ?? "USD",
            CreatedAt = receipt.CreatedAt,
            BlobUrl = receipt.BlobUrl,
            ContentType = receipt.ContentType,
            FileSize = receipt.FileSize,
            Tax = receipt.TaxExtracted,
            LineItems = receipt.LineItems?.Select(li => new LineItemDto
            {
                Description = li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                TotalPrice = li.TotalPrice,
                Confidence = li.Confidence
            }).ToList() ?? new List<LineItemDto>(),
            ConfidenceScores = receipt.ConfidenceScores ?? new Dictionary<string, double>(),
            ErrorMessage = receipt.ErrorMessage,
            RetryCount = receipt.RetryCount,
            PageCount = receipt.PageCount,
            ProcessedAt = receipt.ProcessedAt
        };
    }

    /// <summary>
    /// Generates SAS URLs for thumbnail images in a list of receipt summary DTOs.
    /// </summary>
    private async Task PopulateThumbnailSasUrlsAsync(IEnumerable<ReceiptSummaryDto> dtos)
    {
        var tasks = dtos
            .Where(d => !string.IsNullOrEmpty(d.ThumbnailUrl))
            .Select(async d =>
            {
                d.ThumbnailUrl = await _blobStorageService.GenerateSasUrlAsync(d.ThumbnailUrl!, DefaultSasExpiry);
            });

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Generates SAS URL for thumbnail image in a single receipt summary DTO.
    /// </summary>
    private async Task PopulateThumbnailSasUrlAsync(ReceiptSummaryDto dto)
    {
        if (!string.IsNullOrEmpty(dto.ThumbnailUrl))
        {
            dto.ThumbnailUrl = await _blobStorageService.GenerateSasUrlAsync(dto.ThumbnailUrl, DefaultSasExpiry);
        }
    }
}
