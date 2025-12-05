# Data Model: Infrastructure Setup

**Date**: 2025-12-03
**Branch**: `001-infrastructure-setup`

## Overview

This document defines the Kubernetes resources and their relationships for the ExpenseFlow infrastructure deployment. Since this is an infrastructure sprint, the "data model" consists of Kubernetes resource definitions rather than application entities.

---

## Resource Hierarchy

```
AKS Cluster (dev-aks)
├── Namespaces
│   ├── cert-manager (system)
│   ├── expenseflow-dev (workload)
│   └── expenseflow-staging (workload)
│
├── Cluster-Scoped Resources
│   ├── ClusterIssuer (letsencrypt-staging)
│   ├── ClusterIssuer (letsencrypt-prod)
│   └── StorageClass (managed-csi-premium) [existing]
│
├── cert-manager Namespace
│   └── cert-manager Deployment [via Helm]
│
├── expenseflow-dev Namespace
│   ├── Supabase Stack [via Helm]
│   │   ├── supabase-db (PostgreSQL StatefulSet)
│   │   │   ├── PersistentVolumeClaim (20GB)
│   │   │   └── Service (ClusterIP)
│   │   ├── supabase-kong (API Gateway)
│   │   ├── supabase-rest (PostgREST)
│   │   ├── supabase-realtime (Realtime)
│   │   ├── supabase-meta (Metadata)
│   │   └── supabase-studio (Web UI)
│   ├── CronJob (supabase-backup)
│   ├── Certificate (dev.expense.ii-us.com)
│   ├── Certificate (studio.expense.ii-us.com)
│   ├── ResourceQuota (dev-quota)
│   ├── LimitRange (pod-limits)
│   ├── NetworkPolicy (default-deny-ingress)
│   ├── NetworkPolicy (default-deny-egress)
│   ├── NetworkPolicy (allow-web-app-routing)
│   ├── NetworkPolicy (allow-same-namespace)
│   └── NetworkPolicy (allow-dns-egress)
│
└── expenseflow-staging Namespace
    └── [Same structure as dev, with different quotas]
```

**Note**: Supabase Auth and Storage components are disabled (using Entra ID and Azure Blob respectively).

---

## Namespace Resources

### Namespace: expenseflow-dev

```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: expenseflow-dev
  labels:
    name: expenseflow-dev
    environment: development
    app.kubernetes.io/part-of: expenseflow
```

### Namespace: expenseflow-staging

```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: expenseflow-staging
  labels:
    name: expenseflow-staging
    environment: staging
    app.kubernetes.io/part-of: expenseflow
```

---

## PostgreSQL Database (Supabase Self-Hosted)

### Component Overview

| Component | Enabled | Purpose |
|-----------|---------|---------|
| PostgreSQL | ✅ | Core database with pgvector |
| Studio | ✅ | Graphical database explorer |
| Kong | ✅ | API Gateway (required for Studio) |
| PostgREST | ✅ | REST API (required for Studio) |
| Realtime | ✅ | Live subscriptions |
| Meta | ✅ | Metadata service |
| Auth | ❌ | Disabled - using Entra ID |
| Storage | ❌ | Disabled - using Azure Blob |

### Database Configuration

| Field | Value | Notes |
|-------|-------|-------|
| Service Name | `supabase-db` | Helm release name |
| PostgreSQL Version | 15 | With pgvector extension |
| Storage Size | 20GB | Premium SSD |
| Storage Class | `managed-csi-premium` | Azure Premium SSD |
| CPU Request | 500m | 0.5 cores |
| CPU Limit | 1000m | 1 core |
| Memory Request | 1Gi | |
| Memory Limit | 2Gi | |
| Extensions | pgvector | Enabled via shared_preload_libraries |

### Helm Values

