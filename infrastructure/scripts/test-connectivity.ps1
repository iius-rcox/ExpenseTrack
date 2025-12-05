#!/usr/bin/env pwsh
# Connectivity Test Script
# Tests database connectivity and network policy enforcement

$ErrorActionPreference = "Stop"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "ExpenseFlow Connectivity Tests" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan

# Test 1: PostgreSQL Connectivity
Write-Host "`n--- PostgreSQL Connectivity ---" -ForegroundColor Yellow

$dbPassword = kubectl get secret supabase-postgresql -n expenseflow-dev -o jsonpath='{.data.postgres-password}' 2>$null
if ($dbPassword) {
    $dbPassword = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($dbPassword))

    Write-Host "Testing database connection..."
    $result = kubectl run psql-test --rm -it --restart=Never `
        --image=postgres:15 `
        --namespace=expenseflow-dev `
        --env="PGPASSWORD=$dbPassword" `
        -- psql -h supabase-postgresql -U postgres -d postgres -c "SELECT version();" 2>&1

    if ($result -match "PostgreSQL") {
        Write-Host "[PASS] PostgreSQL connection successful" -ForegroundColor Green
        Write-Host $result | Select-Object -First 3
    } else {
        Write-Host "[FAIL] PostgreSQL connection failed" -ForegroundColor Red
        Write-Host $result
    }
} else {
    Write-Host "[SKIP] Could not retrieve database password" -ForegroundColor Yellow
}

# Test 2: pgvector Extension
Write-Host "`n--- pgvector Extension ---" -ForegroundColor Yellow

if ($dbPassword) {
    Write-Host "Testing pgvector extension..."
    $result = kubectl run psql-test --rm -it --restart=Never `
        --image=postgres:15 `
        --namespace=expenseflow-dev `
        --env="PGPASSWORD=$dbPassword" `
        -- psql -h supabase-postgresql -U postgres -d postgres -c "\dx vector" 2>&1

    if ($result -match "vector") {
        Write-Host "[PASS] pgvector extension is enabled" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] pgvector extension not found" -ForegroundColor Red
        Write-Host $result
    }
}

# Test 3: Same Namespace Communication
Write-Host "`n--- Same Namespace Communication ---" -ForegroundColor Yellow

Write-Host "Deploying test server..."
kubectl run test-server --image=nginx --namespace=expenseflow-dev --port=80 --restart=Never 2>$null
kubectl expose pod test-server --namespace=expenseflow-dev --port=80 2>$null

Start-Sleep -Seconds 10

Write-Host "Testing same-namespace connectivity..."
$result = kubectl run network-test --rm -it --restart=Never `
    --image=busybox `
    --namespace=expenseflow-dev `
    -- wget -T 5 -O- http://test-server 2>&1

if ($result -match "nginx" -or $result -match "Welcome") {
    Write-Host "[PASS] Same namespace communication works" -ForegroundColor Green
} else {
    Write-Host "[FAIL] Same namespace communication blocked" -ForegroundColor Red
}

# Cleanup test server
kubectl delete pod test-server -n expenseflow-dev --ignore-not-found 2>$null
kubectl delete service test-server -n expenseflow-dev --ignore-not-found 2>$null

# Test 4: Cross-Namespace Communication (should be blocked)
Write-Host "`n--- Cross-Namespace Communication (should be BLOCKED) ---" -ForegroundColor Yellow

# Create test server in dev
kubectl run test-server --image=nginx --namespace=expenseflow-dev --port=80 --restart=Never 2>$null
kubectl expose pod test-server --namespace=expenseflow-dev --port=80 2>$null

Start-Sleep -Seconds 10

Write-Host "Testing cross-namespace connectivity (staging -> dev)..."
$result = kubectl run network-test --rm -it --restart=Never `
    --image=busybox `
    --namespace=expenseflow-staging `
    -- wget -T 5 -O- http://test-server.expenseflow-dev.svc 2>&1

if ($result -match "timed out" -or $result -match "bad address") {
    Write-Host "[PASS] Cross-namespace communication correctly blocked" -ForegroundColor Green
} else {
    Write-Host "[FAIL] Cross-namespace communication NOT blocked (security issue!)" -ForegroundColor Red
}

# Cleanup
Write-Host "`n--- Cleanup ---" -ForegroundColor Yellow
kubectl delete pod test-server -n expenseflow-dev --ignore-not-found 2>$null
kubectl delete service test-server -n expenseflow-dev --ignore-not-found 2>$null
Write-Host "Test resources cleaned up" -ForegroundColor Green

Write-Host "`n============================================" -ForegroundColor Cyan
Write-Host "Connectivity tests complete!" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
