# Chaos Testing Runbook

## Overview

This runbook provides operational procedures for running chaos tests against ExpenseFlow. Chaos engineering helps us discover weaknesses in our system before they cause production incidents.

## Prerequisites

- Docker Desktop running
- Access to GitHub Actions (for CI-triggered chaos)
- PowerShell 7+ or Bash shell
- Environment variables configured (see Configuration section)

## Configuration

### Environment Variables

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `CHAOS_ENABLED` | Yes | `false` | Master switch - must be `true` to inject faults |
| `CHAOS_INJECTION_RATE` | No | `0.05` | Probability (0.0-1.0) of fault injection |
| `CHAOS_MAX_LATENCY_MS` | No | `5000` | Maximum artificial latency in milliseconds |

### Safe Defaults for Testing

```bash
# Conservative settings for development
export CHAOS_ENABLED=true
export CHAOS_INJECTION_RATE=0.10
export CHAOS_MAX_LATENCY_MS=3000
```

### Aggressive Settings for Nightly

```bash
# Stress testing settings (used in ci-nightly.yml)
export CHAOS_ENABLED=true
export CHAOS_INJECTION_RATE=0.25
export CHAOS_MAX_LATENCY_MS=5000
```

## Chaos Strategies

### 1. HTTP Fault Injection

**What it does**: Simulates network failures, DNS issues, connection timeouts

**Use case**: Validate retry policies and circuit breakers for HTTP calls

**Expected behavior**:
- System should retry transient failures
- Circuit breaker should open after threshold
- User should see graceful error message

```csharp
// Strategy implementation
ChaosStrategies.CreateHttpFaultStrategy(config);
```

### 2. Latency Injection

**What it does**: Adds artificial delay to operations

**Use case**: Validate timeout handling and user experience under slow conditions

**Expected behavior**:
- Requests should timeout appropriately
- UI should show loading states
- No cascading failures from slow responses

```csharp
// Strategy implementation
ChaosStrategies.CreateLatencyStrategy(config);
```

### 3. Database Chaos

**What it does**: Simulates database connection timeouts and failures

**Use case**: Validate database resilience and connection pooling

**Expected behavior**:
- Connection pool should recover
- Transactions should rollback cleanly
- Application should not deadlock

```csharp
// Strategy implementation (uses half the injection rate)
ChaosStrategies.CreateDatabaseChaosStrategy(config);
```

### 4. API Rate Limiting

**What it does**: Returns HTTP 429 (Too Many Requests) and 503 (Service Unavailable)

**Use case**: Validate handling of external API limits (Azure AI, OpenAI)

**Expected behavior**:
- Tier fallback should activate (Tier 3 → 2 → 1)
- Rate limit responses should trigger backoff
- User should see partial functionality

```csharp
// Strategy implementation
ChaosStrategies.CreateApiChaosStrategy(config);
```

### 5. Combined Chaos

**What it does**: Randomly combines faults and latency (50/50 split)

**Use case**: Stress testing under realistic failure conditions

**Expected behavior**:
- System should remain responsive
- No data corruption
- Graceful degradation

```csharp
// Strategy implementation
ChaosStrategies.CreateCombinedStrategy(config);
```

## Running Chaos Tests

### Local Execution

```bash
# 1. Start test infrastructure
docker-compose -f docker-compose.test.yml up -d

# 2. Configure chaos
export CHAOS_ENABLED=true
export CHAOS_INJECTION_RATE=0.10

# 3. Run chaos tests
dotnet test backend/tests/ExpenseFlow.Scenarios.Tests \
  --filter "Category=Chaos" \
  --logger "console;verbosity=normal"

# 4. Run resilience tests
dotnet test backend/tests/ExpenseFlow.Scenarios.Tests \
  --filter "Category=Resilience" \
  --logger "console;verbosity=normal"
```

### CI Execution

Chaos tests run automatically in the nightly workflow:

```yaml
# .github/workflows/ci-nightly.yml
chaos-tests:
  env:
    CHAOS_ENABLED: true
    CHAOS_INJECTION_RATE: 0.25
```

### Manual Trigger

You can trigger chaos tests manually from GitHub Actions:

1. Go to Actions → CI Nightly
2. Click "Run workflow"
3. Optionally adjust `chaos_rate` input
4. Click "Run workflow"

## Interpreting Results

### Expected Failures

These failures indicate **successful chaos detection**:

