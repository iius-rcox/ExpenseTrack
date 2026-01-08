using System.Diagnostics;
using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// AI-powered HTML receipt extraction using Azure OpenAI.
/// Extracts vendor name, transaction date, total amount, and line items from HTML receipt content.
/// </summary>
public class HtmlReceiptExtractionService : IHtmlReceiptExtractionService
{
    private readonly AzureOpenAIClient _client;
    private readonly string _deploymentName;
    private readonly IHtmlSanitizationService _sanitizationService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<HtmlReceiptExtractionService> _logger;
    private readonly string _promptVersion;
    private readonly bool _storeFailedExtractions;
    private readonly double _confidenceThreshold;
    private readonly bool _enableInvoiceFallback;

    private const string SystemPrompt = @"You are a financial document analyst specialized in extracting receipt data from HTML emails.

Extract the following information from the HTML receipt content:
- vendor_name: The merchant/company name (e.g., ""Amazon.com"", ""Uber"", ""Delta Airlines"")
- transaction_date: The transaction/order date in ISO 8601 format (YYYY-MM-DD)
- total_amount: The final total charged (numeric, e.g., 127.43)
- currency: The currency code (e.g., ""USD"", ""EUR"", ""GBP"")
- tax_amount: Tax amount if shown separately (numeric, or null if not found)
- line_items: Array of items purchased, each with:
  - description: Item description
  - quantity: Number of items (default 1)
  - unit_price: Price per item
  - total_price: Total for this line item

Also provide confidence scores (0.0-1.0) for each extracted field.

Important:
- Extract the TOTAL/GRAND TOTAL, not subtotal
- If multiple dates exist, use the order/transaction date, not shipping/delivery date
- For receipts with multiple currencies, use the charged currency
- Set fields to null if they cannot be determined
- Confidence should reflect certainty: 1.0 = clearly stated, 0.7-0.9 = inferred, <0.7 = uncertain

Respond ONLY with valid JSON:
{
  ""vendor_name"": ""string or null"",
  ""transaction_date"": ""YYYY-MM-DD or null"",
  ""total_amount"": number or null,
  ""currency"": ""string or null"",
  ""tax_amount"": number or null,
  ""line_items"": [
    { ""description"": ""string"", ""quantity"": number, ""unit_price"": number, ""total_price"": number }
  ],
  ""confidence"": {
    ""vendor_name"": 0.0-1.0,
    ""transaction_date"": 0.0-1.0,
    ""total_amount"": 0.0-1.0,
    ""currency"": 0.0-1.0,
    ""tax_amount"": 0.0-1.0,
    ""line_items"": 0.0-1.0
  }
}";

    /// <summary>
    /// Alternative prompt optimized for B2B invoices and formal billing documents.
    /// Used as fallback when the receipt prompt fails to extract critical fields.
    /// </summary>
    private const string InvoiceSystemPrompt = @"You are a financial document analyst specialized in extracting invoice data from HTML business documents.

Extract the following information from the HTML invoice/billing document:
- vendor_name: The supplier/vendor/company name sending the invoice (look for ""From:"", ""Billed By:"", company header, sender info)
- transaction_date: The invoice date, billing date, or statement date in ISO 8601 format (YYYY-MM-DD)
- total_amount: The invoice total, amount due, or total payable (numeric, e.g., 549.40)
- currency: The currency code (e.g., ""USD"", ""EUR"", ""GBP"")
- tax_amount: Tax/VAT amount if shown separately (numeric, or null if not found)
- line_items: Array of line items/services, each with:
  - description: Item/service description
  - quantity: Quantity (default 1)
  - unit_price: Unit price
  - total_price: Line total

Also provide confidence scores (0.0-1.0) for each extracted field.

Important for INVOICES:
- Look for ""Invoice Total"", ""Amount Due"", ""Total Payable"", ""Balance Due"" for the total
- Invoice date may be labeled as ""Invoice Date"", ""Billing Date"", ""Statement Date""
- Vendor may be in letterhead, ""From:"" section, or company logo area
- Due date is NOT the transaction date - use invoice/billing date
- For professional services invoices, line items may be hourly rates or service fees
- Set fields to null if they cannot be determined
- Confidence should reflect certainty: 1.0 = clearly stated, 0.7-0.9 = inferred, <0.7 = uncertain

Respond ONLY with valid JSON:
{
  ""vendor_name"": ""string or null"",
  ""transaction_date"": ""YYYY-MM-DD or null"",
  ""total_amount"": number or null,
  ""currency"": ""string or null"",
  ""tax_amount"": number or null,
  ""line_items"": [
    { ""description"": ""string"", ""quantity"": number, ""unit_price"": number, ""total_price"": number }
  ],
  ""confidence"": {
    ""vendor_name"": 0.0-1.0,
    ""transaction_date"": 0.0-1.0,
    ""total_amount"": 0.0-1.0,
    ""currency"": 0.0-1.0,
    ""tax_amount"": 0.0-1.0,
    ""line_items"": 0.0-1.0
  }
}";

