# UAT User Story 5: Test Results Validation and Reporting

**User Story**: As a tester using Claude Code, I want clear pass/fail JSON output with execution timing and error details, so that I can quickly understand test outcomes and diagnose failures.

**Priority**: P3
**Independent Test**: Yes - can be documented in parallel with US1-US4

---

## Purpose

This document defines the standardized JSON output format that Claude Code should produce when executing UAT tests. Consistent output enables:

1. **Automated CI/CD Integration**: Parse results in pipelines
2. **Trend Analysis**: Track test health over time
3. **Failure Diagnosis**: Pinpoint exact assertion failures
4. **Execution Metrics**: Monitor performance regressions

---

## Test Result JSON Schema

### Top-Level Structure

```json
{
  "testRun": {
    "id": "uuid",
    "startedAt": "2025-12-19T10:00:00Z",
    "completedAt": "2025-12-19T10:05:30Z",
    "durationMs": 330000,
    "environment": {
      "apiUrl": "https://staging.expense.ii-us.com/api",
      "apiVersion": "1.0.0"
    }
  },
  "scenarios": [
    { /* US-1 results */ },
    { /* US-2 results */ },
    { /* US-3 results */ },
    { /* US-4 results */ }
  ],
  "summary": {
    "totalScenarios": 4,
    "passedScenarios": 4,
    "failedScenarios": 0,
    "skippedScenarios": 0,
    "totalTests": 19,
    "passedTests": 18,
    "failedTests": 1,
    "skippedTests": 0,
    "successRate": 0.947
  }
}
```

---

## Specific Test Cases

### US-5.1: Test Result JSON Schema Validation

**Given**: A complete test run has finished
**When**: Claude Code generates the output
**Then**: Output should conform to the defined schema

**Schema Requirements**:

```json
{
  "testRun": {
    "id": "required, uuid format",
    "startedAt": "required, ISO 8601 timestamp",
    "completedAt": "required, ISO 8601 timestamp",
    "durationMs": "required, positive integer"
  },
  "scenarios": "required, array of scenario objects",
  "summary": "required, object with counts"
}
```

**Assertions**:
```json
[
  { "field": "testRun.id", "expected": "uuid format" },
  { "field": "testRun.startedAt", "expected": "ISO 8601" },
  { "field": "testRun.durationMs", "expected": "> 0" },
  { "field": "scenarios", "expected": "array length >= 1" },
  { "field": "summary.totalScenarios", "expected": ">= 1" }
]
```

---

### US-5.2: Pass/Fail Summary Generation

**Given**: Individual test results from US1-US4
**When**: Claude Code aggregates results
**Then**: Summary should accurately reflect pass/fail counts

**Summary Object Schema**:

```json
{
  "summary": {
    "totalScenarios": 4,
    "passedScenarios": 4,
    "failedScenarios": 0,
    "skippedScenarios": 0,
    "totalTests": 19,
    "passedTests": 18,
    "failedTests": 1,
    "skippedTests": 0,
    "successRate": 0.947
  }
}
```

**Calculation Logic**:
```pseudocode
passedScenarios = count(scenarios where status == "passed")
failedScenarios = count(scenarios where status == "failed")
skippedScenarios = count(scenarios where status == "skipped")
totalScenarios = passedScenarios + failedScenarios + skippedScenarios

passedTests = sum(scenario.summary.passed for all scenarios)
failedTests = sum(scenario.summary.failed for all scenarios)
skippedTests = sum(scenario.summary.skipped for all scenarios)
totalTests = passedTests + failedTests + skippedTests

successRate = passedTests / totalTests (0 if totalTests == 0)
```

**Assertions**:
```json
[
  { "field": "summary.totalScenarios", "expected": "== scenarios.length" },
  { "field": "summary.totalTests", "expected": "== sum of all test counts" },
  { "field": "summary.successRate", "expected": "0.0 to 1.0 range" }
]
```

---

### US-5.3: Failure Detail Format

**Given**: A test assertion has failed
**When**: Claude Code reports the failure
**Then**: Error should include expected vs actual values with context

**Failure Object Schema**:

```json
{
  "name": "RDU Receipt Amount Validation",
  "scenario": "US-1.3",
  "status": "failed",
  "durationMs": 12340,
  "error": {
    "message": "Amount extraction mismatch",
    "expected": 84.00,
    "actual": 48.00,
    "tolerance": 0.01,
    "diff": -36.00,
    "context": {
      "receiptFile": "20251211_212334.jpg",
      "receiptId": "uuid",
      "ocrConfidence": 0.65
    }
  },
  "assertions": [
    { "field": "status", "expected": "Ready", "actual": "Ready", "passed": true },
    { "field": "amount", "expected": 84.00, "actual": 48.00, "tolerance": 0.01, "passed": false },
    { "field": "date", "expected": "2025-12-11", "actual": "2025-12-11", "passed": true }
  ]
}
```

