/**
 * MSAL Authentication Mock Utilities
 *
 * Provides controlled mock implementations of MSAL React hooks and
 * authentication states for integration testing.
 *
 * Usage:
 * - Import mockAuthenticatedState or mockUnauthenticatedState
 * - Use in vi.mock('@azure/msal-react', ...) or test wrappers
 */

import { vi } from 'vitest'

// =============================================================================
// Type Definitions (matching MSAL interfaces)
// =============================================================================

/**
 * Mock account matching MSAL AccountInfo interface
 */
export interface MockAccount {
  homeAccountId: string
  username: string
  name: string
  localAccountId: string
  tenantId: string
  environment?: string
  idTokenClaims?: Record<string, unknown>
}

/**
 * Authentication state for testing
 */
export interface AuthState {
  isAuthenticated: boolean
  account: MockAccount | null
  accessToken: string | null
  expiresAt: Date | null
}

/**
 * Mock MSAL instance methods
 */
export interface MockMsalInstance {
  getActiveAccount: () => MockAccount | null
  setActiveAccount: (account: MockAccount | null) => void
  acquireTokenSilent: ReturnType<typeof vi.fn>
  acquireTokenPopup: ReturnType<typeof vi.fn>
  acquireTokenRedirect: ReturnType<typeof vi.fn>
  loginPopup: ReturnType<typeof vi.fn>
  loginRedirect: ReturnType<typeof vi.fn>
  logout: ReturnType<typeof vi.fn>
  logoutPopup: ReturnType<typeof vi.fn>
  logoutRedirect: ReturnType<typeof vi.fn>
  getAllAccounts: () => MockAccount[]
}

// =============================================================================
// Default Mock Data
// =============================================================================

/**
 * Default mock user account
 */
export const mockAccount: MockAccount = {
  homeAccountId: 'test-home-account-id-12345',
  username: 'testuser@expenseflow.example.com',
  name: 'Test User',
  localAccountId: 'test-local-account-id-67890',
  tenantId: 'test-tenant-id-abcdef',
  environment: 'login.microsoftonline.com',
  idTokenClaims: {
    aud: 'test-client-id',
    iss: 'https://login.microsoftonline.com/test-tenant-id/v2.0',
    iat: Math.floor(Date.now() / 1000),
    exp: Math.floor(Date.now() / 1000) + 3600,
    name: 'Test User',
    preferred_username: 'testuser@expenseflow.example.com',
    oid: 'test-object-id',
    sub: 'test-subject-id',
  },
}

/**
 * Mock access token (JWT-like structure for testing)
 */
export const mockAccessToken =
  'eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ0ZXN0LXVzZXItaWQiLCJuYW1lIjoiVGVzdCBVc2VyIiwiaWF0IjoxNTE2MjM5MDIyfQ.test-signature'

// =============================================================================
// Mock State Factories
// =============================================================================

/**
 * Create a mock MSAL instance with configurable behavior
 */
export function createMockMsalInstance(
  account: MockAccount | null = mockAccount
): MockMsalInstance {
  let activeAccount = account

  return {
    getActiveAccount: () => activeAccount,
    setActiveAccount: (acc: MockAccount | null) => {
      activeAccount = acc
    },
    acquireTokenSilent: vi.fn().mockResolvedValue({
      accessToken: mockAccessToken,
      account: activeAccount,
      expiresOn: new Date(Date.now() + 3600000),
      scopes: ['api://expenseflow/.default'],
      tokenType: 'Bearer',
    }),
    acquireTokenPopup: vi.fn().mockResolvedValue({
      accessToken: mockAccessToken,
      account: activeAccount,
      expiresOn: new Date(Date.now() + 3600000),
    }),
    acquireTokenRedirect: vi.fn().mockResolvedValue(undefined),
    loginPopup: vi.fn().mockResolvedValue({
      account: activeAccount,
      accessToken: mockAccessToken,
    }),
    loginRedirect: vi.fn().mockResolvedValue(undefined),
    logout: vi.fn().mockResolvedValue(undefined),
    logoutPopup: vi.fn().mockResolvedValue(undefined),
    logoutRedirect: vi.fn().mockResolvedValue(undefined),
    getAllAccounts: () => (activeAccount ? [activeAccount] : []),
  }
}

