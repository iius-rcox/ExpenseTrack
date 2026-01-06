# Quickstart: Missing Receipts UI

**Feature**: 026-missing-receipts-ui
**Date**: 2026-01-05

## Overview

This feature adds a Missing Receipts UI to help users identify and manage reimbursable transactions that lack matched receipts.

## Prerequisites

- .NET 8 SDK
- Node.js 20+
- PostgreSQL 15+ (Supabase)
- Azure CLI (for deployment)

## Quick Development Setup

### 1. Database Migration

Apply the schema changes to add new fields to the Transaction table:

```bash
# Generate migration
cd backend
dotnet ef migrations add AddMissingReceiptFieldsToTransaction \
  --project src/ExpenseFlow.Infrastructure \
  --startup-project src/ExpenseFlow.Api

# Apply migration (development)
dotnet ef database update \
  --project src/ExpenseFlow.Infrastructure \
  --startup-project src/ExpenseFlow.Api
```

Or apply manually in staging:

```sql
-- Connect to staging database
kubectl exec -it $(kubectl get pods -n expenseflow-dev \
  -l app.kubernetes.io/name=supabase-db -o jsonpath='{.items[0].metadata.name}') \
  -n expenseflow-dev -- psql -U postgres -d expenseflow_staging

-- Run migration
ALTER TABLE "Transactions"
ADD COLUMN "ReceiptUrl" text NULL,
ADD COLUMN "ReceiptDismissed" boolean NULL;

-- Add index for query performance
CREATE INDEX "IX_Transactions_UserId_MissingReceipt"
ON "Transactions" ("UserId", "MatchedReceiptId", "ReceiptDismissed")
WHERE "MatchedReceiptId" IS NULL AND ("ReceiptDismissed" IS NULL OR "ReceiptDismissed" = false);

-- Record migration
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260105000000_AddMissingReceiptFieldsToTransaction', '8.0.0');

\q
```

### 2. Backend Development

```bash
cd backend

# Restore packages
dotnet restore

# Run tests
dotnet test

# Run API locally
cd src/ExpenseFlow.Api
dotnet run
```

API will be available at `https://localhost:7001/api/`

### 3. Frontend Development

```bash
cd frontend

# Install dependencies
npm install

# Run development server
npm run dev
```

Frontend will be available at `http://localhost:5173/`

## Key Files to Implement

### Backend

| File | Purpose |
|------|---------|
| `ExpenseFlow.Core/Entities/Transaction.cs` | Add ReceiptUrl, ReceiptDismissed fields |
| `ExpenseFlow.Shared/DTOs/MissingReceiptDtos.cs` | DTOs for API responses |
| `ExpenseFlow.Infrastructure/Services/MissingReceiptService.cs` | Query logic |
| `ExpenseFlow.Api/Controllers/MissingReceiptsController.cs` | REST endpoints |

### Frontend

| File | Purpose |
|------|---------|
| `src/hooks/queries/use-missing-receipts.ts` | TanStack Query hooks |
| `src/components/missing-receipts/missing-receipts-widget.tsx` | Dashboard widget |
| `src/components/missing-receipts/missing-receipt-card.tsx` | List item component |
| `src/routes/_authenticated/missing-receipts/index.tsx` | Full list page |

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/missing-receipts` | List with pagination & sorting |
| GET | `/api/missing-receipts/widget` | Widget summary (count + top 3) |
| PATCH | `/api/missing-receipts/{id}/url` | Update receipt URL |
| PATCH | `/api/missing-receipts/{id}/dismiss` | Dismiss/restore |

## Testing

### Backend Tests

```bash
cd backend

# Unit tests
dotnet test --filter "Category=Unit"

# Integration tests
dotnet test --filter "Category=Integration"

# All tests
dotnet test
```

### Frontend Tests

```bash
cd frontend

# Unit tests
npm run test

# E2E tests
npm run test:e2e
```

## Deployment

### Build Docker Images

```bash
# Backend API (ALWAYS use --platform linux/amd64 for AKS)
cd backend
docker buildx build --platform linux/amd64 \
  -t iiusacr.azurecr.io/expenseflow-api:v1.6.0-026 \
  --push .

# Frontend
cd frontend
docker buildx build --platform linux/amd64 \
  -t iiusacr.azurecr.io/expenseflow-frontend:v1.6.0-026 \
  --push .
```

### Deploy to Staging

1. Apply database migration (see step 1 above)
2. Update image tags in `infrastructure/kubernetes/staging/`
3. Push to main branch (ArgoCD auto-syncs)

```bash
git add .
git commit -m "feat(026): Deploy missing receipts UI"
git push origin main
```

## Verification

### API Health Check

```bash
curl https://staging-api.expense.ii-us.com/api/missing-receipts/widget \
  -H "Authorization: Bearer $TOKEN"
```

### UI Verification

1. Navigate to https://staging.expense.ii-us.com/matching
2. Verify Missing Receipts widget appears (if you have reimbursable transactions)
3. Click "View All" to access full list page
4. Test: Add URL, Upload receipt, Dismiss/Restore

## Troubleshooting

### Common Issues

**Widget not showing**: Check if you have any confirmed reimbursable predictions without matched receipts.

**Empty list**: Ensure:
1. You have transactions marked as reimbursable
2. Those transactions don't have matched receipts
3. They're not dismissed

**Database column missing**: Run migration (see step 1).

**API 401 errors**: Ensure valid JWT token from Azure AD.
