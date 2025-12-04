# Tasks: Infrastructure Setup

**Input**: Design documents from `/specs/001-infrastructure-setup/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/validation-tests.md, quickstart.md

**Tests**: Infrastructure validation tests defined in contracts/validation-tests.md - executed after each phase checkpoint.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions

Infrastructure-as-Code project structure:
```text
infrastructure/
├── namespaces/
├── cert-manager/
├── supabase/          # Supabase self-hosted (PostgreSQL + Studio, Auth/Storage disabled)
├── storage/
├── monitoring/
└── scripts/
```

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create project structure and add Helm repositories

- [x] T001 Create infrastructure directory structure per plan.md
- [x] T002 [P] Create namespace manifest in infrastructure/namespaces/expenseflow-dev.yaml
- [x] T003 [P] Create namespace manifest in infrastructure/namespaces/expenseflow-staging.yaml
- [x] T004 Add Helm repositories (jetstack, supabase) and verify access
- [x] T005 Install cert-manager v1.19.1 to cert-manager namespace via Helm

**Checkpoint**: cert-manager installed - cluster is ready for resource deployment

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Namespace creation and core configuration that MUST be complete before ANY user story

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T007 Apply namespace manifests to create expenseflow-dev and expenseflow-staging
- [x] T008 [P] Create resource quota manifest in infrastructure/namespaces/resource-quotas.yaml (dev: 2 CPU, 4GB)
- [x] T009 [P] Create staging resource quota in infrastructure/namespaces/resource-quotas.yaml (staging: 4 CPU, 8GB)
- [x] T010 [P] Create LimitRange manifest in infrastructure/namespaces/limit-range.yaml
- [x] T011 Apply resource quotas and limit ranges to namespaces
- [x] T012 Verify namespace labels are correctly set (name label for network policy selectors)

**Checkpoint**: Foundation ready - namespaces exist with quotas applied, user story implementation can begin

---

## Phase 3: User Story 1 - Secure Web Access (Priority: P1) 🎯 MVP

**Goal**: Enable HTTPS access with valid Let's Encrypt TLS certificates

**Independent Test**: Navigate to `https://dev.expense.ii-us.com` and verify browser shows valid certificate with no security warnings

### Implementation for User Story 1

- [x] T013 [P] [US1] Create ClusterIssuer manifest (letsencrypt-staging) in infrastructure/cert-manager/cluster-issuer.yaml
- [x] T014 [P] [US1] Create ClusterIssuer manifest (letsencrypt-prod) in infrastructure/cert-manager/cluster-issuer.yaml
- [x] T015 [US1] Apply ClusterIssuer manifests and verify READY=True status
- [x] T016 [US1] Create Certificate manifest for dev.expense.ii-us.com in infrastructure/cert-manager/certificate.yaml
- [x] T017 [US1] Document DNS CNAME configuration requirement in quickstart.md (manual step)
- [x] T018 [US1] Apply Certificate manifest (after DNS configured) and verify issuance

**Validation** (from contracts/validation-tests.md):
- [x] T019 [US1] Validate cert-manager pods are Running
- [x] T020 [US1] Validate ClusterIssuers show READY=True
- [x] T021 [US1] Validate Certificate issued and Secret created

**Checkpoint**: User Story 1 complete - HTTPS access works with valid Let's Encrypt certificate

---

## Phase 4: User Story 2 - Database Availability (Priority: P1)

**Goal**: Provide PostgreSQL 15 database with pgvector extension and Supabase Studio accessible within cluster

**Independent Test**: Connect to PostgreSQL from within cluster, run `\dx vector` to verify pgvector extension, access Supabase Studio UI

### Implementation for User Story 2

- [x] T022 [P] [US2] Create Supabase Helm values in infrastructure/supabase/values.yaml (Auth/Storage disabled)
- [x] T023 [P] [US2] Create backup PVC manifest in infrastructure/supabase/backup-pvc.yaml
- [x] T024 [P] [US2] Create backup CronJob manifest in infrastructure/supabase/backup-cronjob.yaml
- [x] T025 [US2] Install Supabase via Helm to expenseflow-dev namespace with custom values
- [x] T026 [US2] Wait for all Supabase pods to reach Running status (~5-6 pods)
- [x] T027 [US2] Verify pgvector extension is enabled