**Error Object Requirements**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `message` | string | Yes | Human-readable error description |
| `expected` | any | Yes | The expected value |
| `actual` | any | Yes | The actual value received |
| `tolerance` | number | No | For numeric comparisons |
| `diff` | number | No | Difference for numeric values |
| `context` | object | No | Additional diagnostic information |

**Assertions**:
```json
[
  { "field": "error.message", "expected": "not empty string" },
  { "field": "error.expected", "expected": "present" },
  { "field": "error.actual", "expected": "present" }
]
```

---

### US-5.4: Execution Timing Reporting

**Given**: Tests are executing
**When**: Each test completes
**Then**: Duration should be recorded in milliseconds

**Timing Fields**:

```json
{
  "testRun": {
    "startedAt": "2025-12-19T10:00:00Z",
    "completedAt": "2025-12-19T10:05:30Z",
    "durationMs": 330000
  },
  "scenarios": [
    {
      "scenario": "US-1",
      "durationMs": 145000,
      "results": [
        {
          "name": "Receipt Upload",
          "durationMs": 12340
        }
      ]
    }
  ]
}
```

**Timing Requirements**:
- All `durationMs` values are positive integers
- Test run duration >= sum of scenario durations (parallel execution allowed)
- Individual test duration includes polling/wait time

**Assertions**:
```json
[
  { "field": "testRun.durationMs", "expected": "> 0" },
  { "field": "scenario.durationMs", "expected": "> 0" },
  { "field": "test.durationMs", "expected": "> 0" }
]
```

---

### US-5.5: Skipped Test Handling

**Given**: A test depends on a failed prerequisite
**When**: The test cannot run
**Then**: Status should be "skipped" with reason

**Skipped Test Schema**:

```json
{
  "name": "Auto-Match Execution",
  "scenario": "US-3.1",
  "status": "skipped",
  "skipReason": "Dependency 'Receipt Upload (US-1)' failed",
  "dependsOn": ["US-1.1", "US-2.1"],
  "assertions": []
}
```

**Skip Reason Categories**:

| Reason | Description |
|--------|-------------|
| Dependency failed | Prerequisite test failed |
| Dependency skipped | Prerequisite was also skipped |
| Environment unavailable | Required service not responding |
| Configuration missing | Required config/data not found |

**Assertions**:
```json
[
  { "field": "status", "expected": "skipped" },
  { "field": "skipReason", "expected": "not empty string" },
  { "field": "assertions", "expected": "empty array" }
]
```

---

## Scenario Result Format

Each user story produces a scenario result object:

```json
{
  "scenario": "US-1",
  "name": "Receipt Upload and OCR Processing",
  "status": "passed",
  "durationMs": 145000,
  "results": [
    {
      "name": "Receipt Upload - 20251211_212334.jpg",
      "scenario": "US-1.3",
      "status": "passed",
      "durationMs": 12340,
      "assertions": [
        { "field": "status", "expected": "Ready", "actual": "Ready", "passed": true },
        { "field": "amount", "expected": 84.00, "actual": 84.00, "tolerance": 0.01, "passed": true }
      ]
    }
  ],
  "summary": {
    "total": 19,
    "passed": 18,
    "failed": 1,
    "skipped": 0
  }
}
```

**Scenario Status Rules**:
- `passed`: All tests passed
- `failed`: At least one test failed
- `skipped`: All tests skipped (dependency failure)

---

## Assertion Object Format

Individual assertions within a test:

```json
{
  "field": "amount",
  "expected": 84.00,
  "actual": 84.00,
  "tolerance": 0.01,
  "passed": true,
  "operator": "equals_within_tolerance"
}
```

**Supported Operators**:

| Operator | Description | Example |
|----------|-------------|---------|
| `equals` | Exact match | `expected: "Ready"` |
| `equals_within_tolerance` | Numeric with tolerance | `expected: 84.00, tolerance: 0.01` |
| `greater_than` | Value comparison | `expected: ">= 0.80"` |
| `less_than` | Value comparison | `expected: "< 120000"` |
| `not_null` | Existence check | `expected: "not null"` |
| `contains` | String/array contains | `expected: "contains 'RDU'"` |
| `matches` | Regex match | `expected: "matches /uuid/"` |

---

## Complete Test Run Example

