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
/// AI-powered column mapping inference using Azure OpenAI GPT-4o-mini (Tier 3).
/// </summary>
public class ColumnMappingInferenceService : IColumnMappingInferenceService
{
    private readonly AzureOpenAIClient _client;
    private readonly string _deploymentName;
    private readonly ILogger<ColumnMappingInferenceService> _logger;

    private const string SystemPrompt = @"You are a financial data analyst. Your task is to analyze CSV/Excel headers and sample data to infer column mappings for credit card statement imports.

Map each column to one of these field types:
- date: Transaction date (when the transaction occurred)
- post_date: Posting date (when the transaction was posted to account)
- description: Transaction description/merchant name
- amount: Transaction amount (monetary value)
- category: Transaction category (optional)
- memo: Additional notes/memo field (optional)
- reference: Reference number/transaction ID (optional)
- ignore: Column should be ignored

Also determine:
- dateFormat: The date pattern used (e.g., ""MM/dd/yyyy"", ""yyyy-MM-dd"", ""dd/MM/yyyy"")
- amountSign: ""negative_charges"" if negative amounts represent expenses (most common), or ""positive_charges"" if positive amounts represent expenses (American Express style)

Respond ONLY with valid JSON in this exact format:
{
  ""columnMapping"": { ""<header>"": ""<field_type>"" },
  ""dateFormat"": ""<pattern>"",
  ""amountSign"": ""<convention>"",
  ""confidence"": <0.0-1.0>
}

Confidence score guidelines:
- 1.0: All columns clearly identified, common format
- 0.8-0.9: Most columns identified, some ambiguity
- 0.6-0.7: Several columns unclear, needs review
- <0.6: Very unusual format, low confidence";

    public ColumnMappingInferenceService(
        IConfiguration configuration,
        ILogger<ColumnMappingInferenceService> logger)
    {
        _logger = logger;

        var endpoint = configuration["AzureOpenAI:Endpoint"]
            ?? throw new InvalidOperationException("AzureOpenAI:Endpoint configuration is required");
        var apiKey = configuration["AzureOpenAI:ApiKey"]
            ?? throw new InvalidOperationException("AzureOpenAI:ApiKey configuration is required");

        _deploymentName = configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4o-mini";

        _client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
    }

    /// <inheritdoc />
    public async Task<ColumnMappingInferenceResult> InferMappingAsync(
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> sampleRows,
        CancellationToken cancellationToken = default)
    {
        var userPrompt = BuildUserPrompt(headers, sampleRows);

        try
        {
            var chatClient = _client.GetChatClient(_deploymentName);

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(SystemPrompt),
                new UserChatMessage(userPrompt)
            };

            var options = new ChatCompletionOptions
            {
                Temperature = 0.1f, // Low temperature for consistent results
                MaxOutputTokenCount = 1000
            };

            var response = await chatClient.CompleteChatAsync(messages, options, cancellationToken);
            var content = response.Value.Content[0].Text;

            _logger.LogDebug("AI inference response: {Response}", content);

            // Parse JSON response
            var result = ParseResponse(content, headers);

            _logger.LogInformation(
                "AI inference completed with confidence {Confidence:P0} for {HeaderCount} headers",
                result.Confidence, headers.Count);

            return result;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure OpenAI request failed: {Message}", ex.Message);
            throw new InvalidOperationException("AI service is unavailable", ex);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Unexpected error during AI inference");
            throw new InvalidOperationException("AI inference failed", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var chatClient = _client.GetChatClient(_deploymentName);

            var messages = new List<ChatMessage>
            {
                new UserChatMessage("test")
            };

            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = 5
            };

            await chatClient.CompleteChatAsync(messages, options, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI service availability check failed");
            return false;
        }
    }

    private static string BuildUserPrompt(
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> sampleRows)
    {
        var prompt = $"Headers: {string.Join(", ", headers)}\n\nSample rows:\n";

        for (int i = 0; i < Math.Min(3, sampleRows.Count); i++)
        {
            prompt += $"Row {i + 1}: {string.Join(", ", sampleRows[i])}\n";
        }

        return prompt;
    }

    private ColumnMappingInferenceResult ParseResponse(string content, IReadOnlyList<string> headers)
    {
        try
        {
            // Clean up response (remove markdown code blocks if present)
            content = content.Trim();
            if (content.StartsWith("```"))
            {
                var lines = content.Split('\n');
                content = string.Join('\n', lines.Skip(1).Take(lines.Length - 2));
            }

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            var result = new ColumnMappingInferenceResult
            {
                DateFormat = root.TryGetProperty("dateFormat", out var df) ? df.GetString() : null,
                AmountSign = root.TryGetProperty("amountSign", out var asign)
                    ? asign.GetString() ?? AmountSignConventions.NegativeCharges
                    : AmountSignConventions.NegativeCharges,
                Confidence = root.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 0.5
            };

            if (root.TryGetProperty("columnMapping", out var mapping))
            {
                foreach (var prop in mapping.EnumerateObject())
                {
                    var fieldType = prop.Value.GetString();
                    if (!string.IsNullOrEmpty(fieldType) && ColumnFieldTypes.IsValid(fieldType))
                    {
                        // Find matching header (case-insensitive)
                        var matchingHeader = headers.FirstOrDefault(h =>
                            h.Equals(prop.Name, StringComparison.OrdinalIgnoreCase)) ?? prop.Name;
                        result.ColumnMapping[matchingHeader] = fieldType;
                    }
                }
            }

            // Validate required fields are mapped
            var mappedFields = result.ColumnMapping.Values.ToHashSet();
            if (!mappedFields.Contains(ColumnFieldTypes.Date) ||
                !mappedFields.Contains(ColumnFieldTypes.Amount) ||
                !mappedFields.Contains(ColumnFieldTypes.Description))
            {
                _logger.LogWarning("AI inference missing required field mappings");
                result.Confidence = Math.Min(result.Confidence, 0.5);
            }

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI response as JSON: {Content}", content);

            // Return low-confidence empty result
            return new ColumnMappingInferenceResult
            {
                Confidence = 0.0
            };
        }
    }
}
