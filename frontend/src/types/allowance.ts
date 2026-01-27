/**
 * Recurring Expense Allowance Types
 *
 * These types support the recurring expense allowances feature,
 * which allows users to pre-configure expected recurring expenses
 * (e.g., monthly subscriptions) for automatic categorization.
 */

/**
 * Frequency of the recurring allowance
 */
export enum AllowanceFrequency {
  Weekly = 0,
  Monthly = 1,
  Quarterly = 2,
}

/**
 * Helper function to convert frequency enum to display text
 */
export function getFrequencyDisplayText(frequency: AllowanceFrequency): string {
  switch (frequency) {
    case AllowanceFrequency.Weekly:
      return 'Weekly'
    case AllowanceFrequency.Monthly:
      return 'Monthly'
    case AllowanceFrequency.Quarterly:
      return 'Quarterly'
    default:
      return 'Unknown'
  }
}

/**
 * Sentinel value for "None" option in Radix Select components.
 * Radix Select doesn't handle empty strings well, so we use a sentinel.
 */
export const NONE_SELECTED = '__NONE__'

/**
 * Maximum allowance amount (matches backend validation)
 */
export const MAX_ALLOWANCE_AMOUNT = 100000

/**
 * Maximum description length (matches backend validation)
 */
export const MAX_DESCRIPTION_LENGTH = 500

/**
 * Recurring expense allowance entity
 */
export interface Allowance {
  id: string
  userId: string
  vendorName: string
  amount: number
  frequency: AllowanceFrequency
  glCode: string | null
  glName: string | null
  departmentCode: string | null
  description: string | null
  isActive: boolean
  createdAt: string
  updatedAt: string | null
}

/**
 * Request payload for creating a new allowance
 */
export interface CreateAllowanceRequest {
  vendorName: string
  amount: number
  frequency: AllowanceFrequency
  glCode?: string
  departmentCode?: string
  description?: string
}

/**
 * Request payload for updating an existing allowance
 */
export interface UpdateAllowanceRequest {
  vendorName?: string
  amount?: number
  frequency?: AllowanceFrequency
  glCode?: string | null
  departmentCode?: string | null
  description?: string | null
  isActive?: boolean
}

/**
 * Response from the list allowances endpoint
 */
export interface AllowanceListResponse {
  items: Allowance[]
  totalCount: number
}
