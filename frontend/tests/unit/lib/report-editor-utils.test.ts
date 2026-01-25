import { describe, it, expect } from 'vitest'
import {
  getDbLineId,
  calculateNextPeriod,
  formatPeriodDisplay,
} from '@/lib/report-editor-utils'

describe('report-editor-utils', () => {
  describe('getDbLineId', () => {
    it('strips index suffix from GUID-style frontend IDs', () => {
      // Frontend ID: "abc-def-ghi-jkl-mno-0" -> DB ID: "abc-def-ghi-jkl-mno"
      expect(getDbLineId('abc-def-ghi-jkl-mno-0')).toBe('abc-def-ghi-jkl-mno')
    })

    it('strips index suffix from simple frontend IDs', () => {
      // Frontend ID: "line-001-2" -> DB ID: "line-001"
      expect(getDbLineId('line-001-2')).toBe('line-001')
    })

    it('handles IDs with timestamp suffix', () => {
      // Frontend ID created with Date.now(): "abc-def-1706123456789"
      expect(getDbLineId('abc-def-1706123456789')).toBe('abc-def')
    })

    it('handles single-segment IDs gracefully', () => {
      // Edge case: single segment ID should return empty string
      expect(getDbLineId('abc')).toBe('')
    })

    it('handles two-segment IDs', () => {
      // Two segments: "line-0" -> "line"
      expect(getDbLineId('line-0')).toBe('line')
    })

    it('handles complex GUID format (5 segments + index)', () => {
      // Standard GUID: "a1b2c3d4-e5f6-7890-abcd-ef1234567890-0"
      expect(getDbLineId('a1b2c3d4-e5f6-7890-abcd-ef1234567890-0')).toBe(
        'a1b2c3d4-e5f6-7890-abcd-ef1234567890'
      )
    })
  })

  describe('calculateNextPeriod', () => {
    it('calculates next month within same year', () => {
      expect(calculateNextPeriod('2024-06', 'next')).toBe('2024-07')
    })

    it('calculates previous month within same year', () => {
      expect(calculateNextPeriod('2024-06', 'prev')).toBe('2024-05')
    })

    it('rolls over to next year from December', () => {
      expect(calculateNextPeriod('2024-12', 'next')).toBe('2025-01')
    })

    it('rolls back to previous year from January', () => {
      expect(calculateNextPeriod('2024-01', 'prev')).toBe('2023-12')
    })

    it('handles single-digit months by padding', () => {
      expect(calculateNextPeriod('2024-01', 'next')).toBe('2024-02')
      expect(calculateNextPeriod('2024-10', 'prev')).toBe('2024-09')
    })

    it('handles leap year correctly', () => {
      // Moving from Feb to March in leap year
      expect(calculateNextPeriod('2024-02', 'next')).toBe('2024-03')
      // Moving from March to Feb in leap year
      expect(calculateNextPeriod('2024-03', 'prev')).toBe('2024-02')
    })
  })

  describe('formatPeriodDisplay', () => {
    it('formats period as human-readable month and year', () => {
      expect(formatPeriodDisplay('2024-06')).toBe('June 2024')
    })

    it('formats January correctly', () => {
      expect(formatPeriodDisplay('2024-01')).toBe('January 2024')
    })

    it('formats December correctly', () => {
      expect(formatPeriodDisplay('2024-12')).toBe('December 2024')
    })

    it('handles different years', () => {
      expect(formatPeriodDisplay('2023-03')).toBe('March 2023')
      expect(formatPeriodDisplay('2025-11')).toBe('November 2025')
    })

    it('handles all months correctly', () => {
      const months = [
        ['2024-01', 'January 2024'],
        ['2024-02', 'February 2024'],
        ['2024-03', 'March 2024'],
        ['2024-04', 'April 2024'],
        ['2024-05', 'May 2024'],
        ['2024-06', 'June 2024'],
        ['2024-07', 'July 2024'],
        ['2024-08', 'August 2024'],
        ['2024-09', 'September 2024'],
        ['2024-10', 'October 2024'],
        ['2024-11', 'November 2024'],
        ['2024-12', 'December 2024'],
      ]

      months.forEach(([input, expected]) => {
        expect(formatPeriodDisplay(input)).toBe(expected)
      })
    })
  })
})