**Validation** (from contracts/validation-tests.md):
- [x] T028 [US2] Validate all Supabase pods are Running (postgresql, kong, rest, realtime, meta, studio)
- [x] T029 [US2] Validate PostgreSQL connection succeeds from test pod
- [x] T030 [US2] Validate pgvector extension is listed (\dx vector)
- [x] T031 [US2] Validate vector similarity query works (INSERT and ORDER BY <->)
- [x] T032 [US2] Apply backup CronJob and verify it exists
- [x] T033 [US2] Validate Supabase Studio is accessible via port-forward or ingress

**Checkpoint**: User Story 2 complete - PostgreSQL with pgvector and Studio UI are available

---

## Phase 5: User Story 3 - Document Storage (Priority: P2)

**Goal**: Configure Azure Blob Storage container for receipt and document files

**Independent Test**: Upload a test file via Azure CLI to expenseflow-receipts container and retrieve it successfully

### Implementation for User Story 3

- [x] T034 [P] [US3] Create blob container setup script in infrastructure/storage/blob-container-setup.ps1
- [x] T035 [US3] Execute blob container creation (az storage container create for expenseflow-receipts)
- [x] T036 [US3] Verify container exists in storage account ccproctemp2025

**Validation** (from contracts/validation-tests.md):
- [x] T037 [US3] Validate blob upload succeeds (test file)
- [x] T038 [US3] Validate blob download succeeds (retrieve test file)
- [x] T039 [US3] Cleanup test file from container

**Checkpoint**: User Story 3 complete - Blob storage is accessible for receipt uploads

---

## Phase 6: User Story 4 - Environment Separation (Priority: P2)

**Goal**: Implement network isolation between namespaces with zero-trust policies

**Independent Test**: Deploy test pods in dev and staging, verify cross-namespace communication is blocked while same-namespace works

### Implementation for User Story 4

- [x] T040 [P] [US4] Create default-deny-ingress NetworkPolicy in infrastructure/namespaces/network-policies.yaml
- [x] T041 [P] [US4] Create default-deny-egress NetworkPolicy in infrastructure/namespaces/network-policies.yaml
- [x] T042 [P] [US4] Create allow-web-app-routing NetworkPolicy in infrastructure/namespaces/network-policies.yaml
- [x] T043 [P] [US4] Create allow-same-namespace NetworkPolicy in infrastructure/namespaces/network-policies.yaml
- [x] T044 [P] [US4] Create allow-dns-egress NetworkPolicy in infrastructure/namespaces/network-policies.yaml
- [x] T045 [US4] Apply all network policies to expenseflow-dev namespace
- [x] T046 [US4] Apply all network policies to expenseflow-staging namespace

**Validation** (from contracts/validation-tests.md):
- [x] T047 [US4] Validate default deny blocks external access (test pod wget to kubernetes.default)
- [x] T048 [US4] Validate same-namespace communication allowed (deploy nginx, wget from same ns)
- [x] T049 [US4] Validate cross-namespace communication blocked (wget from staging to dev)
- [x] T050 [US4] Cleanup test pods and services

**Checkpoint**: User Story 4 complete - Zero-trust network policies enforce namespace isolation

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Monitoring, validation scripts, and documentation

- [x] T051 [P] Create deployment script in infrastructure/scripts/deploy-all.ps1
- [x] T052 [P] Create validation script in infrastructure/scripts/validate-deployment.ps1
- [x] T053 [P] Create connectivity test script in infrastructure/scripts/test-connectivity.ps1
- [x] T054 [P] Configure Container Insights alerts in infrastructure/monitoring/alerts.yaml
- [x] T055 Run full validation script and document results
- [x] T056 Update CLAUDE.md with any additional learnings
- [x] T057 Verify all infrastructure costs remain under $25/month target

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-6)**: All depend on Foundational phase completion
  - US1 (TLS) and US2 (Database) can proceed in parallel (both P1)
  - US3 (Storage) and US4 (Network Policies) can proceed in parallel (both P2)
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - Independent
- **User Story 2 (P1)**: Can start after Foundational (Phase 2) - Independent
- **User Story 3 (P2)**: Can start after Foundational (Phase 2) - Independent
- **User Story 4 (P2)**: Can start after Foundational (Phase 2) - Independent

