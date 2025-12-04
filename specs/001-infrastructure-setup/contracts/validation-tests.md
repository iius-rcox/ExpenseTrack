# Infrastructure Validation Contracts

**Date**: 2025-12-03
**Branch**: `001-infrastructure-setup`

## Overview

This document defines the validation tests (contracts) that must pass for the infrastructure deployment to be considered complete. These are infrastructure-level contracts rather than API contracts.

---

## 1. TLS Certificate Validation

### Test: Certificate Issuance

```bash
# Verify cert-manager is running
kubectl get pods -n cert-manager -l app=cert-manager

# Expected: All pods in Running state
```

```bash
# Verify ClusterIssuers are ready
kubectl get clusterissuer

# Expected:
# NAME                  READY   AGE
# letsencrypt-staging   True    ...
# letsencrypt-prod      True    ...
```

```bash
# Verify certificate for dev environment
kubectl get certificate -n expenseflow-dev

# Expected:
# NAME                   READY   SECRET                AGE
# expenseflow-dev-cert   True    expenseflow-dev-tls   ...
```

### Test: HTTPS Access

```bash
# Test HTTPS endpoint (requires CNAME configured)
curl -v https://dev.expense.ii-us.com/

# Expected:
# - TLS handshake succeeds
# - Certificate issued by Let's Encrypt
# - No SSL errors
```

### Test: Certificate Auto-Renewal

```bash
# Check certificate expiration
kubectl get certificate -n expenseflow-dev -o jsonpath='{.items[0].status.notAfter}'

# Expected: Expiration date > 60 days from now (90-day cert, renewed at 30 days)
```

---

## 2. Database Validation (Supabase)

### Test: Supabase Pods Running

```bash
# Verify all Supabase pods are running
kubectl get pods -n expenseflow-dev -l app.kubernetes.io/instance=supabase

# Expected: All pods in Running state (6 pods: postgresql, kong, rest, realtime, meta, studio)
```

### Test: PostgreSQL Connectivity

```bash
# Test database connection from within cluster
# First get password from secret
PGPASSWORD=$(kubectl get secret supabase-postgresql -n expenseflow-dev -o jsonpath='{.data.postgres-password}' | base64 -d)

kubectl run psql-test --rm -it --restart=Never \
  --image=postgres:15 \
  --namespace=expenseflow-dev \
  --env="PGPASSWORD=$PGPASSWORD" \
  -- psql -h supabase-postgresql -U postgres -d postgres -c "SELECT version();"

# Expected: PostgreSQL 15.x version string returned
```

### Test: pgvector Extension

```bash
# Verify pgvector is enabled
PGPASSWORD=$(kubectl get secret supabase-postgresql -n expenseflow-dev -o jsonpath='{.data.postgres-password}' | base64 -d)

kubectl run psql-test --rm -it --restart=Never \
  --image=postgres:15 \
  --namespace=expenseflow-dev \
  --env="PGPASSWORD=$PGPASSWORD" \
  -- psql -h supabase-postgresql -U postgres -d postgres -c "\dx vector"

# Expected: vector extension listed
```

```bash
# Test vector operations
PGPASSWORD=$(kubectl get secret supabase-postgresql -n expenseflow-dev -o jsonpath='{.data.postgres-password}' | base64 -d)

kubectl run psql-test --rm -it --restart=Never \
  --image=postgres:15 \
  --namespace=expenseflow-dev \
  --env="PGPASSWORD=$PGPASSWORD" \
  -- psql -h supabase-postgresql -U postgres -d postgres -c "
    CREATE TABLE IF NOT EXISTS test_vectors (id serial PRIMARY KEY, embedding vector(3));
    INSERT INTO test_vectors (embedding) VALUES ('[1,2,3]'), ('[4,5,6]');
    SELECT * FROM test_vectors ORDER BY embedding <-> '[1,2,3]' LIMIT 1;
    DROP TABLE test_vectors;
  "

# Expected: Query returns row with embedding [1,2,3]
```

### Test: Data Persistence

