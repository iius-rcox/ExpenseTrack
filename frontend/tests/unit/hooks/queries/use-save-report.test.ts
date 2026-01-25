import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor, act } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useSaveReport } from '@/hooks/queries/use-reports'
import { http, HttpResponse } from 'msw'
import { server } from '@/test-utils/msw-server'
import type { BatchUpdateLinesResponse } from '@/types/api'
import { createElement } from 'react'

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

describe('useSaveReport', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('calls the correct API endpoint with batch update request', async () => {
    const reportId = 'test-report-123'
    const lines = [
      { lineId: 'line-1', glCode: '65000', departmentCode: 'ADMIN' },
      { lineId: 'line-2', glCode: '62000', departmentCode: 'IT' },
    ]

    let capturedRequest: any = null

    server.use(
      http.post('*/api/reports/:reportId/save', async ({ request, params }) => {
        capturedRequest = {
          reportId: params.reportId,
          body: await request.json(),
        }

        const response: BatchUpdateLinesResponse = {
          reportId: reportId,
          updatedCount: 2,
          failedCount: 0,
          updatedAt: new Date().toISOString(),
          reportStatus: 'Draft',
          failedLines: [],
        }

        return HttpResponse.json(response)
      })
    )

    const { result } = renderHook(() => useSaveReport(), {
      wrapper: createWrapper(),
    })

    await act(async () => {
      result.current.mutate({ reportId, lines })
    })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(capturedRequest).not.toBeNull()
    expect(capturedRequest.reportId).toBe(reportId)
    expect(capturedRequest.body.lines).toHaveLength(2)
    expect(capturedRequest.body.lines[0].lineId).toBe('line-1')
    expect(capturedRequest.body.lines[0].glCode).toBe('65000')
  })

  it('returns response with updatedCount and reportStatus', async () => {
    const reportId = 'test-report-123'
    const lines = [{ lineId: 'line-1', glCode: '65000' }]

    const mockResponse: BatchUpdateLinesResponse = {
      reportId: reportId,
      updatedCount: 1,
      failedCount: 0,
      updatedAt: '2025-01-25T12:00:00Z',
      reportStatus: 'Draft',
      failedLines: [],
    }

    server.use(
      http.post('*/api/reports/:reportId/save', () => {
        return HttpResponse.json(mockResponse)
      })
    )

    const { result } = renderHook(() => useSaveReport(), {
      wrapper: createWrapper(),
    })

    await act(async () => {
      result.current.mutate({ reportId, lines })
    })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(result.current.data?.updatedCount).toBe(1)
    expect(result.current.data?.reportStatus).toBe('Draft')
    expect(result.current.data?.failedLines).toHaveLength(0)
  })

  it('preserves report Draft status after save (critical requirement)', async () => {
    const reportId = 'test-report-123'
    const lines = [{ lineId: 'line-1', glCode: '65000' }]

    server.use(
      http.post('*/api/reports/:reportId/save', () => {
        return HttpResponse.json({
          reportId,
          updatedCount: 1,
          failedCount: 0,
          updatedAt: new Date().toISOString(),
          reportStatus: 'Draft', // CRITICAL: Must always be Draft
          failedLines: [],
        } satisfies BatchUpdateLinesResponse)
      })
    )

    const { result } = renderHook(() => useSaveReport(), {
      wrapper: createWrapper(),
    })

    await act(async () => {
      result.current.mutate({ reportId, lines })
    })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    // CRITICAL ASSERTION: Status must remain Draft
    expect(result.current.data?.reportStatus).toBe('Draft')
  })

  it('handles partial failures with failedLines', async () => {
    const reportId = 'test-report-123'
    const lines = [
      { lineId: 'valid-line', glCode: '65000' },
      { lineId: 'invalid-line', glCode: '99999' },
    ]

    server.use(
      http.post('*/api/reports/:reportId/save', () => {
        return HttpResponse.json({
          reportId,
          updatedCount: 1,
          failedCount: 1,
          updatedAt: new Date().toISOString(),
          reportStatus: 'Draft',
          failedLines: [
            { lineId: 'invalid-line', error: 'Line not found' },
          ],
        } satisfies BatchUpdateLinesResponse)
      })
    )

    const { result } = renderHook(() => useSaveReport(), {
      wrapper: createWrapper(),
    })

    await act(async () => {
      result.current.mutate({ reportId, lines })
    })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(result.current.data?.updatedCount).toBe(1)
    expect(result.current.data?.failedCount).toBe(1)
    expect(result.current.data?.failedLines).toHaveLength(1)
    expect(result.current.data?.failedLines[0].lineId).toBe('invalid-line')
    expect(result.current.data?.failedLines[0].error).toBe('Line not found')
  })

  it('handles non-Draft report error (locked for editing)', async () => {
    const reportId = 'test-report-123'
    const lines = [{ lineId: 'line-1', glCode: '65000' }]

    server.use(
      http.post('*/api/reports/:reportId/save', () => {
        return HttpResponse.json(
          {
            title: 'Validation Error',
            detail: 'Cannot modify expense lines: report is in Generated status and is locked for editing',
          },
          { status: 400 }
        )
      })
    )

    const { result } = renderHook(() => useSaveReport(), {
      wrapper: createWrapper(),
    })

    await act(async () => {
      result.current.mutate({ reportId, lines })
    })

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
  })

  it('handles not found error', async () => {
    const reportId = 'non-existent-report'
    const lines = [{ lineId: 'line-1', glCode: '65000' }]

    server.use(
      http.post('*/api/reports/:reportId/save', () => {
        return HttpResponse.json(
          {
            title: 'Not Found',
            detail: `Report with ID ${reportId} was not found`,
          },
          { status: 404 }
        )
      })
    )

    const { result } = renderHook(() => useSaveReport(), {
      wrapper: createWrapper(),
    })

    await act(async () => {
      result.current.mutate({ reportId, lines })
    })

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })
  })

  it('handles empty lines array (no changes to save)', async () => {
    const reportId = 'test-report-123'
    const lines: any[] = []

    server.use(
      http.post('*/api/reports/:reportId/save', () => {
        return HttpResponse.json({
          reportId,
          updatedCount: 0,
          failedCount: 0,
          updatedAt: new Date().toISOString(),
          reportStatus: 'Draft',
          failedLines: [],
        } satisfies BatchUpdateLinesResponse)
      })
    )

    const { result } = renderHook(() => useSaveReport(), {
      wrapper: createWrapper(),
    })

    await act(async () => {
      result.current.mutate({ reportId, lines })
    })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(result.current.data?.updatedCount).toBe(0)
  })
})
