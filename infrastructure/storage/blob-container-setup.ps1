#!/usr/bin/env pwsh
# Blob Container Setup Script for ExpenseFlow
# Creates the receipts container in Azure Blob Storage

$ErrorActionPreference = "Stop"

$storageAccount = "ccproctemp2025"
$containerName = "expenseflow-receipts"

Write-Host "Creating blob container '$containerName' in storage account '$storageAccount'..." -ForegroundColor Cyan

# Create the container
az storage container create `
    --name $containerName `
    --account-name $storageAccount `
    --auth-mode login

if ($LASTEXITCODE -eq 0) {
    Write-Host "Container '$containerName' created successfully!" -ForegroundColor Green
} else {
    Write-Host "Failed to create container. Exit code: $LASTEXITCODE" -ForegroundColor Red
    exit 1
}

# Verify container exists
Write-Host "`nVerifying container exists..." -ForegroundColor Cyan
$container = az storage container list `
    --account-name $storageAccount `
    --auth-mode login `
    --query "[?name=='$containerName']" `
    --output json | ConvertFrom-Json

if ($container.Count -gt 0) {
    Write-Host "Container verified: $($container[0].name)" -ForegroundColor Green
} else {
    Write-Host "Container verification failed!" -ForegroundColor Red
    exit 1
}

Write-Host "`nBlob storage setup complete!" -ForegroundColor Green
