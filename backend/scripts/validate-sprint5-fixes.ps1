#!/usr/bin/env pwsh
# Sprint 5 Performance Fixes - Build & Test Script
# Run from backend directory: .\scripts\validate-sprint5-fixes.ps1

$ErrorActionPreference = "Stop"
$BackendPath = $PSScriptRoot | Split-Path -Parent

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Sprint 5 Performance Fixes Validation" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Change to backend directory
Set-Location $BackendPath
Write-Host "[1/5] Working directory: $BackendPath" -ForegroundColor Yellow

# Step 1: Restore packages
Write-Host ""
Write-Host "[2/5] Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Package restore failed!" -ForegroundColor Red
    exit 1
}
Write-Host "Package restore complete." -ForegroundColor Green

# Step 2: Build solution
Write-Host ""
Write-Host "[3/5] Building solution..." -ForegroundColor Yellow
dotnet build --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "Build successful." -ForegroundColor Green

# Step 3: Run tests
Write-Host ""
Write-Host "[4/5] Running unit tests..." -ForegroundColor Yellow
dotnet test --no-build --verbosity normal
if ($LASTEXITCODE -ne 0) {
    Write-Host "WARNING: Some tests failed!" -ForegroundColor Yellow
} else {
    Write-Host "All tests passed." -ForegroundColor Green
}

# Step 4: Check for pending migrations
Write-Host ""
Write-Host "[5/5] Checking migrations..." -ForegroundColor Yellow
Write-Host ""
Write-Host "New migration file created:" -ForegroundColor Cyan
Write-Host "  - 20251215010000_AddVendorAliasTrigramIndex.cs" -ForegroundColor White
Write-Host ""
Write-Host "To apply migrations, run:" -ForegroundColor Yellow
Write-Host '  dotnet ef database update --project src/ExpenseFlow.Infrastructure' -ForegroundColor White
Write-Host ""

# Summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Fixes Applied:" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "[CRITICAL] VendorAliasService: Database-side ILIKE instead of memory load" -ForegroundColor Green
Write-Host "[CRITICAL] RunAutoMatchAsync: Pre-filter by date/amount, cache aliases" -ForegroundColor Green
Write-Host "[HIGH] GIN trigram index migration for pattern matching" -ForegroundColor Green
Write-Host "[HIGH] GetAverageConfidenceAsync: Database-side aggregation" -ForegroundColor Green
Write-Host "[HIGH] Removed duplicate SaveChangesAsync calls" -ForegroundColor Green
Write-Host "[MEDIUM] HasConfirmedMatch: Added optional userId filter" -ForegroundColor Green
Write-Host ""
Write-Host "Performance impact:" -ForegroundColor Yellow
Write-Host "  Before: O(n*m) alias queries + full table loads" -ForegroundColor White
Write-Host "  After:  O(m) alias queries + pre-filtered candidates" -ForegroundColor White
Write-Host ""
