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
        _logger.LogDebug("Starting receipt analysis with Azure Document Intelligence for content type {ContentType}", contentType);

        var result = new ReceiptExtractionResult { ExtractionModel = "receipt" };

        try
        {
            // Use the prebuilt receipt model with BinaryData
            // Copy to MemoryStream first to ensure the entire document is loaded
            using var memoryStream = new MemoryStream();
            await documentStream.CopyToAsync(memoryStream);

            if (memoryStream.Length == 0)
            {
                _logger.LogWarning("Document stream is empty, cannot analyze");
                return result;
            }

            _logger.LogDebug("Analyzing document of {Size} bytes with content type {ContentType}",
                memoryStream.Length, contentType);

            // BUG-002 fix: Use BinaryData.FromBytes for proper binary data submission
            // The SDK accepts BinaryData directly for document analysis
            var bytesSource = BinaryData.FromBytes(memoryStream.ToArray());

            var operation = await _client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                "prebuilt-receipt",
                bytesSource);

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

    public async Task<ReceiptExtractionResult> AnalyzeInvoiceAsync(Stream documentStream, string contentType)
    {
        _logger.LogDebug("Starting invoice analysis with Azure Document Intelligence for content type {ContentType}", contentType);

        var result = new ReceiptExtractionResult { ExtractionModel = "invoice" };

        try
        {
            using var memoryStream = new MemoryStream();
            await documentStream.CopyToAsync(memoryStream);

            if (memoryStream.Length == 0)
            {
                _logger.LogWarning("Document stream is empty, cannot analyze");
                return result;
            }

            _logger.LogDebug("Analyzing invoice of {Size} bytes with content type {ContentType}",
                memoryStream.Length, contentType);

            var bytesSource = BinaryData.FromBytes(memoryStream.ToArray());

            var operation = await _client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                "prebuilt-invoice",
                bytesSource);

            var analyzeResult = operation.Value;

            // Set page count
            result.PageCount = analyzeResult.Pages?.Count ?? 1;

            // Extract data from the first invoice found
            if (analyzeResult.Documents != null && analyzeResult.Documents.Count > 0)
            {
                var invoice = analyzeResult.Documents[0];
                ExtractInvoiceFields(invoice, result);
            }
            else
            {
                _logger.LogWarning("No invoice documents found in the analyzed document");
            }

            _logger.LogInformation(
                "Invoice analysis completed. Vendor: {Vendor}, Amount: {Amount}, Confidence: {Confidence:P1}",
                result.VendorName ?? "Unknown",
                result.TotalAmount?.ToString("C") ?? "Unknown",
                result.OverallConfidence);

            return result;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Document Intelligence invoice request failed: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<ReceiptExtractionResult> AnalyzeWithFallbackAsync(
        Stream documentStream,
        string contentType,
        double fallbackConfidenceThreshold = 0.5)
    {
        // Validate threshold range
        if (fallbackConfidenceThreshold < 0.0 || fallbackConfidenceThreshold > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fallbackConfidenceThreshold),
                fallbackConfidenceThreshold,
                "Confidence threshold must be between 0.0 and 1.0");
        }

        _logger.LogDebug("Starting extraction with fallback for content type {ContentType}", contentType);

        // Prepare document bytes once - avoids redundant stream copies in individual methods
        // This is a significant memory optimization for large documents
        using var memoryStream = new MemoryStream();
        await documentStream.CopyToAsync(memoryStream);

        if (memoryStream.Length == 0)
        {
            _logger.LogWarning("Document stream is empty, cannot analyze");
            return new ReceiptExtractionResult { ExtractionModel = "receipt" };
        }

        var documentData = BinaryData.FromBytes(memoryStream.GetBuffer().AsMemory(0, (int)memoryStream.Length));

        // Try receipt model first (most common case) - use internal method that accepts BinaryData
        var receiptResult = await AnalyzeReceiptInternalAsync(documentData, contentType);

        // Check if receipt extraction was successful enough
        var needsFallback = NeedsFallback(receiptResult, fallbackConfidenceThreshold);

        if (!needsFallback)
        {
            _logger.LogDebug("Receipt model extraction successful, no fallback needed");
            return receiptResult;
        }

        _logger.LogInformation(
            "Receipt model extraction incomplete (Vendor: {HasVendor}, Date: {HasDate}, Amount: {HasAmount}). Trying invoice model...",
            receiptResult.VendorName != null,
            receiptResult.TransactionDate != null,
            receiptResult.TotalAmount != null);

        // Try invoice model - reuse the same BinaryData (no additional allocation)
        var invoiceResult = await AnalyzeInvoiceInternalAsync(documentData, contentType);

        // Merge results, preferring higher confidence values
        var mergedResult = MergeExtractionResults(receiptResult, invoiceResult);

        _logger.LogInformation(
            "Fallback extraction completed. Model: {Model}, Vendor: {Vendor}, Amount: {Amount}, Confidence: {Confidence:P1}",
            mergedResult.ExtractionModel,
            mergedResult.VendorName ?? "Unknown",
            mergedResult.TotalAmount?.ToString("C") ?? "Unknown",
            mergedResult.OverallConfidence);

        return mergedResult;
    }

    /// <summary>
    /// Determines if fallback to invoice model is needed based on missing fields or low confidence.
    /// </summary>
    private bool NeedsFallback(ReceiptExtractionResult result, double confidenceThreshold)
    {
        // Missing critical fields
        if (result.VendorName == null || result.TransactionDate == null || result.TotalAmount == null)
        {
            return true;
        }

        // Check individual field confidence
        var criticalFields = new[] { "VendorName", "TransactionDate", "TotalAmount" };
        foreach (var field in criticalFields)
        {
            if (result.ConfidenceScores.TryGetValue(field, out var confidence) && confidence < confidenceThreshold)
            {
                _logger.LogDebug("Field {Field} has low confidence ({Confidence:P1}), triggering fallback", field, confidence);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Internal receipt analysis using pre-prepared BinaryData.
    /// Used by AnalyzeWithFallbackAsync to avoid redundant memory allocations.
    /// </summary>
    private async Task<ReceiptExtractionResult> AnalyzeReceiptInternalAsync(BinaryData documentData, string contentType)
    {
        _logger.LogDebug("Starting receipt analysis (internal) with content type {ContentType}", contentType);

        var result = new ReceiptExtractionResult { ExtractionModel = "receipt" };

        try
        {
            var operation = await _client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                "prebuilt-receipt",
                documentData);

            var analyzeResult = operation.Value;
            result.PageCount = analyzeResult.Pages?.Count ?? 1;

            if (analyzeResult.Documents != null && analyzeResult.Documents.Count > 0)
            {
                var receipt = analyzeResult.Documents[0];
                ExtractReceiptFields(receipt, result);
            }
            else
            {
                _logger.LogWarning("No receipt documents found in the analyzed document");
            }

            return result;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Document Intelligence request failed: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Internal invoice analysis using pre-prepared BinaryData.
    /// Used by AnalyzeWithFallbackAsync to avoid redundant memory allocations.
    /// </summary>
    private async Task<ReceiptExtractionResult> AnalyzeInvoiceInternalAsync(BinaryData documentData, string contentType)
    {
        _logger.LogDebug("Starting invoice analysis (internal) with content type {ContentType}", contentType);

        var result = new ReceiptExtractionResult { ExtractionModel = "invoice" };

        try
        {
            var operation = await _client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                "prebuilt-invoice",
                documentData);

            var analyzeResult = operation.Value;
            result.PageCount = analyzeResult.Pages?.Count ?? 1;

            if (analyzeResult.Documents != null && analyzeResult.Documents.Count > 0)
            {
                var invoice = analyzeResult.Documents[0];
                ExtractInvoiceFields(invoice, result);
            }
            else
            {
                _logger.LogWarning("No invoice documents found in the analyzed document");
            }

            return result;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Document Intelligence invoice request failed: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Merges extraction results from receipt and invoice models, preferring higher confidence values.
    /// </summary>
    private ReceiptExtractionResult MergeExtractionResults(
        ReceiptExtractionResult receiptResult,
        ReceiptExtractionResult invoiceResult)
    {
        var merged = new ReceiptExtractionResult
        {
            PageCount = Math.Max(receiptResult.PageCount, invoiceResult.PageCount),
            ExtractionModel = "receipt+invoice"
        };

        // Merge VendorName
        merged.VendorName = SelectBestValue(
            receiptResult.VendorName, GetConfidence(receiptResult, "VendorName"),
            invoiceResult.VendorName, GetConfidence(invoiceResult, "VendorName"),
            "VendorName", merged);

        // Merge TransactionDate
        var receiptDate = receiptResult.TransactionDate;
        var invoiceDate = invoiceResult.TransactionDate;
        var receiptDateConf = GetConfidence(receiptResult, "TransactionDate");
        var invoiceDateConf = GetConfidence(invoiceResult, "TransactionDate");

        if (receiptDate == null && invoiceDate != null)
        {
            merged.TransactionDate = invoiceDate;
            merged.FieldSources["TransactionDate"] = "invoice";
            if (invoiceDateConf > 0) merged.ConfidenceScores["TransactionDate"] = invoiceDateConf;
        }
        else if (receiptDate != null && invoiceDate == null)
        {
            merged.TransactionDate = receiptDate;
            merged.FieldSources["TransactionDate"] = "receipt";
            if (receiptDateConf > 0) merged.ConfidenceScores["TransactionDate"] = receiptDateConf;
        }
        else if (receiptDate != null && invoiceDate != null)
        {
            // Both have values, prefer higher confidence
            if (invoiceDateConf > receiptDateConf)
            {
                merged.TransactionDate = invoiceDate;
                merged.FieldSources["TransactionDate"] = "invoice";
                merged.ConfidenceScores["TransactionDate"] = invoiceDateConf;
            }
            else
            {
                merged.TransactionDate = receiptDate;
                merged.FieldSources["TransactionDate"] = "receipt";
                merged.ConfidenceScores["TransactionDate"] = receiptDateConf;
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

        // Merge Currency (prefer non-null, then receipt)
        merged.Currency = receiptResult.Currency ?? invoiceResult.Currency ?? "USD";
        merged.FieldSources["Currency"] = receiptResult.Currency != null ? "receipt" : "invoice";

        // Merge LineItems (prefer the result with more items, or higher total confidence)
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

    private static double GetConfidence(ReceiptExtractionResult result, string field)
    {
        return result.ConfidenceScores.TryGetValue(field, out var conf) ? conf : 0;
    }

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
            // Both have values, prefer higher confidence
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
            // Both have values, prefer higher confidence
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

    private void ExtractReceiptFields(AnalyzedDocument receipt, ReceiptExtractionResult result)
    {
        // Log all available fields for debugging
        _logger.LogDebug("Available receipt fields: {Fields}", string.Join(", ", receipt.Fields.Keys));

        // Extract MerchantName
        if (receipt.Fields.TryGetValue("MerchantName", out var merchantField) && merchantField.Content != null)
        {
            result.VendorName = merchantField.Content;
            if (merchantField.Confidence.HasValue)
            {
                result.ConfidenceScores["VendorName"] = merchantField.Confidence.Value;
            }
        }

        // BUG-004 fix: If MerchantName is missing, try to extract from MerchantAddress
        // Some receipts (like parking) have vendor info only in the address line
        if (string.IsNullOrWhiteSpace(result.VendorName))
        {
            var extractedVendor = TryExtractVendorFromAddress(receipt);
            if (!string.IsNullOrWhiteSpace(extractedVendor))
            {
                result.VendorName = extractedVendor;
                // Use lower confidence since this is inferred from address
                result.ConfidenceScores["VendorName"] = 0.6;
                _logger.LogInformation("Extracted vendor from address: {Vendor}", extractedVendor);
            }
        }

        // Extract TransactionDate
        if (receipt.Fields.TryGetValue("TransactionDate", out var dateField) && dateField.ValueDate.HasValue)
        {
            result.TransactionDate = dateField.ValueDate.Value.DateTime;
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
                if (item.ValueDictionary != null)
                {
                    var lineItem = ExtractLineItem(item.ValueDictionary);
                    if (lineItem != null)
                    {
                        result.LineItems.Add(lineItem);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Extracts invoice fields from Azure Document Intelligence prebuilt-invoice model
    /// and maps them to the ReceiptExtractionResult format.
    /// </summary>
    /// <remarks>
    /// Invoice model field mapping:
    /// - VendorName → VendorName
    /// - InvoiceDate → TransactionDate
    /// - InvoiceTotal → TotalAmount
    /// - TotalTax → TaxAmount
    /// - CurrencyCode → Currency
    /// - Items → LineItems (with different subfield names)
    /// </remarks>
    private void ExtractInvoiceFields(AnalyzedDocument invoice, ReceiptExtractionResult result)
    {
        // Log all available fields for debugging
        _logger.LogDebug("Available invoice fields: {Fields}", string.Join(", ", invoice.Fields.Keys));

        // Extract VendorName
        if (invoice.Fields.TryGetValue("VendorName", out var vendorField) && vendorField.Content != null)
        {
            result.VendorName = vendorField.Content;
            if (vendorField.Confidence.HasValue)
            {
                result.ConfidenceScores["VendorName"] = vendorField.Confidence.Value;
            }
        }

        // Extract InvoiceDate (maps to TransactionDate)
        if (invoice.Fields.TryGetValue("InvoiceDate", out var dateField) && dateField.ValueDate.HasValue)
        {
            result.TransactionDate = dateField.ValueDate.Value.DateTime;
            if (dateField.Confidence.HasValue)
            {
                result.ConfidenceScores["TransactionDate"] = dateField.Confidence.Value;
            }
        }
        // Fallback to DueDate if InvoiceDate not present
        else if (invoice.Fields.TryGetValue("DueDate", out var dueDateField) && dueDateField.ValueDate.HasValue)
        {
            result.TransactionDate = dueDateField.ValueDate.Value.DateTime;
            if (dueDateField.Confidence.HasValue)
            {
                // Slightly lower confidence since DueDate is not the actual invoice date
                result.ConfidenceScores["TransactionDate"] = dueDateField.Confidence.Value * 0.9;
            }
        }

        // Extract InvoiceTotal (maps to TotalAmount)
        if (invoice.Fields.TryGetValue("InvoiceTotal", out var totalField) && totalField.ValueCurrency != null)
        {
            result.TotalAmount = (decimal)totalField.ValueCurrency.Amount;
            result.Currency = totalField.ValueCurrency.CurrencyCode ?? "USD";
            if (totalField.Confidence.HasValue)
            {
                result.ConfidenceScores["TotalAmount"] = totalField.Confidence.Value;
            }
        }
        // Fallback to AmountDue if InvoiceTotal not present
        else if (invoice.Fields.TryGetValue("AmountDue", out var amountDueField) && amountDueField.ValueCurrency != null)
        {
            result.TotalAmount = (decimal)amountDueField.ValueCurrency.Amount;
            result.Currency = amountDueField.ValueCurrency.CurrencyCode ?? "USD";
            if (amountDueField.Confidence.HasValue)
            {
                result.ConfidenceScores["TotalAmount"] = amountDueField.Confidence.Value;
            }
        }
        // Fallback to SubTotal if neither InvoiceTotal nor AmountDue present
        else if (invoice.Fields.TryGetValue("SubTotal", out var subTotalField) && subTotalField.ValueCurrency != null)
        {
            result.TotalAmount = (decimal)subTotalField.ValueCurrency.Amount;
            result.Currency = subTotalField.ValueCurrency.CurrencyCode ?? "USD";
            if (subTotalField.Confidence.HasValue)
            {
                // Lower confidence since SubTotal doesn't include tax
                result.ConfidenceScores["TotalAmount"] = subTotalField.Confidence.Value * 0.8;
            }
        }

        // Extract TotalTax (maps to TaxAmount)
        if (invoice.Fields.TryGetValue("TotalTax", out var taxField) && taxField.ValueCurrency != null)
        {
            result.TaxAmount = (decimal)taxField.ValueCurrency.Amount;
            if (taxField.Confidence.HasValue)
            {
                result.ConfidenceScores["TaxAmount"] = taxField.Confidence.Value;
            }
        }

        // Extract Line Items - invoice model uses different field names
        if (invoice.Fields.TryGetValue("Items", out var invoiceItemsField) && invoiceItemsField.ValueList != null)
        {
            foreach (var item in invoiceItemsField.ValueList)
            {
                if (item.ValueDictionary != null)
                {
                    var lineItem = ExtractInvoiceLineItem(item.ValueDictionary);
                    if (lineItem != null)
                    {
                        result.LineItems.Add(lineItem);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Extracts a line item from invoice model fields.
    /// Invoice model uses different field names than receipt model.
    /// </summary>
    private ReceiptLineItem? ExtractInvoiceLineItem(IReadOnlyDictionary<string, DocumentField> itemFields)
    {
        var lineItem = new ReceiptLineItem();
        var hasData = false;

        // Invoice uses "Description" same as receipt
        if (itemFields.TryGetValue("Description", out var descField) && descField.Content != null)
        {
            lineItem.Description = descField.Content;
            lineItem.Confidence = descField.Confidence;
            hasData = true;
        }
        // Fallback to ProductCode if Description missing
        else if (itemFields.TryGetValue("ProductCode", out var codeField) && codeField.Content != null)
        {
            lineItem.Description = codeField.Content;
            lineItem.Confidence = codeField.Confidence;
            hasData = true;
        }

        // Invoice uses "Quantity" same as receipt
        if (itemFields.TryGetValue("Quantity", out var qtyField) && qtyField.ValueDouble.HasValue)
        {
            lineItem.Quantity = (decimal)qtyField.ValueDouble.Value;
            hasData = true;
        }

        // Invoice uses "UnitPrice" instead of "Price"
        if (itemFields.TryGetValue("UnitPrice", out var priceField) && priceField.ValueCurrency != null)
        {
            lineItem.UnitPrice = (decimal)priceField.ValueCurrency.Amount;
            hasData = true;
        }
        else if (itemFields.TryGetValue("Unit", out var unitField) && unitField.ValueCurrency != null)
        {
            lineItem.UnitPrice = (decimal)unitField.ValueCurrency.Amount;
            hasData = true;
        }

        // Invoice uses "Amount" instead of "TotalPrice"
        if (itemFields.TryGetValue("Amount", out var totalField) && totalField.ValueCurrency != null)
        {
            lineItem.TotalPrice = (decimal)totalField.ValueCurrency.Amount;
            hasData = true;
        }

        return hasData ? lineItem : null;
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

        if (itemFields.TryGetValue("Quantity", out var qtyField) && qtyField.ValueDouble.HasValue)
        {
            lineItem.Quantity = (decimal)qtyField.ValueDouble.Value;
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

    /// <summary>
    /// Attempts to extract a vendor name from the MerchantAddress field when MerchantName is missing.
    /// Handles cases like airport parking where the address contains the venue name.
    /// </summary>
    /// <remarks>
    /// BUG-004 fix: Azure Document Intelligence may not recognize some vendor names,
    /// especially for parking receipts where the vendor info is in address format.
    /// Example: "RDU Airport NC 27623" should extract "RDU Airport Parking"
    /// </remarks>
    private string? TryExtractVendorFromAddress(AnalyzedDocument receipt)
    {
        // Try MerchantAddress field first
        if (receipt.Fields.TryGetValue("MerchantAddress", out var addressField) && addressField.Content != null)
        {
            var address = addressField.Content;
            _logger.LogDebug("Attempting to extract vendor from address: {Address}", address);

            // Check for airport patterns
            var normalizedVendor = TryMatchAirportPattern(address);
            if (!string.IsNullOrWhiteSpace(normalizedVendor))
            {
                return normalizedVendor;
            }
        }

        // Also check raw content for patterns if address field didn't work
        // Look through the receipt's content for recognizable patterns
        foreach (var field in receipt.Fields)
        {
            if (field.Value.Content == null) continue;

            var content = field.Value.Content;

            // Check for airport patterns in any field
            var normalizedVendor = TryMatchAirportPattern(content);
            if (!string.IsNullOrWhiteSpace(normalizedVendor))
            {
                _logger.LogDebug("Found vendor pattern in field {FieldName}: {Content}", field.Key, content);
                return normalizedVendor;
            }
        }

        return null;
    }

    /// <summary>
    /// Matches airport-related patterns and returns a normalized vendor name.
    /// </summary>
    private static string? TryMatchAirportPattern(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var upperText = text.ToUpperInvariant();

        // Common airport parking patterns
        // RDU = Raleigh-Durham International Airport
        if (upperText.Contains("RDU") && (upperText.Contains("AIRPORT") || upperText.Contains("PARKING")))
        {
            return "RDU Airport Parking";
        }

        // Generic airport patterns - [CODE] Airport
        var airportCodes = new[] { "ATL", "ORD", "DFW", "DEN", "JFK", "LAX", "SFO", "SEA", "LAS", "MCO",
                                    "CLT", "PHX", "IAH", "MIA", "BOS", "MSP", "DTW", "FLL", "EWR", "PHL" };

        foreach (var code in airportCodes)
        {
            if (upperText.Contains(code) && upperText.Contains("AIRPORT"))
            {
                return $"{code} Airport Parking";
            }
        }

        // Check for generic "Airport Parking" pattern
        if (upperText.Contains("AIRPORT") && upperText.Contains("PARKING"))
        {
            // Try to extract the airport name
            var match = System.Text.RegularExpressions.Regex.Match(
                text,
                @"(\w+)\s+Airport",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (match.Success)
            {
                return $"{match.Groups[1].Value} Airport Parking";
            }

            return "Airport Parking";
        }

        return null;
    }
}
