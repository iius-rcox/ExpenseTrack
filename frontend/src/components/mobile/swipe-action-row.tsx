'use client'

/**
 * SwipeActionRow Component (T096)
 *
 * Provides swipe-to-reveal actions for mobile list items.
 * Supports left swipe for destructive actions (delete) and
 * right swipe for constructive actions (archive, edit).
 * Only enabled on mobile viewports (<768px).
 */

import * as React from 'react'
import { motion, useAnimation, PanInfo } from 'framer-motion'
import { cn } from '@/lib/utils'
import { Trash2, Archive, Edit, Check, X, type LucideIcon } from 'lucide-react'

export type SwipeActionType = 'delete' | 'archive' | 'edit' | 'approve' | 'reject' | 'custom'

export interface SwipeAction {
  /** Unique identifier for the action */
  id: string
  /** Type of action (determines default icon and color) */
  type: SwipeActionType
  /** Optional custom label */
  label?: string
  /** Optional custom icon (overrides type default) */
  icon?: LucideIcon
  /** Background color class (overrides type default) */
  bgColor?: string
  /** Handler called when action is triggered */
  onAction: () => void
}

export interface SwipeActionRowProps {
  /** Child content to display in the row */
  children: React.ReactNode
  /** Actions to show on left swipe (typically destructive) */
  leftActions?: SwipeAction[]
  /** Actions to show on right swipe (typically constructive) */
  rightActions?: SwipeAction[]
  /** Whether swipe is currently disabled */
  disabled?: boolean
  /** Minimum swipe distance to trigger action (px) */
  threshold?: number
  /** Called when row starts swiping */
  onSwipeStart?: () => void
  /** Called when swipe is complete or cancelled */
  onSwipeEnd?: () => void
  /** Additional CSS classes */
  className?: string
}

const ACTION_WIDTH = 72 // Width of each action button
const DEFAULT_THRESHOLD = 50 // Minimum swipe to trigger
const VELOCITY_THRESHOLD = 500 // Velocity for quick swipe

const ACTION_DEFAULTS: Record<SwipeActionType, { icon: LucideIcon; bgColor: string; label: string }> = {
  delete: { icon: Trash2, bgColor: 'bg-destructive', label: 'Delete' },
  archive: { icon: Archive, bgColor: 'bg-blue-500', label: 'Archive' },
  edit: { icon: Edit, bgColor: 'bg-amber-500', label: 'Edit' },
  approve: { icon: Check, bgColor: 'bg-green-500', label: 'Approve' },
  reject: { icon: X, bgColor: 'bg-destructive', label: 'Reject' },
  custom: { icon: Edit, bgColor: 'bg-muted', label: 'Action' },
}

/**
 * SwipeActionRow - Swipe-to-reveal actions for mobile
 *
 * Features:
 * - Left swipe reveals right actions (destructive)
 * - Right swipe reveals left actions (constructive)
 * - Velocity-based completion for quick swipes
 * - Snap-back animation on incomplete swipe
 * - Touch-friendly 72px action buttons
 * - Only active on mobile (<768px)
 */
export function SwipeActionRow({
  children,
  leftActions = [],
  rightActions = [],
  disabled = false,
  threshold = DEFAULT_THRESHOLD,
  onSwipeStart,
  onSwipeEnd,
  className,
}: SwipeActionRowProps) {
  const controls = useAnimation()
  const [isDragging, setIsDragging] = React.useState(false)
  const [isMobile, setIsMobile] = React.useState(false)

  const leftWidth = leftActions.length * ACTION_WIDTH
  const rightWidth = rightActions.length * ACTION_WIDTH

  // Check if we're on mobile
  React.useEffect(() => {
    const checkMobile = () => {
      setIsMobile(window.innerWidth < 768)
    }
    checkMobile()
    window.addEventListener('resize', checkMobile)
    return () => window.removeEventListener('resize', checkMobile)
  }, [])

  const handleDragStart = () => {
    setIsDragging(true)
    onSwipeStart?.()
  }

  const handleDragEnd = async (
    _event: MouseEvent | TouchEvent | PointerEvent,
    info: PanInfo
  ) => {
    setIsDragging(false)
    const { offset, velocity } = info

    // Quick swipe detection (high velocity)
    const isQuickSwipe = Math.abs(velocity.x) > VELOCITY_THRESHOLD

    // Determine if we should snap open or closed
    if (offset.x < 0 && rightActions.length > 0) {
      // Swiped left - reveal right actions
      const shouldOpen = isQuickSwipe
        ? velocity.x < 0
        : Math.abs(offset.x) > threshold

      if (shouldOpen) {
        await controls.start({ x: -rightWidth })
      } else {
        await controls.start({ x: 0 })
      }
    } else if (offset.x > 0 && leftActions.length > 0) {
      // Swiped right - reveal left actions
      const shouldOpen = isQuickSwipe
        ? velocity.x > 0
        : Math.abs(offset.x) > threshold

      if (shouldOpen) {
        await controls.start({ x: leftWidth })
      } else {
        await controls.start({ x: 0 })
      }
    } else {
      // Snap back
      await controls.start({ x: 0 })
    }

    onSwipeEnd?.()
  }

  const handleActionClick = async (action: SwipeAction) => {
    // Animate closed first
    await controls.start({ x: 0 })
    // Then trigger action
    action.onAction()
  }

  const closeSwipe = () => {
    controls.start({ x: 0 })
  }

  // If not mobile or disabled, just render children
  if (!isMobile || disabled || (leftActions.length === 0 && rightActions.length === 0)) {
    return <div className={className}>{children}</div>
  }

  return (
    <div className={cn('relative overflow-hidden', className)}>
      {/* Left actions (revealed on right swipe) */}
      {leftActions.length > 0 && (
        <div
          className="absolute inset-y-0 left-0 flex"
          style={{ width: leftWidth }}
        >
          {leftActions.map((action) => (
            <ActionButton
              key={action.id}
              action={action}
              onClick={() => handleActionClick(action)}
            />
          ))}
        </div>
      )}

      {/* Right actions (revealed on left swipe) */}
      {rightActions.length > 0 && (
        <div
          className="absolute inset-y-0 right-0 flex"
          style={{ width: rightWidth }}
        >
          {rightActions.map((action) => (
            <ActionButton
              key={action.id}
              action={action}
              onClick={() => handleActionClick(action)}
            />
          ))}
        </div>
      )}

      {/* Main content (draggable) */}
      <motion.div
        drag="x"
        dragDirectionLock
        dragConstraints={{
          left: rightActions.length > 0 ? -rightWidth : 0,
          right: leftActions.length > 0 ? leftWidth : 0,
        }}
        dragElastic={0.1}
        animate={controls}
        onDragStart={handleDragStart}
        onDragEnd={handleDragEnd}
        className={cn(
          'relative bg-background touch-pan-y',
          isDragging && 'cursor-grabbing'
        )}
      >
        {children}
      </motion.div>

      {/* Tap-to-close overlay when open */}
      {isDragging && (
        <div
          className="fixed inset-0 z-40"
          onClick={closeSwipe}
          onTouchStart={closeSwipe}
        />
      )}
    </div>
  )
}

