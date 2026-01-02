# Codebase Issues - Pre-existing Problems

Consolidated list of pre-existing issues identified during Feature 021 development.

**Generated**: 2026-01-02

---

## Summary

| Category | Count | Blocking CI? |
|----------|-------|--------------|
| Frontend Lint Errors | 4 | **Yes** |
| Frontend Lint Warnings | 38 | **Yes** (max-warnings 0) |
| Backend Warnings | 5 | No |
| CI Configuration Issues | 1 | **Yes** |

---

## 1. Frontend Lint Errors (Blocking CI)

These **4 errors** must be fixed to pass CI.

### 1.1 React Hooks Rules Violations

| File | Line | Error |
|------|------|-------|
| `spending-trend-chart.tsx` | 289 | `useMemo` called conditionally after early return |
| `mobile-nav.tsx` | 235 | `React.useState` called conditionally |
| `mobile-nav.tsx` | 237 | `React.useEffect` called conditionally |
| `confidence-indicator.test.tsx` | 64 | Use `const` instead of `let` for `filledDots` |

**Fix**: Move all hooks before any conditional returns.

```tsx
// BAD - hook after early return
function Component({ data }) {
  if (!data) return null;
  const memoized = useMemo(() => ..., [data]); // ERROR
}

// GOOD - hook before early return
function Component({ data }) {
  const memoized = useMemo(() => data ? transform(data) : null, [data]);
  if (!data) return null;
  return <div>{memoized}</div>;
}
```

---

## 2. Frontend Lint Warnings (38 total)

Grouped by type. CI fails because `--max-warnings 0` is set.

### 2.1 Fast Refresh Warnings (7)

Files exporting non-component values alongside components:

| File | Line |
|------|------|
| `matching-factors.tsx` | 257 |
| `mobile-nav.tsx` | 231 |
| `swipe-action-row.tsx` | 293 |
| `badge.tsx` | 40 |
| `button.tsx` | 59 |
| `sidebar.tsx` | 757 |

**Fix**: Move constants/functions to separate files.

### 2.2 Exhaustive Dependencies (2)

| File | Line | Missing Dependency |
|------|------|-------------------|
| `StatementUpload.tsx` | 99 | `getToken` |
| `StatementImportPage.tsx` | 100 | `getToken` |

**Fix**: Add `getToken` to dependency array or use `useCallback` with proper deps.

### 2.3 Unused Variables (29)

**Source files (2)**:
| File | Line | Variable |
|------|------|----------|
| `api.ts` | 42 | `error` |
| `receipt-upload.spec.ts` | 2 | `path` |

**Test files (27)**: Various unused imports in test files:
- `tests/setup.ts`: 11 unused Framer Motion props
- Multiple test files: unused `vi`, `fireEvent`, `waitFor`, `act`, `userEvent`

**Fix**: Prefix unused variables with `_` or remove them.

---

## 3. Backend Warnings (Non-blocking)

| File | Line | Warning |
|------|------|---------|
| `ReportServiceTests.cs` | 1460 | Nullability mismatch in mock setup |
| `MatchingServiceTests.cs` | 162, 164 | Unused variables `nearPoints`, `nearTolerance` |
| `FuzzyMatchingServiceTests.cs` | 212 | Unused parameter `minSimilarity` in Theory |
| `ContractTestBase.cs` | 257 | Async method lacks await |

---

## 4. CI Configuration Issue

### 4.1 NuGet Cache Failure

**Workflow**: `.github/workflows/ci-full.yml`

```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: 9.0.x
    cache: true  # Expects packages.lock.json
```

**Error**: `Dependencies lock file is not found`

**Fix Options**:
1. Remove `cache: true` (simplest)
2. Generate lock files: `dotnet restore --use-lock-file`
3. Add `packages.lock.json` to all projects

---

## Recommended Fix Order

### Quick Wins (30 min)

1. **CI Config**: Remove `cache: true` from workflows
2. **Test setup.ts**: Prefix unused Framer Motion props with `_`
3. **const vs let**: Change `let filledDots` to `const`

### Medium Effort (2 hours)

4. **Unused test imports**: Remove or use the imports
5. **Exhaustive deps**: Fix `getToken` dependencies
6. **Fast refresh**: Extract constants to separate files

### Requires Refactoring (4 hours)

7. **React hooks violations**: Restructure components to call hooks unconditionally

---

## Commands

```bash
# Run frontend lint
cd frontend && npm run lint

# Auto-fix what's possible
cd frontend && npm run lint -- --fix

# Build backend and check warnings
docker run --rm -v $(pwd):/app -w /app/backend mcr.microsoft.com/dotnet/sdk:9.0 \
  dotnet build ExpenseFlow.sln -c Release 2>&1 | grep warning
```

---

## Files to Modify

### Critical Path (fix these first)

1. `frontend/src/components/analytics/spending-trend-chart.tsx`
2. `frontend/src/components/mobile/mobile-nav.tsx`
3. `frontend/tests/unit/components/confidence-indicator.test.tsx`
4. `.github/workflows/ci-full.yml`
5. `.github/workflows/ci-quick.yml`

### Secondary

6. `frontend/tests/setup.ts`
7. `frontend/src/components/statements/StatementUpload.tsx`
8. `frontend/src/pages/StatementImportPage.tsx`
9. All test files with unused imports
