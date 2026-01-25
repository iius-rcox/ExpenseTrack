import { describe, it, expect } from 'vitest'
import { parseExcelPaste, isExcelData } from '@/lib/parse-excel-paste'

describe('parseExcelPaste', () => {
  describe('2-column format (Department, Amount)', () => {
    it('should parse basic 2-column data', () => {
      const input = `07\t348.34
11\t31.67
22\t31.67`

      const result = parseExcelPaste(input)

      expect(result.success).toBe(true)
      expect(result.format).toBe('2-column')
      expect(result.allocations).toHaveLength(3)
      expect(result.allocations[0]).toEqual({ departmentCode: '07', amount: 348.34 })
      expect(result.allocations[1]).toEqual({ departmentCode: '11', amount: 31.67 })
      expect(result.allocations[2]).toEqual({ departmentCode: '22', amount: 31.67 })
    })

    it('should handle the user example data', () => {
      // Real example from user: "07    348.34\n11    31.67\n22    31.67\n43    31.67\n44    31.67\n92633.    95"
      // Note: The last line seems malformed (92633.    95), let's handle gracefully
      const input = `07\t348.34
11\t31.67
22\t31.67
43\t31.67
44\t31.67`

      const result = parseExcelPaste(input)

      expect(result.success).toBe(true)
      expect(result.allocations).toHaveLength(5)
      expect(result.allocations[0]).toEqual({ departmentCode: '07', amount: 348.34 })
      expect(result.allocations[4]).toEqual({ departmentCode: '44', amount: 31.67 })
    })

    it('should handle amounts with dollar signs and commas', () => {
      const input = `07\t$1,234.56
11\t$99.99`

      const result = parseExcelPaste(input)

      expect(result.success).toBe(true)
      expect(result.allocations[0].amount).toBe(1234.56)
      expect(result.allocations[1].amount).toBe(99.99)
    })

    it('should handle Windows line endings (CRLF)', () => {
      const input = `07\t100.00\r\n11\t200.00\r\n22\t300.00`

      const result = parseExcelPaste(input)

      expect(result.success).toBe(true)
      expect(result.allocations).toHaveLength(3)
    })

    it('should skip empty lines', () => {
      const input = `07\t100.00

11\t200.00

22\t300.00`

      const result = parseExcelPaste(input)

      expect(result.success).toBe(true)
      expect(result.allocations).toHaveLength(3)
    })

    it('should trim whitespace from values', () => {
      const input = `  07  \t  100.00
  11  \t  200.00  `

      const result = parseExcelPaste(input)

      expect(result.success).toBe(true)
      expect(result.allocations[0].departmentCode).toBe('07')
      expect(result.allocations[0].amount).toBe(100)
    })
  })

  describe('3-column format (GL Code, Department, Amount)', () => {
    it('should parse 3-column data with GL codes', () => {
      const input = `5000\t07\t348.34
5100\t11\t31.67
5200\t22\t31.67`

      const result = parseExcelPaste(input)

      expect(result.success).toBe(true)
      expect(result.format).toBe('3-column')
      expect(result.allocations).toHaveLength(3)
      expect(result.allocations[0]).toEqual({
        glCode: '5000',
        departmentCode: '07',
        amount: 348.34,
      })
    })

    it('should handle GL codes with various formats', () => {
      const input = `5000-100\t07\t100.00
GL-5100\t11\t200.00`

      const result = parseExcelPaste(input)

      expect(result.success).toBe(true)
      expect(result.allocations[0].glCode).toBe('5000-100')
      expect(result.allocations[1].glCode).toBe('GL-5100')
    })
  })

  describe('error handling', () => {
    it('should return error for empty input', () => {
      const result = parseExcelPaste('')

      expect(result.success).toBe(false)
      expect(result.errors).toContain('No data found in clipboard')
    })

    it('should return error for whitespace-only input', () => {
      const result = parseExcelPaste('   \n\n   ')

      expect(result.success).toBe(false)
      expect(result.errors).toContain('No data found in clipboard')
    })

    it('should return error for single column data', () => {
      const input = `07
11
22`

      const result = parseExcelPaste(input)

      expect(result.success).toBe(false)
      expect(result.errors[0]).toContain('Expected 2 or 3 columns')
    })

    it('should return error for 4+ column data', () => {
      const input = `5000\t07\t100.00\textra`

      const result = parseExcelPaste(input)

      expect(result.success).toBe(false)
      expect(result.errors[0]).toContain('Expected 2 or 3 columns')
    })

    it('should report invalid amounts but continue parsing', () => {
      const input = `07\t100.00
11\tinvalid
22\t200.00`

      const result = parseExcelPaste(input)

      expect(result.success).toBe(true)
      expect(result.allocations).toHaveLength(2) // Row 2 skipped
      expect(result.errors).toHaveLength(1)
      expect(result.errors[0]).toContain('Invalid amount')
    })

    it('should report rows with wrong column count', () => {
      const input = `07\t100.00
11\t200.00\textra
22\t300.00`

      const result = parseExcelPaste(input)

      expect(result.success).toBe(true)
      expect(result.allocations).toHaveLength(2) // Row 2 skipped
      expect(result.errors).toHaveLength(1)
      expect(result.errors[0]).toContain('Expected 2 columns')
    })
  })

  describe('special number formats', () => {
    it('should handle negative amounts', () => {
      const input = `07\t-100.00
11\t-50.50`

      const result = parseExcelPaste(input)

      expect(result.success).toBe(true)
      expect(result.allocations[0].amount).toBe(-100)
      expect(result.allocations[1].amount).toBe(-50.5)
    })

    it('should handle amounts without decimals', () => {
      const input = `07\t100
11\t200`

      const result = parseExcelPaste(input)

      expect(result.success).toBe(true)
      expect(result.allocations[0].amount).toBe(100)
      expect(result.allocations[1].amount).toBe(200)
    })

    it('should handle amounts with leading zeros', () => {
      const input = `07\t00100.00
11\t050.00`

      const result = parseExcelPaste(input)

      expect(result.success).toBe(true)
      expect(result.allocations[0].amount).toBe(100)
      expect(result.allocations[1].amount).toBe(50)
    })
  })
})

describe('isExcelData', () => {
  it('should return true for tab-separated data', () => {
    expect(isExcelData('07\t100.00')).toBe(true)
    expect(isExcelData('a\tb\tc')).toBe(true)
  })

  it('should return false for plain text without tabs', () => {
    expect(isExcelData('hello world')).toBe(false)
    expect(isExcelData('07 100.00')).toBe(false)
  })

  it('should return false for empty string', () => {
    expect(isExcelData('')).toBe(false)
    expect(isExcelData('   ')).toBe(false)
  })
})
