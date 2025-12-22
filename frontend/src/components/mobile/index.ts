/**
 * Mobile Components Index
 *
 * Exports all mobile-specific components for the ExpenseFlow application.
 * These components are optimized for touch interactions and smaller viewports.
 */

// Bottom navigation for mobile
export { MobileNav, MobileNavSpacer, useIsMobile } from './mobile-nav'
export type { MobileNavProps } from './mobile-nav'

// Swipe-to-reveal actions
export {
  SwipeActionRow,
  SwipeActionList,
  SwipeActionItem,
  useSwipeActionList,
} from './swipe-action-row'
export type {
  SwipeActionRowProps,
  SwipeAction,
  SwipeActionType,
} from './swipe-action-row'

// Camera capture for receipts
export { CameraCapture, CameraCaptureButton } from './camera-capture'
export type { CameraCaptureProps } from './camera-capture'
