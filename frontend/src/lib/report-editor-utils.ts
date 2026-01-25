/**
 * Pure utility functions for the Report Editor component.
 * These functions have zero external dependencies and are easily testable.
 */

/**
 * Strips the index suffix from a frontend line ID to get the database ID.
 *
 * Frontend IDs have a "-{index}" or "-{timestamp}" suffix added by the reducer
 * (e.g., "abc-def-ghi-jkl-mno-0" or "abc-def-1706123456789").
 * This function removes just the last segment to get the original API ID.
 *
 * @param lineId - The frontend line ID with index/timestamp suffix
 * @returns The database line ID without the suffix
 *
 * @example
 * getDbLineId('abc-def-ghi-jkl-mno-0') // Returns 'abc-def-ghi-jkl-mno'
 * getDbLineId('line-001-2') // Returns 'line-001'
 */
export function getDbLineId(lineId: string): string {
  const parts = lineId.split('-')
  return parts.slice(0, -1).join('-')
}

/**
 * Calculates the next or previous period from a given YYYY-MM period string.
 *
 * Handles year rollovers correctly (December -> January and January -> December).
 *
 * @param period - The current period in YYYY-MM format
 * @param direction - 'prev' for previous month, 'next' for next month
 * @returns The new period in YYYY-MM format
 *
 * @example
 * calculateNextPeriod('2024-06', 'next') // Returns '2024-07'
 * calculateNextPeriod('2024-12', 'next') // Returns '2025-01'
 * calculateNextPeriod('2024-01', 'prev') // Returns '2023-12'
 */
export function calculateNextPeriod(
  period: string,
  direction: 'prev' | 'next'
): string {
  const [year, month] = period.split('-').map(Number)
  const date = new Date(year, month - 1, 1) // month is 0-indexed in Date constructor
  date.setMonth(date.getMonth() + (direction === 'next' ? 1 : -1))
  return date.toISOString().slice(0, 7)
}

/**
 * Formats a YYYY-MM period string into a human-readable format.
 *
 * @param period - The period in YYYY-MM format
 * @returns Human-readable string like "June 2024"
 *
 * @example
 * formatPeriodDisplay('2024-06') // Returns 'June 2024'
 * formatPeriodDisplay('2024-01') // Returns 'January 2024'
 */
export function formatPeriodDisplay(period: string): string {
  const [year, month] = period.split('-').map(Number)
  const date = new Date(year, month - 1, 1) // month is 0-indexed in Date constructor
  return date.toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
  })
}
