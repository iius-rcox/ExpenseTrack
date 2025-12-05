# Research: Infrastructure Setup

**Date**: 2025-12-03
**Branch**: `001-infrastructure-setup`

## Summary

This document captures technology decisions and best practices research for Sprint 1 infrastructure deployment on Azure Kubernetes Service (AKS).

---

## 1. TLS Certificate Management (cert-manager)

### Decision
Deploy **cert-manager v1.19.x** with Let's Encrypt ClusterIssuers (staging + production) using Workload Identity for Azure DNS authentication.

### Rationale
- cert-manager v1.19.1 is the latest stable release with full Kubernetes 1.33.x support
- Workload Identity is more secure than service principal secrets (no credentials stored in Kubernetes)
- Let's Encrypt provides free, auto-renewing certificates with 90-day validity
- Built-in renewal at 30 days before expiry (no configuration needed)
- Web App Routing (NGINX) integration is straightforward via ingress annotations

### Alternatives Considered

| Alternative | Why Not Chosen |
|-------------|----------------|
| Azure Key Vault for certs | Higher complexity and cost; cert-manager renewal automation is simpler |
| Service Principal auth | Less secure; credentials stored in K8s Secrets |
| Manual certificate management | No automation; high operational burden |
| Application Gateway TLS | Not using App Gateway; would conflict with NGINX |

### Key Configuration

```yaml
# ClusterIssuer for Let's Encrypt Production
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: letsencrypt-prod
spec:
  acme:
    server: https://acme-v02.api.letsencrypt.org/directory
    email: admin@ii-us.com
    privateKeySecretRef:
      name: letsencrypt-prod-key
    solvers:
      - http01:
          ingress:
            class: webapprouting.kubernetes.azure.com
```

### Important Notes
- Use staging issuer (`acme-staging-v02.api.letsencrypt.org`) for testing to avoid rate limits
- Production rate limit: ~50 certificates per domain per week
- Do NOT edit the `app-routing-system` ConfigMap (managed by Azure)
- Use `cert-manager.io/cluster-issuer` annotation on Ingress resources

---

## 2. PostgreSQL Database (Supabase Self-Hosted)

### Decision
Deploy **Supabase self-hosted** via Helm chart with PostgreSQL 15 and pgvector extension. Disable Supabase Auth (using Entra ID) and Supabase Storage (using Azure Blob directly).

### Rationale
- Supabase Studio provides graphical database exploration (table editor, SQL editor, schema visualization)
- Aligns with constitution requirement for "Supabase self-hosted in AKS"
- pgvector extension included in Supabase PostgreSQL image
- Realtime subscriptions available for future features
- Familiar developer experience with modern UI
- With Auth and Storage disabled, reduces to ~5-6 pods (manageable overhead)

### Alternatives Considered

| Alternative | Why Not Chosen |
|-------------|----------------|
| CloudNativePG | Simpler (2 pods), but no graphical UI - requires port-forward + local tools |
| Azure Database for PostgreSQL | ~$200-500/month; exceeds $25/month target |
| StackGres operator | Steeper learning curve; no built-in UI |
| Bitnami Supabase | DEPRECATED - no longer maintained |

### Component Configuration

| Component | Enabled | Rationale |
|-----------|---------|-----------|
| PostgreSQL | ✅ Yes | Core database requirement |
| pgvector | ✅ Yes | Vector similarity for AI features |
| Studio | ✅ Yes | Graphical database exploration |
| Realtime | ✅ Yes | Future live update features |
| Kong (API Gateway) | ✅ Yes | Required for Studio routing |
| PostgREST | ✅ Yes | Required for Studio |
| Auth (GoTrue) | ❌ No | Using Entra ID exclusively |
| Storage API | ❌ No | Using Azure Blob directly |

### Helm Values Configuration

```yaml
# infrastructure/supabase/values.yaml
studio:
  enabled: true
  ingress:
    enabled: true
    className: webapprouting.kubernetes.azure.com
    hosts:
      - host: studio.expense.ii-us.com
        paths:
          - path: /
            pathType: Prefix

auth:
  enabled: false  # Using Entra ID exclusively

storage:
  enabled: false  # Using Azure Blob Storage directly

realtime:
  enabled: true

postgresql:
  enabled: true
  image:
    tag: "15-3.1.1"  # Includes pgvector
  primary:
    persistence:
      size: 20Gi
      storageClass: managed-csi-premium
    resources:
      requests:
        memory: "1Gi"
        cpu: "500m"
      limits:
        memory: "2Gi"
        cpu: "1000m"
    extendedConfiguration: |
      shared_preload_libraries = 'vector'
```

### Backup Configuration (Daily, 7-Day Retention)

```yaml
# Using Kubernetes CronJob for pg_dump backups
apiVersion: batch/v1
kind: CronJob
metadata:
  name: supabase-backup
  namespace: expenseflow-dev
spec:
  schedule: "0 2 * * *"  # Daily at 2 AM UTC
  jobTemplate:
    spec:
      template:
        spec:
          containers:
          - name: backup
            image: postgres:15
            command:
            - /bin/sh
            - -c
            - |
              pg_dump -h supabase-postgresql -U postgres -d postgres | \
              gzip > /backup/backup-$(date +%Y%m%d).sql.gz
            volumeMounts:
            - name: backup-volume
              mountPath: /backup
          volumes:
          - name: backup-volume
            persistentVolumeClaim:
              claimName: supabase-backup-pvc
          restartPolicy: OnFailure
```

