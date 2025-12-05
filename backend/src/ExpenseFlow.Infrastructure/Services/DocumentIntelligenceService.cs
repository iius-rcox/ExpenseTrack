using Azure;
using Azure.AI.DocumentIntelligence;
using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Azure Document Intelligence implementation for receipt OCR extraction.
/// Uses the prebuilt-receipt model for structured data extraction.
/// </summary>
public class DocumentIntelligenceService : IDocumentIntelligenceService
{
    private readonly DocumentIntelligenceClient _client;
    private readonly ILogger<DocumentIntelligenceService> _logger;

    public DocumentIntelligenceService(
        IConfiguration configuration,
        ILogger<DocumentIntelligenceService> logger)
    {
        _logger = logger;

        var endpoint = configuration["DocumentIntelligence:Endpoint"]
            ?? throw new InvalidOperationException("DocumentIntelligence:Endpoint is required");
        var apiKey = configuration["DocumentIntelligence:ApiKey"]
            ?? throw new InvalidOperationException("DocumentIntelligence:ApiKey is required");

        _client = new DocumentIntelligenceClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
    }

    public async Task<ReceiptExtractionResult> AnalyzeReceiptAsync(Stream documentStream, string contentType)
    {
        _logger.LogDebug("Starting receipt analysis with Azure Document Intelligence");

        var result = new ReceiptExtractionResult();

        try
        {
            // Use the prebuilt receipt model
            var content = new AnalyzeDocumentContent
            {
                Base64Source = await ConvertToBase64Async(documentStream)
            };

            var operation = await _client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                "prebuilt-receipt",
                content);

            var analyzeResult = operation.Value;

            // Set page count
            result.PageCount = analyzeResult.Pages?.Count ?? 1;

            // Extract data from the first receipt found
            if (analyzeResult.Documents != null && analyzeResult.Documents.Count > 0)
            {
                var receipt = analyzeResult.Documents[0];
                ExtractReceiptFields(receipt, result);
            }
            else
            {
                _logger.LogWarning("No receipt documents found in the analyzed document");
            }

            _logger.LogInformation(
                "Receipt analysis completed. Vendor: {Vendor}, Amount: {Amount}, Confidence: {Confidence:P1}",
                result.VendorName ?? "Unknown",
                result.TotalAmount?.ToString("C") ?? "Unknown",
                result.OverallConfidence);

            return result;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Document Intelligence request failed: {Message}", ex.Message);
            throw;
        }
    }

    private void ExtractReceiptFields(AnalyzedDocument receipt, ReceiptExtractionResult result)
    {
        // Extract MerchantName
        if (receipt.Fields.TryGetValue("MerchantName", out var merchantField) && merchantField.Content != null)
        {
            result.VendorName = merchantField.Content;
            if (merchantField.Confidence.HasValue)
            {
                result.ConfidenceScores["VendorName"] = merchantField.Confidence.Value;
            }
        }

        // Extract TransactionDate
        if (receipt.Fields.TryGetValue("TransactionDate", out var dateField) && dateField.ValueDate.HasValue)
        {
            result.TransactionDate = dateField.ValueDate.Value.ToDateTime(TimeOnly.MinValue);
            if (dateField.Confidence.HasValue)
            {
                result.ConfidenceScores["TransactionDate"] = dateField.Confidence.Value;
            }
        }

        // Extract Total
        if (receipt.Fields.TryGetValue("Total", out var totalField) && totalField.ValueCurrency != null)
        {
            result.TotalAmount = (decimal)totalField.ValueCurrency.Amount;
            result.Currency = totalField.ValueCurrency.CurrencyCode ?? "USD";
            if (totalField.Confidence.HasValue)
            {
                result.ConfidenceScores["TotalAmount"] = totalField.Confidence.Value;
            }
        }

        // Extract Tax
        if (receipt.Fields.TryGetValue("TotalTax", out var taxField) && taxField.ValueCurrency != null)
        {
            result.TaxAmount = (decimal)taxField.ValueCurrency.Amount;
            if (taxField.Confidence.HasValue)
            {
                result.ConfidenceScores["TaxAmount"] = taxField.Confidence.Value;
            }
        }

        // Extract Line Items
        if (receipt.Fields.TryGetValue("Items", out var itemsField) && itemsField.ValueList != null)
        {
            foreach (var item in itemsField.ValueList)
            {
                if (item.ValueObject != null)
                {
                    var lineItem = ExtractLineItem(item.ValueObject);
                    if (lineItem != null)
                    {
                        result.LineItems.Add(lineItem);
                    }
                }
            }
        }
    }

    private ReceiptLineItem? ExtractLineItem(IReadOnlyDictionary<string, DocumentField> itemFields)
    {
        var lineItem = new ReceiptLineItem();
        var hasData = false;

        if (itemFields.TryGetValue("Description", out var descField) && descField.Content != null)
        {
            lineItem.Description = descField.Content;
            lineItem.Confidence = descField.Confidence;
            hasData = true;
        }

        if (itemFields.TryGetValue("Quantity", out var qtyField) && qtyField.ValueNumber.HasValue)
        {
            lineItem.Quantity = (decimal)qtyField.ValueNumber.Value;
            hasData = true;
        }

        if (itemFields.TryGetValue("Price", out var priceField) && priceField.ValueCurrency != null)
        {
            lineItem.UnitPrice = (decimal)priceField.ValueCurrency.Amount;
            hasData = true;
        }

        if (itemFields.TryGetValue("TotalPrice", out var totalField) && totalField.ValueCurrency != null)
        {
            lineItem.TotalPrice = (decimal)totalField.ValueCurrency.Amount;
            hasData = true;
        }

        return hasData ? lineItem : null;
    }

    private static async Task<BinaryData> ConvertToBase64Async(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        return BinaryData.FromBytes(memoryStream.ToArray());
    }
}
