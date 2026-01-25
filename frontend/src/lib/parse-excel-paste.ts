/**
 * Parses Excel copy/paste data (tab-separated values) into structured allocation data.
 *
 * Supports two formats:
 * - 2 columns: Department Code, Amount
 * - 3 columns: GL Code, Department Code, Amount
 *
 * @example
 * // 2-column format (Dept, Amount)
 * parseExcelPaste("07\t348.34\n11\t31.67")
 * // Returns: [{ departmentCode: "07", amount: 348.34 }, { departmentCode: "11", amount: 31.67 }]
 *
 * @example
 * // 3-column format (GL, Dept, Amount)
 * parseExcelPaste("5000\t07\t348.34\n5100\t11\t31.67")
 * // Returns: [{ glCode: "5000", departmentCode: "07", amount: 348.34 }, ...]
 */

export interface ParsedAllocation {
  glCode?: string
  departmentCode: string
  amount: number
}

export interface ParseResult {
  success: boolean
  allocations: ParsedAllocation[]
  errors: string[]
  format: '2-column' | '3-column' | 'unknown'
}

/**
 * Parses tab-separated clipboard text into allocation data.
 */
export function parseExcelPaste(clipboardText: string): ParseResult {
  const errors: string[] = []
  const allocations: ParsedAllocation[] = []

  // Normalize line endings and split into rows
  const rows = clipboardText
    .trim()
    .replace(/\r\n/g, '\n')
    .replace(/\r/g, '\n')
    .split('\n')
    .filter(row => row.trim() !== '')

  if (rows.length === 0) {
    return {
      success: false,
      allocations: [],
      errors: ['No data found in clipboard'],
      format: 'unknown',
    }
  }

  // Detect format from first row
  const firstRowColumns = rows[0].split('\t')
  const columnCount = firstRowColumns.length

  if (columnCount < 2 || columnCount > 3) {
    return {
      success: false,
      allocations: [],
      errors: [`Expected 2 or 3 columns, found ${columnCount}`],
      format: 'unknown',
    }
  }

  const format = columnCount === 2 ? '2-column' : '3-column'

  // Parse each row
  rows.forEach((row, index) => {
    const columns = row.split('\t').map(col => col.trim())

    // Skip rows with wrong column count
    if (columns.length !== columnCount) {
      errors.push(`Row ${index + 1}: Expected ${columnCount} columns, found ${columns.length}`)
      return
    }

    let glCode: string | undefined
    let departmentCode: string
    let amountStr: string

    if (format === '2-column') {
      // Format: Department, Amount
      departmentCode = columns[0]
      amountStr = columns[1]
    } else {
      // Format: GL Code, Department, Amount
      glCode = columns[0]
      departmentCode = columns[1]
      amountStr = columns[2]
    }

    // Parse amount - handle various number formats
    const cleanAmount = amountStr
      .replace(/[$,]/g, '') // Remove $ and commas
      .replace(/\s/g, '') // Remove whitespace

    const amount = parseFloat(cleanAmount)

    if (isNaN(amount)) {
      errors.push(`Row ${index + 1}: Invalid amount "${amountStr}"`)
      return
    }

    allocations.push({
      ...(glCode && { glCode }),
      departmentCode,
      amount,
    })
  })

  return {
    success: allocations.length > 0,
    allocations,
    errors,
    format,
  }
}

/**
 * Detects if clipboard text looks like Excel data (tab-separated).
 */
export function isExcelData(text: string): boolean {
  // Check if text contains tabs (Excel uses tabs between columns)
  return text.includes('\t') && text.trim().length > 0
}
