# Azure Environment Reference

**Generated**: 2025-12-03
**Subscription**: Azure subscription 1
**Tenant**: INSULATIONS, INC (iius1.onmicrosoft.com)

---

## Account & Subscriptions

| Property | Value |
|----------|-------|
| **Current User** | iiadminRC@INSULATIONSINC1.onmicrosoft.com |
| **Tenant ID** | 953922e6-5370-4a01-a3d5-773a30df726b |
| **Environment** | AzureCloud |

### Available Subscriptions

| Subscription Name | Subscription ID | Default |
|-------------------|-----------------|---------|
| Azure subscription 1 | a78954fe-f6fe-4279-8be0-2c748be2f266 | Yes |
| Test Subscription | 3c2442b9-104d-43b2-832a-ae52f893e1b4 | No |

---

## Resource Groups

| Resource Group | Location | Purpose |
|----------------|----------|---------|
| **rg_prod** | southcentralus | Primary production resources |
| rg_azure_migrate | westus2 | Azure Migrate project |
| MC_rg_prod_dev-aks_southcentralus | southcentralus | AKS managed resources |
| MA_defaultazuremonitorworkspace-scus_southcentralus_managed | southcentralus | Azure Monitor managed |
| AzureBackupRG_southcentralus_1 | southcentralus | Backup resources |
| DefaultResourceGroup-SCUS | southcentralus | Default resources |
| DefaultResourceGroup-CUS | centralus | Default resources |
| DefaultResourceGroup-EUS | eastus | Default resources |
| DefaultResourceGroup-EUS2 | eastus2 | Default resources |
| NetworkWatcherRG | southcentralus | Network monitoring |

---

## Kubernetes (AKS)

### Cluster: dev-aks

| Property | Value |
|----------|-------|
| **Resource Group** | rg_prod |
| **Location** | southcentralus |
| **Kubernetes Version** | 1.33.3 |
| **SKU** | Automatic / Standard |
| **Private Cluster** | Yes |
| **OIDC Issuer** | Enabled |
| **Workload Identity** | Enabled |

#### Network Configuration

| Property | Value |
|----------|-------|
| **Network Plugin** | Azure CNI (Overlay mode) |
| **Network Policy** | Cilium |
| **Network Dataplane** | Cilium |
| **Pod CIDR** | 10.244.0.0/16 |
| **Service CIDR** | 10.240.0.0/16 |
| **DNS Service IP** | 10.240.0.10 |
| **VNet** | vnet_prod |
| **Subnet** | default (10.0.0.0/24) |

#### Node Pools

| Pool Name | Mode | VM Size | Count | OS | Kubernetes Version |
|-----------|------|---------|-------|-----|---------------------|
| **systempool** | System | standard_d4lds_v5 | 3 | AzureLinux | 1.32.6 |
| **optimized** | User | Standard_B2ms | 1 | Ubuntu | 1.33.3 |

**System Pool Features**:
- Availability Zones: 1, 2, 3
- Ephemeral OS Disk (150GB)
- Max Pods: 250
- Taint: `CriticalAddonsOnly=true:NoSchedule`

**Optimized Pool Features**:
- Managed OS Disk (128GB)
- Max Pods: 110
- No taints (workload pool)

#### Enabled Add-ons & Features

| Feature | Status |
|---------|--------|
| Azure Key Vault Secrets Provider | Enabled (with rotation) |
| Azure Policy | Enabled |
| Container Insights (OMS Agent) | Enabled |
| Web App Routing (NGINX) | Enabled |
| KEDA | Enabled |
| Vertical Pod Autoscaler | Enabled |
| Image Cleaner | Enabled (weekly) |
| Disk CSI Driver | Enabled |
| File CSI Driver | Enabled |
| Snapshot Controller | Enabled |

#### Identity Configuration

| Identity Type | Client ID |
|---------------|-----------|
| Cluster (User Assigned) | a2bcb3ce-a89b-43af-804c-e8029e0bafb4 |
| Kubelet | b7275093-e2c3-4ff5-92fa-4af1530c0e21 |
| Key Vault Secrets Provider | f8e9e7ae-2b71-4bca-97ac-8901717bc7a8 |
| Azure Policy | 1bd62bac-aad6-49c5-8d61-297472cd41c5 |
| Web App Routing | e35db091-7066-483a-b474-143537a93003 |

---

## Storage Accounts

| Name | Location | SKU | Kind | Purpose |
|------|----------|-----|------|---------|
| **ccproctemp2025** | southcentralus | Standard_LRS | StorageV2 | General purpose |
| **cssa915121f46f2ae0d374e7** | southcentralus | Standard_LRS | StorageV2 | WAP Custom Scripts |
| **rgprodperfdiag310** | southcentralus | Standard_LRS | StorageV2 | Performance diagnostics |
| **rgprodperfdiag473** | southcentralus | Standard_LRS | StorageV2 | Performance diagnostics |
| migratea7895lsa808540 | southcentralus | Standard_LRS | Storage | Azure Migrate |

### Storage Endpoints (ccproctemp2025)

| Service | Endpoint |
|---------|----------|
| Blob | https://ccproctemp2025.blob.core.windows.net/ |
| File | https://ccproctemp2025.file.core.windows.net/ |
| Queue | https://ccproctemp2025.queue.core.windows.net/ |
| Table | https://ccproctemp2025.table.core.windows.net/ |
| Data Lake | https://ccproctemp2025.dfs.core.windows.net/ |

---

## Key Vaults

