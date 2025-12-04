# Quickstart: Infrastructure Setup

**Date**: 2025-12-03
**Branch**: `001-infrastructure-setup`

## Prerequisites

Before deploying, ensure you have:

1. **Azure CLI** installed and authenticated (`az login`)
2. **kubectl** configured for `dev-aks` cluster
3. **Helm 3.x** installed
4. Access to Key Vault `iius-akv`
5. DNS access to configure CNAME for `expense.ii-us.com`

### Verify AKS Access

```powershell
# Get AKS credentials
az aks get-credentials --resource-group rg_prod --name dev-aks

# Verify cluster access
kubectl cluster-info
kubectl get nodes
```

---

## Step 1: Create Namespaces

```powershell
# Apply namespace manifests
kubectl apply -f infrastructure/namespaces/expenseflow-dev.yaml
kubectl apply -f infrastructure/namespaces/expenseflow-staging.yaml

# Verify
kubectl get namespaces | Select-String "expenseflow"
```

Expected output:
```
expenseflow-dev       Active   ...
expenseflow-staging   Active   ...
```

---

## Step 2: Deploy cert-manager

```powershell
# Add Jetstack Helm repo
helm repo add jetstack https://charts.jetstack.io
helm repo update

# Install cert-manager with CRDs
helm install cert-manager jetstack/cert-manager `
  --namespace cert-manager `
  --create-namespace `
  --version v1.19.1 `
  --set crds.enabled=true

# Wait for pods to be ready
kubectl wait --for=condition=Ready pods -l app.kubernetes.io/instance=cert-manager -n cert-manager --timeout=120s
```

### Apply ClusterIssuers

```powershell
# Apply Let's Encrypt issuers
kubectl apply -f infrastructure/cert-manager/cluster-issuer.yaml

# Verify issuers are ready
kubectl get clusterissuer
```

Expected output:
```
NAME                  READY   AGE
letsencrypt-staging   True    ...
letsencrypt-prod      True    ...
```

---

## Step 3: Deploy Supabase

```powershell
# Clone Supabase Helm chart from GitHub (no Helm repo available)
git clone --depth 1 https://github.com/supabase-community/supabase-kubernetes.git $env:TEMP\supabase-kubernetes

# Install Supabase with custom values (Auth/Storage disabled)
helm install supabase "$env:TEMP\supabase-kubernetes\charts\supabase" `
  --namespace expenseflow-dev `
  --values infrastructure/supabase/values.yaml `
  --timeout 10m

# Wait for pods to be ready (may take 3-5 minutes)
kubectl wait --for=condition=Ready pods -l app.kubernetes.io/instance=supabase -n expenseflow-dev --timeout=300s
```

### Verify Supabase Pods

```powershell
# Check all Supabase pods
kubectl get pods -n expenseflow-dev -l app.kubernetes.io/instance=supabase
```

Expected output:
```
NAME                                  READY   STATUS    RESTARTS   AGE
supabase-kong-...                     1/1     Running   0          ...
supabase-meta-...                     1/1     Running   0          ...
supabase-postgresql-0                 1/1     Running   0          ...
supabase-realtime-...                 1/1     Running   0          ...
supabase-rest-...                     1/1     Running   0          ...
supabase-studio-...                   1/1     Running   0          ...
```

### Verify pgvector Extension

```powershell
# Get database password
$dbPassword = kubectl get secret supabase-postgresql -n expenseflow-dev -o jsonpath='{.data.postgres-password}' |
  ForEach-Object { [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($_)) }

# Test pgvector
kubectl run psql-test --rm -it --restart=Never `
  --image=postgres:15 `
  --namespace=expenseflow-dev `
  --env="PGPASSWORD=$dbPassword" `
  -- psql -h supabase-postgresql -U postgres -d postgres -c "\dx vector"
```

---

## Step 4: Access Supabase Studio

Once DNS is configured (Step 7), access Supabase Studio at:
- **URL**: `https://studio.expense.ii-us.com`

For immediate local access before DNS:
```powershell
# Port-forward to Studio
kubectl port-forward svc/supabase-studio 3000:3000 -n expenseflow-dev

# Open in browser: http://localhost:3000
```

---

## Step 5: Configure Blob Storage

```powershell
# Create receipts container
az storage container create `
  --name expenseflow-receipts `
  --account-name ccproctemp2025 `
  --auth-mode login

# Verify container exists
az storage container list --account-name ccproctemp2025 --auth-mode login --query "[?name=='expenseflow-receipts']"
```

---

## Step 6: Apply Resource Quotas and Network Policies

```powershell
# Apply quotas
kubectl apply -f infrastructure/namespaces/resource-quotas.yaml

