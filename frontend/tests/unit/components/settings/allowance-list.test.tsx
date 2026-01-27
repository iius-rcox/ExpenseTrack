/**
 * Tests for AllowanceList component
 *
 * These tests verify the allowance list renders correctly
 * and handles user interactions.
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { AllowanceList } from '@/components/settings/allowance-list'
import type { Allowance, AllowanceListResponse } from '@/types/allowance'
import { AllowanceFrequency } from '@/types/allowance'

// Mock the hooks
vi.mock('@/hooks/queries/use-allowances', () => ({
  useAllowances: vi.fn(),
  useCreateAllowance: vi.fn(),
  useUpdateAllowance: vi.fn(),
  useDeleteAllowance: vi.fn(),
}))

vi.mock('@/hooks/queries/use-reference-data', () => ({
  useGLAccounts: vi.fn(() => ({ data: [], isLoading: false })),
  useDepartments: vi.fn(() => ({ data: [], isLoading: false })),
}))

import {
  useAllowances,
  useCreateAllowance,
  useUpdateAllowance,
  useDeleteAllowance,
} from '@/hooks/queries/use-allowances'

const mockUseAllowances = vi.mocked(useAllowances)
const mockUseCreateAllowance = vi.mocked(useCreateAllowance)
const mockUseUpdateAllowance = vi.mocked(useUpdateAllowance)
const mockUseDeleteAllowance = vi.mocked(useDeleteAllowance)

// Mock data
const mockAllowances: Allowance[] = [
  {
    id: 'allowance-1',
    userId: 'user-123',
    vendorName: 'Netflix',
    amount: 15.99,
    frequency: AllowanceFrequency.Monthly,
    glCode: '65000',
    glName: 'Subscriptions',
    departmentCode: 'IT',
    description: 'Monthly streaming subscription',
    isActive: true,
    createdAt: '2025-01-01T00:00:00Z',
    updatedAt: null,
  },
  {
    id: 'allowance-2',
    userId: 'user-123',
    vendorName: 'Adobe',
    amount: 54.99,
    frequency: AllowanceFrequency.Monthly,
    glCode: null,
    glName: null,
    departmentCode: null,
    description: null,
    isActive: false,
    createdAt: '2025-01-02T00:00:00Z',
    updatedAt: null,
  },
]

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  return function Wrapper({ children }: { children: React.ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    )
  }
}

describe('AllowanceList', () => {
  beforeEach(() => {
    vi.clearAllMocks()

    // Default mock implementations
    mockUseCreateAllowance.mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
    } as any)

    mockUseUpdateAllowance.mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
    } as any)

    mockUseDeleteAllowance.mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
    } as any)
  })

  it('renders loading skeleton when loading', () => {
    mockUseAllowances.mockReturnValue({
      data: undefined,
      isLoading: true,
      isError: false,
    } as any)

    render(<AllowanceList />, { wrapper: createWrapper() })

    // Should show skeleton loading states
    expect(screen.queryByText('Netflix')).not.toBeInTheDocument()
  })

  it('renders empty state when no allowances', () => {
    mockUseAllowances.mockReturnValue({
      data: { items: [], totalCount: 0 } as AllowanceListResponse,
      isLoading: false,
      isError: false,
    } as any)

    render(<AllowanceList />, { wrapper: createWrapper() })

    expect(screen.getByText(/no recurring allowances yet/i)).toBeInTheDocument()
  })

  it('renders error state when query fails', () => {
    mockUseAllowances.mockReturnValue({
      data: undefined,
      isLoading: false,
      isError: true,
    } as any)

    render(<AllowanceList />, { wrapper: createWrapper() })

    expect(screen.getByText(/failed to load allowances/i)).toBeInTheDocument()
  })

  it('renders list of allowances', () => {
    mockUseAllowances.mockReturnValue({
      data: { items: mockAllowances, totalCount: 2 } as AllowanceListResponse,
      isLoading: false,
      isError: false,
    } as any)

    render(<AllowanceList />, { wrapper: createWrapper() })

    expect(screen.getByText('Netflix')).toBeInTheDocument()
    expect(screen.getByText('Adobe')).toBeInTheDocument()
    expect(screen.getByText('$15.99')).toBeInTheDocument()
    expect(screen.getByText('$54.99')).toBeInTheDocument()
  })

  it('displays inactive badge for inactive allowances', () => {
    mockUseAllowances.mockReturnValue({
      data: { items: mockAllowances, totalCount: 2 } as AllowanceListResponse,
      isLoading: false,
      isError: false,
    } as any)

    render(<AllowanceList />, { wrapper: createWrapper() })

    // Adobe is inactive
    expect(screen.getByText('Inactive')).toBeInTheDocument()
  })

  it('displays description when available', () => {
    mockUseAllowances.mockReturnValue({
      data: { items: mockAllowances, totalCount: 2 } as AllowanceListResponse,
      isLoading: false,
      isError: false,
    } as any)

    render(<AllowanceList />, { wrapper: createWrapper() })

    expect(screen.getByText('Monthly streaming subscription')).toBeInTheDocument()
  })

  it('displays GL code when available', () => {
    mockUseAllowances.mockReturnValue({
      data: { items: mockAllowances, totalCount: 2 } as AllowanceListResponse,
      isLoading: false,
      isError: false,
    } as any)

    render(<AllowanceList />, { wrapper: createWrapper() })

    // GL code is displayed (hidden on small screens via CSS, but still in DOM)
    expect(screen.getByText('GL: 65000')).toBeInTheDocument()
  })

  it('calls update mutation when toggle switch is clicked', async () => {
    const user = userEvent.setup()
    const mockMutate = vi.fn()

    mockUseAllowances.mockReturnValue({
      data: { items: mockAllowances, totalCount: 2 } as AllowanceListResponse,
      isLoading: false,
      isError: false,
    } as any)

    mockUseUpdateAllowance.mockReturnValue({
      mutate: mockMutate,
      isPending: false,
    } as any)

    render(<AllowanceList />, { wrapper: createWrapper() })

    // Find switches by role
    const switches = screen.getAllByRole('switch')
    expect(switches.length).toBe(2)

    // Click the first switch (Netflix is active, should deactivate)
    await user.click(switches[0])

    await waitFor(() => {
      expect(mockMutate).toHaveBeenCalledWith(
        expect.objectContaining({
          id: 'allowance-1',
          data: { isActive: false },
        }),
        expect.anything()
      )
    })
  })

  it('opens delete dialog when delete button is clicked', async () => {
    const user = userEvent.setup()

    mockUseAllowances.mockReturnValue({
      data: { items: mockAllowances, totalCount: 2 } as AllowanceListResponse,
      isLoading: false,
      isError: false,
    } as any)

    render(<AllowanceList />, { wrapper: createWrapper() })

    // Find delete button by aria-label
    const deleteButton = screen.getByRole('button', { name: /delete netflix allowance/i })

    // Click the delete button
    await user.click(deleteButton)

    // Delete confirmation dialog should appear
    await waitFor(() => {
      expect(screen.getByText(/delete allowance\?/i)).toBeInTheDocument()
    })
  })

  it('has accessible aria-labels on edit and delete buttons', () => {
    mockUseAllowances.mockReturnValue({
      data: { items: mockAllowances, totalCount: 2 } as AllowanceListResponse,
      isLoading: false,
      isError: false,
    } as any)

    render(<AllowanceList />, { wrapper: createWrapper() })

    // Check aria-labels on buttons
    expect(screen.getByRole('button', { name: /edit netflix allowance/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /delete netflix allowance/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /edit adobe allowance/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /delete adobe allowance/i })).toBeInTheDocument()
  })
})
