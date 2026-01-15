/**
 * Types for the editable expense report workflow.
 * Supports stateless editing with on-demand export to Excel/PDF.
 */

/**
 * Editable expense line with dirty tracking and validation.
 */
export interface EditableExpenseLine {
  // Immutable identifiers
  readonly id: string
  readonly transactionId: string
  readonly receiptId: string | null
  readonly originalAmount: number

  // Editable fields
  expenseDate: string // ISO date string
  vendor: string
  glCode: string
  departmentCode: string
  description: string

  // Metadata
  hasReceipt: boolean
  isDirty: boolean
  validationWarnings: string[]
}

/**
 * Report editor state managed by useReducer.
 */
export interface ReportEditorState {
  period: string
  lines: EditableExpenseLine[]
  sortBy: keyof EditableExpenseLine | null
  sortOrder: 'asc' | 'desc'
  selectedLineIds: Set<string>
}

/**
 * Actions for report editor state updates.
 */
export type ReportEditorAction =
  | { type: 'LOAD_PREVIEW'; lines: any[] } // Accepts ExpenseLineDto from API
  | { type: 'UPDATE_LINE'; id: string; field: keyof EditableExpenseLine; value: any }
  | { type: 'BULK_UPDATE'; ids: string[]; field: keyof EditableExpenseLine; value: any }
  | { type: 'SORT_BY'; column: keyof EditableExpenseLine }
  | { type: 'SELECT_LINES'; ids: string[] }
  | { type: 'TOGGLE_LINE'; id: string }
  | { type: 'SELECT_ALL' }
  | { type: 'CLEAR_SELECTION' }
  | { type: 'RESET_LINE'; id: string }

/**
 * Export request payload matching backend ExportPreviewRequest DTO.
 */
export interface ExportPreviewRequest {
  period: string
  lines: ExportLineDto[]
}

/**
 * Export line DTO matching backend ExportLineDto.
 */
export interface ExportLineDto {
  expenseDate: string // DateOnly serialized as string
  vendorName: string
  glCode: string
  departmentCode: string
  description: string
  hasReceipt: boolean
  amount: number
}

/**
 * Reference data for GL accounts.
 */
export interface GLAccount {
  code: string
  name: string
  description?: string
}

/**
 * Reference data for departments.
 */
export interface Department {
  code: string
  name: string
  description?: string
}
