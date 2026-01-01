# Quickstart: Running Tests Locally

**Branch**: `020-testing-suite` | **Date**: 2025-12-31
**Phase**: 1 - Design

## Overview

This guide ensures developers can run the same tests locally with identical results to CI (per SC-006: 95% identical results).

---

## Prerequisites

### Required Software

| Tool | Version | Installation |
|------|---------|--------------|
| .NET SDK | 8.0.x | `winget install Microsoft.DotNet.SDK.8` |
| Node.js | 20.x | `winget install OpenJS.NodeJS.LTS` |
| Docker Desktop | Latest | [docker.com/products/docker-desktop](https://www.docker.com/products/docker-desktop) |
| PowerShell | 7.x | `winget install Microsoft.PowerShell` |

### Verify Installation

```powershell
# Verify .NET
dotnet --version
# Expected: 8.0.x

# Verify Node.js
node --version
# Expected: v20.x.x

# Verify Docker
docker --version
docker compose version
# Docker should be running
```

---

## Quick Start

### Option 1: Run All Tests (Recommended)

```powershell
# From repository root
./scripts/test-all.ps1
```

This script:
1. Starts required Docker containers (PostgreSQL, WireMock)
2. Restores dependencies
3. Runs all backend tests
4. Runs all frontend tests
5. Generates coverage report
6. Cleans up containers

### Option 2: Run Specific Test Suites

```powershell
# Backend unit tests only (fastest)
dotnet test backend/tests/ExpenseFlow.Core.Tests

# Frontend unit tests only
cd frontend && npm run test:run

# E2E tests with Playwright
cd frontend && npm run test:e2e
```

---

## Detailed Setup

### 1. Clone and Restore

```powershell
# Clone repository
git clone https://github.com/your-org/expenseflow.git
cd expenseflow

# Restore backend
dotnet restore backend/ExpenseFlow.sln --locked-mode

# Restore frontend
cd frontend
npm ci
cd ..
```

### 2. Start Test Infrastructure

```powershell
# Start PostgreSQL and WireMock containers
docker compose -f docker-compose.test.yml up -d

# Wait for services to be healthy
docker compose -f docker-compose.test.yml ps

# Verify PostgreSQL is ready
docker exec test-postgres pg_isready -U test
```

**docker-compose.test.yml** (create if doesn't exist):

```yaml
version: '3.8'

services:
  postgres:
    container_name: test-postgres
    image: postgres:15-alpine
    environment:
      POSTGRES_DB: expenseflow_test
      POSTGRES_USER: test
      POSTGRES_PASSWORD: test
    ports:
      - "5432:5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U test"]
      interval: 5s
      timeout: 5s
      retries: 5

  wiremock:
    container_name: test-wiremock
    image: wiremock/wiremock:3.3.1
    ports:
      - "8080:8080"
    volumes:
      - ./backend/tests/ExpenseFlow.Scenarios.Tests/Mocks:/home/wiremock/mappings
```

### 3. Run Backend Tests

```powershell
# Set test configuration
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Database=expenseflow_test;Username=test;Password=test"
$env:Services__DocumentIntelligence__Endpoint = "http://localhost:8080"
$env:Services__OpenAI__Endpoint = "http://localhost:8080"

# Run all tests with coverage
dotnet test backend/ExpenseFlow.sln `
  --collect:"XPlat Code Coverage" `
  --results-directory ./coverage

# Run specific test categories
dotnet test backend/ExpenseFlow.sln --filter "Category=Unit"
dotnet test backend/ExpenseFlow.sln --filter "Category=Integration"
dotnet test backend/ExpenseFlow.sln --filter "Category=Contract"
dotnet test backend/ExpenseFlow.sln --filter "Category=Property"
```

### 4. Run Frontend Tests

```powershell
cd frontend

# Unit tests with coverage
npm run test:coverage

# Run specific test file
npm run test -- src/components/receipts/receipt-card.test.tsx

# E2E tests (requires backend running)
npm run test:e2e

# E2E with UI (debugging)
npm run test:e2e -- --ui
```

### 5. Cleanup

```powershell
# Stop and remove containers
docker compose -f docker-compose.test.yml down -v

# Remove test artifacts
Remove-Item -Recurse -Force ./coverage
Remove-Item -Recurse -Force ./test-results
```

---

## Test Categories

### Backend Test Projects

| Project | Category | Command |
|---------|----------|---------|
| `ExpenseFlow.Core.Tests` | Unit | `dotnet test backend/tests/ExpenseFlow.Core.Tests` |
| `ExpenseFlow.Api.Tests` | Unit | `dotnet test backend/tests/ExpenseFlow.Api.Tests` |
| `ExpenseFlow.Infrastructure.Tests` | Integration | `dotnet test backend/tests/ExpenseFlow.Infrastructure.Tests` |
| `ExpenseFlow.Contracts.Tests` | Contract | `dotnet test backend/tests/ExpenseFlow.Contracts.Tests` |
| `ExpenseFlow.PropertyTests` | Property | `dotnet test backend/tests/ExpenseFlow.PropertyTests` |
| `ExpenseFlow.Scenarios.Tests` | Scenario | `dotnet test backend/tests/ExpenseFlow.Scenarios.Tests` |
| `ExpenseFlow.LoadTests` | Load | `dotnet test backend/tests/ExpenseFlow.LoadTests` |

### Frontend Test Files

| Test Type | Location | Command |
|-----------|----------|---------|
| Unit | `frontend/src/**/*.test.tsx` | `npm run test` |
| Integration | `frontend/tests/integration/` | `npm run test -- tests/integration` |
| E2E | `frontend/tests/e2e/` | `npm run test:e2e` |

---

## Running Chaos Tests Locally

Chaos tests require special configuration to inject failures:

```powershell
# Enable chaos mode
$env:CHAOS_ENABLED = "true"
$env:CHAOS_INJECTION_RATE = "0.25"

# Run chaos tests
dotnet test backend/tests/ExpenseFlow.Scenarios.Tests `
  --filter "Category=Chaos"

# Disable chaos mode
$env:CHAOS_ENABLED = "false"
```

**Warning**: Do not run chaos tests against production databases!

---

## Property-Based Testing

FsCheck property tests run with default settings locally. For exhaustive testing:

```powershell
# Extended property test run
$env:FSCHECK_MAX_TEST = "1000"
$env:FSCHECK_END_SIZE = "200"

dotnet test backend/tests/ExpenseFlow.PropertyTests

# Reset to defaults
Remove-Item Env:\FSCHECK_MAX_TEST
Remove-Item Env:\FSCHECK_END_SIZE
```

---

## Coverage Reports

### Generate HTML Coverage Report

```powershell
# Install ReportGenerator globally
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate report
reportgenerator `
  -reports:"./coverage/**/coverage.cobertura.xml" `
  -targetdir:"./coverage/html" `
  -reporttypes:"Html;Cobertura"

# Open report
Start-Process "./coverage/html/index.html"
```

### View Coverage Threshold

```powershell
# Check if coverage meets 80% threshold
$xml = [xml](Get-Content ./coverage/**/coverage.cobertura.xml)
$lineRate = $xml.coverage.'line-rate'
$percentage = [math]::Round([double]$lineRate * 100, 2)

if ($percentage -ge 80) {
    Write-Host "✅ Coverage: $percentage% (threshold: 80%)" -ForegroundColor Green
} else {
    Write-Host "❌ Coverage: $percentage% (below 80% threshold)" -ForegroundColor Red
}
```

---

## Troubleshooting

### Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| "Connection refused" to PostgreSQL | Container not running | `docker compose -f docker-compose.test.yml up -d` |
| "Port 5432 already in use" | Local PostgreSQL running | Stop local PostgreSQL or use different port |
| Tests pass locally but fail in CI | Environment differences | Ensure Docker is used, not local DB |
| Playwright tests fail | Missing browsers | `npx playwright install chromium` |
| Testcontainers fail | Docker not running | Start Docker Desktop |

### Reset Test Environment

```powershell
# Nuclear option: remove all test containers and volumes
docker compose -f docker-compose.test.yml down -v --remove-orphans
docker system prune -f

# Rebuild and restart
docker compose -f docker-compose.test.yml up -d --build
```

### Debugging Failing Tests

```powershell
# Run with verbose output
dotnet test backend/ExpenseFlow.sln --verbosity detailed

# Run single test with output
dotnet test --filter "FullyQualifiedName~YourTestName" --logger "console;verbosity=detailed"

# Attach debugger in VS Code
# Add breakpoint, then run:
dotnet test --filter "FullyQualifiedName~YourTestName" --no-build
# Select "Attach to Process" in VS Code
```

---

## CI Parity Checklist

Before pushing, verify your local setup matches CI:

- [ ] Docker Desktop running
- [ ] PostgreSQL container healthy
- [ ] WireMock container with stubs loaded
- [ ] `packages.lock.json` committed (NuGet lock files)
- [ ] `package-lock.json` committed (npm lock file)
- [ ] All tests pass: `./scripts/test-all.ps1`
- [ ] Coverage ≥ 80%

---

## Scripts Reference

### `scripts/test-all.ps1`

```powershell
#!/usr/bin/env pwsh
$ErrorActionPreference = 'Stop'

Write-Host "🧪 Starting test environment..." -ForegroundColor Cyan
docker compose -f docker-compose.test.yml up -d

Write-Host "⏳ Waiting for services..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

try {
    Write-Host "🔧 Restoring dependencies..." -ForegroundColor Cyan
    dotnet restore backend/ExpenseFlow.sln --locked-mode

    Write-Host "🏗️ Building..." -ForegroundColor Cyan
    dotnet build backend/ExpenseFlow.sln -c Release --no-restore

    Write-Host "🧪 Running backend tests..." -ForegroundColor Cyan
    $env:ConnectionStrings__DefaultConnection = "Host=localhost;Database=expenseflow_test;Username=test;Password=test"
    dotnet test backend/ExpenseFlow.sln `
      --no-build -c Release `
      --filter "Category!=Load" `
      --collect:"XPlat Code Coverage" `
      --results-directory ./coverage

    Write-Host "🧪 Running frontend tests..." -ForegroundColor Cyan
    Push-Location frontend
    npm ci
    npm run test:coverage
    Pop-Location

    Write-Host "✅ All tests passed!" -ForegroundColor Green
}
finally {
    Write-Host "🧹 Cleaning up..." -ForegroundColor Yellow
    docker compose -f docker-compose.test.yml down -v
}
```

### `scripts/test-quick.ps1`

```powershell
#!/usr/bin/env pwsh
# Quick feedback - unit + contract tests only (matches ci-quick.yml)
$ErrorActionPreference = 'Stop'

Write-Host "⚡ Quick test run (unit + contract)..." -ForegroundColor Cyan

dotnet test backend/tests/ExpenseFlow.Core.Tests --no-build -c Release
dotnet test backend/tests/ExpenseFlow.Api.Tests --no-build -c Release
dotnet test backend/tests/ExpenseFlow.Contracts.Tests --no-build -c Release

Write-Host "✅ Quick tests passed!" -ForegroundColor Green
```
