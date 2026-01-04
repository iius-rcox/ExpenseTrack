/**
 * TanStack Query hooks for extraction corrections (training feedback).
 * Feature 024: Extraction Editor Training
 */

import { useQuery } from '@tanstack/react-query';
import { apiFetch } from '@/services/api';

/**
 * Extraction correction summary DTO.
 */
export interface ExtractionCorrection {
  id: string;
  receiptId: string;
  userId: string;
  userName: string;
  fieldName: string;
  originalValue: string;
  correctedValue: string | null;
  createdAt: string;
}

/**
 * Paginated result for extraction corrections.
 */
export interface ExtractionCorrectionPagedResult {
  items: ExtractionCorrection[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

/**
 * Query parameters for fetching extraction corrections.
 */
export interface ExtractionCorrectionQueryParams {
  page?: number;
  pageSize?: number;
  fieldName?: string;
  startDate?: string;
  endDate?: string;
  userId?: string;
  receiptId?: string;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

/**
 * Query keys for extraction corrections.
 */
export const extractionCorrectionKeys = {
  all: ['extraction-corrections'] as const,
  lists: () => [...extractionCorrectionKeys.all, 'list'] as const,
  list: (params: ExtractionCorrectionQueryParams) =>
    [...extractionCorrectionKeys.lists(), params] as const,
  details: () => [...extractionCorrectionKeys.all, 'detail'] as const,
  detail: (id: string) => [...extractionCorrectionKeys.details(), id] as const,
};

/**
 * Hook for fetching a paginated list of extraction corrections.
 * Supports filtering by field name, date range, user, and receipt.
 *
 * @example
 * const { data, isLoading } = useExtractionCorrections({
 *   page: 1,
 *   pageSize: 20,
 *   fieldName: 'vendor'
 * });
 */
export function useExtractionCorrections(
  params: ExtractionCorrectionQueryParams = {}
) {
  const {
    page = 1,
    pageSize = 20,
    fieldName,
    startDate,
    endDate,
    userId,
    receiptId,
    sortBy,
    sortDirection,
  } = params;

  return useQuery({
    queryKey: extractionCorrectionKeys.list(params),
    queryFn: async () => {
      const searchParams = new URLSearchParams();
      searchParams.set('page', String(page));
      searchParams.set('pageSize', String(pageSize));

      if (fieldName) searchParams.set('fieldName', fieldName);
      if (startDate) searchParams.set('startDate', startDate);
      if (endDate) searchParams.set('endDate', endDate);
      if (userId) searchParams.set('userId', userId);
      if (receiptId) searchParams.set('receiptId', receiptId);
      if (sortBy) searchParams.set('sortBy', sortBy);
      if (sortDirection) searchParams.set('sortDirection', sortDirection);

      return apiFetch<ExtractionCorrectionPagedResult>(
        `/extraction-corrections?${searchParams}`
      );
    },
  });
}

/**
 * Available field names for filtering corrections.
 */
export const CORRECTION_FIELD_NAMES = [
  { value: 'vendor', label: 'Vendor' },
  { value: 'amount', label: 'Amount' },
  { value: 'date', label: 'Date' },
  { value: 'tax', label: 'Tax' },
  { value: 'currency', label: 'Currency' },
] as const;