    public HtmlReceiptExtractionService(
        IHtmlSanitizationService sanitizationService,
        IBlobStorageService blobStorageService,
        IConfiguration configuration,
        ILogger<HtmlReceiptExtractionService> logger)
    {
        _sanitizationService = sanitizationService;
        _blobStorageService = blobStorageService;
        _logger = logger;

        var endpoint = configuration["AzureOpenAI:Endpoint"]
            ?? throw new InvalidOperationException("AzureOpenAI:Endpoint configuration is required");
        var apiKey = configuration["AzureOpenAI:ApiKey"]
            ?? throw new InvalidOperationException("AzureOpenAI:ApiKey configuration is required");

        _deploymentName = configuration["AzureOpenAI:ChatDeployment"] ?? "gpt-4o-mini";
        _promptVersion = configuration["ReceiptProcessing:Html:ExtractionPromptVersion"] ?? "1.0";
        _storeFailedExtractions = configuration.GetValue("ReceiptProcessing:Html:StoreFailedExtractions", true);
        _confidenceThreshold = configuration.GetValue("ReceiptProcessing:Html:ConfidenceThreshold", 0.60);
        _enableInvoiceFallback = configuration.GetValue("ReceiptProcessing:Html:EnableInvoiceFallback", true);

        // Validate confidence threshold is in valid range
        if (_confidenceThreshold < 0.0 || _confidenceThreshold > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(_confidenceThreshold),
                _confidenceThreshold,
                "HTML confidence threshold must be between 0.0 and 1.0");
        }

