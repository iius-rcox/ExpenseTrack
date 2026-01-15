/**
 * Types for the editable expense report workflow.
 * Supports stateless editing with on-demand export to Excel/PDF.
 */

/**
 * Split allocation within an expense.
 */
export interface SplitAllocation {
  id: string // Temporary UI ID
  glCode: string
  departmentCode: string
  percentage: number // User enters this OR amount
  amount: number // Auto-calculated OR user entered
  description?: string
  entryMode: 'percentage' | 'amount' // Which field user is actively editing
}

/**
 * Editable expense line with dirty tracking, validation, and split support.
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

  // Split allocation support
  isSplit: boolean // Has been split into multiple allocations
  isExpanded: boolean // UI expansion state
  allocations: SplitAllocation[] // Child allocations (editing state)
  appliedAllocations?: SplitAllocation[] // Saved allocations after Apply
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
  // Split allocation actions
  | { type: 'START_SPLIT'; id: string }
  | { type: 'CANCEL_SPLIT'; id: string }
  | { type: 'ADD_ALLOCATION'; parentId: string }
  | { type: 'REMOVE_ALLOCATION'; parentId: string; allocationId: string }
  | { type: 'UPDATE_ALLOCATION'; parentId: string; allocationId: string; field: keyof SplitAllocation; value: any }
  | { type: 'TOGGLE_ENTRY_MODE'; parentId: string; allocationId: string }
  | { type: 'APPLY_SPLIT'; parentId: string }
  | { type: 'REMOVE_SPLIT'; id: string }
  | { type: 'TOGGLE_EXPANSION'; id: string }

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
  childAllocations?: ExportLineDto[] // Split allocations if this is a split parent
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
