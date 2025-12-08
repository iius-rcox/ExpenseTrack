# Quickstart: Statement Import & Fingerprinting

**Feature**: 004-statement-fingerprinting
**Date**: 2025-12-05

## Prerequisites

- .NET 8 SDK installed
- Node.js 18+ (for frontend)
- PostgreSQL access (via Supabase port-forward or local)
- Azure OpenAI access configured in appsettings

## Backend Setup

### 1. Add NuGet Packages

```bash
cd backend/src/ExpenseFlow.Infrastructure
dotnet add package CsvHelper --version 31.0.0
dotnet add package ClosedXML --version 0.102.2
```

### 2. Create Migration

```bash
cd backend/src/ExpenseFlow.Api
dotnet ef migrations add AddStatementImportTables -p ../ExpenseFlow.Infrastructure -c ExpenseFlowDbContext
```

### 3. Apply Migration

```bash
# With port-forward active
dotnet ef database update -p ../ExpenseFlow.Infrastructure -c ExpenseFlowDbContext
```

### 4. Seed System Fingerprints

```bash
# Run seed script (or apply via migration)
dotnet run -- seed-fingerprints
```

## Configuration

### appsettings.Development.json

```json
{
  "StatementImport": {
    "MaxFileSizeMB": 10,
    "AllowedExtensions": [".csv", ".xlsx", ".xls"],
    "AnalysisSessionTimeoutMinutes": 30,
    "SampleRowCount": 5,
    "BatchSize": 500
  },
  "AzureOpenAI": {
    "Endpoint": "https://iius-embedding.openai.azure.com/",
    "DeploymentName": "gpt-4o-mini",
    "ApiVersion": "2024-02-15-preview"
  }
}
```

## API Testing

### Analyze Statement

```bash
# Upload and analyze a CSV file
curl -X POST "https://localhost:7001/api/statements/analyze" \
  -H "Authorization: Bearer $TOKEN" \
  -F "file=@chase_statement.csv"
```

Expected response:
```json
{
  "analysisId": "550e8400-e29b-41d4-a716-446655440000",
  "fileName": "chase_statement.csv",
  "rowCount": 45,
  "headers": ["Transaction Date", "Post Date", "Description", "Amount"],
  "sampleRows": [["01/15/2025", "01/16/2025", "AMAZON MARKETPLACE", "-45.99"]],
  "mappingOptions": [
    {
      "source": "system_fingerprint",
      "tier": 1,
      "fingerprintId": "a0000000-0000-0000-0000-000000000001",
      "sourceName": "Chase Business Card",
      "columnMapping": {
        "Transaction Date": "date",
        "Post Date": "post_date",
        "Description": "description",
        "Amount": "amount"
      },
      "dateFormat": "MM/dd/yyyy",
      "amountSign": "negative_charges"
    }
  ]
}
```

### Import with Mapping

```bash
curl -X POST "https://localhost:7001/api/statements/import" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "analysisId": "550e8400-e29b-41d4-a716-446655440000",
    "columnMapping": {
      "Transaction Date": "date",
      "Post Date": "post_date",
      "Description": "description",
      "Amount": "amount"
    },
    "dateFormat": "MM/dd/yyyy",
    "amountSign": "negative_charges",
    "saveAsFingerprint": false
  }'
```

Expected response:
```json
{
  "importId": "660e8400-e29b-41d4-a716-446655440000",
  "tierUsed": 1,
  "imported": 43,
  "skipped": 1,
  "duplicates": 1,
  "fingerprintSaved": false,
  "transactions": [
    {
      "id": "770e8400-e29b-41d4-a716-446655440000",
      "transactionDate": "2025-01-15",
      "description": "AMAZON MARKETPLACE",
      "amount": 45.99,
      "hasMatchedReceipt": false
    }
  ]
}
```

## Frontend Setup

### 1. Install Dependencies

```bash
cd frontend
npm install
```

### 2. Create Statement Import Page

Navigate to `/statements/import` to access the import wizard.

### 3. Test Flow

1. Click "Upload Statement"
2. Select a CSV or Excel file
3. Review detected column mapping
4. Confirm or adjust mappings
5. Click "Import"
6. View import summary

## Verification Checklist

- [ ] CSV file uploads successfully
- [ ] Excel file uploads successfully
- [ ] Chase format auto-detected (Tier 1)
- [ ] Unknown format triggers AI inference (Tier 3)
- [ ] Column mapping UI allows corrections
- [ ] Duplicate transactions are skipped
- [ ] Missing field rows are skipped
- [ ] Import summary shows correct counts
- [ ] Tier usage is logged
- [ ] User fingerprint is saved on confirm
- [ ] Saved fingerprint works on re-import

## Troubleshooting

### "AI service unavailable" error

1. Check Azure OpenAI endpoint configuration
2. Verify API key is in Key Vault
3. Check network connectivity from AKS pod

### "Invalid date format" on import

1. Review detected date format in analyze response
2. Adjust dateFormat in import request
3. Check sample rows for actual date format

### Duplicate hash collisions

If legitimate different transactions are marked as duplicates:
1. Check if same date + amount + description truly occurs
2. Consider including additional fields in hash (future enhancement)

## Sample Test Files

### chase_sample.csv

```csv
Transaction Date,Post Date,Description,Category,Type,Amount,Memo
01/15/2025,01/16/2025,AMAZON MARKETPLACE,Shopping,Sale,-45.99,
01/16/2025,01/17/2025,STARBUCKS COFFEE,Food & Drink,Sale,-6.50,Coffee
01/18/2025,01/19/2025,PAYMENT THANK YOU,Payment,Payment,500.00,
```

### amex_sample.csv

```csv
Date,Description,Amount
01/15/2025,DELTA AIRLINES,425.00
01/16/2025,UBER TRIP,32.50
01/17/2025,PAYMENT RECEIVED,-500.00
```

### unknown_bank.csv

```csv
Txn_Date,Posting_Date,Merchant,Debit,Credit,Notes
2025-01-15,2025-01-16,Amazon,45.99,,Online purchase
2025-01-17,2025-01-18,Payroll,,,Salary deposit
```
