import { z } from 'zod'

// Receipts page search params
export const receiptSearchSchema = z.object({
  page: z.number().optional().default(1),
  pageSize: z.number().optional().default(20),
  status: z.enum(['Pending', 'Processing', 'Processed', 'Unmatched', 'Matched', 'Error']).optional(),
  fromDate: z.string().optional(),
  toDate: z.string().optional(),
})
export type ReceiptSearchParams = z.infer<typeof receiptSearchSchema>

// Transactions page search params
export const transactionSearchSchema = z.object({
  page: z.number().optional().default(1),
  pageSize: z.number().optional().default(50),
  startDate: z.string().optional(),
  endDate: z.string().optional(),
  matched: z.boolean().optional(),
  importId: z.string().uuid().optional(),
  search: z.string().optional(),
})
export type TransactionSearchParams = z.infer<typeof transactionSearchSchema>

// Matching page search params
export const matchingSearchSchema = z.object({
  page: z.number().optional().default(1),
  pageSize: z.number().optional().default(20),
  tab: z.enum(['proposals', 'unmatched-receipts', 'unmatched-transactions']).optional().default('proposals'),
})
export type MatchingSearchParams = z.infer<typeof matchingSearchSchema>

// Reports page search params
export const reportSearchSchema = z.object({
  page: z.number().optional().default(1),
  pageSize: z.number().optional().default(20),
  status: z.enum(['Draft', 'Submitted', 'Approved', 'Rejected']).optional(),
  period: z.string().optional(),
})
export type ReportSearchParams = z.infer<typeof reportSearchSchema>

// Analytics page search params
export const analyticsSearchSchema = z.object({
  currentPeriod: z.string().optional(),
  previousPeriod: z.string().optional(),
  view: z.enum(['comparison', 'cache-stats']).optional().default('comparison'),
})
export type AnalyticsSearchParams = z.infer<typeof analyticsSearchSchema>

// Login redirect
export const loginSearchSchema = z.object({
  redirect: z.string().optional(),
})
export type LoginSearchParams = z.infer<typeof loginSearchSchema>