```json
{
  "testRun": {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "startedAt": "2025-12-19T10:00:00Z",
    "completedAt": "2025-12-19T10:05:30Z",
    "durationMs": 330000,
    "environment": {
      "apiUrl": "https://staging.expense.ii-us.com/api",
      "apiVersion": "1.0.0"
    }
  },
  "scenarios": [
    {
      "scenario": "US-1",
      "name": "Receipt Upload and OCR Processing",
      "status": "passed",
      "durationMs": 145000,
      "results": [
        {
          "name": "Receipt Upload - 20251211_212334.jpg",
          "scenario": "US-1.3",
          "status": "passed",
          "durationMs": 12340,
          "assertions": [
            { "field": "status", "expected": "Ready", "actual": "Ready", "passed": true },
            { "field": "amount", "expected": 84.00, "actual": 84.00, "tolerance": 0.01, "passed": true },
            { "field": "date", "expected": "2025-12-11", "actual": "2025-12-11", "passed": true }
          ]
        }
      ],
      "summary": {
        "total": 19,
        "passed": 19,
        "failed": 0,
        "skipped": 0
      }
    },
    {
      "scenario": "US-2",
      "name": "Statement Import and Transaction Fingerprinting",
      "status": "passed",
      "durationMs": 8500,
      "results": [
        {
          "name": "Statement Import",
          "scenario": "US-2.1",
          "status": "passed",
          "durationMs": 5200,
          "assertions": [
            { "field": "imported", "expected": 484, "actual": 484, "passed": true },
            { "field": "skipped", "expected": 0, "actual": 0, "passed": true }
          ]
        }
      ],
      "summary": {
        "total": 4,
        "passed": 4,
        "failed": 0,
        "skipped": 0
      }
    },
    {
      "scenario": "US-3",
      "name": "Automated Receipt-Transaction Matching",
      "status": "passed",
      "durationMs": 3500,
      "results": [
        {
          "name": "RDU Receipt-Transaction Match",
          "scenario": "US-3.2",
          "status": "passed",
          "durationMs": 500,
          "assertions": [
            { "field": "matchExists", "expected": true, "actual": true, "passed": true },
            { "field": "confidenceScore", "expected": ">= 0.80", "actual": 0.92, "passed": true }
          ]
        }
      ],
      "summary": {
        "total": 4,
        "passed": 4,
        "failed": 0,
        "skipped": 0
      }
    },
    {
      "scenario": "US-4",
      "name": "End-to-End Workflow Validation",
      "status": "passed",
      "durationMs": 2500,
      "results": [
        {
          "name": "Draft Report Generation",
          "scenario": "US-4.1",
          "status": "passed",
          "durationMs": 1200,
          "assertions": [
            { "field": "status", "expected": "Draft", "actual": "Draft", "passed": true },
            { "field": "reportId", "expected": "not null", "actual": "uuid-value", "passed": true }
          ]
        }
      ],
      "summary": {
        "total": 4,
        "passed": 4,
        "failed": 0,
        "skipped": 0
      }
    }
  ],
  "summary": {
    "totalScenarios": 4,
    "passedScenarios": 4,
    "failedScenarios": 0,
    "skippedScenarios": 0,
    "totalTests": 31,
    "passedTests": 31,
    "failedTests": 0,
    "skippedTests": 0,
    "successRate": 1.0
  }
}
```

---

## Error Scenarios

### Partial Failure Example

When some tests fail but others pass:

```json
{
  "summary": {
    "totalScenarios": 4,
    "passedScenarios": 2,
    "failedScenarios": 1,
    "skippedScenarios": 1,
    "totalTests": 31,
    "passedTests": 25,
    "failedTests": 2,
    "skippedTests": 4,
    "successRate": 0.806
  }
}
```

### Cascade Failure Example

When a dependency failure causes downstream skips:

```json
{
  "scenarios": [
    {
      "scenario": "US-1",
      "status": "failed",
      "summary": { "total": 19, "passed": 0, "failed": 19, "skipped": 0 }
    },
    {
      "scenario": "US-2",
      "status": "passed",
      "summary": { "total": 4, "passed": 4, "failed": 0, "skipped": 0 }
    },
    {
      "scenario": "US-3",
      "status": "skipped",
      "skipReason": "Dependency 'Receipt Upload (US-1)' failed",
      "summary": { "total": 4, "passed": 0, "failed": 0, "skipped": 4 }
    },
    {
      "scenario": "US-4",
      "status": "skipped",
      "skipReason": "Dependency 'Matching (US-3)' skipped",
      "summary": { "total": 4, "passed": 0, "failed": 0, "skipped": 4 }
    }
  ]
}
```

---

## Output Validation Checklist

When Claude Code produces test results, verify:

- [ ] All required fields present in schema
- [ ] Timestamps are ISO 8601 format
- [ ] Duration values are positive integers
- [ ] Status values are one of: `passed`, `failed`, `skipped`
- [ ] Failed tests have `error` object with message, expected, actual
- [ ] Skipped tests have `skipReason` string
- [ ] Summary counts match actual test counts
- [ ] Success rate is between 0.0 and 1.0
