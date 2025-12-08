using System.Text.Json;
using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Services;
using ExpenseFlow.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseFlow.Api.Controllers;

/// <summary>
/// Controller for statement import operations.
/// </summary>
[Authorize]
public class StatementsController : ApiControllerBase
{
    private readonly IStatementParsingService _parsingService;
    private readonly IStatementFingerprintService _fingerprintService;
    private readonly IColumnMappingInferenceService _inferenceService;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IStatementImportRepository _importRepository;
    private readonly IUserService _userService;
    private readonly AnalysisSessionCache _sessionCache;
    private readonly ILogger<StatementsController> _logger;
    private readonly int _maxFileSizeBytes;
    private readonly int _sampleRowCount;

    private static readonly string[] AllowedExtensions = { ".csv", ".xlsx", ".xls" };

    public StatementsController(
        IStatementParsingService parsingService,
        IStatementFingerprintService fingerprintService,
        IColumnMappingInferenceService inferenceService,
        ITransactionRepository transactionRepository,
        IStatementImportRepository importRepository,
        IUserService userService,
        AnalysisSessionCache sessionCache,
        IConfiguration configuration,
        ILogger<StatementsController> logger)
    {
        _parsingService = parsingService;
        _fingerprintService = fingerprintService;
        _inferenceService = inferenceService;
        _transactionRepository = transactionRepository;
        _importRepository = importRepository;
        _userService = userService;
        _sessionCache = sessionCache;
        _logger = logger;

        var maxSizeMb = configuration.GetValue<int>("StatementImport:MaxFileSizeMB", 10);
        _maxFileSizeBytes = maxSizeMb * 1024 * 1024;
        _sampleRowCount = configuration.GetValue<int>("StatementImport:SampleRowCount", 5);
    }

