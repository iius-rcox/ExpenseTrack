# ExpenseFlow Load Test Report

**Test Date**: [YYYY-MM-DD]
**Environment**: Staging
**Tester**: [Name]
**Build Version**: [Version/Commit]

## 1. Executive Summary

### 1.1 Overall Results

| Test | Target | Actual | Status |
|------|--------|--------|--------|
| Batch Receipt Processing (50 receipts) | < 5 minutes | [X] minutes | Pass/Fail |
| Concurrent Users (20) P95 Response Time | < 2 seconds | [X] ms | Pass/Fail |
| Success Rate | > 95% | [X]% | Pass/Fail |

### 1.2 Recommendation
[PASS/FAIL with brief explanation]

---

## 2. Test Environment

### 2.1 Infrastructure
- **AKS Cluster**: dev-aks
- **Namespace**: expenseflow-staging
- **API Replicas**: [X]
- **Database**: PostgreSQL (Supabase)
- **Cache**: Description cache warmed with [X] entries

### 2.2 Configuration
- **API Base URL**: https://[staging-url]
- **Connection Pooling**: [X] max connections
- **Rate Limiting**: [Configuration]

### 2.3 Test Machine
- **OS**: [OS Version]
- **CPU**: [Cores]
- **Memory**: [GB]
- **Network**: [Bandwidth]

---

## 3. Test Scenarios

### 3.1 Batch Receipt Processing Test

**Objective**: Verify 50 receipts can be processed within 5 minutes (SC-005)

**Configuration**:
- Receipts uploaded: 50
- Concurrent upload threads: 5
- Test duration: [X] minutes

**Results**:

| Metric | Value |
|--------|-------|
| Total Receipts Uploaded | [X] |
| Successfully Processed | [X] |
| Failed | [X] |
| Total Processing Time | [X] minutes |
| Average Per-Receipt Time | [X] seconds |

**Observations**:
- [Key observation 1]
- [Key observation 2]

**Status**: Pass/Fail

---

### 3.2 Concurrent User Test

**Objective**: Verify P95 response time < 2s with 20 concurrent users (SC-006)

**Configuration**:
- Concurrent users: 20
- Warm-up duration: 30 seconds
- Sustained load duration: 3 minutes
- Operations: View receipts, transactions, reports, cache stats

**Results by Operation**:

| Operation | Requests | Success Rate | Mean (ms) | P95 (ms) | P99 (ms) |
|-----------|----------|--------------|-----------|----------|----------|
| View Receipts | [X] | [X]% | [X] | [X] | [X] |
| View Transactions | [X] | [X]% | [X] | [X] | [X] |
| View Reports | [X] | [X]% | [X] | [X] | [X] |
| View Cache Stats | [X] | [X]% | [X] | [X] | [X] |
| **Overall** | [X] | [X]% | [X] | [X] | [X] |

**Throughput**:
- Requests per second (RPS): [X]
- Peak RPS achieved: [X]

**Observations**:
- [Key observation 1]
- [Key observation 2]

**Status**: Pass/Fail

---

### 3.3 Mixed Workload Test

**Objective**: Simulate real-world usage patterns

**Traffic Distribution**:
- View receipts: 40%
- View transactions: 30%
- View reports: 15%
- View matches: 10%
- View travel periods: 5%

**Results**:

| Metric | Value |
|--------|-------|
| Total Requests | [X] |
| Success Rate | [X]% |
| Mean Latency | [X] ms |
| P95 Latency | [X] ms |
| RPS | [X] |

**Status**: Pass/Fail

---

### 3.4 Spike Test (Optional)

**Objective**: Verify system handles sudden load increases

**Profile**:
- Normal load: 10 RPS
- Spike load: 50 RPS
- Pattern: Normal → Spike → Normal → Spike → Recovery

**Results**:

| Phase | Success Rate | P95 Latency |
|-------|--------------|-------------|
| Normal (baseline) | [X]% | [X] ms |
| First spike | [X]% | [X] ms |
| Recovery 1 | [X]% | [X] ms |
| Second spike | [X]% | [X] ms |
| Final recovery | [X]% | [X] ms |

**Observations**:
- System recovery time after spike: [X] seconds
- Error rate during spike: [X]%

---

## 4. Slow Query Analysis

*From pg_stat_statements and EF Core logs*

### 4.1 Queries Exceeding 500ms

| Query Pattern | Avg Time (ms) | Max Time (ms) | Call Count | Recommendation |
|---------------|---------------|---------------|------------|----------------|
| [Query 1] | [X] | [X] | [X] | [Add index/rewrite] |
| [Query 2] | [X] | [X] | [X] | [Recommendation] |

### 4.2 Optimization Status

| Query | Before (ms) | After (ms) | Improvement | Status |
|-------|-------------|------------|-------------|--------|
| [Query 1] | [X] | [X] | [X]% | Done/Pending |

---

## 5. Resource Utilization

### 5.1 API Pod Metrics

| Metric | Baseline | Under Load | Peak |
|--------|----------|------------|------|
| CPU Usage | [X]% | [X]% | [X]% |
| Memory Usage | [X] MB | [X] MB | [X] MB |
| Pod Restarts | 0 | [X] | [X] |

### 5.2 Database Metrics

| Metric | Baseline | Under Load | Peak |
|--------|----------|------------|------|
| Active Connections | [X] | [X] | [X] |
| CPU Usage | [X]% | [X]% | [X]% |
| IOPS | [X] | [X] | [X] |

---

## 6. Issues Discovered

### 6.1 Performance Issues

| ID | Description | Severity | Status | Resolution |
|----|-------------|----------|--------|------------|
| PERF-001 | [Description] | High/Medium/Low | Open/Fixed | [Resolution] |

### 6.2 Stability Issues

| ID | Description | Severity | Status | Resolution |
|----|-------------|----------|--------|------------|
| STAB-001 | [Description] | High/Medium/Low | Open/Fixed | [Resolution] |

---

## 7. Recommendations

### 7.1 Immediate Actions
1. [Action 1]
2. [Action 2]

### 7.2 Future Improvements
1. [Improvement 1]
2. [Improvement 2]

### 7.3 Production Deployment Notes
- [Note 1]
- [Note 2]

---

## 8. Appendix

### 8.1 NBomber Report Files
- HTML Report: `Reports/[test-name]/[timestamp]/report.html`
- Markdown Report: `Reports/[test-name]/[timestamp]/report.md`

### 8.2 Test Commands Used

```bash
# Run batch receipt processing test
dotnet test --filter "BatchReceiptProcessingTests" --no-build

# Run concurrent user tests
dotnet test --filter "ConcurrentUserTests" --no-build

# Run all load tests
dotnet test ExpenseFlow.LoadTests.csproj --no-build
```

### 8.3 Configuration Files
- Load test config: `backend/tests/ExpenseFlow.LoadTests/appsettings.json`
- Staging deployment: `infrastructure/kubernetes/staging/deployment.yaml`

---

**Report Prepared By**: [Name]
**Report Date**: [Date]
**Next Review Date**: [Date]
