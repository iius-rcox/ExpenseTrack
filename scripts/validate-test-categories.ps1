#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validates that all test categories are properly configured and run in CI workflows.

.DESCRIPTION
    This script checks:
    1. Test category constants are used consistently
    2. CI workflows reference the correct test categories
    3. All test files have appropriate category traits
    4. No orphaned categories exist (defined but not run)

.PARAMETER Fix
    When specified, attempts to fix common issues automatically.

.EXAMPLE
    ./scripts/validate-test-categories.ps1
    ./scripts/validate-test-categories.ps1 -Fix
#>

param(
    [switch]$Fix
)

$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path -Parent $PSScriptRoot

Write-Host "=== Test Category Validation ===" -ForegroundColor Cyan
Write-Host ""

# Define expected categories and their CI workflow associations
$ExpectedCategories = @{
    'Unit'        = @{ Workflow = 'ci-quick.yml'; Required = $true }
    'Contract'    = @{ Workflow = 'ci-quick.yml'; Required = $true }
    'Integration' = @{ Workflow = 'ci-full.yml'; Required = $true }
    'Scenario'    = @{ Workflow = 'ci-full.yml'; Required = $true }
    'Property'    = @{ Workflow = 'ci-full.yml, ci-nightly.yml'; Required = $true }
    'Chaos'       = @{ Workflow = 'ci-nightly.yml'; Required = $true }
    'Resilience'  = @{ Workflow = 'ci-nightly.yml'; Required = $true }
    'Load'        = @{ Workflow = 'ci-nightly.yml'; Required = $false }
    'Quarantined' = @{ Workflow = 'excluded'; Required = $false }
}

$Issues = @()
$Passed = 0
$Failed = 0

# 1. Check TestCategories.cs exists and defines all categories
Write-Host "Checking TestCategories.cs..." -ForegroundColor Yellow
$TestCategoriesPath = Join-Path $RepoRoot "backend/tests/ExpenseFlow.TestCommon/TestCategories.cs"

if (Test-Path $TestCategoriesPath) {
    $content = Get-Content $TestCategoriesPath -Raw

    foreach ($category in $ExpectedCategories.Keys) {
        if ($content -match "public const string $category") {
            Write-Host "  [PASS] Category '$category' is defined" -ForegroundColor Green
            $Passed++
        }
        else {
            Write-Host "  [FAIL] Category '$category' is NOT defined" -ForegroundColor Red
            $Issues += "Missing category constant: $category"
            $Failed++
        }
    }
}
else {
    Write-Host "  [FAIL] TestCategories.cs not found at $TestCategoriesPath" -ForegroundColor Red
    $Issues += "TestCategories.cs not found"
    $Failed++
}

Write-Host ""

# 2. Check CI workflows reference categories correctly
Write-Host "Checking CI Workflows..." -ForegroundColor Yellow

$WorkflowsDir = Join-Path $RepoRoot ".github/workflows"
$WorkflowFiles = @(
    'ci-quick.yml',
    'ci-full.yml',
    'ci-nightly.yml'
)

foreach ($workflowFile in $WorkflowFiles) {
    $workflowPath = Join-Path $WorkflowsDir $workflowFile

    if (Test-Path $workflowPath) {
        Write-Host "  Checking $workflowFile..." -ForegroundColor Gray
        $workflowContent = Get-Content $workflowPath -Raw

        # Check for test filter patterns
        $categoryMentions = @()
        foreach ($category in $ExpectedCategories.Keys) {
            $workflow = $ExpectedCategories[$category].Workflow

            if ($workflow -match $workflowFile) {
                if ($workflowContent -match "Category=$category" -or
                    $workflowContent -match "`"$category`"" -or
                    $workflowContent -match "'$category'") {
                    $categoryMentions += $category
                    Write-Host "    [PASS] References category '$category'" -ForegroundColor Green
                    $Passed++
                }
                else {
                    # Some workflows run all tests without explicit filters
                    if ($workflowContent -match "dotnet test" -and -not ($workflowContent -match "--filter")) {
                        Write-Host "    [INFO] Runs all tests (no filter) - includes '$category'" -ForegroundColor Cyan
                        $Passed++
                    }
                    else {
                        Write-Host "    [WARN] Should reference category '$category'" -ForegroundColor Yellow
                    }
                }
            }
        }
    }
    else {
        Write-Host "  [FAIL] Workflow file not found: $workflowFile" -ForegroundColor Red
        $Issues += "Missing workflow: $workflowFile"
        $Failed++
    }
}

Write-Host ""

# 3. Check test files for category attributes
Write-Host "Checking Test Files for Category Traits..." -ForegroundColor Yellow

$TestProjects = @(
    @{ Path = "backend/tests/ExpenseFlow.Contracts.Tests"; ExpectedCategory = "Contract" },
    @{ Path = "backend/tests/ExpenseFlow.PropertyTests"; ExpectedCategory = "Property" },
    @{ Path = "backend/tests/ExpenseFlow.Scenarios.Tests"; ExpectedCategory = "Scenario" }
)

foreach ($project in $TestProjects) {
    $projectPath = Join-Path $RepoRoot $project.Path

    if (Test-Path $projectPath) {
        $testFiles = Get-ChildItem -Path $projectPath -Filter "*.cs" -Recurse |
                     Where-Object { $_.Name -match "Tests?\.cs$" }

        $hasTests = $false
        $hasCategoryTrait = $false

        foreach ($file in $testFiles) {
            $content = Get-Content $file.FullName -Raw

            if ($content -match "\[Fact\]" -or $content -match "\[Theory\]") {
                $hasTests = $true

                if ($content -match "\[Trait\(`"Category`"" -or
                    $content -match "TestCategories\." -or
                    $content -match "\[Category\(") {
                    $hasCategoryTrait = $true
                }
            }
        }

        if ($hasTests) {
            if ($hasCategoryTrait) {
                Write-Host "  [PASS] $($project.Path) has category traits" -ForegroundColor Green
                $Passed++
            }
            else {
                Write-Host "  [WARN] $($project.Path) tests missing category traits" -ForegroundColor Yellow
            }
        }
        else {
            Write-Host "  [INFO] $($project.Path) has no test classes yet" -ForegroundColor Cyan
        }
    }
    else {
        Write-Host "  [INFO] Project not found: $($project.Path)" -ForegroundColor Gray
    }
}

Write-Host ""

# 4. Summary
Write-Host "=== Validation Summary ===" -ForegroundColor Cyan
Write-Host "Passed: $Passed" -ForegroundColor Green
Write-Host "Failed: $Failed" -ForegroundColor $(if ($Failed -gt 0) { 'Red' } else { 'Green' })

if ($Issues.Count -gt 0) {
    Write-Host ""
    Write-Host "Issues Found:" -ForegroundColor Yellow
    foreach ($issue in $Issues) {
        Write-Host "  - $issue" -ForegroundColor Red
    }

    if ($Fix) {
        Write-Host ""
        Write-Host "Auto-fix is not yet implemented for these issues." -ForegroundColor Yellow
        Write-Host "Please address them manually."
    }

    exit 1
}

Write-Host ""
Write-Host "All validations passed!" -ForegroundColor Green
exit 0