    /// <summary>
    /// Analyzes an uploaded statement file and returns column mapping options.
    /// </summary>
    /// <param name="file">CSV or Excel statement file</param>
    /// <returns>Analysis results with mapping options</returns>
    [HttpPost("analyze")]
    [RequestSizeLimit(10_485_760)] // 10MB
    [ProducesResponseType(typeof(StatementAnalyzeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StatementAnalyzeResponse>> Analyze(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "No file provided",
                Detail = "A statement file must be provided for analysis"
            });
        }

        if (file.Length > _maxFileSizeBytes)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "File too large",
                Detail = $"File exceeds maximum size of {_maxFileSizeBytes / 1024 / 1024}MB"
            });
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid file format",
                Detail = $"Allowed formats: {string.Join(", ", AllowedExtensions)}"
            });
        }

        var user = await _userService.GetOrCreateUserAsync(User);

        try
        {
            // Parse the statement file
            using var stream = file.OpenReadStream();
            var parsedData = await _parsingService.ParseAsync(stream, file.FileName);

            if (parsedData.Headers.Count == 0)
            {
                return BadRequest(new ProblemDetailsResponse
                {
                    Title = "No header row found",
                    Detail = "The file appears to have no header row"
                });
            }

            // Look for matching fingerprints (user-specific + system)
            var fingerprints = await _fingerprintService.GetByHashAsync(user.Id, parsedData.HeaderHash);
            var mappingOptions = new List<MappingOptionDto>();
            var tierUsed = 3; // Default to AI inference
            Guid? matchedFingerprintId = null;

            foreach (var fingerprint in fingerprints)
            {
                var columnMapping = JsonSerializer.Deserialize<Dictionary<string, string>>(fingerprint.ColumnMapping)
                    ?? new Dictionary<string, string>();

                mappingOptions.Add(new MappingOptionDto
                {
                    Source = fingerprint.IsSystem ? MappingSources.SystemFingerprint : MappingSources.UserFingerprint,
                    Tier = 1,
                    FingerprintId = fingerprint.Id,
                    SourceName = fingerprint.SourceName,
                    ColumnMapping = columnMapping,
                    DateFormat = fingerprint.DateFormat,
                    AmountSign = fingerprint.AmountSign
                });

                if (matchedFingerprintId == null)
                {
                    matchedFingerprintId = fingerprint.Id;
                    tierUsed = 1;
                }
            }

            // If no fingerprint match, use AI inference (Tier 3)
            if (mappingOptions.Count == 0)
            {
                _logger.LogInformation(
                    "No fingerprint match for header hash {HeaderHash}, invoking AI inference",
                    parsedData.HeaderHash);

                try
                {
                    var sampleRowsForAi = parsedData.Rows
                        .Take(3)
                        .Select(r => (IReadOnlyList<string>)r)
                        .ToList();

                    var inference = await _inferenceService.InferMappingAsync(
                        parsedData.Headers,
                        sampleRowsForAi);

                    mappingOptions.Add(new MappingOptionDto
                    {
                        Source = MappingSources.AiInference,
                        Tier = 3,
                        ColumnMapping = inference.ColumnMapping,
                        DateFormat = inference.DateFormat,
                        AmountSign = inference.AmountSign,
                        Confidence = inference.Confidence
                    });

                    tierUsed = 3;

                    _logger.LogInformation(
                        "AI inference completed with confidence {Confidence:P0}",
                        inference.Confidence);
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError(ex, "AI service unavailable");
                    return StatusCode(503, new ProblemDetailsResponse
                    {
                        Title = "AI service unavailable",
                        Detail = "The AI inference service is currently unavailable. Please try again later or use a known statement format."
                    });
                }
            }

            // Store session for import step
            var analysisId = Guid.NewGuid();
            _sessionCache.Store(analysisId, new AnalysisSession
            {
                UserId = user.Id,
                FileName = file.FileName,
                FileSize = file.Length,
                ParsedData = parsedData,
                TierUsed = tierUsed,
                MatchedFingerprintId = matchedFingerprintId
            });

            var response = new StatementAnalyzeResponse
            {
                AnalysisId = analysisId,
                FileName = file.FileName,
                RowCount = parsedData.RowCount,
                Headers = parsedData.Headers,
                SampleRows = parsedData.Rows.Take(_sampleRowCount).ToList(),
                MappingOptions = mappingOptions
            };

            _logger.LogInformation(
                "Analyzed statement {FileName} for user {UserId}: {RowCount} rows, {OptionCount} mapping options",
                file.FileName, user.Id, parsedData.RowCount, mappingOptions.Count);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid file",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid file structure",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Imports transactions from an analyzed statement file.
    /// </summary>
    /// <param name="request">Import request with column mapping</param>
    /// <returns>Import results</returns>
    [HttpPost("import")]
    [ProducesResponseType(typeof(StatementImportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StatementImportResponse>> Import([FromBody] StatementImportRequest request)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        // Retrieve session data
        var session = _sessionCache.Retrieve(request.AnalysisId);
        if (session == null)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Analysis session expired",
                Detail = "The analysis session has expired. Please re-upload the file."
            });
        }

        if (session.UserId != user.Id)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid session",
                Detail = "The analysis session does not belong to the current user"
            });
        }

        // Validate column mapping has required fields
        var requiredFields = new[] { ColumnFieldTypes.Date, ColumnFieldTypes.Amount, ColumnFieldTypes.Description };
        var mappedFields = request.ColumnMapping.Values.ToHashSet();
        var missingFields = requiredFields.Where(f => !mappedFields.Contains(f)).ToList();

        if (missingFields.Any())
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Incomplete column mapping",
                Detail = $"Missing required field mappings: {string.Join(", ", missingFields)}"
            });
        }

        // Create import record
        var import = new StatementImport
        {
            UserId = user.Id,
            FingerprintId = session.MatchedFingerprintId,
            FileName = session.FileName,
            FileSize = session.FileSize,
            TierUsed = session.TierUsed
        };

        // Process rows
        var transactions = new List<Transaction>();
        var skippedCount = 0;
        var duplicateCount = 0;

        // Build column index map
        var columnIndexMap = new Dictionary<string, int>();
        for (int i = 0; i < session.ParsedData.Headers.Count; i++)
        {
            var header = session.ParsedData.Headers[i];
            if (request.ColumnMapping.TryGetValue(header, out var fieldType) && fieldType != ColumnFieldTypes.Ignore)
            {
                columnIndexMap[fieldType] = i;
            }
        }

        var parsingService = (StatementParsingService)_parsingService;

        foreach (var row in session.ParsedData.Rows)
        {
            // Extract values
            var dateStr = GetColumnValue(row, columnIndexMap, ColumnFieldTypes.Date);
            var amountStr = GetColumnValue(row, columnIndexMap, ColumnFieldTypes.Amount);
            var description = GetColumnValue(row, columnIndexMap, ColumnFieldTypes.Description);
            var postDateStr = GetColumnValue(row, columnIndexMap, ColumnFieldTypes.PostDate);

            // Parse date
            var date = parsingService.ParseDate(dateStr, request.DateFormat);
            if (date == null)
            {
                skippedCount++;
                continue;
            }

            // Validate date range (within last 2 years)
            var twoYearsAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-2));
            if (date.Value < twoYearsAgo)
            {
                skippedCount++;
                continue;
            }

            // Parse amount
            var amount = parsingService.ParseAmount(amountStr);
            if (amount == null || amount == 0)
            {
                skippedCount++;
                continue;
            }

            // Validate description
            if (string.IsNullOrWhiteSpace(description))
            {
                skippedCount++;
                continue;
            }

            // Apply amount sign convention
            var finalAmount = request.AmountSign == AmountSignConventions.NegativeCharges
                ? -amount.Value  // Negative charges = flip sign so negative becomes positive expense
                : amount.Value;   // Positive charges = keep sign

            // If amount is negative after conversion (payment/refund), make it negative expense
            if (request.AmountSign == AmountSignConventions.NegativeCharges && amount.Value > 0)
            {
                finalAmount = -amount.Value; // Positive in negative_charges = payment/refund
            }
            else if (request.AmountSign == AmountSignConventions.NegativeCharges && amount.Value < 0)
            {
                finalAmount = -amount.Value; // Negative in negative_charges = expense
            }
            else if (request.AmountSign == AmountSignConventions.PositiveCharges)
            {
                // In positive_charges convention, positive = expense, negative = credit
                finalAmount = amount.Value;
            }

            // Check for duplicates
            var duplicateHash = _parsingService.ComputeDuplicateHash(date.Value, finalAmount, description);
            if (await _transactionRepository.ExistsByDuplicateHashAsync(user.Id, duplicateHash))
            {
                duplicateCount++;
                continue;
            }

            // Parse post date if available
            DateOnly? postDate = null;
            if (!string.IsNullOrEmpty(postDateStr))
            {
                postDate = parsingService.ParseDate(postDateStr, request.DateFormat);
            }

            transactions.Add(new Transaction
            {
                UserId = user.Id,
                ImportId = import.Id,
                TransactionDate = date.Value,
                PostDate = postDate,
                Description = description.Trim(),
                OriginalDescription = description,
                Amount = finalAmount,
                DuplicateHash = duplicateHash
            });
        }

        // Save import record and transactions
        import.TransactionCount = transactions.Count;
        import.SkippedCount = skippedCount;
        import.DuplicateCount = duplicateCount;

        await _importRepository.AddAsync(import);

        if (transactions.Count > 0)
        {
            // Set import ID on all transactions
            foreach (var txn in transactions)
            {
                txn.ImportId = import.Id;
            }

            // Batch insert for large files (>500 rows)
            const int batchSize = 100;
            if (transactions.Count > 500)
            {
                _logger.LogInformation(
                    "Processing large import with {Count} transactions in batches of {BatchSize}",
                    transactions.Count, batchSize);

                for (int i = 0; i < transactions.Count; i += batchSize)
                {
                    var batch = transactions.Skip(i).Take(batchSize).ToList();
                    await _transactionRepository.AddRangeAsync(batch);
                    await _transactionRepository.SaveChangesAsync();
                }
            }
            else
            {
                await _transactionRepository.AddRangeAsync(transactions);
                await _transactionRepository.SaveChangesAsync();
            }
        }

        // Record fingerprint hit if we used one
        if (session.MatchedFingerprintId.HasValue)
        {
            await _fingerprintService.RecordHitAsync(session.MatchedFingerprintId.Value);
        }

        // Save new fingerprint if requested
        var fingerprintSaved = false;
        if (request.SaveAsFingerprint && !session.MatchedFingerprintId.HasValue)
        {
            var fingerprint = new StatementFingerprint
            {
                UserId = user.Id,
                SourceName = request.FingerprintName ?? Path.GetFileNameWithoutExtension(session.FileName),
                HeaderHash = session.ParsedData.HeaderHash,
                ColumnMapping = JsonSerializer.Serialize(request.ColumnMapping),
                DateFormat = request.DateFormat,
                AmountSign = request.AmountSign
            };

            await _fingerprintService.AddOrUpdateAsync(fingerprint);
            fingerprintSaved = true;
            import.FingerprintId = fingerprint.Id;
            await _importRepository.SaveChangesAsync();
        }

        var response = new StatementImportResponse
        {
            ImportId = import.Id,
            TierUsed = session.TierUsed,
            Imported = transactions.Count,
            Skipped = skippedCount,
            Duplicates = duplicateCount,
            FingerprintSaved = fingerprintSaved,
            Transactions = transactions.Take(10).Select(t => new TransactionSummaryDto
            {
                Id = t.Id,
                TransactionDate = t.TransactionDate,
                Description = t.Description,
                Amount = t.Amount,
                HasMatchedReceipt = false
            }).ToList()
        };

        _logger.LogInformation(
            "Imported statement {FileName} for user {UserId}: {Imported} imported, {Skipped} skipped, {Duplicates} duplicates (Tier {Tier})",
            session.FileName, user.Id, transactions.Count, skippedCount, duplicateCount, session.TierUsed);

        return Ok(response);
    }

    /// <summary>
    /// Gets import history for the current user.
    /// </summary>
    [HttpGet("imports")]
    [ProducesResponseType(typeof(StatementImportListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<StatementImportListResponse>> GetImports(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var user = await _userService.GetOrCreateUserAsync(User);
        var (imports, totalCount) = await _importRepository.GetPagedAsync(user.Id, page, pageSize);

        var response = new StatementImportListResponse
        {
            Imports = imports.Select(i => new ImportSummaryDto
            {
                Id = i.Id,
                FileName = i.FileName,
                SourceName = i.Fingerprint?.SourceName ?? "AI Detected",
                TierUsed = i.TierUsed,
                TransactionCount = i.TransactionCount,
                CreatedAt = i.CreatedAt
            }).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return Ok(response);
    }

    /// <summary>
    /// Gets available fingerprints for the current user.
    /// </summary>
    [HttpGet("fingerprints")]
    [ProducesResponseType(typeof(FingerprintListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<FingerprintListResponse>> GetFingerprints()
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var fingerprints = await _fingerprintService.GetByUserAsync(user.Id);

        var response = new FingerprintListResponse
        {
            Fingerprints = fingerprints.Select(f => new FingerprintSummaryDto
            {
                Id = f.Id,
                SourceName = f.SourceName,
                IsSystem = f.IsSystem,
                HitCount = f.HitCount,
                LastUsedAt = f.LastUsedAt,
                CreatedAt = f.CreatedAt
            }).ToList()
        };

        return Ok(response);
    }

    private static string GetColumnValue(List<string> row, Dictionary<string, int> columnIndexMap, string fieldType)
    {
        if (columnIndexMap.TryGetValue(fieldType, out var index) && index < row.Count)
        {
            return row[index];
        }
        return string.Empty;
    }
}