| Name | Location | Resource Group |
|------|----------|----------------|
| **iius-akv** | southcentralus | rg_prod |
| akv-6-qvslkiesvo7xm6a | southcentralus | rg_prod |

---

## Container Registry

### iiusacr

| Property | Value |
|----------|-------|
| **Login Server** | iiusacr.azurecr.io |
| **SKU** | Premium |
| **Location** | southcentralus |
| **Admin User** | Enabled |
| **Zone Redundancy** | Enabled |
| **Public Access** | Enabled |

---

## Cognitive Services

### Azure OpenAI: iius-embedding

| Property | Value |
|----------|-------|
| **Endpoint** | https://iius-embedding.openai.azure.com/ |
| **Kind** | OpenAI |
| **SKU** | S0 |
| **Location** | southcentralus |

**Capabilities**: Embeddings, DALL-E, Assistants API, Whisper, Moderations, Fine-tuning

### Document Intelligence: iius-doc-intelligence

| Property | Value |
|----------|-------|
| **Endpoint** | https://iius-doc-intelligence.cognitiveservices.azure.com/ |
| **Kind** | FormRecognizer |
| **SKU** | S0 |
| **Location** | southcentralus |

**Capabilities**: Receipt analysis, Invoice analysis, ID Document analysis, Layout analysis, Custom models

---

## Virtual Networks

### vnet_prod (Production)

| Property | Value |
|----------|-------|
| **Address Space** | 10.0.0.0/16 |
| **Location** | southcentralus |
| **DNS Servers** | 10.0.0.200, 8.8.8.8 |

**Subnets**:
- **default**: 10.0.0.0/24 (AKS nodes)
- **aks-subnet**: API Server VNet Integration

### rg_azure_vnet (Migration)

| Property | Value |
|----------|-------|
| **Address Space** | 10.0.0.0/16 |
| **Location** | southcentralus |

**Subnets**:
- **default**: 10.0.0.0/24

---

## Virtual Machines

| VM Name | Size | OS | Purpose | Resource Group |
|---------|------|-----|---------|----------------|
| **INSCOLPVAULT** | Standard_E4ads_v5 | Windows | SQL Server (Procore Vault) | rg_prod |
| **INSCOLVSQL** | Standard_D4s_v3 | Windows | SQL Server | rg_prod |
| **INSDAL9DC01** | Standard_B2ms | Windows | Domain Controller | rg_prod |
| **INSDAL9DC02** | Standard_B2ms | Windows | Domain Controller | rg_prod |
| **INSCOLAVONTUS** | Standard_D2s_v3 | Windows | SQL Server (Avontus) | rg_prod |
| **INSCOLFIL001** | Standard_A2_v2 | Windows | File Server | rg_prod |
| **INSCOLRDS01** | Standard_D2s_v3 | Windows | RDS Session Host | rg_prod |
| **INSCOLRDSWEB** | Standard_F2s_v2 | Windows | RDS Web Gateway | rg_prod |
| **INSCOLVISTA** | Standard_D2s_v3 | Windows | Vista Application | rg_prod |
| **INSDALFILE01** | Standard_D2s_v3 | Windows | File Server | rg_prod |

---

## Log Analytics

### DefaultWorkspace-SCUS

| Property | Value |
|----------|-------|
| **Workspace ID** | e8f8224e-1030-4fed-952d-bfc0c11fc146 |
| **Location** | southcentralus |
| **SKU** | PerGB2018 |
| **Retention** | 30 days |

---

## ExpenseFlow Development Notes

### Recommended Resources for ExpenseFlow

Based on the existing infrastructure, the following resources are available for ExpenseFlow development:

#### Compute
- **AKS Cluster**: `dev-aks` in `rg_prod`
  - Use the `optimized` node pool for workloads
  - Web App Routing already enabled for NGINX ingress
  - Workload Identity enabled for secure secret access

#### Storage
- **Blob Storage**: Create a new container in `ccproctemp2025` for receipt storage
- **Container Registry**: Use `iiusacr.azurecr.io` for Docker images

#### Security
- **Key Vault**: Use `iius-akv` for secrets management
- **Managed Identities**: AKS has Key Vault Secrets Provider enabled

#### AI Services
- **Document Intelligence**: `iius-doc-intelligence` - Ready for receipt OCR
- **Azure OpenAI**: `iius-embedding` - Ready for embeddings and GPT-4o-mini

#### Database
- **Supabase**: Will need to be deployed to AKS (self-hosted)
- **Existing SQL Server**: `INSCOLVSQL` available for GL sync if needed

#### Networking
- **VNet**: `vnet_prod` (10.0.0.0/16)
- **Private Cluster**: API server accessible via VNet integration

### Required New Resources for ExpenseFlow

1. **Kubernetes Namespaces**: `expenseflow-dev`, `expenseflow-staging`, `expenseflow-prod`
2. **Persistent Volume Claims**: For Supabase PostgreSQL data
3. **TLS Certificates**: Let's Encrypt via cert-manager (to be deployed)
4. **Blob Containers**: For receipt storage

### Connection Quick Reference

```bash
# Connect to AKS
az aks get-credentials --resource-group rg_prod --name dev-aks

# Login to ACR
az acr login --name iiusacr

# Get Key Vault secrets
az keyvault secret list --vault-name iius-akv

# Test Document Intelligence
az cognitiveservices account keys list --name iius-doc-intelligence --resource-group rg_prod

# Test Azure OpenAI
az cognitiveservices account keys list --name iius-embedding --resource-group rg_prod
```

---

*Document generated from Azure CLI enumeration*
