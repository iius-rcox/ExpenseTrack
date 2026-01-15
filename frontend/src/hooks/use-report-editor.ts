import { useReducer, useMemo } from 'react'
import type {
  ReportEditorState,
  ReportEditorAction,
  EditableExpenseLine,
} from '@/types/report-editor'

/**
 * Validates a field and returns warning message if any.
 * Warnings are non-blocking - users can still export with warnings.
 */
function validateField(field: keyof EditableExpenseLine, value: any): string | null {
  switch (field) {
    case 'expenseDate': {
      const date = new Date(value)
      const daysOld = Math.floor((Date.now() - date.getTime()) / (1000 * 60 * 60 * 24))
      return daysOld > 90 ? 'Date is more than 90 days old' : null
    }
    case 'vendor':
      return !value || value.trim() === '' ? 'Vendor name recommended for AP processing' : null
    case 'glCode':
      return !value || value.trim() === '' ? 'GL code recommended' : null
    case 'departmentCode':
      return !value || value.trim() === '' ? 'Department code recommended' : null
    case 'description':
      return !value || value.trim() === '' ? 'Description is required' : null
    default:
      return null
  }
}

/**
 * Report editor reducer - handles all state updates.
 */
function reportEditorReducer(
  state: ReportEditorState,
  action: ReportEditorAction
): ReportEditorState {
  switch (action.type) {
    case 'LOAD_PREVIEW': {
      return {
        ...state,
        lines: action.lines.map((line: any, index: number) => {
          // Map API response fields to editor state
          const expenseDate = line.expenseDate
            ? new Date(line.expenseDate).toISOString().split('T')[0]
            : (line.transactionDate ? new Date(line.transactionDate).toISOString().split('T')[0] : new Date().toISOString().split('T')[0])

          // Generate truly unique ID: combine API ID (if exists) with index and random suffix
          const uniqueId = line.id && line.id !== '00000000-0000-0000-0000-000000000000'
            ? `${line.id}-${index}`
            : `${crypto.randomUUID()}-${index}`

          return {
            id: uniqueId,
            transactionId: line.transactionId || '',
            receiptId: line.receiptId || null,
            originalAmount: line.amount || 0,
            expenseDate,
            vendor: line.vendorName || line.vendor || '',
            glCode: line.glCode || line.glCodeSuggested || '',
            departmentCode: line.departmentCode || line.departmentSuggested || '',
            description: line.normalizedDescription || line.description || line.originalDescription || '',
            hasReceipt: line.hasReceipt || false,
            isDirty: false,
            validationWarnings: [],
            // Split allocation fields (initialize as not split)
            isSplit: false,
            isExpanded: false,
            allocations: [],
            appliedAllocations: undefined,
          }
        }),
        selectedLineIds: new Set(),
      }
    }

    case 'UPDATE_LINE': {
      return {
        ...state,
        lines: state.lines.map((line) => {
          // CRITICAL: Only update the specific line, return others unchanged
          if (line.id !== action.id) return line

          const newValue = action.value
          const warning = validateField(action.field, newValue)
          const newWarnings = [...line.validationWarnings].filter(
            (w) => !w.startsWith(action.field)
          )
          if (warning) {
            newWarnings.push(`${action.field}: ${warning}`)
          }

          // Deep clone to prevent shared references
          return {
            ...line,
            [action.field]: newValue,
            isDirty: true,
            validationWarnings: newWarnings,
            // Ensure nested objects are not shared
            allocations: [...line.allocations],
            appliedAllocations: line.appliedAllocations ? [...line.appliedAllocations] : undefined,
          }
        }),
      }
    }

    case 'BULK_UPDATE': {
      return {
        ...state,
        lines: state.lines.map((line) => {
          if (!action.ids.includes(line.id)) return line

          const newValue = action.value
          const warning = validateField(action.field, newValue)
          const newWarnings = line.validationWarnings.filter(
            (w) => !w.startsWith(action.field)
          )
          if (warning) {
            newWarnings.push(`${action.field}: ${warning}`)
          }

          return {
            ...line,
            [action.field]: newValue,
            isDirty: true,
            validationWarnings: newWarnings,
          }
        }),
      }
    }

    case 'SORT_BY': {
      const newOrder =
        state.sortBy === action.column && state.sortOrder === 'asc' ? 'desc' : 'asc'

      const sorted = [...state.lines].sort((a, b) => {
        const aVal = a[action.column]
        const bVal = b[action.column]

        // Handle different types
        if (typeof aVal === 'number' && typeof bVal === 'number') {
          return newOrder === 'asc' ? aVal - bVal : bVal - aVal
        }

        // String comparison
        const aStr = String(aVal || '')
        const bStr = String(bVal || '')
        return newOrder === 'asc'
          ? aStr.localeCompare(bStr)
          : bStr.localeCompare(aStr)
      })

      return {
        ...state,
        lines: sorted,
        sortBy: action.column,
        sortOrder: newOrder,
      }
    }

    case 'SELECT_LINES': {
      return {
        ...state,
        selectedLineIds: new Set(action.ids),
      }
    }

    case 'TOGGLE_LINE': {
      const newSelected = new Set(state.selectedLineIds)
      if (newSelected.has(action.id)) {
        newSelected.delete(action.id)
      } else {
        newSelected.add(action.id)
      }
      return {
        ...state,
        selectedLineIds: newSelected,
      }
    }

    case 'SELECT_ALL': {
      return {
        ...state,
        selectedLineIds: new Set(state.lines.map((l) => l.id)),
      }
    }

    case 'CLEAR_SELECTION': {
      return {
        ...state,
        selectedLineIds: new Set(),
      }
    }

    case 'RESET_LINE': {
      return {
        ...state,
        lines: state.lines.map((line) =>
          line.id === action.id
            ? { ...line, isDirty: false, validationWarnings: [] }
            : line
        ),
      }
    }

    // Split allocation actions
    case 'START_SPLIT': {
      return {
        ...state,
        lines: state.lines.map((line) =>
          line.id === action.id
            ? {
                ...line,
                isExpanded: true,
                allocations: [
                  {
                    id: crypto.randomUUID(),
                    glCode: line.glCode,
                    departmentCode: line.departmentCode,
                    percentage: 50,
                    amount: line.originalAmount * 0.5,
                    entryMode: 'amount' as const,
                  },
                  {
                    id: crypto.randomUUID(),
                    glCode: '',
                    departmentCode: '',
                    percentage: 50,
                    amount: line.originalAmount * 0.5,
                    entryMode: 'amount' as const,
                  },
                ],
              }
            : line
        ),
      }
    }

    case 'CANCEL_SPLIT': {
      return {
        ...state,
        lines: state.lines.map((line) =>
          line.id === action.id
            ? { ...line, isExpanded: false, allocations: [], isSplit: false }
            : line
        ),
      }
    }

    case 'ADD_ALLOCATION': {
      return {
        ...state,
        lines: state.lines.map((line) => {
          if (line.id !== action.parentId) return line

          const newAllocation = {
            id: crypto.randomUUID(),
            glCode: '',
            departmentCode: '',
            percentage: 0,
            amount: 0,
            entryMode: 'amount' as const,
          }

          return {
            ...line,
            allocations: [...line.allocations, newAllocation],
          }
        }),
      }
    }

    case 'REMOVE_ALLOCATION': {
      return {
        ...state,
        lines: state.lines.map((line) => {
          if (line.id !== action.parentId) return line

          return {
            ...line,
            allocations: line.allocations.filter((a) => a.id !== action.allocationId),
          }
        }),
      }
    }

    case 'UPDATE_ALLOCATION': {
      return {
        ...state,
        lines: state.lines.map((line) => {
          if (line.id !== action.parentId) return line

          return {
            ...line,
            allocations: line.allocations.map((alloc) => {
              if (alloc.id !== action.allocationId) return alloc

              const updated = { ...alloc, [action.field]: action.value }

              // Auto-calculate: if user updates percentage, recalc amount
              if (action.field === 'percentage') {
                updated.amount = (line.originalAmount * Number(action.value)) / 100
              }
              // Auto-calculate: if user updates amount, recalc percentage
              else if (action.field === 'amount') {
                updated.percentage = (Number(action.value) / line.originalAmount) * 100
              }

              return updated
            }),
          }
        }),
      }
    }

    case 'TOGGLE_ENTRY_MODE': {
      return {
        ...state,
        lines: state.lines.map((line) => {
          if (line.id !== action.parentId) return line

          return {
            ...line,
            allocations: line.allocations.map((alloc) =>
              alloc.id === action.allocationId
                ? { ...alloc, entryMode: alloc.entryMode === 'percentage' ? 'amount' as const : 'percentage' as const }
                : alloc
            ),
          }
        }),
      }
    }

    case 'APPLY_SPLIT': {
      return {
        ...state,
        lines: state.lines.map((line) => {
          if (line.id !== action.parentId) return line

          return {
            ...line,
            isSplit: true,
            isExpanded: false,
            appliedAllocations: [...line.allocations],
            isDirty: true,
          }
        }),
      }
    }

    case 'REMOVE_SPLIT': {
      return {
        ...state,
        lines: state.lines.map((line) =>
          line.id === action.id
            ? {
                ...line,
                isSplit: false,
                isExpanded: false,
                allocations: [],
                appliedAllocations: undefined,
                isDirty: true,
              }
            : line
        ),
      }
    }

    case 'TOGGLE_EXPANSION': {
      return {
        ...state,
        lines: state.lines.map((line) =>
          line.id === action.id ? { ...line, isExpanded: !line.isExpanded } : line
        ),
      }
    }

    default:
      return state
  }
}

/**
 * Hook for managing editable expense report state.
 */
export function useReportEditor(period: string) {
  const [state, dispatch] = useReducer(reportEditorReducer, {
    period,
    lines: [],
    sortBy: 'expenseDate' as keyof EditableExpenseLine,
    sortOrder: 'asc' as const,
    selectedLineIds: new Set<string>(),
  })

  // Computed metrics
  const metrics = useMemo(() => {
    const dirtyCount = state.lines.filter((l: EditableExpenseLine) => l.isDirty).length
    const warningCount = state.lines.filter((l: EditableExpenseLine) => l.validationWarnings.length > 0).length
    const totalAmount = state.lines.reduce((sum: number, l: EditableExpenseLine) => sum + l.originalAmount, 0)
    const selectedCount = state.selectedLineIds.size

    return {
      dirtyCount,
      warningCount,
      totalAmount,
      selectedCount,
    }
  }, [state.lines, state.selectedLineIds])

  return {
    state,
    dispatch,
    metrics,
  }
}
