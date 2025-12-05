#!/usr/bin/env pwsh
# ExpenseFlow Infrastructure Deployment Script
# Deploys all infrastructure components in the correct order

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$infrastructureDir = Split-Path -Parent $scriptDir

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "ExpenseFlow Infrastructure Deployment" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan

# Phase 1: Namespaces
Write-Host "`n[Phase 1] Creating namespaces..." -ForegroundColor Yellow
kubectl apply -f "$infrastructureDir/namespaces/expenseflow-dev.yaml"
kubectl apply -f "$infrastructureDir/namespaces/expenseflow-staging.yaml"

# Wait for namespaces to be active
Write-Host "Waiting for namespaces to be active..."
kubectl wait --for=jsonpath='{.status.phase}'=Active namespace/expenseflow-dev --timeout=30s
kubectl wait --for=jsonpath='{.status.phase}'=Active namespace/expenseflow-staging --timeout=30s

# Phase 2: Resource Quotas and Limit Ranges
Write-Host "`n[Phase 2] Applying resource quotas and limit ranges..." -ForegroundColor Yellow
kubectl apply -f "$infrastructureDir/namespaces/resource-quotas.yaml"
kubectl apply -f "$infrastructureDir/namespaces/limit-range.yaml"

# Phase 3: cert-manager
Write-Host "`n[Phase 3] Installing cert-manager..." -ForegroundColor Yellow
helm repo add jetstack https://charts.jetstack.io 2>$null
helm repo update

# Check if cert-manager is already installed
$certManagerInstalled = helm list -n cert-manager --filter cert-manager -q 2>$null
if (-not $certManagerInstalled) {
    helm install cert-manager jetstack/cert-manager `
        --namespace cert-manager `
        --create-namespace `
        --version v1.19.1 `
        --set crds.enabled=true

    Write-Host "Waiting for cert-manager pods to be ready..."
    kubectl wait --for=condition=Ready pods -l app.kubernetes.io/instance=cert-manager -n cert-manager --timeout=120s
} else {
    Write-Host "cert-manager already installed, skipping..."
}

# Apply ClusterIssuers
Write-Host "Applying ClusterIssuers..."
kubectl apply -f "$infrastructureDir/cert-manager/cluster-issuer.yaml"

# Wait for issuers to be ready
Start-Sleep -Seconds 5
kubectl get clusterissuer

# Phase 4: Supabase
Write-Host "`n[Phase 4] Installing Supabase..." -ForegroundColor Yellow

# Clone Supabase Helm chart from GitHub (no Helm repo available)
$supabaseChartDir = "$env:TEMP\supabase-kubernetes"
if (-not (Test-Path "$supabaseChartDir\charts\supabase")) {
    Write-Host "Cloning Supabase Helm chart from GitHub..."
    git clone --depth 1 https://github.com/supabase-community/supabase-kubernetes.git $supabaseChartDir
}

# Check if Supabase is already installed
$supabaseInstalled = helm list -n expenseflow-dev --filter supabase -q 2>$null
if (-not $supabaseInstalled) {
    helm install supabase "$supabaseChartDir/charts/supabase" `
        --namespace expenseflow-dev `
        --values "$infrastructureDir/supabase/values.yaml" `
        --timeout 10m

    Write-Host "Waiting for Supabase pods to be ready (this may take a few minutes)..."
    kubectl wait --for=condition=Ready pods -l app.kubernetes.io/instance=supabase -n expenseflow-dev --timeout=300s
} else {
    Write-Host "Supabase already installed, skipping..."
}

# Phase 5: Certificates
Write-Host "`n[Phase 5] Requesting TLS certificates..." -ForegroundColor Yellow
kubectl apply -f "$infrastructureDir/cert-manager/certificate.yaml"

# Phase 6: Backup infrastructure
Write-Host "`n[Phase 6] Setting up backup infrastructure..." -ForegroundColor Yellow
kubectl apply -f "$infrastructureDir/supabase/backup-pvc.yaml"
kubectl apply -f "$infrastructureDir/supabase/backup-cronjob.yaml"

# Phase 7: Network Policies
Write-Host "`n[Phase 7] Applying network policies..." -ForegroundColor Yellow
kubectl apply -f "$infrastructureDir/namespaces/network-policies.yaml"

# Summary
Write-Host "`n============================================" -ForegroundColor Cyan
Write-Host "Deployment Complete!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan

Write-Host "`nResources deployed:"
Write-Host "  - Namespaces: expenseflow-dev, expenseflow-staging"
Write-Host "  - cert-manager with Let's Encrypt issuers"
Write-Host "  - Supabase (PostgreSQL + pgvector + Studio)"
Write-Host "  - TLS certificates for dev.expense.ii-us.com"
Write-Host "  - Daily backup CronJob"
Write-Host "  - Network policies (zero-trust)"

Write-Host "`nNext steps:"
Write-Host "  1. Configure DNS CNAME records (see quickstart.md)"
Write-Host "  2. Run validation: ./validate-deployment.ps1"
Write-Host "  3. Access Supabase Studio: kubectl port-forward svc/supabase-studio 3000:3000 -n expenseflow-dev"