        _client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
    }

    /// <inheritdoc />
    public async Task<ReceiptExtractionResult> ExtractAsync(
        string htmlContent,
        CancellationToken ct = default)
    {
        var (result, _) = await ExtractWithMetricsAsync(htmlContent, Guid.Empty, ct);
        return result;
    }

    /// <inheritdoc />
    public async Task<(ReceiptExtractionResult Result, HtmlExtractionMetricsDto Metrics)> ExtractWithMetricsAsync(
        string htmlContent,
        Guid receiptId,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var metrics = new HtmlExtractionMetricsDto
        {
            ReceiptId = receiptId,
            ExtractedAt = DateTime.UtcNow,
            HtmlSizeBytes = System.Text.Encoding.UTF8.GetByteCount(htmlContent),
            PromptVersion = _promptVersion
        };

        try
        {
            // Extract plain text from HTML for AI processing
            var textContent = _sanitizationService.ExtractText(htmlContent);
            metrics = metrics with { TextContentLength = textContent.Length };

            if (string.IsNullOrWhiteSpace(textContent) || textContent.Length < 20)
            {
                _logger.LogWarning("HTML content has insufficient text content for extraction: {Length} chars",
                    textContent.Length);

                return CreateFailedResult(stopwatch, metrics with
                {
                    Success = false,
                    ErrorMessage = "HTML content has insufficient text content",
                    ErrorType = "InsufficientContent"
                }, htmlContent, receiptId);
            }

            // Truncate text if too long (avoid token limits)
            const int maxTextLength = 15000;
            if (textContent.Length > maxTextLength)
            {
                _logger.LogDebug("Truncating text content from {Original} to {Max} chars",
                    textContent.Length, maxTextLength);
                textContent = textContent[..maxTextLength];
            }

            // Call Azure OpenAI
            var chatClient = _client.GetChatClient(_deploymentName);

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(SystemPrompt),
                new UserChatMessage($"Extract receipt data from this HTML email content:\n\n{textContent}")
            };

            var options = new ChatCompletionOptions
            {
                Temperature = 0.1f, // Low temperature for consistent extraction
                MaxOutputTokenCount = 2000
            };

            var response = await chatClient.CompleteChatAsync(messages, options, ct);
            var responseContent = response.Value.Content[0].Text;

            _logger.LogDebug("AI extraction response for receipt {ReceiptId}: {Response}",
                receiptId, responseContent);

            // Parse the JSON response
            var result = ParseExtractionResponse(responseContent);
            result.ExtractionModel = "receipt";

            // Check if fallback to invoice prompt is needed
            if (_enableInvoiceFallback && NeedsFallback(result))
            {
                _logger.LogInformation(
                    "Receipt prompt extraction incomplete for {ReceiptId} (Vendor: {HasVendor}, Date: {HasDate}, Amount: {HasAmount}). Trying invoice prompt...",
                    receiptId,
                    result.VendorName != null,
                    result.TransactionDate != null,
                    result.TotalAmount != null);

                var invoiceResult = await ExtractWithInvoicePromptAsync(textContent, ct);

                if (invoiceResult != null)
                {
                    result = MergeExtractionResults(result, invoiceResult);
                    _logger.LogInformation(
                        "Invoice fallback completed for {ReceiptId}. Model: {Model}, Field sources: {Sources}",
                        receiptId,
                        result.ExtractionModel,
                        string.Join(", ", result.FieldSources.Select(kv => $"{kv.Key}={kv.Value}")));
                }
            }

            stopwatch.Stop();

            // Calculate fields extracted count
            var fieldsExtracted = 0;
            if (result.VendorName != null) fieldsExtracted++;
            if (result.TransactionDate.HasValue) fieldsExtracted++;
            if (result.TotalAmount.HasValue) fieldsExtracted++;
            if (result.Currency != null) fieldsExtracted++;
            if (result.TaxAmount.HasValue) fieldsExtracted++;
            if (result.LineItems.Count > 0) fieldsExtracted++;

            metrics = metrics with
            {
                Success = true,
                ProcessingTime = stopwatch.Elapsed,
                OverallConfidence = result.OverallConfidence,
                FieldConfidences = result.ConfidenceScores,
                FieldsExtracted = fieldsExtracted
            };

            // Log structured metrics
            _logger.LogInformation(
                "HTML extraction completed for receipt {ReceiptId}. " +
                "Vendor: {Vendor}, Date: {Date}, Amount: {Amount}, " +
                "Confidence: {Confidence:P0}, Fields: {Fields}, Time: {Time}ms",
                receiptId,
                result.VendorName ?? "(none)",
                result.TransactionDate?.ToString("yyyy-MM-dd") ?? "(none)",
                result.TotalAmount?.ToString("F2") ?? "(none)",
                result.OverallConfidence,
                fieldsExtracted,
                stopwatch.ElapsedMilliseconds);

            // Store raw HTML if confidence is below threshold
            if (result.OverallConfidence < _confidenceThreshold && _storeFailedExtractions)
            {
                metrics = await StoreFailedExtractionAsync(htmlContent, receiptId, metrics, responseContent);
            }

            return (result, metrics);
        }
        catch (RequestFailedException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Azure OpenAI request failed for receipt {ReceiptId}: {Message}",
                receiptId, ex.Message);

            return CreateFailedResult(stopwatch, metrics with
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorType = "AzureOpenAIError"
            }, htmlContent, receiptId);
        }
        catch (JsonException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to parse AI response for receipt {ReceiptId}", receiptId);

            return CreateFailedResult(stopwatch, metrics with
            {
                Success = false,
                ErrorMessage = "Failed to parse AI response: " + ex.Message,
                ErrorType = "ParseError"
            }, htmlContent, receiptId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error during HTML extraction for receipt {ReceiptId}", receiptId);

            return CreateFailedResult(stopwatch, metrics with
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorType = "UnexpectedError"
            }, htmlContent, receiptId);
        }
    }

    private (ReceiptExtractionResult, HtmlExtractionMetricsDto) CreateFailedResult(
        Stopwatch stopwatch,
        HtmlExtractionMetricsDto metrics,
        string htmlContent,
        Guid receiptId)
    {
        stopwatch.Stop();
        metrics = metrics with { ProcessingTime = stopwatch.Elapsed };

        var result = new ReceiptExtractionResult
        {
            ConfidenceScores = new Dictionary<string, double>
            {
                ["overall"] = 0.0
            }
        };

        // Store failed extraction for analysis
        if (_storeFailedExtractions && receiptId != Guid.Empty)
        {
            _ = StoreFailedExtractionAsync(htmlContent, receiptId, metrics, null);
        }

        return (result, metrics);
    }

    private async Task<HtmlExtractionMetricsDto> StoreFailedExtractionAsync(
        string htmlContent,
        Guid receiptId,
        HtmlExtractionMetricsDto metrics,
        string? aiResponse)
    {
        try
        {
            var basePath = $"extraction-failures/{receiptId:N}";

            // Store raw HTML
            using var htmlStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(htmlContent));
            var htmlBlobPath = $"{basePath}/raw.html";
            await _blobStorageService.UploadAsync(htmlStream, htmlBlobPath, "text/html");

            // Store metrics
            var metricsJson = JsonSerializer.Serialize(metrics, new JsonSerializerOptions { WriteIndented = true });
            using var metricsStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(metricsJson));
            await _blobStorageService.UploadAsync(metricsStream, $"{basePath}/metrics.json", "application/json");

            // Store AI response if available
            if (!string.IsNullOrEmpty(aiResponse))
            {
                using var responseStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(aiResponse));
                await _blobStorageService.UploadAsync(responseStream, $"{basePath}/response.json", "application/json");
            }

            _logger.LogInformation("Stored failed extraction artifacts for receipt {ReceiptId} at {Path}",
                receiptId, basePath);

            return metrics with { RawHtmlBlobPath = htmlBlobPath };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to store extraction artifacts for receipt {ReceiptId}", receiptId);
            return metrics;
        }
    }

    private static ReceiptExtractionResult ParseExtractionResponse(string jsonResponse)
    {
        // Clean up response - extract JSON if wrapped in markdown code blocks
        var json = jsonResponse.Trim();
        if (json.StartsWith("```json"))
        {
            json = json[7..];
        }
        else if (json.StartsWith("```"))
        {
            json = json[3..];
        }
        if (json.EndsWith("```"))
        {
            json = json[..^3];
        }
        json = json.Trim();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var result = new ReceiptExtractionResult();

        // Parse vendor name
        if (root.TryGetProperty("vendor_name", out var vendorEl) && vendorEl.ValueKind != JsonValueKind.Null)
        {
            result.VendorName = vendorEl.GetString();
        }

        // Parse transaction date
        if (root.TryGetProperty("transaction_date", out var dateEl) && dateEl.ValueKind != JsonValueKind.Null)
        {
            var dateStr = dateEl.GetString();
            if (DateTime.TryParse(dateStr, out var parsedDate))
            {
                result.TransactionDate = parsedDate;
            }
        }

        // Parse total amount
        if (root.TryGetProperty("total_amount", out var amountEl) && amountEl.ValueKind == JsonValueKind.Number)
        {
            result.TotalAmount = amountEl.GetDecimal();
        }

        // Parse currency
        if (root.TryGetProperty("currency", out var currencyEl) && currencyEl.ValueKind != JsonValueKind.Null)
        {
            result.Currency = currencyEl.GetString();
        }

        // Parse tax amount
        if (root.TryGetProperty("tax_amount", out var taxEl) && taxEl.ValueKind == JsonValueKind.Number)
        {
            result.TaxAmount = taxEl.GetDecimal();
        }

        // Parse line items
        if (root.TryGetProperty("line_items", out var itemsEl) && itemsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in itemsEl.EnumerateArray())
            {
                var lineItem = new ExpenseFlow.Core.Entities.ReceiptLineItem
                {
                    Description = item.TryGetProperty("description", out var descEl) ? descEl.GetString() ?? "" : "",
                    Quantity = item.TryGetProperty("quantity", out var qtyEl) && qtyEl.ValueKind == JsonValueKind.Number ? qtyEl.GetInt32() : 1,
                    UnitPrice = item.TryGetProperty("unit_price", out var upEl) && upEl.ValueKind == JsonValueKind.Number ? upEl.GetDecimal() : 0,
                    TotalPrice = item.TryGetProperty("total_price", out var tpEl) && tpEl.ValueKind == JsonValueKind.Number ? tpEl.GetDecimal() : 0
                };
                result.LineItems.Add(lineItem);
            }
        }

        // Parse confidence scores - normalize to PascalCase keys to match ReceiptExtractionResult properties
        // AI returns snake_case (vendor_name), we convert to PascalCase (VendorName) for consistency
        // with DocumentIntelligenceService which uses PascalCase
        if (root.TryGetProperty("confidence", out var confidenceEl) && confidenceEl.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in confidenceEl.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Number)
                {
                    var normalizedKey = NormalizeConfidenceKey(prop.Name);
                    result.ConfidenceScores[normalizedKey] = prop.Value.GetDouble();
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Determines if fallback to invoice prompt is needed based on missing critical fields.
    /// </summary>
    private static bool NeedsFallback(ReceiptExtractionResult result)
    {
        // Missing any critical field triggers fallback
        return result.VendorName == null ||
               result.TransactionDate == null ||
               result.TotalAmount == null;
    }

    /// <summary>
    /// Normalizes confidence score keys from snake_case to PascalCase to match
    /// ReceiptExtractionResult property names and DocumentIntelligenceService conventions.
    /// </summary>
    /// <remarks>
    /// Maps: vendor_name → VendorName, transaction_date → TransactionDate,
    /// total_amount → TotalAmount, tax_amount → TaxAmount, line_items → LineItems
    /// </remarks>
    private static string NormalizeConfidenceKey(string snakeCaseKey)
    {
        return snakeCaseKey switch
        {
            "vendor_name" => "VendorName",
            "transaction_date" => "TransactionDate",
            "total_amount" => "TotalAmount",
            "tax_amount" => "TaxAmount",
            "currency" => "Currency",
            "line_items" => "LineItems",
            _ => snakeCaseKey // Return as-is if not recognized
        };
    }

    /// <summary>
    /// Extracts data using the invoice-optimized prompt.
    /// </summary>
    private async Task<ReceiptExtractionResult?> ExtractWithInvoicePromptAsync(
        string textContent,
        CancellationToken ct)
    {
        try
        {
            var chatClient = _client.GetChatClient(_deploymentName);

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(InvoiceSystemPrompt),
                new UserChatMessage($"Extract invoice data from this HTML document content:\n\n{textContent}")
            };

            var options = new ChatCompletionOptions
            {
                Temperature = 0.1f,
                MaxOutputTokenCount = 2000
            };

            var response = await chatClient.CompleteChatAsync(messages, options, ct);
            var responseContent = response.Value.Content[0].Text;

            _logger.LogDebug("Invoice prompt extraction response: {Response}", responseContent);

            var result = ParseExtractionResponse(responseContent);
            result.ExtractionModel = "invoice";

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invoice prompt extraction failed, using receipt-only result");
            return null;
        }
    }

    /// <summary>
    /// Merges extraction results from receipt and invoice prompts, preferring higher confidence values.
    /// All confidence keys use PascalCase to match DocumentIntelligenceService conventions.
    /// </summary>
    private static ReceiptExtractionResult MergeExtractionResults(
        ReceiptExtractionResult receiptResult,
        ReceiptExtractionResult invoiceResult)
    {
        var merged = new ReceiptExtractionResult
        {
            PageCount = 1, // HTML documents are single-page
            ExtractionModel = "receipt+invoice"
        };

        // Merge VendorName (use PascalCase key for confidence lookup)
        merged.VendorName = SelectBestValue(
            receiptResult.VendorName, GetConfidence(receiptResult, "VendorName"),
            invoiceResult.VendorName, GetConfidence(invoiceResult, "VendorName"),
            "VendorName", merged);

        // Merge TransactionDate
        if (receiptResult.TransactionDate == null && invoiceResult.TransactionDate != null)
        {
            merged.TransactionDate = invoiceResult.TransactionDate;
            merged.FieldSources["TransactionDate"] = "invoice";
            var conf = GetConfidence(invoiceResult, "TransactionDate");
            if (conf > 0) merged.ConfidenceScores["TransactionDate"] = conf;
        }
        else if (receiptResult.TransactionDate != null && invoiceResult.TransactionDate == null)
        {
            merged.TransactionDate = receiptResult.TransactionDate;
            merged.FieldSources["TransactionDate"] = "receipt";
            var conf = GetConfidence(receiptResult, "TransactionDate");
            if (conf > 0) merged.ConfidenceScores["TransactionDate"] = conf;
        }
        else if (receiptResult.TransactionDate != null && invoiceResult.TransactionDate != null)
        {
            var receiptConf = GetConfidence(receiptResult, "TransactionDate");
            var invoiceConf = GetConfidence(invoiceResult, "TransactionDate");
            if (invoiceConf > receiptConf)
            {
                merged.TransactionDate = invoiceResult.TransactionDate;
                merged.FieldSources["TransactionDate"] = "invoice";
                merged.ConfidenceScores["TransactionDate"] = invoiceConf;
            }
            else
            {
                merged.TransactionDate = receiptResult.TransactionDate;
                merged.FieldSources["TransactionDate"] = "receipt";
                merged.ConfidenceScores["TransactionDate"] = receiptConf;
            }
        }

        // Merge TotalAmount
        merged.TotalAmount = SelectBestDecimalValue(
            receiptResult.TotalAmount, GetConfidence(receiptResult, "TotalAmount"),
            invoiceResult.TotalAmount, GetConfidence(invoiceResult, "TotalAmount"),
            "TotalAmount", merged);

        // Merge TaxAmount
        merged.TaxAmount = SelectBestDecimalValue(
            receiptResult.TaxAmount, GetConfidence(receiptResult, "TaxAmount"),
            invoiceResult.TaxAmount, GetConfidence(invoiceResult, "TaxAmount"),
            "TaxAmount", merged);

        // Merge Currency
        merged.Currency = receiptResult.Currency ?? invoiceResult.Currency ?? "USD";
        merged.FieldSources["Currency"] = receiptResult.Currency != null ? "receipt" : "invoice";
        var currencyConf = GetConfidence(receiptResult.Currency != null ? receiptResult : invoiceResult, "Currency");
        if (currencyConf > 0) merged.ConfidenceScores["Currency"] = currencyConf;

        // Merge LineItems (prefer the result with more items)
        if (invoiceResult.LineItems.Count > receiptResult.LineItems.Count)
        {
            merged.LineItems = invoiceResult.LineItems;
            merged.FieldSources["LineItems"] = "invoice";
        }
        else
        {
            merged.LineItems = receiptResult.LineItems;
            merged.FieldSources["LineItems"] = "receipt";
        }

        return merged;
    }

    /// <summary>
    /// Gets confidence score for a field using PascalCase key convention.
    /// </summary>
    private static double GetConfidence(ReceiptExtractionResult result, string pascalCaseField)
    {
        return result.ConfidenceScores.TryGetValue(pascalCaseField, out var conf) ? conf : 0;
    }

    /// <summary>
    /// Selects the best string value from two results based on confidence scores.
    /// Stores result with PascalCase key convention.
    /// </summary>
    private static string? SelectBestValue(
        string? receiptValue, double receiptConf,
        string? invoiceValue, double invoiceConf,
        string fieldName,
        ReceiptExtractionResult merged)
    {
        if (receiptValue == null && invoiceValue != null)
        {
            merged.FieldSources[fieldName] = "invoice";
            if (invoiceConf > 0) merged.ConfidenceScores[fieldName] = invoiceConf;
            return invoiceValue;
        }
        if (receiptValue != null && invoiceValue == null)
        {
            merged.FieldSources[fieldName] = "receipt";
            if (receiptConf > 0) merged.ConfidenceScores[fieldName] = receiptConf;
            return receiptValue;
        }
        if (receiptValue != null && invoiceValue != null)
        {
            if (invoiceConf > receiptConf)
            {
                merged.FieldSources[fieldName] = "invoice";
                merged.ConfidenceScores[fieldName] = invoiceConf;
                return invoiceValue;
            }
            merged.FieldSources[fieldName] = "receipt";
            merged.ConfidenceScores[fieldName] = receiptConf;
            return receiptValue;
        }
        return null;
    }

    /// <summary>
    /// Selects the best decimal value from two results based on confidence scores.
    /// Stores result with PascalCase key convention.
    /// </summary>
    private static decimal? SelectBestDecimalValue(
        decimal? receiptValue, double receiptConf,
        decimal? invoiceValue, double invoiceConf,
        string fieldName,
        ReceiptExtractionResult merged)
    {
        if (receiptValue == null && invoiceValue != null)
        {
            merged.FieldSources[fieldName] = "invoice";
            if (invoiceConf > 0) merged.ConfidenceScores[fieldName] = invoiceConf;
            return invoiceValue;
        }
        if (receiptValue != null && invoiceValue == null)
        {
            merged.FieldSources[fieldName] = "receipt";
            if (receiptConf > 0) merged.ConfidenceScores[fieldName] = receiptConf;
            return receiptValue;
        }
        if (receiptValue != null && invoiceValue != null)
        {
            if (invoiceConf > receiptConf)
            {
                merged.FieldSources[fieldName] = "invoice";
                merged.ConfidenceScores[fieldName] = invoiceConf;
                return invoiceValue;
            }
            merged.FieldSources[fieldName] = "receipt";
            merged.ConfidenceScores[fieldName] = receiptConf;
            return receiptValue;
        }
        return null;
    }
}