### Cost Estimate
- Supabase pods (~5-6): Included in existing AKS node capacity
- Premium SSD storage (20GB): ~$3/month
- Blob Storage backups (~5GB): ~$0.10/month
- **Total: ~$3-4/month** (fits within existing node, no extra compute cost)

---

## 3. Network Policies (Cilium)

### Decision
Implement **Zero-Trust network policies** with Cilium using default-deny rules and explicit allow rules for cross-namespace traffic.

### Rationale
- Default-deny prevents lateral movement attacks between namespaces
- Cilium (already enabled on AKS) provides eBPF-based enforcement with low overhead
- Explicit allowlisting ensures only necessary traffic flows
- Web App Routing namespace (`app-routing-system`) needs explicit ingress rules

### Alternatives Considered

| Alternative | Why Not Chosen |
|-------------|----------------|
| Allow-all by default | Violates zero-trust; security risk |
| Network Policy Manager (NPM) | Legacy; being deprecated; performance overhead |
| Single global namespace | No isolation; RBAC becomes unmanageable |
| Istio service mesh | Overkill for this use case; adds complexity |

### Policy Structure

1. **Default Deny** (all namespaces):
   - Deny all ingress
   - Deny all egress (except DNS)

2. **Explicit Allows**:
   - Ingress from `app-routing-system` (NGINX)
   - Same-namespace communication
   - DNS egress (UDP 53)
   - Azure services egress (Key Vault, Blob Storage)

3. **Resource Quotas** (per namespace):

| Namespace | CPU Requests | Memory Requests | Pods |
|-----------|-------------|-----------------|------|
| expenseflow-dev | 2 cores | 4 GB | 20 |
| expenseflow-staging | 4 cores | 8 GB | 30 |

### LimitRange Configuration

```yaml
apiVersion: v1
kind: LimitRange
metadata:
  name: pod-limits
spec:
  limits:
    - type: Container
      default:
        cpu: 100m
        memory: 128Mi
      defaultRequest:
        cpu: 50m
        memory: 64Mi
      max:
        cpu: 1
        memory: 512Mi
```

---

## 4. Azure Blob Storage

### Decision
Use existing storage account `ccproctemp2025` with a new container `expenseflow-receipts` for document storage.

### Rationale
- Storage account already exists and is in the same region (southcentralus)
- Standard LRS tier is sufficient for receipts (cost-effective)
- Hierarchical namespace organization: `receipts/{userId}/{year}/{month}/`
- Access via Workload Identity (no storage keys in secrets)

### Configuration

```powershell
# Create container for receipts
az storage container create `
  --name expenseflow-receipts `
  --account-name ccproctemp2025 `
  --auth-mode login
```

### Cost Estimate
- Storage (~10GB first year): ~$2/month
- Transactions: ~$1/month
- **Total: ~$3/month**

---

## 5. Observability

### Decision
Use existing **Container Insights** with Azure Monitor alerts for critical infrastructure failures.

### Rationale
- Container Insights is already enabled on the AKS cluster
- No additional cost for basic metrics and logs
- Integrates with Azure Monitor for alerting
- Sufficient for infrastructure monitoring (10-20 users)

### Alert Configuration

| Alert | Condition | Severity |
|-------|-----------|----------|
| Database Down | PostgreSQL pod not running > 5 min | Critical |
| Certificate Expiry | Cert expires in < 14 days | Warning |
| Storage Unavailable | Blob operations failing > 5 min | Critical |
| High Memory Usage | Namespace memory > 80% | Warning |

---

## Summary Table

| Component | Technology | Monthly Cost |
|-----------|------------|--------------|
| TLS Certificates | cert-manager + Let's Encrypt | $0 |
| Database | Supabase self-hosted (PostgreSQL 15 + pgvector) | ~$3-4 |
| Database UI | Supabase Studio | $0 (included) |
| Storage | Azure Blob Storage | ~$3 |
| Monitoring | Container Insights (existing) | $0 |
| Network Policies | Cilium (built-in) | $0 |
| **Total New Infrastructure** | | **~$6-7/month** |

This is well under the $25/month target established in the constitution.

---

## References

- [cert-manager AKS Tutorial](https://cert-manager.io/docs/tutorials/getting-started-aks-letsencrypt/)
- [Supabase Self-Hosting with Kubernetes](https://supabase.com/docs/guides/self-hosting/docker)
- [Supabase Kubernetes Helm Chart](https://github.com/supabase-community/supabase-kubernetes)
- [AKS Network Policy Best Practices](https://learn.microsoft.com/en-us/azure/aks/network-policy-best-practices)
- [Azure CNI with Cilium](https://learn.microsoft.com/en-us/azure/aks/azure-cni-powered-by-cilium)
