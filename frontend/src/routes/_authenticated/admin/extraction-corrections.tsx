'use client'

/**
 * Extraction Corrections Admin Page (T050)
 *
 * Admin view for extraction correction history with filtering.
 * Feature 024: Extraction Editor Training
 *
 * Features:
 * - Paginated list of all extraction corrections
 * - Filter by field name, date range, and user
 * - View original vs corrected values for training analysis
 * - Link to receipt detail for context
 */

import { useState, useCallback } from 'react'
import { createFileRoute, Link } from '@tanstack/react-router'
import { z } from 'zod'
import { motion } from 'framer-motion'
import { fadeIn } from '@/lib/animations'
import { Button } from '@/components/ui/button'
import {
  ArrowLeft,
  RefreshCcw,
} from 'lucide-react'
import { ExtractionCorrectionsList } from '@/components/admin/extraction-corrections-list'
import type { ExtractionCorrectionQueryParams } from '@/hooks/queries/use-extraction-corrections'

// Search params schema for URL state
const correctionsSearchSchema = z.object({
  page: z.coerce.number().optional().default(1),
  pageSize: z.coerce.number().optional().default(20),
  fieldName: z.string().optional(),
  startDate: z.string().optional(),
  endDate: z.string().optional(),
  userId: z.string().optional(),
  sortBy: z.string().optional().default('createdAt'),
  sortDirection: z.enum(['asc', 'desc']).optional().default('desc'),
})

export const Route = createFileRoute('/_authenticated/admin/extraction-corrections')({
  validateSearch: correctionsSearchSchema,
  component: ExtractionCorrectionsPage,
})

function ExtractionCorrectionsPage() {
  const search = Route.useSearch()
  const navigate = Route.useNavigate()

  // Build query params from URL search
  const [queryParams, setQueryParams] = useState<ExtractionCorrectionQueryParams>({
    page: search.page,
    pageSize: search.pageSize,
    fieldName: search.fieldName,
    startDate: search.startDate,
    endDate: search.endDate,
    userId: search.userId,
    sortBy: search.sortBy,
    sortDirection: search.sortDirection,
  })

  // Handle filter changes
  const handleFilterChange = useCallback(
    (newParams: ExtractionCorrectionQueryParams) => {
      setQueryParams(newParams)
      navigate({
        search: {
          page: newParams.page ?? 1,
          pageSize: newParams.pageSize ?? 20,
          fieldName: newParams.fieldName,
          startDate: newParams.startDate,
          endDate: newParams.endDate,
          userId: newParams.userId,
          sortBy: newParams.sortBy ?? 'createdAt',
          sortDirection: newParams.sortDirection ?? 'desc',
        },
      })
    },
    [navigate]
  )

  // Handle page change
  const handlePageChange = useCallback(
    (page: number) => {
      handleFilterChange({ ...queryParams, page })
    },
    [queryParams, handleFilterChange]
  )

  // Handle refresh
  const handleRefresh = useCallback(() => {
    // Re-trigger by resetting with same params
    handleFilterChange({ ...queryParams })
  }, [queryParams, handleFilterChange])

  return (
    <motion.div
      variants={fadeIn}
      initial="hidden"
      animate="visible"
      className="space-y-6"
    >
      {/* Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-4">
          <Button variant="ghost" size="icon" asChild>
            <Link to="/analytics">
              <ArrowLeft className="h-4 w-4" />
            </Link>
          </Button>
          <div>
            <h1 className="text-3xl font-bold tracking-tight">Extraction Corrections</h1>
            <p className="text-muted-foreground">
              View correction history for model training analysis
            </p>
          </div>
        </div>
        <Button variant="outline" size="sm" onClick={handleRefresh}>
          <RefreshCcw className="h-4 w-4 mr-2" />
          Refresh
        </Button>
      </div>

      {/* Corrections List */}
      <ExtractionCorrectionsList
        queryParams={queryParams}
        onFilterChange={handleFilterChange}
        onPageChange={handlePageChange}
      />
    </motion.div>
  )
}

export default ExtractionCorrectionsPage