All user stories are fully independent and can be implemented/tested in any order.

### Within Each User Story

- Create manifests before applying them
- Apply resources before validating them
- Core implementation before validation tests
- Story complete before moving to next priority

### Parallel Opportunities

**Phase 1 (Setup):**
- T002, T003 (namespace manifests) can run in parallel

**Phase 2 (Foundational):**
- T008, T009, T010 (quota and limit manifests) can run in parallel

**User Stories:**
- US1 and US2 can run in parallel (both P1, independent)
- US3 and US4 can run in parallel (both P2, independent)
- Within US1: T013, T014 (ClusterIssuer manifests) can run in parallel
- Within US2: T022, T023, T024 (Supabase values and backup manifests) can run in parallel
- Within US4: T040-T044 (all network policy manifests) can run in parallel

---

## Parallel Example: User Story 1

```powershell
# Launch ClusterIssuer manifest creation in parallel:
# Task T013: "Create ClusterIssuer manifest (letsencrypt-staging)"
# Task T014: "Create ClusterIssuer manifest (letsencrypt-prod)"

# Both can be written simultaneously as they're in the same file but separate resources
```

## Parallel Example: User Story 2

```powershell
# Launch Supabase config files in parallel:
# Task T022: "Create Supabase Helm values (Auth/Storage disabled)"
# Task T023: "Create backup PVC manifest"
# Task T024: "Create backup CronJob manifest"

# All can be written simultaneously as they're separate files
```

## Parallel Example: User Story 4

```powershell
# Launch all network policy manifests in parallel:
# Task T040: "Create default-deny-ingress NetworkPolicy"
# Task T041: "Create default-deny-egress NetworkPolicy"
# Task T042: "Create allow-web-app-routing NetworkPolicy"
# Task T043: "Create allow-same-namespace NetworkPolicy"
# Task T044: "Create allow-dns-egress NetworkPolicy"

# All can be written to network-policies.yaml simultaneously
```

---

## Implementation Strategy

### MVP First (User Story 1 + User Story 2)

1. Complete Phase 1: Setup (operators installed)
2. Complete Phase 2: Foundational (namespaces with quotas)
3. Complete Phase 3: User Story 1 (TLS certificates working)
4. Complete Phase 4: User Story 2 (Database available)
5. **STOP and VALIDATE**: Both P1 stories independently testable
6. Deploy/demo if ready - core infrastructure functional

### Incremental Delivery

1. Setup + Foundational → Foundation ready (~10 min)
2. Add User Story 1 → Test HTTPS → Secure ingress works (~10 min)
3. Add User Story 2 → Test database + Studio → Supabase available (~10 min)
4. Add User Story 3 → Test blob → Storage ready (~5 min)
5. Add User Story 4 → Test isolation → Network policies active (~10 min)
6. Polish → Full validation → Production ready

### Single Developer Strategy

Execute in priority order:
1. Phase 1 + Phase 2 → Foundation
2. Phase 3 (US1: TLS) → Validate
3. Phase 4 (US2: Database) → Validate
4. Phase 5 (US3: Storage) → Validate
5. Phase 6 (US4: Network) → Validate
6. Phase 7 (Polish) → Full validation

---

## Notes

- [P] tasks = different files or independent resources, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story is independently completable and testable
- Infrastructure manifests reference data-model.md for exact YAML
- Validation tests reference contracts/validation-tests.md for test commands
- All kubectl commands should target the correct namespace
- DNS configuration for TLS is a manual external step (GoDaddy)
- Commit after each phase completion
- Stop at any checkpoint to validate story independently
