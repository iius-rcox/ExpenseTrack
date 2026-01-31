import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from '@/services/api'
import type { UserInfo, UserPreferences } from '@/types/api'

export const settingsKeys = {
  all: ['settings'] as const,
  user: () => [...settingsKeys.all, 'user'] as const,
  preferences: () => [...settingsKeys.all, 'preferences'] as const,
  departments: () => [...settingsKeys.all, 'departments'] as const,
  projects: () => [...settingsKeys.all, 'projects'] as const,
  categories: () => [...settingsKeys.all, 'categories'] as const,
}

export function useUserInfo() {
  return useQuery({
    queryKey: settingsKeys.user(),
    queryFn: () => apiFetch<UserInfo>('/user/me'),
    staleTime: 5 * 60 * 1000,
  })
}

export function useUserPreferences() {
  return useQuery({
    queryKey: settingsKeys.preferences(),
    queryFn: () => apiFetch<UserPreferences>('/user/preferences'),
    staleTime: 5 * 60 * 1000,
  })
}

export function useUpdatePreferences() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (preferences: Partial<UserPreferences>) => {
      return apiFetch<UserPreferences>('/user/preferences', {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(preferences),
      })
    },
    onSuccess: (data) => {
      queryClient.setQueryData(settingsKeys.preferences(), data)
      queryClient.invalidateQueries({ queryKey: settingsKeys.user() })
    },
  })
}

interface Department {
  id: string
  name: string
  code: string
}

interface Project {
  id: string
  name: string
  code: string
  departmentId: string
}

export interface Category {
  id: string
  name: string
  description?: string
  isActive: boolean
}

export function useDepartments() {
  return useQuery({
    queryKey: settingsKeys.departments(),
    queryFn: () => apiFetch<Department[]>('/reference/departments'),
    staleTime: 10 * 60 * 1000,
  })
}

export function useProjects(departmentId?: string) {
  return useQuery({
    queryKey: [...settingsKeys.projects(), departmentId],
    queryFn: () => {
      const params = departmentId ? `?departmentId=${departmentId}` : ''
      return apiFetch<Project[]>(`/reference/projects${params}`)
    },
    staleTime: 10 * 60 * 1000,
  })
}

/**
 * Category response from backend API.
 * The backend returns CategoryDto objects with id and name.
 */
interface CategoryApiResponse {
  categories: Array<{ id: string; name: string }>
}

/**
 * Safely extract category id - handles both object and string formats
 * for backwards compatibility with cached data.
 */
function safeCategoryId(cat: unknown, index: number): string {
  if (typeof cat === 'string') return `cat-${index}`
  if (typeof cat === 'object' && cat !== null && 'id' in cat) {
    return String((cat as { id: unknown }).id)
  }
  return `cat-${index}`
}

/**
 * Safely extract category name - handles both object and string formats
 * for backwards compatibility with cached data.
 */
function safeCategoryName(cat: unknown): string {
  if (typeof cat === 'string') return cat
  if (typeof cat === 'object' && cat !== null && 'name' in cat) {
    return String((cat as { name: unknown }).name)
  }
  return 'Unknown'
}

export function useCategories() {
  return useQuery({
    queryKey: settingsKeys.categories(),
    queryFn: () => apiFetch<CategoryApiResponse>('/transactions/categories')
      .then(res => res.categories.map((cat, index): Category => ({
        id: safeCategoryId(cat, index),
        name: safeCategoryName(cat),
        description: undefined, // Categories from transactions don't have descriptions
        isActive: true
      }))),
    staleTime: 10 * 60 * 1000,
  })
}

// Note: Category CRUD operations are not yet implemented in the backend.
// Categories are currently derived from transaction patterns.
// These hooks are stubbed for future implementation.

export function useCreateCategory() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (_data: { name: string; description?: string }) => {
      throw new Error('Category creation not yet implemented')
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: settingsKeys.categories() })
    },
  })
}

export function useUpdateCategory() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (_params: { id: string; name?: string; description?: string; isActive?: boolean }) => {
      throw new Error('Category update not yet implemented')
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: settingsKeys.categories() })
    },
  })
}

export function useDeleteCategory() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (_id: string) => {
      throw new Error('Category deletion not yet implemented')
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: settingsKeys.categories() })
    },
  })
}
