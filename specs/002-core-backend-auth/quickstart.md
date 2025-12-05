# Quickstart: Core Backend & Authentication

**Feature**: 002-core-backend-auth
**Date**: 2025-12-04

## Prerequisites

- .NET 8 SDK installed
- Docker Desktop running (for local PostgreSQL)
- Azure CLI installed and logged in (`az login`)
- Access to Azure Entra ID tenant for app registration
- kubectl configured for dev-aks cluster

## Local Development Setup

### 1. Clone and Navigate

```bash
cd ExpenseTrack
git checkout 002-core-backend-auth
```

### 2. Create Solution Structure

```bash
# Create solution
mkdir -p backend/src backend/tests
cd backend
dotnet new sln -n ExpenseFlow

# Create projects
dotnet new webapi -n ExpenseFlow.Api -o src/ExpenseFlow.Api
dotnet new classlib -n ExpenseFlow.Core -o src/ExpenseFlow.Core
dotnet new classlib -n ExpenseFlow.Infrastructure -o src/ExpenseFlow.Infrastructure
dotnet new classlib -n ExpenseFlow.Shared -o src/ExpenseFlow.Shared

# Create test projects
dotnet new xunit -n ExpenseFlow.Api.Tests -o tests/ExpenseFlow.Api.Tests
dotnet new xunit -n ExpenseFlow.Core.Tests -o tests/ExpenseFlow.Core.Tests
dotnet new xunit -n ExpenseFlow.Infrastructure.Tests -o tests/ExpenseFlow.Infrastructure.Tests

# Add projects to solution
dotnet sln add src/ExpenseFlow.Api/ExpenseFlow.Api.csproj
dotnet sln add src/ExpenseFlow.Core/ExpenseFlow.Core.csproj
dotnet sln add src/ExpenseFlow.Infrastructure/ExpenseFlow.Infrastructure.csproj
dotnet sln add src/ExpenseFlow.Shared/ExpenseFlow.Shared.csproj
dotnet sln add tests/ExpenseFlow.Api.Tests/ExpenseFlow.Api.Tests.csproj
dotnet sln add tests/ExpenseFlow.Core.Tests/ExpenseFlow.Core.Tests.csproj
dotnet sln add tests/ExpenseFlow.Infrastructure.Tests/ExpenseFlow.Infrastructure.Tests.csproj

# Add project references
dotnet add src/ExpenseFlow.Api reference src/ExpenseFlow.Core
dotnet add src/ExpenseFlow.Api reference src/ExpenseFlow.Infrastructure
dotnet add src/ExpenseFlow.Api reference src/ExpenseFlow.Shared
dotnet add src/ExpenseFlow.Infrastructure reference src/ExpenseFlow.Core
dotnet add src/ExpenseFlow.Infrastructure reference src/ExpenseFlow.Shared
dotnet add src/ExpenseFlow.Core reference src/ExpenseFlow.Shared
```

### 3. Install NuGet Packages

```bash
# API project
cd src/ExpenseFlow.Api
dotnet add package Microsoft.Identity.Web
dotnet add package Hangfire.AspNetCore
dotnet add package Swashbuckle.AspNetCore

# Infrastructure project
cd ../ExpenseFlow.Infrastructure
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL.Pgvector
dotnet add package Hangfire.PostgreSql
dotnet add package Microsoft.Data.SqlClient
dotnet add package Azure.Identity
dotnet add package Polly

# Core project
cd ../ExpenseFlow.Core
dotnet add package Pgvector  # For Vector type

# Test projects
cd ../../tests/ExpenseFlow.Api.Tests
dotnet add package Moq
dotnet add package FluentAssertions
dotnet add package Testcontainers.PostgreSql
```

### 4. Local PostgreSQL with Docker

```bash
# Start PostgreSQL with pgvector
docker run -d \
  --name expenseflow-postgres \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=localdev123 \
  -e POSTGRES_DB=expenseflow \
  -p 5432:5432 \
  pgvector/pgvector:pg15

# Verify connection
docker exec -it expenseflow-postgres psql -U postgres -c "CREATE EXTENSION IF NOT EXISTS vector;"
```

### 5. Configure appsettings.Development.json

