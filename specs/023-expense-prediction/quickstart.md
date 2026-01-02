# Quickstart: Expense Prediction Feature

**Feature**: 023-expense-prediction
**Date**: 2026-01-02

## Prerequisites

- .NET 8 SDK installed
- Node.js 20+ installed
- PostgreSQL 15+ running (or Supabase)
- Access to ExpenseFlow development environment

## Quick Setup

### 1. Database Migration

```bash
# Navigate to backend directory
cd backend/src/ExpenseFlow.Api

# Create migration for new entities
dotnet ef migrations add AddExpensePrediction \
  --project ../ExpenseFlow.Infrastructure \
  --startup-project .

# Apply migration
dotnet ef database update \
  --project ../ExpenseFlow.Infrastructure \
  --startup-project .
```

### 2. Run Backend

```bash
cd backend/src/ExpenseFlow.Api
dotnet run
```

### 3. Run Frontend

```bash
cd frontend
npm install
npm run dev
```

### 4. Verify Setup

```bash
# Check predictions endpoint is available
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:5000/api/predictions/availability
```

Expected response:
```json
{
  "isAvailable": false,
  "patternCount": 0,
  "message": "Submit at least one expense report to enable predictions"
}
```

## Testing the Feature

### Generate Test Data

1. Import a bank statement with transactions
2. Create an expense report draft for a period
3. Add some transactions to the report
4. Submit the report (status = Submitted)

### Verify Pattern Extraction

```bash
# Check patterns were created
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:5000/api/predictions/patterns
```

### Verify Predictions

```bash
# Import new transactions, then check predictions
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:5000/api/predictions
```

## Key Files

| File | Purpose |
|------|---------|
| `ExpensePattern.cs` | Learned pattern entity |
| `TransactionPrediction.cs` | Prediction entity |
| `ExpensePredictionService.cs` | Core prediction logic |
| `PredictionsController.cs` | API endpoints |
| `use-predictions.ts` | Frontend hooks |
| `expense-badge.tsx` | UI badge component |

## Environment Variables

No new environment variables required. Feature uses existing database connection.

## Debugging Tips

### Check Pattern Count
```sql
SELECT user_id, COUNT(*) as pattern_count
FROM expense_patterns
GROUP BY user_id;
```

### Check Prediction Accuracy
```sql
SELECT
  status,
  COUNT(*) as count,
  ROUND(COUNT(*) * 100.0 / SUM(COUNT(*)) OVER(), 2) as percentage
FROM transaction_predictions
WHERE user_id = 'your-user-id'
GROUP BY status;
```

### Rebuild Patterns from Approved Reports
```bash
# Rebuild all patterns from approved expense reports
curl -X POST -H "Authorization: Bearer $TOKEN" \
  http://localhost:5000/api/predictions/patterns/rebuild
```

## Common Issues

### No predictions appearing
1. Ensure at least one expense report is submitted (not just Draft)
2. Check that patterns were created in `expense_patterns` table
3. Verify transactions have normalized vendor names

### Low confidence scores
1. More expense report history improves confidence
2. Check `confirm_count` vs `reject_count` for patterns
3. Verify amount variance isn't too high

### Performance issues
1. Check index on `expense_patterns(user_id)`
2. Verify `transaction_predictions(user_id, status)` index exists
3. Monitor batch size for pattern matching

## Next Steps

After setup, run the test suite:

```bash
# Backend tests
cd backend
dotnet test --filter "Category=Unit"

# Frontend tests
cd frontend
npm run test
```
