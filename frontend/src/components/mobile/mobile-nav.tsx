'use client'

/**
 * MobileNav Component (T095)
 *
 * Bottom navigation bar for mobile viewports (<768px).
 * Provides thumb-friendly access to main app sections.
 * Hidden on tablet (768px+) and desktop viewports.
 */

import * as React from 'react'
import { useLocation, useNavigate } from '@tanstack/react-router'
import { motion, AnimatePresence } from 'framer-motion'
import { cn } from '@/lib/utils'
import {
  LayoutDashboard,
  Receipt,
  ArrowLeftRight,
  GitCompare,
  BarChart3,
  Plus,
  type LucideIcon,
} from 'lucide-react'

interface NavItem {
  id: string
  label: string
  icon: LucideIcon
  path: string
  badge?: number
}

export interface MobileNavProps {
  /** Additional CSS classes */
  className?: string
  /** Pending action counts for badges */
  pendingCounts?: {
    receipts?: number
    matching?: number
  }
  /** Callback when quick action button is pressed */
  onQuickAction?: () => void
}

const NAV_ITEMS: NavItem[] = [
  { id: 'dashboard', label: 'Home', icon: LayoutDashboard, path: '/dashboard' },
  { id: 'receipts', label: 'Receipts', icon: Receipt, path: '/receipts' },
  { id: 'transactions', label: 'Transactions', icon: ArrowLeftRight, path: '/transactions' },
  { id: 'matching', label: 'Matching', icon: GitCompare, path: '/matching' },
  { id: 'analytics', label: 'Analytics', icon: BarChart3, path: '/analytics' },
]

/**
 * MobileNav - Bottom navigation for mobile devices
 *
 * Features:
 * - Fixed bottom positioning with safe area insets
 * - Active state indication with animation
 * - Badge support for pending items
 * - Central quick action button (optional)
 * - 44x44pt minimum touch targets
 */
export function MobileNav({ className, pendingCounts, onQuickAction }: MobileNavProps) {
  const location = useLocation()
  const navigate = useNavigate()

  const isActive = (path: string) => {
    return location.pathname === path || location.pathname.startsWith(path + '/')
  }

  const getBadge = (id: string): number | undefined => {
    if (id === 'receipts') return pendingCounts?.receipts
    if (id === 'matching') return pendingCounts?.matching
    return undefined
  }

  return (
    <nav
      className={cn(
        // Base styles - fixed bottom with safe area
        'fixed bottom-0 left-0 right-0 z-50',
        'bg-background/95 backdrop-blur-lg border-t',
        'pb-[env(safe-area-inset-bottom)]',
        // Only show on mobile
        'md:hidden',
        className
      )}
    >
      <div className="flex items-center justify-around px-2 py-1">
        {NAV_ITEMS.map((item, index) => {
          const active = isActive(item.path)
          const badge = getBadge(item.id)

          // Insert quick action button in the middle
          if (index === 2 && onQuickAction) {
            return (
              <div key="quick-action-wrapper" className="flex items-center">
                <NavButton
                  key={item.id}
                  item={item}
                  active={active}
                  badge={badge}
                  onClick={() => navigate({ to: item.path })}
                />
                <QuickActionButton onClick={onQuickAction} />
              </div>
            )
          }

          return (
            <NavButton
              key={item.id}
              item={item}
              active={active}
              badge={badge}
              onClick={() => navigate({ to: item.path })}
            />
          )
        })}
      </div>
    </nav>
  )
}

interface NavButtonProps {
  item: NavItem
  active: boolean
  badge?: number
  onClick: () => void
}

function NavButton({ item, active, badge, onClick }: NavButtonProps) {
  const Icon = item.icon

  return (
    <button
      onClick={onClick}
      className={cn(
        // Touch target: minimum 44x44pt
        'relative flex flex-col items-center justify-center',
        'min-w-[44px] min-h-[44px] px-3 py-2',
        'rounded-lg transition-colors',
        // Active/inactive states
        active
          ? 'text-primary'
          : 'text-muted-foreground hover:text-foreground hover:bg-muted/50'
      )}
      aria-label={item.label}
      aria-current={active ? 'page' : undefined}
    >
      <div className="relative">
        <Icon className="h-5 w-5" />

        {/* Badge for pending counts */}
        <AnimatePresence>
          {badge !== undefined && badge > 0 && (
            <motion.span
              initial={{ scale: 0 }}
              animate={{ scale: 1 }}
              exit={{ scale: 0 }}
              className={cn(
                'absolute -top-1 -right-1',
                'flex items-center justify-center',
                'min-w-[16px] h-4 px-1',
                'bg-destructive text-destructive-foreground',
                'text-[10px] font-medium rounded-full'
              )}
            >
              {badge > 99 ? '99+' : badge}
            </motion.span>
          )}
        </AnimatePresence>
      </div>

      <span className="text-[10px] mt-1 font-medium">{item.label}</span>

      {/* Active indicator */}
      {active && (
        <motion.div
          layoutId="mobile-nav-active"
          className="absolute bottom-0 left-1/2 -translate-x-1/2 w-8 h-0.5 bg-primary rounded-full"
          transition={{ type: 'spring', stiffness: 500, damping: 30 }}
        />
      )}
    </button>
  )
}

interface QuickActionButtonProps {
  onClick: () => void
}

function QuickActionButton({ onClick }: QuickActionButtonProps) {
  return (
    <motion.button
      onClick={onClick}
      whileTap={{ scale: 0.95 }}
      className={cn(
        // Elevated circular button
        'flex items-center justify-center',
        'w-12 h-12 -mt-4',
        'bg-primary text-primary-foreground',
        'rounded-full shadow-lg',
        'border-4 border-background'
      )}
      aria-label="Quick action"
    >
      <Plus className="h-6 w-6" />
    </motion.button>
  )
}

/**
 * MobileNavSpacer - Adds spacing to prevent content from being hidden behind the nav
 */
export function MobileNavSpacer({ className }: { className?: string }) {
  return (
    <div
      className={cn(
        'h-[calc(60px+env(safe-area-inset-bottom))]',
        'md:hidden',
        className
      )}
    />
  )
}

/**
 * useMobileNav - Hook to detect if mobile nav should be visible
 * Note: Hooks must be called unconditionally (Rules of Hooks).
 * SSR check is done inside useEffect instead of early return.
 */
export function useIsMobile(): boolean {
  // Always call hooks unconditionally - SSR check moves inside useEffect
  const [isMobile, setIsMobile] = React.useState(false)

  React.useEffect(() => {
    // SSR safety: window is only available in browser
    if (typeof window === 'undefined') return

    const mediaQuery = window.matchMedia('(max-width: 767px)')
    setIsMobile(mediaQuery.matches)

    const handler = (e: MediaQueryListEvent) => setIsMobile(e.matches)
    mediaQuery.addEventListener('change', handler)
    return () => mediaQuery.removeEventListener('change', handler)
  }, [])

  return isMobile
}

export default MobileNav
