/**
 * Filter Presets Hook
 *
 * Manages saved filter presets using localStorage.
 * Allows users to save, load, and delete named filter configurations.
 *
 * Storage key: 'expenseflow-filter-presets'
 */

import { useState, useCallback, useEffect } from 'react'
import type { TransactionFilters } from '@/types/transaction'

/**
 * A saved filter preset with metadata.
 */
export interface FilterPreset {
  /** Unique preset ID */
  id: string
  /** User-defined preset name */
  name: string
  /** The saved filter configuration */
  filters: TransactionFilters
  /** When the preset was created */
  createdAt: string
  /** When the preset was last used */
  lastUsedAt?: string
}

const STORAGE_KEY = 'expenseflow-filter-presets'

/**
 * Generate a unique ID for presets.
 */
function generateId(): string {
  return `preset-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`
}

/**
 * Load presets from localStorage.
 */
function loadPresets(): FilterPreset[] {
  if (typeof window === 'undefined') return []
  try {
    const stored = localStorage.getItem(STORAGE_KEY)
    if (!stored) return []
    const parsed = JSON.parse(stored)
    // Validate structure
    if (!Array.isArray(parsed)) return []
    return parsed.filter(
      (p): p is FilterPreset =>
        typeof p === 'object' &&
        p !== null &&
        typeof p.id === 'string' &&
        typeof p.name === 'string' &&
        typeof p.filters === 'object'
    )
  } catch {
    console.warn('[useFilterPresets] Failed to parse stored presets')
    return []
  }
}

/**
 * Save presets to localStorage.
 */
function savePresets(presets: FilterPreset[]): void {
  if (typeof window === 'undefined') return
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(presets))
  } catch (error) {
    console.error('[useFilterPresets] Failed to save presets:', error)
  }
}

/**
 * Hook for managing filter presets.
 *
 * @example
 * ```tsx
 * const { presets, savePreset, loadPreset, deletePreset } = useFilterPresets();
 *
 * // Save current filters
 * savePreset('My Filters', currentFilters);
 *
 * // Load a preset
 * const filters = loadPreset('preset-123');
 * if (filters) setFilters(filters);
 *
 * // Delete a preset
 * deletePreset('preset-123');
 * ```
 */
export function useFilterPresets() {
  const [presets, setPresets] = useState<FilterPreset[]>([])

  // Load presets on mount
  useEffect(() => {
    setPresets(loadPresets())
  }, [])

  /**
   * Save a new filter preset.
   */
  const savePreset = useCallback(
    (name: string, filters: TransactionFilters): FilterPreset => {
      const newPreset: FilterPreset = {
        id: generateId(),
        name: name.trim(),
        filters: { ...filters },
        createdAt: new Date().toISOString(),
      }

      const updatedPresets = [...presets, newPreset]
      setPresets(updatedPresets)
      savePresets(updatedPresets)

      return newPreset
    },
    [presets]
  )

  /**
   * Load filters from a preset (marks it as recently used).
   */
  const loadPreset = useCallback(
    (presetId: string): TransactionFilters | null => {
      const preset = presets.find((p) => p.id === presetId)
      if (!preset) return null

      // Update lastUsedAt
      const updatedPresets = presets.map((p) =>
        p.id === presetId ? { ...p, lastUsedAt: new Date().toISOString() } : p
      )
      setPresets(updatedPresets)
      savePresets(updatedPresets)

      return preset.filters
    },
    [presets]
  )

  /**
   * Delete a preset.
   */
  const deletePreset = useCallback(
    (presetId: string): void => {
      const updatedPresets = presets.filter((p) => p.id !== presetId)
      setPresets(updatedPresets)
      savePresets(updatedPresets)
    },
    [presets]
  )

  /**
   * Update an existing preset's filters.
   */
  const updatePreset = useCallback(
    (presetId: string, filters: TransactionFilters): void => {
      const updatedPresets = presets.map((p) =>
        p.id === presetId ? { ...p, filters: { ...filters } } : p
      )
      setPresets(updatedPresets)
      savePresets(updatedPresets)
    },
    [presets]
  )

  /**
   * Rename a preset.
   */
  const renamePreset = useCallback(
    (presetId: string, newName: string): void => {
      const updatedPresets = presets.map((p) =>
        p.id === presetId ? { ...p, name: newName.trim() } : p
      )
      setPresets(updatedPresets)
      savePresets(updatedPresets)
    },
    [presets]
  )

  return {
    /** All saved presets */
    presets,
    /** Save a new preset */
    savePreset,
    /** Load filters from a preset */
    loadPreset,
    /** Delete a preset */
    deletePreset,
    /** Update a preset's filters */
    updatePreset,
    /** Rename a preset */
    renamePreset,
    /** Whether user has any presets */
    hasPresets: presets.length > 0,
  }
}
