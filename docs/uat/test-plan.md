# ExpenseFlow UAT Test Plan

**Version**: 1.0
**Created**: 2025-12-17
**Sprint**: 10 - Testing & Cache Warming
**Status**: In Progress

## 1. Introduction

### 1.1 Purpose
This User Acceptance Testing (UAT) plan defines the testing approach, scope, and criteria for validating ExpenseFlow before production deployment. UAT ensures all features developed in Sprints 3-9 meet business requirements from an end-user perspective.

### 1.2 Scope
Testing covers the 7 critical user workflows identified in the Sprint 10 specification:
1. Receipt Upload Flow (Sprint 3)
2. Statement Import (Sprint 4)
3. Receipt-to-Transaction Matching (Sprint 5)
4. AI Categorization (Sprint 6)
5. Travel Period Detection (Sprint 7)
6. Draft Report Generation (Sprint 8)
7. Month-over-Month Comparison (Sprint 9)

### 1.3 References
- Feature Specification: `specs/010-testing-cache-warming/spec.md`
- Implementation Plan: `specs/010-testing-cache-warming/plan.md`
- Previous Sprint Specifications: `specs/003-*` through `specs/009-*`

## 2. Test Environment

### 2.1 Staging Environment
- **Namespace**: `expenseflow-staging`
- **URL**: https://expenseflow-staging.yourdomain.com (TBD after deployment)
- **Database**: PostgreSQL 15 with pgvector (isolated staging instance)
- **Storage**: Azure Blob Storage (staging container)
- **Authentication**: Entra ID (staging tenant/app registration)

### 2.2 Pre-requisites
- [x] Staging Kubernetes manifests created (`infrastructure/kubernetes/staging/`)
- [x] Cache warming implemented and tested
- [ ] Staging environment deployed and health checks passing (T010)
- [ ] Cache warmed with historical data (T062)
- [ ] Test user accounts provisioned in Entra ID

### 2.3 Test Data Requirements
- Historical expense data (6 months minimum) for cache warming
- Sample receipt images (various formats: JPG, PNG, PDF, HEIC)
- Sample bank/credit card statements (CSV, Excel)
- Test user credentials with appropriate permissions

## 3. Test Team

| Role | Name | Responsibilities |
|------|------|------------------|
| Test Lead | TBD | Overall test coordination, sign-off approval |
| Tester 1 | TBD | TC-001, TC-002, TC-003 execution |
| Tester 2 | TBD | TC-004, TC-005 execution |
| Tester 3 | TBD | TC-006, TC-007 execution |
| Developer | TBD | Defect triage and fixes |

**Minimum Testers Required**: 3 (per SC-004)

## 4. Test Cases

### 4.1 Test Case Summary

| ID | Name | Priority | Sprint | Status | Assigned To |
|----|------|----------|--------|--------|-------------|
| TC-001 | Receipt Upload Flow | Critical | 3 | Not Started | - |
| TC-002 | Statement Import | Critical | 4 | Not Started | - |
| TC-003 | Receipt-to-Transaction Matching | Critical | 5 | Not Started | - |
| TC-004 | AI Categorization | High | 6 | Not Started | - |
| TC-005 | Travel Period Detection | Medium | 7 | Not Started | - |
| TC-006 | Draft Report Generation | Critical | 8 | Not Started | - |
| TC-007 | Month-over-Month Comparison | High | 9 | Not Started | - |

### 4.2 Test Case Files
- `test-cases/TC-001-receipt-upload.md`
- `test-cases/TC-002-statement-import.md`
- `test-cases/TC-003-matching.md`
- `test-cases/TC-004-categorization.md`
- `test-cases/TC-005-travel-detection.md`
- `test-cases/TC-006-report-generation.md`
- `test-cases/TC-007-mom-comparison.md`

## 5. Entry Criteria

Before UAT can begin:
- [ ] All Sprint 3-9 development complete
- [ ] Unit tests passing (>80% coverage)
- [ ] Staging environment deployed and accessible
- [ ] Cache warming completed with >500 descriptions cached
- [ ] Test data prepared and available
- [ ] Test team identified and available

## 6. Exit Criteria

UAT is complete when:
- [ ] All 7 test cases executed (100% coverage per SC-002)
- [ ] All P1 (Critical) defects resolved and verified (SC-003)
- [ ] All P2 (High) defects resolved and verified (SC-003)
- [ ] Sign-off obtained from at least 3 test users (SC-004)
- [ ] UAT summary report generated

## 7. Defect Management

### 7.1 Priority Definitions

| Priority | Definition | Resolution Target |
|----------|------------|-------------------|
| P1 - Critical | System unusable, data loss, security issue | Immediate (within hours) |
| P2 - High | Major feature broken, no workaround | Before UAT sign-off |
| P3 - Medium | Feature issue with workaround available | Next release |
| P4 - Low | Cosmetic, minor inconvenience | Backlog |

### 7.2 Defect Workflow
1. Tester discovers issue
2. Tester documents defect in `defects/` folder
3. Developer triages and assigns priority
4. Developer fixes defect
5. Tester verifies fix
6. Defect marked as Verified/Closed

### 7.3 Defect Tracking
Defects are tracked in two locations:
- Detailed documentation: `docs/uat/defects/DEF-XXX.md`
- GitHub Issues: Created with `uat-defect` label and priority label

## 8. Schedule

| Phase | Tasks | Duration | Status |
|-------|-------|----------|--------|
| Preparation | Deploy staging, warm cache, provision users | Day 1 | Pending |
| Execution | Execute TC-001 through TC-007 | Days 2-4 | Pending |
| Defect Resolution | Fix P1/P2 defects, re-test | Days 5-6 | Pending |
| Sign-off | Collect user approvals | Day 7 | Pending |

## 9. Risks and Mitigations

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Test users unavailable | High | Medium | Identify backup testers |
| Staging environment issues | High | Low | Validate deployment before UAT start |
| Cache warming fails | Medium | Low | Test cache warming in dev first |
| Too many P1 defects | High | Medium | Prioritize critical paths in testing |

## 10. UAT Summary Report

*To be completed after test execution (T068)*

### 10.1 Execution Summary

| Metric | Value |
|--------|-------|
| Total Test Cases | 7 |
| Executed | - |
| Passed | - |
| Failed | - |
| Blocked | - |
| Pass Rate | - |

### 10.2 Defect Summary

| Priority | Found | Fixed | Verified | Open |
|----------|-------|-------|----------|------|
| P1 - Critical | - | - | - | - |
| P2 - High | - | - | - | - |
| P3 - Medium | - | - | - | - |
| P4 - Low | - | - | - | - |
| **Total** | - | - | - | - |

### 10.3 Sign-off

| Tester | Date | Approved | Comments |
|--------|------|----------|----------|
| Tester 1 | - | [ ] | - |
| Tester 2 | - | [ ] | - |
| Tester 3 | - | [ ] | - |

**UAT Status**: Pending

---

*Document maintained by: Development Team*
*Last updated: 2025-12-17*
