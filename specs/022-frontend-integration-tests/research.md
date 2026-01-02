# Research: Frontend Integration Tests

**Feature**: 022-frontend-integration-tests
**Date**: 2026-01-02
**Status**: Complete

## Research Topics

### 1. MSW (Mock Service Worker) for API Mocking

**Decision**: Use MSW 2.x for API mocking in integration tests

**Rationale**:
- Industry standard for API mocking in React/Vitest environments
- Intercepts at network level (not module mocking) - more realistic
- Same handlers work in browser and Node.js
- Supports both REST and GraphQL
- Already compatible with existing Vitest setup

**Alternatives Considered**:
- **Nock**: Node-only, doesn't work in browser
- **fetch-mock**: Less ergonomic API, harder to maintain
- **Custom mocks**: Brittle, doesn't catch real fetch issues

**Implementation Notes**:
```typescript
// Setup in tests/setup.ts
import { setupServer } from 'msw/node'
import { handlers } from './msw-handlers'
export const server = setupServer(...handlers)
beforeAll(() => server.listen())
afterEach(() => server.resetHandlers())
afterAll(() => server.close())
```

---

### 2. MSAL Testing Strategy

**Decision**: Use `@azure/msal-browser` test utilities with controlled mocks

**Rationale**:
- MSAL provides `PublicClientApplication` that can be configured in test mode
- Real redirect testing requires E2E (Playwright) with auth state injection
- Unit/integration tests use mocked `useMsal()` hook responses
- Avoids hitting real Azure AD in CI

**Alternatives Considered**:
- **Real Azure AD in CI**: Slow, flaky, requires secrets management
- **Custom auth mock**: Would miss real MSAL behavior issues
- **MSAL test-utils package**: Doesn't exist; must create own utilities

**Implementation Notes**:
- For E2E: Use Playwright `storageState` to persist auth tokens between tests
- For integration: Mock `useMsal()` to return controlled account/token states
- For redirect testing: Intercept `loginRedirect` call and verify parameters

---

### 3. Contract Validation Approach

**Decision**: Generate TypeScript types from backend OpenAPI spec and compare at build time

**Rationale**:
- Backend already generates OpenAPI spec via Swagger
- TypeScript compiler can validate type compatibility
- Fails fast at build time, not runtime
- No runtime overhead

**Alternatives Considered**:
- **Runtime validation (Zod/Yup)**: Adds bundle size, slower
- **Manual type sync**: Error-prone, requires discipline
- **GraphQL**: Would require backend migration

**Implementation Notes**:
- Use `openapi-typescript` to generate types from OpenAPI spec
- Create test that imports both frontend and generated types
- TypeScript `Expect<Equal<A, B>>` pattern for compile-time checks
- CI fetches latest OpenAPI spec before running contract tests

---

### 4. Error Boundary Implementation

**Decision**: Use React 18 error boundary pattern with retry capability

**Rationale**:
- React's built-in `componentDidCatch` lifecycle
- Can wrap individual page sections for granular recovery
- Retry button resets error state and re-renders children
- Integrates with existing error tracking (if any)

**Alternatives Considered**:
- **react-error-boundary package**: Good but adds dependency for simple use case
- **Global error handler only**: Doesn't allow section-level recovery
- **No error boundaries**: Current state - entire page crashes

**Implementation Notes**:
```typescript
// Minimal error boundary with retry
class ErrorBoundary extends React.Component {
  state = { hasError: false }
  static getDerivedStateFromError() { return { hasError: true } }
  resetError = () => this.setState({ hasError: false })
  render() {
    if (this.state.hasError) return <ErrorFallback onRetry={this.resetError} />
    return this.props.children
  }
}
```

---

### 5. Integration Test Structure

**Decision**: Separate `tests/integration/` directory with MSW, distinct from unit tests

**Rationale**:
- Integration tests are slower (full React tree + API mocking)
- Different setup requirements (MSW server)
- Can run separately in CI for faster feedback
- Clear mental model: unit = isolated, integration = composed

**Alternatives Considered**:
- **Co-locate with components**: Harder to run separately, mixed concerns
- **Single test directory**: Unclear which tests need MSW
- **E2E for everything**: Too slow for page-level testing

**Implementation Notes**:
- Update `vitest.config.ts` with separate project for integration tests
- Integration tests get `tests/integration/**/*.test.tsx` pattern
- Unit tests remain at `tests/unit/**/*.test.tsx`
- Both share `tests/setup.ts` base, integration adds MSW layer

---

### 6. CI Integration

**Decision**: Add integration tests to existing `ci-full.yml` frontend-tests job

**Rationale**:
- Reuses existing Node.js setup
- Runs in parallel with backend tests
- No new workflow files needed
- Target: <3 min total for all frontend tests

**Alternatives Considered**:
- **Separate workflow**: More complex, harder to maintain
- **Nightly only**: Defeats purpose of catching bugs before merge
- **Quick CI only**: Too slow for commit-level feedback

**Implementation Notes**:
- Modify `ci-full.yml` frontend-tests step
- Run `npm run test:integration` after `npm run test:coverage`
- Contract tests run as part of integration suite
- E2E auth tests remain in existing e2e-tests job

---

## Dependencies to Install

| Package | Version | Purpose |
|---------|---------|---------|
| msw | ^2.7.x | API mocking |
| openapi-typescript | ^7.x | OpenAPI → TypeScript generation |
| @testing-library/react | ^16.x | Already installed |
| @playwright/test | ^1.57.x | Already installed |

## Files to Create

| Path | Purpose |
|------|---------|
| `frontend/src/test-utils/msw-handlers.ts` | API mock handlers |
| `frontend/src/test-utils/auth-mock.ts` | MSAL mock utilities |
| `frontend/src/test-utils/render-with-providers.tsx` | Test wrapper component |
| `frontend/src/components/error-boundary/ErrorBoundary.tsx` | Error boundary component |
| `frontend/src/components/error-boundary/ErrorFallback.tsx` | Error UI component |
| `frontend/tests/integration/*.test.tsx` | Page integration tests |
| `frontend/tests/contracts/api-contracts.test.ts` | Contract validation |
| `frontend/tests/e2e/auth-flow.spec.ts` | Auth E2E tests |

## Files to Modify

| Path | Changes |
|------|---------|
| `frontend/tests/setup.ts` | Add MSW server setup |
| `frontend/vitest.config.ts` | Add integration test project |
| `frontend/package.json` | Add test:integration script |
| `.github/workflows/ci-full.yml` | Add integration test step |

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| MSW setup complexity | Follow official Vitest integration guide |
| Flaky auth E2E tests | Use Playwright's auth state persistence |
| Contract tests too slow | Generate types at build time, not test time |
| Error boundaries breaking existing UX | Start with analytics page only, expand gradually |