```yaml
# infrastructure/supabase/values.yaml
studio:
  enabled: true
  image:
    tag: "20231123-64a766a"
  ingress:
    enabled: true
    className: webapprouting.kubernetes.azure.com
    annotations:
      cert-manager.io/cluster-issuer: letsencrypt-prod
    hosts:
      - host: studio.expense.ii-us.com
        paths:
          - path: /
            pathType: Prefix
    tls:
      - secretName: studio-tls
        hosts:
          - studio.expense.ii-us.com

auth:
  enabled: false  # Using Entra ID

storage:
  enabled: false  # Using Azure Blob

realtime:
  enabled: true

kong:
  enabled: true

rest:
  enabled: true

meta:
  enabled: true

postgresql:
  enabled: true
  image:
    tag: "15-3.1.1"  # Includes pgvector
  primary:
    persistence:
      enabled: true
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
      max_connections = 100
      shared_buffers = 256MB
      work_mem = 32MB
      maintenance_work_mem = 128MB
      effective_cache_size = 512MB
```

### Backup CronJob

```yaml
apiVersion: batch/v1
kind: CronJob
metadata:
  name: supabase-backup
  namespace: expenseflow-dev
spec:
  schedule: "0 2 * * *"  # Daily at 2 AM UTC
  concurrencyPolicy: Forbid
  successfulJobsHistoryLimit: 7
  failedJobsHistoryLimit: 3
  jobTemplate:
    spec:
      template:
        spec:
          containers:
          - name: backup
            image: postgres:15
            env:
            - name: PGPASSWORD
              valueFrom:
                secretKeyRef:
                  name: supabase-postgresql
                  key: postgres-password
            command:
            - /bin/sh
            - -c
            - |
              pg_dump -h supabase-postgresql -U postgres -d postgres | \
              gzip > /backup/backup-$(date +%Y%m%d-%H%M%S).sql.gz
              # Keep only last 7 days
              find /backup -name "*.sql.gz" -mtime +7 -delete
            volumeMounts:
            - name: backup-volume
              mountPath: /backup
          volumes:
          - name: backup-volume
            persistentVolumeClaim:
              claimName: supabase-backup-pvc
          restartPolicy: OnFailure
---
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: supabase-backup-pvc
  namespace: expenseflow-dev
spec:
  accessModes:
    - ReadWriteOnce
  storageClassName: managed-csi-premium
  resources:
    requests:
      storage: 10Gi
```

---

## TLS Certificate Resources

### ClusterIssuer: Let's Encrypt Staging

```yaml
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: letsencrypt-staging
spec:
  acme:
    server: https://acme-staging-v02.api.letsencrypt.org/directory
    email: admin@ii-us.com
    privateKeySecretRef:
      name: letsencrypt-staging-key
    solvers:
      - http01:
          ingress:
            class: webapprouting.kubernetes.azure.com
```

### ClusterIssuer: Let's Encrypt Production

```yaml
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

### Certificate: Dev Environment

```yaml
apiVersion: cert-manager.io/v1
kind: Certificate
metadata:
  name: expenseflow-dev-cert
  namespace: expenseflow-dev
spec:
  secretName: expenseflow-dev-tls
  issuerRef:
    name: letsencrypt-prod
    kind: ClusterIssuer
  dnsNames:
    - dev.expense.ii-us.com
```

---

## Network Policies

### Default Deny Ingress

```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: default-deny-ingress
  namespace: expenseflow-dev
spec:
  podSelector: {}
  policyTypes:
    - Ingress
```

### Default Deny Egress

```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: default-deny-egress
  namespace: expenseflow-dev
spec:
  podSelector: {}
  policyTypes:
    - Egress
```

### Allow Web App Routing Ingress

```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: allow-web-app-routing
  namespace: expenseflow-dev
spec:
  podSelector: {}
  policyTypes:
    - Ingress
  ingress:
    - from:
        - namespaceSelector:
            matchLabels:
              name: app-routing-system
```

### Allow Same Namespace

```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: allow-same-namespace
  namespace: expenseflow-dev
spec:
  podSelector: {}
  policyTypes:
    - Ingress
    - Egress
  ingress:
    - from:
        - namespaceSelector:
            matchLabels:
              name: expenseflow-dev
  egress:
    - to:
        - namespaceSelector:
            matchLabels:
              name: expenseflow-dev
```

### Allow DNS Egress

```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: allow-dns-egress
  namespace: expenseflow-dev
