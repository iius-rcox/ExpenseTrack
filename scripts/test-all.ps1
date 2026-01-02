#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Runs the complete ExpenseFlow test suite locally.

.DESCRIPTION
    Executes all test categories with optional coverage collection.
    Mirrors the CI Full workflow for local validation.

.PARAMETER Coverage
    Enable code coverage collection (default: false)

.PARAMETER Category
    Filter tests by category (Unit, Contract, Property, Scenario, Integration)
    If not specified, runs all categories except Load, Chaos, and Resilience.

.PARAMETER Verbose
    Enable verbose output

.EXAMPLE
    ./scripts/test-all.ps1
    Run all tests without coverage

.EXAMPLE
    ./scripts/test-all.ps1 -Coverage
    Run all tests with coverage collection

.EXAMPLE
    ./scripts/test-all.ps1 -Category Unit
    Run only unit tests
#>

param(
    [switch]$Coverage,
    [ValidateSet("Unit", "Contract", "Property", "Scenario", "Integration", "All")]
    [string]$Category = "All",
    [switch]$VerboseOutput
)

$ErrorActionPreference = "Stop"
$startTime = Get-Date

Write-Host "=== ExpenseFlow Test Suite ===" -ForegroundColor Cyan
Write-Host "Started at: $startTime"
Write-Host ""

# Determine repository root
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $repoRoot) {
    $repoRoot = Get-Location
}

# Check Docker is running for integration tests
if ($Category -eq "All" -or $Category -eq "Scenario" -or $Category -eq "Integration") {
    $dockerRunning = docker info 2>$null
    if (-not $dockerRunning) {
        Write-Warning "Docker is not running. Integration and Scenario tests may fail."
        Write-Host "Start Docker and run: docker-compose -f docker-compose.test.yml up -d"
    }
}

# Build solution
Write-Host "Building solution..." -ForegroundColor Yellow
Push-Location "$repoRoot/backend"
try {
    dotnet build ExpenseFlow.sln --configuration Release --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }
} finally {
    Pop-Location
}

# Prepare test filter
$filter = switch ($Category) {
    "Unit"        { "Category=Unit" }
    "Contract"    { "Category=Contract" }
    "Property"    { "Category=Property" }
    "Scenario"    { "Category=Scenario" }
    "Integration" { "Category=Integration" }
    "All"         { "Category!=Load&Category!=Chaos&Category!=Resilience&Category!=Quarantined" }
}

# Prepare coverage arguments
$coverageArgs = @()
if ($Coverage) {
    $coverageArgs = @(
        '--collect:"XPlat Code Coverage"'
        '--results-directory', "$repoRoot/coverage"
    )
    Write-Host "Coverage collection enabled" -ForegroundColor Green
}

# Prepare verbosity
$verbosity = if ($VerboseOutput) { "normal" } else { "minimal" }

# Run backend tests
Write-Host ""
Write-Host "Running backend tests (filter: $filter)..." -ForegroundColor Yellow
Push-Location "$repoRoot/backend"
try {
    $testArgs = @(
        "test"
        "ExpenseFlow.sln"
        "--no-build"
        "--configuration", "Release"
        "--filter", $filter
        "--logger", "trx;LogFileName=test-results.trx"
        "--verbosity", $verbosity
    ) + $coverageArgs

    & dotnet @testArgs
    $backendExitCode = $LASTEXITCODE
} finally {
    Pop-Location
}

# Run frontend tests
Write-Host ""
Write-Host "Running frontend tests..." -ForegroundColor Yellow
Push-Location "$repoRoot/frontend"
try {
    if ($Coverage) {
        npm run test:coverage
    } else {
        npm run test
    }
    $frontendExitCode = $LASTEXITCODE
} finally {
    Pop-Location
}

# Calculate duration
$endTime = Get-Date
$duration = $endTime - $startTime

# Summary
Write-Host ""
Write-Host "=== Test Summary ===" -ForegroundColor Cyan
Write-Host "Duration: $($duration.ToString('mm\:ss'))"
Write-Host ""

if ($backendExitCode -eq 0) {
    Write-Host "Backend Tests: PASSED" -ForegroundColor Green
} else {
    Write-Host "Backend Tests: FAILED" -ForegroundColor Red
}

if ($frontendExitCode -eq 0) {
    Write-Host "Frontend Tests: PASSED" -ForegroundColor Green
} else {
    Write-Host "Frontend Tests: FAILED" -ForegroundColor Red
}

if ($Coverage) {
    Write-Host ""
    Write-Host "Coverage reports available at: $repoRoot/coverage" -ForegroundColor Cyan
}

# Exit with combined result
if ($backendExitCode -ne 0 -or $frontendExitCode -ne 0) {
    exit 1
}
exit 0
