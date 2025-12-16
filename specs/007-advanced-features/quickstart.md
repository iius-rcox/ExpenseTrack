# Quickstart: Advanced Features

**Feature**: 007-advanced-features
**Date**: 2025-12-16

## Overview

This sprint adds three advanced expense management capabilities:
1. **Travel Period Detection** - Automatic trip detection from flight/hotel receipts
2. **Subscription Detection** - Pattern recognition for recurring charges
3. **Expense Splitting** - Split expenses across multiple GL codes/departments

## Prerequisites

- ExpenseFlow backend running (Sprint 2 complete)
- Receipt pipeline functional (Sprint 3 complete)
- Statement import working (Sprint 4 complete)
- Vendor alias system in place (Sprint 5 complete)
- AI categorization system deployed (Sprint 6 complete)

## Quick Verification

### 1. Travel Period Detection

```bash
# Upload a flight receipt
curl -X POST http://localhost:5000/api/receipts/upload \
  -H "Authorization: Bearer $TOKEN" \
  -F "file=@delta_flight_receipt.pdf"

# Check if travel period was created
curl http://localhost:5000/api/travel-periods \
  -H "Authorization: Bearer $TOKEN"

# Response should include:
# {
#   "items": [{
#     "startDate": "2025-01-10",
#     "endDate": "2025-01-10",
#     "source": "Flight",
#     "destination": "ATL"
#   }]
# }
```

### 2. Subscription Detection

```bash
# Trigger subscription detection
curl -X POST http://localhost:5000/api/subscriptions/detect \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"lookbackMonths": 3}'

# List detected subscriptions
curl http://localhost:5000/api/subscriptions \
  -H "Authorization: Bearer $TOKEN"

# Check for alerts (missing subscriptions)
curl http://localhost:5000/api/subscriptions/alerts \
  -H "Authorization: Bearer $TOKEN"
```

### 3. Expense Splitting

```bash
# Get split suggestion for a transaction
curl http://localhost:5000/api/expenses/{transactionId}/split \
  -H "Authorization: Bearer $TOKEN"

# Apply a split
curl -X POST http://localhost:5000/api/expenses/{transactionId}/split \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "allocations": [
      {"glCode": "64100", "department": "07", "percentage": 60},
      {"glCode": "65100", "department": "12", "percentage": 40}
    ],
    "saveAsPattern": true,
    "vendorAliasId": "..."
  }'
```

## Database Migrations

Run migrations to create new tables:

```bash
cd backend/src/ExpenseFlow.Api
dotnet ef database update
```

New tables created:
- `travel_periods`
- `detected_subscriptions`
- `known_subscription_vendors` (with seed data)

Modified tables:
- `vendor_aliases` (added Category column)
- `split_patterns` (added UserId column)

## Configuration

Add to `appsettings.json`:

```json
{
  "AdvancedFeatures": {
    "TravelDetection": {
      "DefaultTravelGLCode": "66300",
      "EnableAIFallback": true
    },
    "SubscriptionDetection": {
      "AmountVarianceTolerance": 0.20,
      "MinConsecutiveMonths": 2,
      "AlertCheckCron": "0 4 1 * *"
    },
    "ExpenseSplitting": {
      "MaxAllocationsPerSplit": 10
    }
  }
}
```

## Hangfire Jobs

New recurring job registered:
- `subscription-alert-check` - Runs 1st of each month at 4 AM to detect missing subscriptions

View in Hangfire dashboard: `/hangfire`

## Testing

### Unit Tests

```bash
cd backend/tests/ExpenseFlow.Tests.Unit
dotnet test --filter "Category=TravelDetection|Category=Subscription|Category=Splitting"
```

### Integration Tests

```bash
cd backend/tests/ExpenseFlow.Tests.Integration
dotnet test --filter "Category=AdvancedFeatures"
```

## Key API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/travel-periods` | GET | List user's travel periods |
| `/api/travel-periods` | POST | Manually create travel period |
| `/api/travel-periods/{id}` | PUT | Update travel period |
| `/api/travel-periods/{id}` | DELETE | Delete travel period |
| `/api/travel-periods/{id}/expenses` | GET | Get expenses within period |
| `/api/travel-periods/detect` | POST | Trigger detection for receipt |
| `/api/subscriptions` | GET | List detected subscriptions |
| `/api/subscriptions/alerts` | GET | Get missing subscription alerts |
| `/api/subscriptions/detect` | POST | Trigger subscription detection |
| `/api/expenses/{id}/split` | GET | Get split suggestion |
| `/api/expenses/{id}/split` | POST | Apply split to expense |
| `/api/split-patterns` | GET | List user's split patterns |
| `/api/split-patterns` | POST | Create split pattern |

## Troubleshooting

### Travel period not detected

1. Check vendor alias has `Category = Airline` or `Hotel`:
   ```sql
   SELECT * FROM vendor_aliases
   WHERE canonical_name LIKE '%DELTA%';
   ```

2. Verify receipt was processed successfully:
   ```sql
   SELECT status, vendor_extracted FROM receipts
   WHERE id = 'receipt-id';
   ```

### Subscription not detected

1. Verify at least 2 consecutive months of transactions:
   ```sql
   SELECT DATE_TRUNC('month', transaction_date) as month, COUNT(*)
   FROM transactions
   WHERE description LIKE '%OPENAI%'
   GROUP BY DATE_TRUNC('month', transaction_date)
   ORDER BY month;
   ```

2. Check known subscription vendors table:
   ```sql
   SELECT * FROM known_subscription_vendors
   WHERE vendor_pattern LIKE '%OPENAI%';
   ```

### Split validation failing

1. Ensure allocations sum to exactly 100%
2. Verify GL codes exist in reference data
3. Check minimum 2 allocations provided

## Success Criteria Validation

| Criteria | How to Verify |
|----------|--------------|
| SC-001: 90%+ travel detection | Check `travel_periods` vs flight/hotel receipts |
| SC-002: <30s review time | UI timing metrics |
| SC-003: 95%+ subscription accuracy | Compare detected vs actual recurring charges |
| SC-004: Month-end alerts | Check `subscription_alerts` table on 2nd of month |
| SC-005: 80%+ pattern application | Split pattern `usage_count` vs total splits |
| SC-006: 95% Tier 1 usage | Query `tier_usage_logs` for travel/subscription ops |
| SC-007: <60s split completion | UI timing metrics |
| SC-008: Performance at scale | Load test with 50+ travel periods, 20+ subscriptions |
