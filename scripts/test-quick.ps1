#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Runs quick tests for fast local feedback.

.DESCRIPTION
    Executes only Unit and Contract tests to provide fast feedback.
    Mirrors the CI Quick workflow for local validation.
    Target: < 3 minutes execution time.

.PARAMETER Watch
    Enable watch mode for continuous testing

.EXAMPLE
    ./scripts/test-quick.ps1
    Run quick tests once

.EXAMPLE
    ./scripts/test-quick.ps1 -Watch
    Run tests in watch mode (requires dotnet-watch)
#>

param(
    [switch]$Watch
)

$ErrorActionPreference = "Stop"
$startTime = Get-Date

Write-Host "=== ExpenseFlow Quick Tests ===" -ForegroundColor Cyan
Write-Host "Target: < 3 minutes"
Write-Host "Started at: $startTime"
Write-Host ""

# Determine repository root
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $repoRoot) {
    $repoRoot = Get-Location
}

# Build solution
Write-Host "Building solution..." -ForegroundColor Yellow
Push-Location "$repoRoot/backend"
try {
    dotnet build ExpenseFlow.sln --configuration Release --nologo --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }
    Write-Host "Build completed" -ForegroundColor Green
} finally {
    Pop-Location
}

# Run Core unit tests
Write-Host ""
Write-Host "Running Core.Tests..." -ForegroundColor Yellow
$coreStart = Get-Date
Push-Location "$repoRoot/backend"
try {
    dotnet test tests/ExpenseFlow.Core.Tests `
        --no-build `
        --configuration Release `
        --verbosity minimal `
        --nologo
    $coreExitCode = $LASTEXITCODE
    $coreDuration = (Get-Date) - $coreStart
    Write-Host "Core.Tests completed in $($coreDuration.TotalSeconds.ToString('F1'))s"
} finally {
    Pop-Location
}

# Run Api unit tests
Write-Host ""
Write-Host "Running Api.Tests..." -ForegroundColor Yellow
$apiStart = Get-Date
Push-Location "$repoRoot/backend"
try {
    dotnet test tests/ExpenseFlow.Api.Tests `
        --no-build `
        --configuration Release `
        --verbosity minimal `
        --nologo
    $apiExitCode = $LASTEXITCODE
    $apiDuration = (Get-Date) - $apiStart
    Write-Host "Api.Tests completed in $($apiDuration.TotalSeconds.ToString('F1'))s"
} finally {
    Pop-Location
}

# Run Contract tests
Write-Host ""
Write-Host "Running Contracts.Tests..." -ForegroundColor Yellow
$contractStart = Get-Date
Push-Location "$repoRoot/backend"
try {
    dotnet test tests/ExpenseFlow.Contracts.Tests `
        --no-build `
        --configuration Release `
        --verbosity minimal `
        --nologo
    $contractExitCode = $LASTEXITCODE
    $contractDuration = (Get-Date) - $contractStart
    Write-Host "Contracts.Tests completed in $($contractDuration.TotalSeconds.ToString('F1'))s"
} finally {
    Pop-Location
}

# Calculate total duration
$endTime = Get-Date
$totalDuration = $endTime - $startTime

# Summary
Write-Host ""
Write-Host "=== Quick Test Summary ===" -ForegroundColor Cyan
Write-Host "Total Duration: $($totalDuration.ToString('mm\:ss'))"

if ($totalDuration.TotalMinutes -le 3) {
    Write-Host "✅ Within 3-minute target" -ForegroundColor Green
} else {
    Write-Host "⚠️ Exceeded 3-minute target" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Results:"
if ($coreExitCode -eq 0) {
    Write-Host "  Core.Tests:      PASSED" -ForegroundColor Green
} else {
    Write-Host "  Core.Tests:      FAILED" -ForegroundColor Red
}

if ($apiExitCode -eq 0) {
    Write-Host "  Api.Tests:       PASSED" -ForegroundColor Green
} else {
    Write-Host "  Api.Tests:       FAILED" -ForegroundColor Red
}

if ($contractExitCode -eq 0) {
    Write-Host "  Contracts.Tests: PASSED" -ForegroundColor Green
} else {
    Write-Host "  Contracts.Tests: FAILED" -ForegroundColor Red
}

# Exit with combined result
if ($coreExitCode -ne 0 -or $apiExitCode -ne 0 -or $contractExitCode -ne 0) {
    exit 1
}
exit 0