# Apply network policies
kubectl apply -f infrastructure/namespaces/network-policies.yaml

# Verify quotas
kubectl describe resourcequota -n expenseflow-dev
```

---

## Step 7: Configure DNS (Manual Step)

Add CNAME records in GoDaddy DNS:

| Host | Type | Value |
|------|------|-------|
| dev.expense | CNAME | `<NGINX-EXTERNAL-IP>` |
| studio.expense | CNAME | `<NGINX-EXTERNAL-IP>` |
| staging.expense | CNAME | `<NGINX-EXTERNAL-IP>` |

Get the external IP:
```powershell
kubectl get svc -n app-routing-system
```

**Note**: `studio.expense.ii-us.com` provides access to Supabase Studio for graphical database exploration.

---

## Step 8: Request TLS Certificate

```powershell
# Apply certificate request
kubectl apply -f infrastructure/cert-manager/certificate.yaml

# Monitor certificate status
kubectl get certificate -n expenseflow-dev -w
```

Wait until `READY` shows `True`. This may take 1-2 minutes after DNS propagation.

---

## Step 9: Apply Scheduled Backups

```powershell
# Apply backup PVC and CronJob
kubectl apply -f infrastructure/supabase/backup-pvc.yaml
kubectl apply -f infrastructure/supabase/backup-cronjob.yaml

# Verify CronJob schedule
kubectl get cronjob -n expenseflow-dev
```

Expected output:
```
NAME              SCHEDULE    SUSPEND   ACTIVE   LAST SCHEDULE   AGE
supabase-backup   0 2 * * *   False     0        <none>          ...
```

---

## Validation

Run the validation script to confirm all components are working:

```powershell
./infrastructure/scripts/validate-deployment.ps1
```

### Manual Validation Checklist

- [ ] `kubectl get pods -n cert-manager` - All pods Running
- [ ] `kubectl get clusterissuer` - Both issuers READY=True
- [ ] `kubectl get pods -n expenseflow-dev -l app.kubernetes.io/instance=supabase` - All Supabase pods Running
- [ ] `curl -v https://dev.expense.ii-us.com/` - Valid TLS certificate
- [ ] `curl -v https://studio.expense.ii-us.com/` - Supabase Studio accessible
- [ ] Blob upload test succeeds
- [ ] pgvector extension enabled (`\dx vector`)

---

## Troubleshooting

### Certificate Not Issuing

```powershell
# Check certificate status
kubectl describe certificate expenseflow-dev-cert -n expenseflow-dev

# Check challenge status
kubectl get challenges -n expenseflow-dev

# Check cert-manager logs
kubectl logs -n cert-manager -l app=cert-manager
```

### Database Connection Issues

```powershell
# Check PostgreSQL pod status
kubectl describe pod supabase-postgresql-0 -n expenseflow-dev

# Check PostgreSQL logs
kubectl logs supabase-postgresql-0 -n expenseflow-dev

# Check all Supabase pod logs
kubectl logs -n expenseflow-dev -l app.kubernetes.io/instance=supabase --all-containers
```

### Supabase Studio Not Loading

```powershell
# Check Studio pod
kubectl describe pod -n expenseflow-dev -l app.kubernetes.io/name=supabase-studio

# Check Kong (API Gateway) - required for Studio
kubectl logs -n expenseflow-dev -l app.kubernetes.io/name=supabase-kong

# Verify ingress
kubectl get ingress -n expenseflow-dev
```

### Network Policy Blocking Traffic

```powershell
# Temporarily disable default deny (for debugging only)
kubectl delete networkpolicy default-deny-ingress -n expenseflow-dev

# Re-apply after debugging
kubectl apply -f infrastructure/namespaces/network-policies.yaml
```

---

## Estimated Deployment Time

| Step | Duration |
|------|----------|
| Namespaces | 30 seconds |
| cert-manager | 2 minutes |
| Supabase (all components) | 5 minutes |
| Blob storage | 30 seconds |
| Quotas & policies | 30 seconds |
| DNS propagation | 5-30 minutes |
| Certificate issuance | 2 minutes |
| **Total** | **~15-45 minutes** |

---

## Rollback

To remove all infrastructure:

```powershell
# Uninstall Supabase
helm uninstall supabase -n expenseflow-dev

# Delete namespaces (removes all resources within)
kubectl delete namespace expenseflow-dev expenseflow-staging

# Uninstall cert-manager
helm uninstall cert-manager -n cert-manager

# Delete system namespaces
kubectl delete namespace cert-manager

# Delete cluster-scoped resources
kubectl delete clusterissuer letsencrypt-staging letsencrypt-prod
```
