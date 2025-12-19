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

interface Category {
  id: string
  name: string
  description?: string
  isActive: boolean
}

export function useDepartments() {
  return useQuery({
    queryKey: settingsKeys.departments(),
    queryFn: () => apiFetch<Department[]>('/settings/departments'),
    staleTime: 10 * 60 * 1000,
  })
}

export function useProjects(departmentId?: string) {
  return useQuery({
    queryKey: [...settingsKeys.projects(), departmentId],
    queryFn: () => {
      const params = departmentId ? `?departmentId=${departmentId}` : ''
      return apiFetch<Project[]>(`/settings/projects${params}`)
    },
    staleTime: 10 * 60 * 1000,
  })
}

export function useCategories() {
  return useQuery({
    queryKey: settingsKeys.categories(),
    queryFn: () => apiFetch<Category[]>('/settings/categories'),
    staleTime: 10 * 60 * 1000,
  })
}

export function useCreateCategory() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (data: { name: string; description?: string }) => {
      return apiFetch<Category>('/settings/categories', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: settingsKeys.categories() })
    },
  })
}

export function useUpdateCategory() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, ...data }: { id: string; name?: string; description?: string; isActive?: boolean }) => {
      return apiFetch<Category>(`/settings/categories/${id}`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: settingsKeys.categories() })
    },
  })
}

export function useDeleteCategory() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (id: string) => {
      return apiFetch(`/settings/categories/${id}`, {
        method: 'DELETE',
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: settingsKeys.categories() })
    },
  })
}