/**
 * Authenticated state - user is logged in with valid token
 */
export const mockAuthenticatedState = {
  accounts: [mockAccount],
  inProgress: 'none' as const,
  instance: createMockMsalInstance(mockAccount),
}

/**
 * Unauthenticated state - no user logged in
 */
export const mockUnauthenticatedState = {
  accounts: [],
  inProgress: 'none' as const,
  instance: createMockMsalInstance(null),
}

/**
 * Login in progress state - redirect or popup is active
 */
export const mockLoginInProgressState = {
  accounts: [],
  inProgress: 'login' as const,
  instance: createMockMsalInstance(null),
}

/**
 * Token acquisition in progress state
 */
export const mockAcquireTokenInProgressState = {
  accounts: [mockAccount],
  inProgress: 'acquireToken' as const,
  instance: createMockMsalInstance(mockAccount),
}

// =============================================================================
// Mock Hook Returns
// =============================================================================

/**
 * Mock return value for useMsal() hook when authenticated
 */
export function createMockUseMsal(isAuthenticated: boolean = true) {
  const state = isAuthenticated
    ? mockAuthenticatedState
    : mockUnauthenticatedState

  return {
    instance: state.instance,
    accounts: state.accounts,
    inProgress: state.inProgress,
  }
}

/**
 * Mock return value for useIsAuthenticated() hook
 */
export function createMockUseIsAuthenticated(isAuthenticated: boolean = true) {
  return isAuthenticated
}

/**
 * Mock return value for useAccount() hook
 */
export function createMockUseAccount(account: MockAccount | null = mockAccount) {
  return account
}

// =============================================================================
// Full MSAL React Module Mock
// =============================================================================

/**
 * Complete mock of @azure/msal-react module
 * Use with vi.mock('@azure/msal-react', () => createMsalReactMock())
 */
export function createMsalReactMock(isAuthenticated: boolean = true) {
  const state = isAuthenticated
    ? mockAuthenticatedState
    : mockUnauthenticatedState

  return {
    useMsal: () => ({
      instance: state.instance,
      accounts: state.accounts,
      inProgress: state.inProgress,
    }),
    useIsAuthenticated: () => isAuthenticated,
    useAccount: () => (isAuthenticated ? mockAccount : null),
    useMsalAuthentication: () => ({
      login: vi.fn(),
      result: isAuthenticated
        ? { account: mockAccount, accessToken: mockAccessToken }
        : null,
      error: null,
    }),
    MsalProvider: ({ children }: { children: React.ReactNode }) => children,
    AuthenticatedTemplate: ({ children }: { children: React.ReactNode }) =>
      isAuthenticated ? children : null,
    UnauthenticatedTemplate: ({ children }: { children: React.ReactNode }) =>
      !isAuthenticated ? children : null,
    MsalAuthenticationTemplate: ({
      children,
    }: {
      children: React.ReactNode
    }) => (isAuthenticated ? children : null),
  }
}

// =============================================================================
// Test Helpers
// =============================================================================

/**
 * Create a custom account for testing specific scenarios
 */
export function createMockAccount(
  overrides: Partial<MockAccount> = {}
): MockAccount {
  return {
    ...mockAccount,
    ...overrides,
  }
}

/**
 * Create an expired token state for testing token refresh
 */
export function createExpiredTokenState() {
  const instance = createMockMsalInstance(mockAccount)
  instance.acquireTokenSilent = vi.fn().mockRejectedValue(
    new Error('Token expired')
  )
  return {
    accounts: [mockAccount],
    inProgress: 'none' as const,
    instance,
  }
}

/**
 * Create a login failure state for testing error handling
 */
export function createLoginFailureState(errorMessage: string = 'Login failed') {
  const instance = createMockMsalInstance(null)
  instance.loginRedirect = vi.fn().mockRejectedValue(new Error(errorMessage))
  instance.loginPopup = vi.fn().mockRejectedValue(new Error(errorMessage))
  return {
    accounts: [],
    inProgress: 'none' as const,
    instance,
  }
}
