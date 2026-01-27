/**
 * Tests for useAllowances hooks
 *
 * These tests verify the TanStack Query hooks for managing
 * recurring expense allowances work correctly with the API.
 *
 * NOTE: We mock apiFetch directly to avoid MSAL authentication issues in tests.
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor, act } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { createElement } from 'react'
import {
  useAllowances,
  useCreateAllowance,
  useUpdateAllowance,
  useDeleteAllowance,
  allowanceKeys,
} from '@/hooks/queries/use-allowances'
import type {
  Allowance,
  AllowanceListResponse,
  CreateAllowanceRequest,
} from '@/types/allowance'
import { AllowanceFrequency } from '@/types/allowance'

// Mock apiFetch to avoid MSAL authentication
vi.mock('@/services/api', () => ({
  apiFetch: vi.fn(),
}))

import { apiFetch } from '@/services/api'
const mockApiFetch = vi.mocked(apiFetch)

// Helper to create wrapper with QueryClient
function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  return function Wrapper({ children }: { children: React.ReactNode }) {
    return createElement(QueryClientProvider, { client: queryClient }, children)
  }
}

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
    glCode: '62000',
    glName: 'Software',
    departmentCode: 'DESIGN',
    description: 'Creative Cloud subscription',
    isActive: true,
    createdAt: '2025-01-02T00:00:00Z',
    updatedAt: null,
  },
  {
    id: 'allowance-3',
    userId: 'user-123',
    vendorName: 'Old Service',
    amount: 9.99,
    frequency: AllowanceFrequency.Monthly,
    glCode: null,
    glName: null,
    departmentCode: null,
    description: null,
    isActive: false,
    createdAt: '2024-06-01T00:00:00Z',
    updatedAt: '2025-01-15T00:00:00Z',
  },
]

describe('allowanceKeys', () => {
  it('generates correct query keys', () => {
    expect(allowanceKeys.all).toEqual(['allowances'])
    expect(allowanceKeys.list()).toEqual(['allowances', 'list'])
    expect(allowanceKeys.listActive()).toEqual(['allowances', 'list', { activeOnly: true }])
    expect(allowanceKeys.listAll()).toEqual(['allowances', 'list', { activeOnly: false }])
    expect(allowanceKeys.detail('abc-123')).toEqual(['allowances', 'detail', 'abc-123'])
  })
})

describe('useAllowances', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('fetches all allowances by default', async () => {
    const mockResponse: AllowanceListResponse = {
      items: mockAllowances,
      totalCount: 3,
    }

    mockApiFetch.mockResolvedValueOnce(mockResponse)

    const { result } = renderHook(() => useAllowances(), {
      wrapper: createWrapper(),
    })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(mockApiFetch).toHaveBeenCalledWith('/allowances?activeOnly=false')
    expect(result.current.data?.items).toHaveLength(3)
    expect(result.current.data?.totalCount).toBe(3)
  })

  it('fetches only active allowances when activeOnly is true', async () => {
    const activeAllowances = mockAllowances.filter((a) => a.isActive)
    const mockResponse: AllowanceListResponse = {
      items: activeAllowances,
      totalCount: 2,
    }

    mockApiFetch.mockResolvedValueOnce(mockResponse)

    const { result } = renderHook(() => useAllowances(true), {
      wrapper: createWrapper(),
    })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(mockApiFetch).toHaveBeenCalledWith('/allowances?activeOnly=true')
    expect(result.current.data?.items).toHaveLength(2)
    expect(result.current.data?.items.every((a) => a.isActive)).toBe(true)
  })

  it('handles empty allowances list', async () => {
    const mockResponse: AllowanceListResponse = {
      items: [],
      totalCount: 0,
    }

    mockApiFetch.mockResolvedValueOnce(mockResponse)

    const { result } = renderHook(() => useAllowances(), {
      wrapper: createWrapper(),
    })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(result.current.data?.items).toHaveLength(0)
    expect(result.current.data?.totalCount).toBe(0)
  })

  it('handles API error gracefully', async () => {
    mockApiFetch.mockRejectedValueOnce(new Error('Database connection failed'))

    const { result } = renderHook(() => useAllowances(), {
      wrapper: createWrapper(),
    })

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
  })
})

describe('useCreateAllowance', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('creates a new allowance with all fields', async () => {
    const newAllowance: CreateAllowanceRequest = {
      vendorName: 'Spotify',
      amount: 14.99,
      frequency: AllowanceFrequency.Monthly,
      glCode: '65000',
      departmentCode: 'IT',
      description: 'Music streaming',
    }

    const createdAllowance: Allowance = {
      id: 'new-allowance-id',
      userId: 'user-123',
      vendorName: newAllowance.vendorName,
      amount: newAllowance.amount,
      frequency: newAllowance.frequency,
      glCode: newAllowance.glCode || null,
      glName: 'Subscriptions',
      departmentCode: newAllowance.departmentCode || null,
      description: newAllowance.description || null,
      isActive: true,
      createdAt: new Date().toISOString(),
      updatedAt: null,
    }

    mockApiFetch.mockResolvedValueOnce(createdAllowance)

    const { result } = renderHook(() => useCreateAllowance(), {
      wrapper: createWrapper(),
    })

    await act(async () => {
      result.current.mutate(newAllowance)
    })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(mockApiFetch).toHaveBeenCalledWith('/allowances', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(newAllowance),
    })
    expect(result.current.data?.id).toBe('new-allowance-id')
    expect(result.current.data?.vendorName).toBe('Spotify')
  })

  it('creates allowance with minimal fields', async () => {
    const newAllowance: CreateAllowanceRequest = {
      vendorName: 'Simple Subscription',
      amount: 9.99,
      frequency: AllowanceFrequency.Weekly,
    }

    const createdAllowance: Allowance = {
      id: 'minimal-allowance-id',
      userId: 'user-123',
      vendorName: newAllowance.vendorName,
      amount: newAllowance.amount,
      frequency: newAllowance.frequency,
      glCode: null,
      glName: null,
      departmentCode: null,
      description: null,
      isActive: true,
      createdAt: new Date().toISOString(),
      updatedAt: null,
    }

    mockApiFetch.mockResolvedValueOnce(createdAllowance)

    const { result } = renderHook(() => useCreateAllowance(), {
      wrapper: createWrapper(),
    })

    await act(async () => {
      result.current.mutate(newAllowance)
    })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(result.current.data?.vendorName).toBe('Simple Subscription')
    expect(result.current.data?.glCode).toBeNull()
    expect(result.current.data?.departmentCode).toBeNull()
  })

  it('handles validation error for duplicate vendor', async () => {
    mockApiFetch.mockRejectedValueOnce(new Error('An allowance for this vendor already exists'))

    const { result } = renderHook(() => useCreateAllowance(), {
      wrapper: createWrapper(),
    })

    await act(async () => {
      result.current.mutate({
        vendorName: 'Netflix',
        amount: 15.99,
        frequency: AllowanceFrequency.Monthly,
      })
    })

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
  })
})

describe('useUpdateAllowance', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('updates allowance amount', async () => {
    const allowanceId = 'allowance-1'
    const updatedAllowance: Allowance = {
      ...mockAllowances[0],
      amount: 19.99,
      updatedAt: new Date().toISOString(),
    }

    mockApiFetch.mockResolvedValueOnce(updatedAllowance)

    const { result } = renderHook(() => useUpdateAllowance(), {
      wrapper: createWrapper(),
    })

    await act(async () => {
      result.current.mutate({
        id: allowanceId,
        data: { amount: 19.99 },
      })
    })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(mockApiFetch).toHaveBeenCalledWith(`/allowances/${allowanceId}`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ amount: 19.99 }),
    })
    expect(result.current.data?.amount).toBe(19.99)
  })

  it('deactivates an allowance', async () => {
    const allowanceId = 'allowance-2'
    const updatedAllowance: Allowance = {
      ...mockAllowances[1],
      isActive: false,
      updatedAt: new Date().toISOString(),
    }

    mockApiFetch.mockResolvedValueOnce(updatedAllowance)

    const { result } = renderHook(() => useUpdateAllowance(), {
      wrapper: createWrapper(),
    })

    await act(async () => {
      result.current.mutate({
        id: allowanceId,
        data: { isActive: false },
      })
    })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(result.current.data?.isActive).toBe(false)
  })

  it('clears GL code by setting to null', async () => {
    const allowanceId = 'allowance-1'
    const updatedAllowance: Allowance = {
      ...mockAllowances[0],
      glCode: null,
      glName: null,
      updatedAt: new Date().toISOString(),
    }

    mockApiFetch.mockResolvedValueOnce(updatedAllowance)

    const { result } = renderHook(() => useUpdateAllowance(), {
      wrapper: createWrapper(),
    })

    await act(async () => {
      result.current.mutate({
        id: allowanceId,
        data: { glCode: null },
      })
    })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(result.current.data?.glCode).toBeNull()
  })

  it('handles not found error', async () => {
    mockApiFetch.mockRejectedValueOnce(new Error('Allowance not found'))

    const { result } = renderHook(() => useUpdateAllowance(), {
      wrapper: createWrapper(),
    })

    await act(async () => {
      result.current.mutate({
        id: 'non-existent',
        data: { amount: 10 },
      })
    })

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
  })
})

describe('useDeleteAllowance', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('deletes (soft-deletes) an allowance', async () => {
    const allowanceId = 'allowance-1'

    mockApiFetch.mockResolvedValueOnce(undefined)

    const { result } = renderHook(() => useDeleteAllowance(), {
      wrapper: createWrapper(),
    })

    await act(async () => {
      result.current.mutate(allowanceId)
    })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(mockApiFetch).toHaveBeenCalledWith(`/allowances/${allowanceId}`, {
      method: 'DELETE',
    })
  })

  it('handles not found error on delete', async () => {
    mockApiFetch.mockRejectedValueOnce(new Error('Allowance not found'))

    const { result } = renderHook(() => useDeleteAllowance(), {
      wrapper: createWrapper(),
    })

    await act(async () => {
      result.current.mutate('non-existent-id')
    })

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
  })

  it('handles forbidden error when deleting another user allowance', async () => {
    mockApiFetch.mockRejectedValueOnce(new Error('You do not have permission to delete this allowance'))

    const { result } = renderHook(() => useDeleteAllowance(), {
      wrapper: createWrapper(),
    })

    await act(async () => {
      result.current.mutate('other-user-allowance')
    })

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
  })
})
