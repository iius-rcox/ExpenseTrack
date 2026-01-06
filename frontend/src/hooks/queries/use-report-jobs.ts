import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from '@/services/api'
import type {
  ReportJob,
  ReportJobListResponse,
  CreateReportJobRequest,
  ActiveJobResponse,
  ReportJobStatus,
} from '@/types/api'

/**
 * Query keys for report job caching.
 */
export const reportJobKeys = {
  all: ['report-jobs'] as const,
  lists: () => [...reportJobKeys.all, 'list'] as const,
  list: (filters: Record<string, unknown>) => [...reportJobKeys.lists(), filters] as const,
  details: () => [...reportJobKeys.all, 'detail'] as const,
  detail: (id: string) => [...reportJobKeys.details(), id] as const,
  active: (period: string) => [...reportJobKeys.all, 'active', period] as const,
}

/**
 * Check if a job status is terminal (no more progress possible).
 */
export function isTerminalStatus(status: ReportJobStatus): boolean {
  return status === 'Completed' || status === 'Failed' || status === 'Cancelled'
}

/**
 * Calculate adaptive polling interval based on job progress.
 * - Just started (< 10%): 2s - fast updates for initial feedback
 * - Mid progress (10-90%): 3s - moderate updates
 * - Near completion (> 90%): 5s - slower as we're almost done
 * - Terminal states: false (no polling)
 */
function getPollingInterval(job: ReportJob | undefined): number | false {
  if (!job) return false
  if (isTerminalStatus(job.status)) return false

  const progress = job.progressPercent
  if (progress < 10) return 2000 // 2 seconds - fast initial updates
  if (progress < 90) return 3000 // 3 seconds - moderate updates
  return 5000 // 5 seconds - near completion
}

interface ReportJobListParams {
  page?: number
  pageSize?: number
  status?: ReportJobStatus
}

/**
 * Fetch paginated list of report generation jobs.
 */
export function useReportJobList(params: ReportJobListParams = {}) {
  const { page = 1, pageSize = 20, status } = params

  return useQuery({
    queryKey: reportJobKeys.list({ page, pageSize, status }),
    queryFn: async () => {
      const searchParams = new URLSearchParams()
      searchParams.set('page', String(page))
      searchParams.set('pageSize', String(pageSize))
      if (status) searchParams.set('status', status)

      return apiFetch<ReportJobListResponse>(`/report-jobs?${searchParams}`)
    },
  })
}

/**
 * Fetch a single report job by ID with adaptive polling.
 * Automatically polls while job is in progress, stops when terminal.
 */
export function useReportJob(jobId: string | null, options?: { enabled?: boolean }) {
  return useQuery({
    queryKey: reportJobKeys.detail(jobId ?? ''),
    queryFn: () => apiFetch<ReportJob>(`/report-jobs/${jobId}`),
    enabled: !!jobId && (options?.enabled ?? true),
    refetchInterval: (query) => {
      // Adaptive polling based on job progress
      return getPollingInterval(query.state.data)
    },
    // Keep previous data to avoid flickering during refetch
    placeholderData: (previousData) => previousData,
  })
}

/**
 * Check if there's an active job for a specific period.
 * Useful before creating a new job to avoid duplicates.
 */
export function useActiveReportJob(period: string, options?: { enabled?: boolean }) {
  return useQuery({
    queryKey: reportJobKeys.active(period),
    queryFn: () => apiFetch<ActiveJobResponse>(`/report-jobs/active?period=${period}`),
    enabled: !!period && (options?.enabled ?? true),
    staleTime: 10_000, // 10 seconds - don't refetch too frequently
  })
}

/**
 * Create a new report generation job.
 * Returns the created job immediately (202 Accepted).
 */
export function useCreateReportJob() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (data: CreateReportJobRequest) => {
      return apiFetch<ReportJob>('/report-jobs', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
      })
    },
    onSuccess: (job) => {
      // Invalidate list and active job queries
      queryClient.invalidateQueries({ queryKey: reportJobKeys.lists() })
      queryClient.invalidateQueries({ queryKey: reportJobKeys.active(job.period) })

      // Pre-populate the job detail cache
      queryClient.setQueryData(reportJobKeys.detail(job.id), job)
    },
  })
}

/**
 * Cancel an in-progress report generation job.
 * Job will be marked as CancellationRequested and stop at next checkpoint.
 */
export function useCancelReportJob() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (jobId: string) => {
      return apiFetch<ReportJob>(`/report-jobs/${jobId}`, {
        method: 'DELETE',
      })
    },
    onSuccess: (job) => {
      // Update the job in cache
      queryClient.setQueryData(reportJobKeys.detail(job.id), job)
      queryClient.invalidateQueries({ queryKey: reportJobKeys.lists() })
      queryClient.invalidateQueries({ queryKey: reportJobKeys.active(job.period) })
    },
  })
}

/**
 * Hook that provides the complete async report generation flow.
 * Combines job creation with status polling for a seamless experience.
 */
export function useAsyncReportGeneration(options?: {
  onComplete?: (reportId: string) => void
  onError?: (error: Error) => void
}) {
  const createJob = useCreateReportJob()
  const cancelJob = useCancelReportJob()

  // Track the current job ID for polling
  const currentJobId = createJob.data?.id ?? null

  // Poll the job status
  const jobStatus = useReportJob(currentJobId, {
    enabled: !!currentJobId,
  })

  // Handle completion callback
  const job = jobStatus.data
  if (job && isTerminalStatus(job.status)) {
    if (job.status === 'Completed' && job.generatedReportId) {
      options?.onComplete?.(job.generatedReportId)
    } else if (job.status === 'Failed' && job.errorMessage) {
      options?.onError?.(new Error(job.errorMessage))
    }
  }

  return {
    // Job creation
    createJob: createJob.mutate,
    createJobAsync: createJob.mutateAsync,
    isCreating: createJob.isPending,
    createError: createJob.error,

    // Job status
    job: jobStatus.data,
    isPolling: jobStatus.isFetching && !isTerminalStatus(job?.status ?? 'Pending'),
    jobError: jobStatus.error,

    // Cancellation
    cancelJob: cancelJob.mutate,
    isCancelling: cancelJob.isPending,

    // Computed state
    isActive: !!currentJobId && !isTerminalStatus(job?.status ?? 'Pending'),
    isComplete: job?.status === 'Completed',
    isFailed: job?.status === 'Failed',
    isCancelled: job?.status === 'Cancelled',

    // Reset
    reset: () => {
      createJob.reset()
    },
  }
}
