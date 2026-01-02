/**
 * Test Utilities Index
 *
 * Central export point for all testing utilities.
 */

// MSW Server and Handlers
export { server } from './msw-server'
export {
  handlers,
  analyticsHandlers,
  dashboardHandlers,
  errorHandlers,
  createUnauthorizedHandler,
  createServerErrorHandler,
  createEmptyResponseHandler,
  createTimeoutHandler,
  createMalformedResponseHandler,
  createNetworkErrorHandler,
  createForbiddenHandler,
  createNotFoundHandler,
  createRateLimitHandler,
} from './msw-handlers'

// Auth Mocking
export {
  mockAccount,
  mockAccessToken,
  mockAuthenticatedState,
  mockUnauthenticatedState,
  mockLoginInProgressState,
  mockAcquireTokenInProgressState,
  createMockMsalInstance,
  createMockUseMsal,
  createMockUseIsAuthenticated,
  createMockUseAccount,
  createMsalReactMock,
  createMockAccount,
  createExpiredTokenState,
  createLoginFailureState,
  type MockAccount,
  type AuthState,
  type MockMsalInstance,
} from './auth-mock'

// Test Fixtures
export {
  createMonthlyComparison,
  createSpendingTrend,
  createCategoryBreakdown,
  createMerchantAnalytics,
  createSubscriptions,
  createDashboardSummary,
  createApiError,
  fixtureVariants,
  type VendorChange,
  type MonthlyComparisonResponse,
  type SpendingTrendItem,
  type CategoryBreakdownItem,
  type MerchantItem,
  type MerchantAnalyticsResponse,
  type SubscriptionItem,
  type SubscriptionDetectionResponse,
  type ActivityItem,
  type DashboardSummaryResponse,
  type ApiErrorResponse,
} from './fixtures'

// Render Utilities
export {
  renderWithProviders,
  createTestQueryClient,
  TestProviders,
} from './render-with-providers'