interface ActionButtonProps {
  action: SwipeAction
  onClick: () => void
}

function ActionButton({ action, onClick }: ActionButtonProps) {
  const defaults = ACTION_DEFAULTS[action.type]
  const Icon = action.icon || defaults.icon
  const bgColor = action.bgColor || defaults.bgColor
  const label = action.label || defaults.label

  return (
    <button
      onClick={onClick}
      className={cn(
        'flex flex-col items-center justify-center',
        'w-[72px] h-full',
        'text-white text-xs font-medium',
        'active:opacity-80 transition-opacity',
        bgColor
      )}
      aria-label={label}
    >
      <Icon className="h-5 w-5 mb-1" />
      <span>{label}</span>
    </button>
  )
}

/**
 * SwipeActionList - Wrapper for a list of swipeable items
 *
 * Provides context for coordinating swipe states across items
 * (e.g., closing one item when another is swiped)
 */
interface SwipeActionListContextValue {
  openItemId: string | null
  setOpenItemId: (id: string | null) => void
}

const SwipeActionListContext = React.createContext<SwipeActionListContextValue | null>(null)

export function SwipeActionList({
  children,
  className,
}: {
  children: React.ReactNode
  className?: string
}) {
  const [openItemId, setOpenItemId] = React.useState<string | null>(null)

  return (
    <SwipeActionListContext.Provider value={{ openItemId, setOpenItemId }}>
      <div className={cn('divide-y divide-border', className)}>{children}</div>
    </SwipeActionListContext.Provider>
  )
}

/**
 * useSwipeActionList - Hook to access swipe list context
 */
export function useSwipeActionList() {
  return React.useContext(SwipeActionListContext)
}

/**
 * SwipeActionItem - Individual item in a SwipeActionList
 *
 * Automatically closes when another item is swiped
 */
export function SwipeActionItem({
  id,
  children,
  leftActions,
  rightActions,
  className,
  onDelete,
  onArchive,
  onEdit,
}: {
  id: string
  children: React.ReactNode
  leftActions?: SwipeAction[]
  rightActions?: SwipeAction[]
  className?: string
  /** Convenience prop: adds delete action to right swipe */
  onDelete?: () => void
  /** Convenience prop: adds archive action to left swipe */
  onArchive?: () => void
  /** Convenience prop: adds edit action to left swipe */
  onEdit?: () => void
}) {
  const context = useSwipeActionList()
  const controls = useAnimation()

  // Build action arrays from convenience props
  const computedLeftActions = React.useMemo(() => {
    const actions = [...(leftActions || [])]
    if (onArchive) {
      actions.push({ id: 'archive', type: 'archive', onAction: onArchive })
    }
    if (onEdit) {
      actions.push({ id: 'edit', type: 'edit', onAction: onEdit })
    }
    return actions
  }, [leftActions, onArchive, onEdit])

  const computedRightActions = React.useMemo(() => {
    const actions = [...(rightActions || [])]
    if (onDelete) {
      actions.push({ id: 'delete', type: 'delete', onAction: onDelete })
    }
    return actions
  }, [rightActions, onDelete])

  // Close this item if another item is opened
  React.useEffect(() => {
    if (context && context.openItemId !== null && context.openItemId !== id) {
      controls.start({ x: 0 })
    }
  }, [context, context?.openItemId, id, controls])

  const handleSwipeStart = () => {
    context?.setOpenItemId(id)
  }

  const handleSwipeEnd = () => {
    // Keep open state managed by context
  }

  return (
    <SwipeActionRow
      leftActions={computedLeftActions}
      rightActions={computedRightActions}
      className={className}
      onSwipeStart={handleSwipeStart}
      onSwipeEnd={handleSwipeEnd}
    >
      {children}
    </SwipeActionRow>
  )
}

export default SwipeActionRow
