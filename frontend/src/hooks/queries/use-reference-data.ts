import { useQuery } from '@tanstack/react-query'
import { apiFetch } from '@/services/api'

/**
 * GL Account from Vista.
 */
export interface GLAccount {
  id: string
  code: string
  name: string
  description?: string
  isActive: boolean
}

/**
 * Department from Vista.
 */
export interface Department {
  id: string
  code: string
  name: string
  description?: string
  isActive: boolean
}

export const referenceKeys = {
  glAccounts: ['reference', 'gl-accounts'] as const,
  departments: ['reference', 'departments'] as const,
}

/**
 * Fetches GL accounts from Vista (synced to local PostgreSQL).
 * Cached for 30 minutes.
 */
export function useGLAccounts(activeOnly: boolean = true) {
  return useQuery({
    queryKey: [...referenceKeys.glAccounts, { activeOnly }],
    queryFn: () => apiFetch<GLAccount[]>(`/reference/gl-accounts?activeOnly=${activeOnly}`),
    staleTime: 30 * 60 * 1000, // 30 minutes
  })
}

/**
 * Fetches departments from Vista (synced to local PostgreSQL).
 * Cached for 30 minutes.
 */
export function useDepartments(activeOnly: boolean = true) {
  return useQuery({
    queryKey: [...referenceKeys.departments, { activeOnly }],
    queryFn: () => apiFetch<Department[]>(`/reference/departments?activeOnly=${activeOnly}`),
    staleTime: 30 * 60 * 1000, // 30 minutes
  })
}

/**
 * Helper: Lookup GL account name by code.
 */
export function lookupGLName(glCode: string, glAccounts: GLAccount[]): string {
  const account = glAccounts.find((a) => a.code === glCode)
  return account?.name || ''
}

/**
 * Helper: Lookup department name by code.
 */
export function lookupDepartmentName(deptCode: string, departments: Department[]): string {
  const dept = departments.find((d) => d.code === deptCode)
  return dept?.name || ''
}
