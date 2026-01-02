/**
 * Prediction Availability Hook (T039)
 *
 * TanStack Query hook for checking if predictions are available.
 * Used to determine if expense prediction features should be shown.
 *
 * @see specs/023-expense-prediction/contracts/predictions-api.yaml for API contract
 */

import { useQuery } from '@tanstack/react-query'
import { apiFetch } from '@/services/api'
import type { PredictionAvailability } from '@/types/prediction'
import { predictionKeys } from './use-predictions'

/**
 * Hook to check if predictions are available for the current user.
 *
 * Returns:
 * - isAvailable: true if user has at least one learned pattern
 * - patternCount: number of learned patterns
 * - message: user-friendly status message
 *
 * Use cases:
 * - Conditionally render expense badge features
 * - Show onboarding prompts when no patterns exist
 * - Display cold-start guidance
 */
export function usePredictionAvailability() {
  return useQuery({
    queryKey: [...predictionKeys.all, 'availability'] as const,
    queryFn: async () => {
      return apiFetch<PredictionAvailability>('/predictions/availability')
    },
    staleTime: 60_000, // 1 minute - patterns don't change often
    gcTime: 300_000, // 5 minutes - keep in cache longer
  })
}

/**
 * Hook to check availability with prefetching for performance.
 * Call this early in the app lifecycle to warm the cache.
 */
export function usePrefetchPredictionAvailability() {
  const query = usePredictionAvailability()
  return {
    isAvailable: query.data?.isAvailable ?? false,
    patternCount: query.data?.patternCount ?? 0,
    message: query.data?.message ?? '',
    isLoading: query.isLoading,
    isError: query.isError,
  }
}

/**
 * Hook for conditional rendering based on availability.
 * Returns null during loading to prevent flash of wrong state.
 */
export function usePredictionEnabled() {
  const { data, isLoading } = usePredictionAvailability()

  if (isLoading) {
    return { enabled: null, isLoading: true }
  }

  return {
    enabled: data?.isAvailable ?? false,
    isLoading: false,
    patternCount: data?.patternCount ?? 0,
  }
}