```bash
# Create test data
PGPASSWORD=$(kubectl get secret supabase-postgresql -n expenseflow-dev -o jsonpath='{.data.postgres-password}' | base64 -d)

kubectl run psql-test --rm -it --restart=Never \
  --image=postgres:15 \
  --namespace=expenseflow-dev \
  --env="PGPASSWORD=$PGPASSWORD" \
  -- psql -h supabase-postgresql -U postgres -d postgres -c "
    CREATE TABLE IF NOT EXISTS persistence_test (id serial PRIMARY KEY, data text);
    INSERT INTO persistence_test (data) VALUES ('test-data-12345');
  "

# Restart the database pod
kubectl delete pod supabase-postgresql-0 -n expenseflow-dev

# Wait for pod to recover
kubectl wait --for=condition=Ready pod/supabase-postgresql-0 -n expenseflow-dev --timeout=120s

# Verify data persisted
kubectl run psql-test --rm -it --restart=Never \
  --image=postgres:15 \
  --namespace=expenseflow-dev \
  --env="PGPASSWORD=$PGPASSWORD" \
  -- psql -h supabase-postgresql -U postgres -d postgres -c "
    SELECT data FROM persistence_test WHERE data = 'test-data-12345';
    DROP TABLE persistence_test;
  "

# Expected: Returns 'test-data-12345'
```

### Test: Backup CronJob Configuration

```bash
# Verify backup CronJob exists
kubectl get cronjob -n expenseflow-dev

# Expected:
# NAME              SCHEDULE      SUSPEND   ACTIVE   LAST SCHEDULE   AGE
# supabase-backup   0 2 * * *     False     0        ...             ...
```

### Test: Supabase Studio Accessibility

```bash
# Verify Studio ingress exists
kubectl get ingress -n expenseflow-dev

# Expected: Ingress for studio.expense.ii-us.com listed

# Test Studio endpoint (requires DNS configured)
curl -v https://studio.expense.ii-us.com/

# Expected:
# - HTTP 200 or redirect to Studio UI
# - Valid TLS certificate
```

---

## 3. Storage Validation

### Test: Blob Container Access

```powershell
# Test blob upload
$testContent = "test-file-content-$(Get-Date -Format 'yyyyMMddHHmmss')"
az storage blob upload `
  --account-name ccproctemp2025 `
  --container-name expenseflow-receipts `
  --name "test/validation-test.txt" `
  --data $testContent `
  --auth-mode login

# Expected: Upload succeeds without error
```

```powershell
# Test blob download
az storage blob download `
  --account-name ccproctemp2025 `
  --container-name expenseflow-receipts `
  --name "test/validation-test.txt" `
  --auth-mode login

# Expected: Returns content matching uploaded test content
```

```powershell
# Cleanup test file
az storage blob delete `
  --account-name ccproctemp2025 `
  --container-name expenseflow-receipts `
  --name "test/validation-test.txt" `
  --auth-mode login
```

---

## 4. Namespace Validation

### Test: Namespace Existence

```bash
# Verify namespaces exist
kubectl get namespace expenseflow-dev expenseflow-staging

# Expected: Both namespaces listed with Status: Active
```

### Test: Namespace Labels

```bash
# Verify labels for network policy support
kubectl get namespace expenseflow-dev -o jsonpath='{.metadata.labels.name}'

# Expected: expenseflow-dev
```

### Test: Resource Quotas

```bash
# Verify quota is applied
kubectl describe resourcequota -n expenseflow-dev

# Expected:
# Name:            dev-quota
# Resource         Used  Hard
# --------         ----  ----
# limits.cpu       ...   4
# limits.memory    ...   8Gi
# ...
```

### Test: LimitRange

```bash
# Verify limit range is applied
kubectl describe limitrange -n expenseflow-dev

# Expected: Shows default container limits
```

---

## 5. Network Policy Validation

### Test: Default Deny

```bash
# Deploy a test pod without specific allow rules
kubectl run network-test --rm -it --restart=Never \
  --image=busybox \
  --namespace=expenseflow-dev \
  -- wget -T 5 -O- http://kubernetes.default.svc

# Expected: Connection times out (blocked by default deny)
```

### Test: Same Namespace Allowed

```bash
# Deploy a test service
kubectl run test-server --image=nginx --namespace=expenseflow-dev --port=80
kubectl expose pod test-server --namespace=expenseflow-dev --port=80

# Test connectivity from another pod in same namespace
kubectl run network-test --rm -it --restart=Never \
  --image=busybox \
  --namespace=expenseflow-dev \
  -- wget -T 5 -O- http://test-server

# Expected: Connection succeeds (same namespace allowed)

# Cleanup
kubectl delete pod test-server -n expenseflow-dev
kubectl delete service test-server -n expenseflow-dev
```

### Test: Cross-Namespace Denied

```bash
# Test connectivity from staging to dev
kubectl run test-server --image=nginx --namespace=expenseflow-dev --port=80
kubectl expose pod test-server --namespace=expenseflow-dev --port=80

kubectl run network-test --rm -it --restart=Never \
  --image=busybox \
  --namespace=expenseflow-staging \
  -- wget -T 5 -O- http://test-server.expenseflow-dev.svc

# Expected: Connection times out (cross-namespace blocked)

