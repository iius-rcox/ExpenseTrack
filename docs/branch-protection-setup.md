# Branch Protection Setup

This document describes the recommended branch protection rules for the ExpenseFlow repository.

## Main Branch Protection

Navigate to: **Settings** → **Branches** → **Add rule** for `main` branch.

### Required Settings

| Setting | Value | Purpose |
|---------|-------|---------|
| **Require a pull request before merging** | ✅ Enabled | Prevents direct pushes to main |
| **Require approvals** | 1 | At least one reviewer approval |
| **Dismiss stale pull request approvals** | ✅ Enabled | Re-review after new commits |
| **Require status checks to pass** | ✅ Enabled | CI must pass before merge |
| **Require branches to be up to date** | ✅ Enabled | Must rebase on latest main |

### Required Status Checks

Add these required status checks:

1. **merge-gate** (from `ci-full.yml`)
   - Aggregates all test results
   - Must pass for merge

2. **backend-tests** (from `ci-full.yml`)
   - .NET unit, contract, property, and integration tests

3. **frontend-tests** (from `ci-full.yml`)
   - Vitest unit tests with coverage

4. **coverage-gate** (from `ci-full.yml`)
   - Enforces 80% coverage threshold

### Optional but Recommended

| Setting | Value | Purpose |
|---------|-------|---------|
| **Require conversation resolution** | ✅ Enabled | All comments must be resolved |
| **Require signed commits** | Optional | Enhanced security |
| **Include administrators** | ✅ Enabled | No bypassing rules |

## Codecov Integration

### Setup

1. Add repository to [Codecov](https://codecov.io)
2. Add `CODECOV_TOKEN` to repository secrets
3. Configure coverage thresholds in `.github/codecov.yml`

### Codecov Configuration

```yaml
# .github/codecov.yml
coverage:
  precision: 2
  round: down
  status:
    project:
      default:
        target: 80%
        threshold: 1%
    patch:
      default:
        target: 80%

comment:
  layout: "reach,diff,flags,files"
  behavior: default
  require_changes: true
```

## Workflow Summary

```
Developer commits → ci-quick.yml (< 3 min)
       ↓
Opens PR to main → ci-full.yml (< 15 min)
       ↓
   merge-gate passes → Merge enabled
       ↓
   Nightly → ci-nightly.yml (chaos + load tests)
```

## Troubleshooting

### Status checks not appearing

1. Ensure workflow has run at least once on the branch
2. Check workflow file syntax with `actionlint`
3. Verify job names match exactly in branch protection settings

### Coverage gate failing

1. Check Codecov dashboard for coverage breakdown
2. Review uncovered lines in PR diff
3. Add tests for new code paths

### Flaky test failures

1. Check test quarantine list
2. Review test logs for timing issues
3. Consider adding retry logic for inherently flaky operations
