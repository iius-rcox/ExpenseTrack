#!/usr/bin/env pwsh
# Infrastructure Validation Script
# Validates all ExpenseFlow infrastructure components

$ErrorActionPreference = "Stop"
$testResults = @()

function Test-Condition {
    param(
        [string]$Name,
        [scriptblock]$Test
    )
    try {
        $result = & $Test
        if ($result) {
            Write-Host "[PASS] $Name" -ForegroundColor Green
            $script:testResults += @{Name=$Name; Status="PASS"}
            return $true
        } else {
            Write-Host "[FAIL] $Name" -ForegroundColor Red
            $script:testResults += @{Name=$Name; Status="FAIL"}
            return $false
        }
    } catch {
        Write-Host "[FAIL] $Name - $($_.Exception.Message)" -ForegroundColor Red
        $script:testResults += @{Name=$Name; Status="FAIL"; Error=$_.Exception.Message}
        return $false
    }
}

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "ExpenseFlow Infrastructure Validation" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Namespace tests
Write-Host "--- Namespace Validation ---" -ForegroundColor Yellow
Test-Condition "Namespaces exist" {
    $ns = kubectl get namespace expenseflow-dev expenseflow-staging -o json 2>$null | ConvertFrom-Json
    $ns.items.Count -eq 2
}

Test-Condition "Dev namespace has correct labels" {
    $label = kubectl get namespace expenseflow-dev -o jsonpath='{.metadata.labels.name}' 2>$null
    $label -eq "expenseflow-dev"
}

Test-Condition "Resource quota applied to dev" {
    $quota = kubectl get resourcequota -n expenseflow-dev -o json 2>$null | ConvertFrom-Json
    $quota.items.Count -gt 0
}

Test-Condition "LimitRange applied to dev" {
    $lr = kubectl get limitrange -n expenseflow-dev -o json 2>$null | ConvertFrom-Json
    $lr.items.Count -gt 0
}

# cert-manager tests
Write-Host "`n--- cert-manager Validation ---" -ForegroundColor Yellow
Test-Condition "cert-manager pods running" {
    $pods = kubectl get pods -n cert-manager -l app.kubernetes.io/instance=cert-manager -o json 2>$null | ConvertFrom-Json
    ($pods.items | Where-Object { $_.status.phase -eq "Running" }).Count -gt 0
}

Test-Condition "ClusterIssuers ready" {
    $issuers = kubectl get clusterissuer -o json 2>$null | ConvertFrom-Json
    $readyIssuers = $issuers.items | Where-Object {
        $_.status.conditions | Where-Object { $_.type -eq "Ready" -and $_.status -eq "True" }
    }
    $readyIssuers.Count -ge 2
}

# Supabase tests
Write-Host "`n--- Supabase Validation ---" -ForegroundColor Yellow
Test-Condition "Supabase pods running" {
    $pods = kubectl get pods -n expenseflow-dev -l app.kubernetes.io/instance=supabase -o json 2>$null | ConvertFrom-Json
    $runningPods = ($pods.items | Where-Object { $_.status.phase -eq "Running" }).Count
    $runningPods -ge 5  # At least 5 core pods
}

Test-Condition "PostgreSQL pod running" {
    $podOutput = kubectl get pods -n expenseflow-dev -l app.kubernetes.io/name=supabase-db -o jsonpath='{.items[0].status.phase}' 2>$null
    $podOutput -eq "Running"
}

Test-Condition "Supabase Studio accessible" {
    $svc = kubectl get svc supabase-supabase-studio -n expenseflow-dev -o json 2>$null | ConvertFrom-Json
    $svc.spec.ports.Count -gt 0
}

Test-Condition "Backup CronJob exists" {
    $cj = kubectl get cronjob supabase-backup -n expenseflow-dev -o json 2>$null | ConvertFrom-Json
    $cj.spec.schedule -eq "0 2 * * *"
}

# Network Policy tests
Write-Host "`n--- Network Policy Validation ---" -ForegroundColor Yellow
Test-Condition "Default deny ingress policy exists" {
    $np = kubectl get networkpolicy default-deny-ingress -n expenseflow-dev -o json 2>$null | ConvertFrom-Json
    $np.metadata.name -eq "default-deny-ingress"
}

Test-Condition "Default deny egress policy exists" {
    $np = kubectl get networkpolicy default-deny-egress -n expenseflow-dev -o json 2>$null | ConvertFrom-Json
    $np.metadata.name -eq "default-deny-egress"
}

Test-Condition "Allow same namespace policy exists" {
    $np = kubectl get networkpolicy allow-same-namespace -n expenseflow-dev -o json 2>$null | ConvertFrom-Json
    $np.metadata.name -eq "allow-same-namespace"
}

# Summary
Write-Host "`n============================================" -ForegroundColor Cyan
$passed = ($testResults | Where-Object { $_.Status -eq "PASS" }).Count
$failed = ($testResults | Where-Object { $_.Status -eq "FAIL" }).Count
$total = $testResults.Count

Write-Host "Results: $passed passed, $failed failed (of $total tests)" -ForegroundColor $(if ($failed -gt 0) { "Red" } else { "Green" })

if ($failed -gt 0) {
    Write-Host "`nFailed tests:" -ForegroundColor Red
    $testResults | Where-Object { $_.Status -eq "FAIL" } | ForEach-Object {
        Write-Host "  - $($_.Name)" -ForegroundColor Red
        if ($_.Error) {
            Write-Host "    Error: $($_.Error)" -ForegroundColor DarkRed
        }
    }
}

exit $(if ($failed -gt 0) { 1 } else { 0 })