# Cleanup
kubectl delete pod test-server -n expenseflow-dev
kubectl delete service test-server -n expenseflow-dev
```

---

## 6. Observability Validation

### Test: Container Insights Metrics

```bash
# Verify pods are sending metrics
az monitor metrics list \
  --resource "/subscriptions/a78954fe-f6fe-4279-8be0-2c748be2f266/resourceGroups/rg_prod/providers/Microsoft.ContainerService/managedClusters/dev-aks" \
  --metric "node_cpu_usage_percentage" \
  --interval PT1M

# Expected: Metrics data returned for recent time period
```

### Test: Alert Rules (if configured)

```bash
# List alert rules
az monitor metrics alert list --resource-group rg_prod

# Expected: Infrastructure alerts listed
```

---

## Validation Summary Checklist

| Category | Test | Pass Criteria |
|----------|------|---------------|
| **TLS** | cert-manager running | All pods Running |
| **TLS** | ClusterIssuers ready | READY=True |
| **TLS** | Certificate issued | READY=True, Secret exists |
| **TLS** | HTTPS access | Valid Let's Encrypt cert |
| **Database** | Supabase pods running | All 6 pods Running |
| **Database** | PostgreSQL connection | Version returned |
| **Database** | pgvector enabled | Extension listed |
| **Database** | Vector query works | Results returned |
| **Database** | Data persistence | Data survives restart |
| **Database** | Backup CronJob | CronJob exists with schedule |
| **Database** | Studio accessible | UI loads at studio.expense.ii-us.com |
| **Storage** | Blob upload | No errors |
| **Storage** | Blob download | Content matches |
| **Namespace** | Namespaces exist | Status=Active |
| **Namespace** | Labels set | name label present |
| **Namespace** | Quota applied | ResourceQuota exists |
| **Namespace** | LimitRange applied | LimitRange exists |
| **Network** | Default deny works | External blocked |
| **Network** | Same namespace allowed | Internal works |
| **Network** | Cross-namespace denied | Cross-ns blocked |
| **Observability** | Metrics flowing | Data in Container Insights |

---

## Automated Validation Script

Save as `validate-deployment.ps1`:

```powershell
#!/usr/bin/env pwsh
# Infrastructure Validation Script

$ErrorActionPreference = "Stop"
$testResults = @()

function Test-Condition {
    param([string]$Name, [scriptblock]$Test)
    try {
        $result = & $Test
        if ($result) {
            Write-Host "[PASS] $Name" -ForegroundColor Green
            $script:testResults += @{Name=$Name; Status="PASS"}
        } else {
            Write-Host "[FAIL] $Name" -ForegroundColor Red
            $script:testResults += @{Name=$Name; Status="FAIL"}
        }
    } catch {
        Write-Host "[FAIL] $Name - $($_.Exception.Message)" -ForegroundColor Red
        $script:testResults += @{Name=$Name; Status="FAIL"}
    }
}

# Run tests
Test-Condition "cert-manager pods running" {
    $pods = kubectl get pods -n cert-manager -o json | ConvertFrom-Json
    ($pods.items | Where-Object { $_.status.phase -eq "Running" }).Count -gt 0
}

Test-Condition "ClusterIssuers ready" {
    $issuers = kubectl get clusterissuer -o json | ConvertFrom-Json
    ($issuers.items | Where-Object { $_.status.conditions[0].status -eq "True" }).Count -eq 2
}

Test-Condition "Supabase pods running" {
    $pods = kubectl get pods -n expenseflow-dev -l app.kubernetes.io/instance=supabase -o json | ConvertFrom-Json
    $runningPods = ($pods.items | Where-Object { $_.status.phase -eq "Running" }).Count
    $runningPods -ge 5  # At least 5 core pods (postgresql, kong, rest, meta, studio)
}

Test-Condition "Supabase Studio ingress exists" {
    $ingress = kubectl get ingress -n expenseflow-dev -o json | ConvertFrom-Json
    ($ingress.items | Where-Object { $_.spec.rules[0].host -like "*studio*" }).Count -gt 0
}

Test-Condition "Namespaces exist" {
    $ns = kubectl get namespace expenseflow-dev expenseflow-staging -o json 2>$null | ConvertFrom-Json
    $ns.items.Count -eq 2
}

# Summary
$passed = ($testResults | Where-Object { $_.Status -eq "PASS" }).Count
$failed = ($testResults | Where-Object { $_.Status -eq "FAIL" }).Count
Write-Host "`nResults: $passed passed, $failed failed" -ForegroundColor $(if ($failed -gt 0) { "Red" } else { "Green" })

exit $(if ($failed -gt 0) { 1 } else { 0 })
```