```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Port=5432;Database=expenseflow;Username=postgres;Password=localdev123"
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "{your-tenant-id}",
    "ClientId": "{your-client-id}",
    "Audience": "api://{your-client-id}"
  },
  "Hangfire": {
    "DashboardPath": "/hangfire"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### 6. Run Database Migrations

```bash
cd src/ExpenseFlow.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../ExpenseFlow.Api
dotnet ef database update --startup-project ../ExpenseFlow.Api
```

### 7. Run the Application

```bash
cd src/ExpenseFlow.Api
dotnet run

# API available at: https://localhost:5001
# Swagger UI: https://localhost:5001/swagger
# Hangfire Dashboard: https://localhost:5001/hangfire
```

## Azure Deployment

### 1. Register Application in Entra ID

```bash
# Create app registration
az ad app create \
  --display-name "ExpenseFlow API" \
  --sign-in-audience "AzureADMyOrg" \
  --web-redirect-uris "https://dev.expense.ii-us.com/signin-oidc"

# Note the appId (client ID) from output
# Add API scope
az ad app permission add \
  --id {app-id} \
  --api 00000003-0000-0000-c000-000000000000 \
  --api-permissions e1fe6dd8-ba31-4d61-89e7-88639da4683d=Scope
```

### 2. Build and Push Container

```bash
cd backend

# Build container
docker build -t iiusacr.azurecr.io/expenseflow-api:latest .

# Login to ACR
az acr login --name iiusacr

# Push
docker push iiusacr.azurecr.io/expenseflow-api:latest
```

### 3. Create Kubernetes Secrets

```bash
# Create namespace if not exists
kubectl create namespace expenseflow-dev --dry-run=client -o yaml | kubectl apply -f -

# Store secrets in Key Vault and reference via CSI driver
# (Secrets already configured in Sprint 1)
```

### 4. Deploy to AKS

```bash
# Apply Kubernetes manifests
kubectl apply -f infrastructure/kubernetes/deployment.yaml -n expenseflow-dev
kubectl apply -f infrastructure/kubernetes/service.yaml -n expenseflow-dev
kubectl apply -f infrastructure/kubernetes/ingress.yaml -n expenseflow-dev

# Verify deployment
kubectl get pods -n expenseflow-dev -l app=expenseflow-api
kubectl logs -f deployment/expenseflow-api -n expenseflow-dev
```

### 5. Verify Deployment

```bash
# Health check
curl https://dev.expense.ii-us.com/api/health

# Get token and test authenticated endpoint
TOKEN=$(az account get-access-token --resource api://{client-id} --query accessToken -o tsv)
curl -H "Authorization: Bearer $TOKEN" https://dev.expense.ii-us.com/api/users/me
```

## Common Tasks

### Run Tests

```bash
cd backend
dotnet test --logger "console;verbosity=detailed"
```

### Add New Migration

```bash
cd src/ExpenseFlow.Infrastructure
dotnet ef migrations add {MigrationName} --startup-project ../ExpenseFlow.Api
```

### Trigger Reference Data Sync

```bash
# Via API (requires admin role)
TOKEN=$(az account get-access-token --resource api://{client-id} --query accessToken -o tsv)
curl -X POST https://dev.expense.ii-us.com/api/reference/sync \
  -H "Authorization: Bearer $TOKEN"

# Via Hangfire Dashboard
# Navigate to /hangfire → Recurring Jobs → Trigger "SyncReferenceData"
```

### View Cache Statistics

```bash
TOKEN=$(az account get-access-token --resource api://{client-id} --query accessToken -o tsv)
curl https://dev.expense.ii-us.com/api/cache/stats \
  -H "Authorization: Bearer $TOKEN" | jq
```

## Troubleshooting

### Authentication Errors

```bash
# Verify token audience
TOKEN=$(az account get-access-token --resource api://{client-id} --query accessToken -o tsv)
echo $TOKEN | cut -d. -f2 | base64 -d 2>/dev/null | jq .aud

# Should match: "api://{client-id}"
```

### Database Connection Issues

```bash
# Test connection from pod
kubectl exec -it deployment/expenseflow-api -n expenseflow-dev -- \
  /bin/sh -c "nc -zv supabase-supabase-db 5432"
```

### Hangfire Not Processing Jobs

```bash
# Check Hangfire server status in logs
kubectl logs deployment/expenseflow-api -n expenseflow-dev | grep -i hangfire

# Verify PostgreSQL Hangfire schema exists
kubectl exec -it supabase-supabase-db-xxx -n expenseflow-dev -- \
  psql -U postgres -c "\dn" | grep hangfire
```