| Error Message | Meaning | Expected |
|--------------|---------|----------|
| `Chaos: Simulated network failure` | HTTP fault injected | ✓ |
| `Chaos: Database timeout` | Database timeout injected | ✓ |
| `Chaos: 429 TooManyRequests` | Rate limit simulated | ✓ |
| `Chaos: 503 ServiceUnavailable` | Service unavailable simulated | ✓ |

### Unexpected Failures

These failures indicate **bugs in resilience patterns**:

| Issue | Investigation |
|-------|---------------|
| Circuit breaker never opens | Check threshold configuration |
| Infinite retry loops | Verify retry policy limits |
| Unhandled exception propagates | Missing catch block or fallback |
| Data corruption after chaos | Transaction isolation issue |
| Deadlock during recovery | Resource contention bug |

## Resilience Verification Checklist

After chaos testing, verify:

- [ ] **Circuit Breakers**: Open after threshold failures, half-open after timeout
- [ ] **Retry Policies**: Retry appropriate errors, respect max attempts
- [ ] **Fallbacks**: Tier fallback works (Tier 3 → 2 → 1)
- [ ] **Timeouts**: Requests timeout appropriately, no hanging operations
- [ ] **Graceful Degradation**: Partial functionality available during outages
- [ ] **Recovery**: System returns to normal when faults stop
- [ ] **Logging**: Failures are logged with context for debugging
- [ ] **Metrics**: Circuit breaker state is observable

## Incident Response Integration

### When Chaos Reveals Issues

1. **Create issue**: Document the failure scenario
2. **Tag appropriately**: `bug`, `resilience`, `chaos-testing`
3. **Include reproduction steps**: Environment variables, test command
4. **Link to CI run**: Include GitHub Actions URL

### When Production Matches Chaos

If a production incident matches chaos test scenarios:

1. Review chaos test implementation for mitigation insights
2. Add regression chaos test if not already covered
3. Update resilience patterns based on learnings
4. Document in post-mortem

## Safety Guidelines

### Never Do

- ❌ Run chaos against production
- ❌ Set injection rate above 0.50 (50%)
- ❌ Disable chaos tests in CI without justification
- ❌ Ignore chaos test failures

### Always Do

- ✓ Run chaos in isolated test environment
- ✓ Start with low injection rate, increase gradually
- ✓ Monitor resource usage during chaos tests
- ✓ Document new chaos scenarios before implementing

## Troubleshooting

### Chaos Not Injecting

```bash
# Verify environment variable
echo $CHAOS_ENABLED  # Should be "true"

# Check injection rate
echo $CHAOS_INJECTION_RATE  # Should be > 0
```

### Tests Timing Out

```bash
# Reduce injection rate
export CHAOS_INJECTION_RATE=0.05

# Reduce max latency
export CHAOS_MAX_LATENCY_MS=1000
```

### Container Issues

```bash
# Reset test infrastructure
docker-compose -f docker-compose.test.yml down -v
docker-compose -f docker-compose.test.yml up -d
```

## Metrics and Reporting

### Key Metrics to Track

1. **Mean Time to Recovery (MTTR)**: How long until system recovers
2. **Circuit Breaker Activations**: How often circuit breakers open
3. **Fallback Success Rate**: How often fallbacks succeed
4. **Error Rate Under Chaos**: Percentage of operations failing

### Generating Reports

```bash
# Run chaos tests with TRX output
dotnet test backend/tests/ExpenseFlow.Scenarios.Tests \
  --filter "Category=Chaos|Category=Resilience" \
  --logger "trx;LogFileName=chaos-results.trx"
```

Reports are automatically generated in CI and attached as artifacts.

## Appendix: Polly v8 Patterns

The chaos strategies use Polly v8's `ResiliencePipeline` pattern:

```csharp
// Building a chaos pipeline
var pipeline = new ResiliencePipelineBuilder()
    .AddChaosFault(new ChaosFaultStrategyOptions
    {
        InjectionRate = 0.10,
        EnabledGenerator = static args =>
            new ValueTask<bool>(Environment.GetEnvironmentVariable("CHAOS_ENABLED") == "true"),
        FaultGenerator = static args =>
            new ValueTask<Exception?>(new HttpRequestException("Chaos: Network failure"))
    })
    .Build();

// Using the pipeline
await pipeline.ExecuteAsync(async ct =>
{
    await httpClient.GetAsync("/api/endpoint", ct);
}, cancellationToken);
```

For more information, see the [Polly documentation](https://github.com/App-vNext/Polly).