spec:
  podSelector: {}
  policyTypes:
    - Egress
  egress:
    - ports:
        - protocol: UDP
          port: 53
```

---

## Resource Quotas

### Development Quota

```yaml
apiVersion: v1
kind: ResourceQuota
metadata:
  name: dev-quota
  namespace: expenseflow-dev
spec:
  hard:
    requests.cpu: "2"
    requests.memory: 4Gi
    limits.cpu: "4"
    limits.memory: 8Gi
    pods: "20"
    services: "10"
    persistentvolumeclaims: "5"
    configmaps: "15"
    secrets: "15"
```

### Staging Quota

```yaml
apiVersion: v1
kind: ResourceQuota
metadata:
  name: staging-quota
  namespace: expenseflow-staging
spec:
  hard:
    requests.cpu: "4"
    requests.memory: 8Gi
    limits.cpu: "8"
    limits.memory: 16Gi
    pods: "30"
    services: "15"
    persistentvolumeclaims: "10"
```

---

## LimitRange

```yaml
apiVersion: v1
kind: LimitRange
metadata:
  name: pod-limits
  namespace: expenseflow-dev
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
      min:
        cpu: 10m
        memory: 32Mi
```

---

## Azure Resources (External)

### Blob Storage Container

| Property | Value |
|----------|-------|
| Storage Account | `ccproctemp2025` |
| Container Name | `expenseflow-receipts` |
| Access Level | Private |
| Structure | `receipts/{userId}/{year}/{month}/{filename}` |

### Key Vault Secrets

| Secret Name | Purpose |
|-------------|---------|
| `expenseflow-db-password` | PostgreSQL admin password |
| `storage-connection-string` | Blob storage access |

---

## Relationships Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         app-routing-system                               │
│                         (NGINX Ingress)                                  │
│                               │                                          │
│              ┌────────────────┼────────────────┐                        │
│              ▼                ▼                ▼                        │
│     dev.expense.ii-us.com  studio.expense.ii-us.com  staging...         │
│              │                │                                          │
│              ▼                ▼                                          │
│  ┌───────────────────────────────────────────┐  ┌────────────────────┐  │
│  │           expenseflow-dev                  │  │expenseflow-staging│  │
│  │                                            │  │                   │  │
│  │  ┌──────────────────────────────────────┐  │  │  [Same structure] │  │
│  │  │           Supabase Stack             │  │  │                   │  │
│  │  │  ┌────────┐  ┌────────┐  ┌────────┐  │  │  │                   │  │
│  │  │  │ Studio │  │  Kong  │  │  REST  │  │  │  │                   │  │
│  │  │  └────┬───┘  └────┬───┘  └────┬───┘  │  │  │                   │  │
│  │  │       │           │           │      │  │  │                   │  │
│  │  │       └───────────┼───────────┘      │  │  │                   │  │
│  │  │                   ▼                  │  │  │                   │  │
│  │  │            ┌──────────────┐          │  │  │                   │  │
│  │  │            │  PostgreSQL  │          │  │  │                   │  │
│  │  │            │  + pgvector  │          │  │  │                   │  │
│  │  │            └──────┬───────┘          │  │  │                   │  │
│  │  │                   │                  │  │  │                   │  │
│  │  │                   ▼                  │  │  │                   │  │
│  │  │            ┌──────────────┐          │  │  │                   │  │
│  │  │            │  PVC (20GB)  │          │  │  │                   │  │
│  │  │            └──────────────┘          │  │  │                   │  │
│  │  └──────────────────────────────────────┘  │  │                   │  │
│  └────────────────────────────────────────────┘  └───────────────────┘  │
│                                                                          │
│         ┌───────────────────────────────────────┐                       │
│         │           Azure Resources             │                       │
│         │  ┌─────────────┐  ┌───────────────┐   │                       │
│         │  │ Key Vault   │  │ Blob Storage  │   │                       │
│         │  │ (iius-akv)  │  │(ccproctemp2025)│  │                       │
│         │  └─────────────┘  └───────────────┘   │                       │
│         └───────────────────────────────────────┘                       │
└─────────────────────────────────────────────────────────────────────────┘
```
