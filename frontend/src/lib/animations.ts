/**
 * Framer Motion Animation Presets for Refined Intelligence Design System
 *
 * These presets provide consistent, polished animations across the application.
 * Used for page transitions, list item reveals, and micro-interactions.
 */

import { type Variants, type Transition } from 'framer-motion';

// ============================================================================
// Transition Configs
// ============================================================================

export const defaultTransition: Transition = {
  duration: 0.3,
  ease: [0.4, 0, 0.2, 1], // Default easing
};

export const springTransition: Transition = {
  type: 'spring',
  stiffness: 300,
  damping: 20,
};

export const fastTransition: Transition = {
  duration: 0.15,
  ease: [0.4, 0, 0.2, 1],
};

// ============================================================================
// Basic Variants
// ============================================================================

export const fadeIn: Variants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: defaultTransition,
  },
  exit: { opacity: 0, transition: fastTransition },
};

export const slideUp: Variants = {
  hidden: { opacity: 0, y: 8 },
  visible: {
    opacity: 1,
    y: 0,
    transition: defaultTransition,
  },
  exit: {
    opacity: 0,
    y: -8,
    transition: fastTransition,
  },
};

export const slideDown: Variants = {
  hidden: { opacity: 0, y: -8 },
  visible: {
    opacity: 1,
    y: 0,
    transition: defaultTransition,
  },
  exit: {
    opacity: 0,
    y: 8,
    transition: fastTransition,
  },
};

export const slideInFromRight: Variants = {
  hidden: { opacity: 0, x: 20 },
  visible: {
    opacity: 1,
    x: 0,
    transition: defaultTransition,
  },
  exit: {
    opacity: 0,
    x: -20,
    transition: fastTransition,
  },
};

export const scaleIn: Variants = {
  hidden: { opacity: 0, scale: 0.95 },
  visible: {
    opacity: 1,
    scale: 1,
    transition: springTransition,
  },
  exit: {
    opacity: 0,
    scale: 0.95,
    transition: fastTransition,
  },
};

// ============================================================================
// Stagger Containers
// ============================================================================

export const staggerContainer: Variants = {
  hidden: {},
  visible: {
    transition: {
      staggerChildren: 0.05,
    },
  },
};

export const staggerContainerSlow: Variants = {
  hidden: {},
  visible: {
    transition: {
      staggerChildren: 0.1,
    },
  },
};

// Children variant for use with stagger containers
export const staggerChild: Variants = {
  hidden: { opacity: 0, y: 8 },
  visible: {
    opacity: 1,
    y: 0,
    transition: defaultTransition,
  },
};

// ============================================================================
// Signature Animations
// ============================================================================

/**
 * Confidence Glow - The signature animation for AI-detected fields
 * Creates a subtle, pulsing glow effect that indicates AI confidence
 */
export const confidenceGlow: Variants = {
  initial: { opacity: 0 },
  animate: {
    opacity: [0.4, 0.7, 0.4],
    transition: {
      duration: 2,
      repeat: Infinity,
      ease: 'easeInOut',
    },
  },
};

/**
 * Processing Indicator - For async operations like receipt processing
 */
export const processingPulse: Variants = {
  initial: { opacity: 0.5, scale: 1 },
  animate: {
    opacity: [0.5, 1, 0.5],
    scale: [1, 1.02, 1],
    transition: {
      duration: 1.5,
      repeat: Infinity,
      ease: 'easeInOut',
    },
  },
};

/**
 * Success checkmark animation
 */
export const successCheck: Variants = {
  hidden: { pathLength: 0, opacity: 0 },
  visible: {
    pathLength: 1,
    opacity: 1,
    transition: {
      pathLength: { duration: 0.4, ease: 'easeOut' },
      opacity: { duration: 0.2 },
    },
  },
};

// ============================================================================
// List Animations
// ============================================================================

/**
 * For expense stream items and activity feeds
 */
export const listItemVariants: Variants = {
  hidden: { opacity: 0, x: -10 },
  visible: {
    opacity: 1,
    x: 0,
    transition: defaultTransition,
  },
  exit: {
    opacity: 0,
    x: 10,
    transition: fastTransition,
  },
};

/**
 * For action queue items with priority emphasis
 */
export const priorityItemVariants: Variants = {
  hidden: { opacity: 0, scale: 0.98 },
  visible: {
    opacity: 1,
    scale: 1,
    transition: springTransition,
  },
  hover: {
    scale: 1.01,
    transition: fastTransition,
  },
};

// ============================================================================
// Page Transitions
// ============================================================================

export const pageTransition: Variants = {
  initial: { opacity: 0, y: 20 },
  animate: {
    opacity: 1,
    y: 0,
    transition: {
      duration: 0.4,
      ease: [0.4, 0, 0.2, 1],
    },
  },
  exit: {
    opacity: 0,
    y: -10,
    transition: {
      duration: 0.2,
    },
  },
};

// ============================================================================
// Utility Functions
// ============================================================================

/**
 * Creates a stagger delay for indexed items
 */
export function getStaggerDelay(index: number, baseDelay = 0.05): number {
  return index * baseDelay;
}

/**
 * Creates motion props for an animated list item
 */
export function getListItemMotionProps(index: number) {
  return {
    initial: 'hidden',
    animate: 'visible',
    exit: 'exit',
    variants: listItemVariants,
    transition: {
      ...defaultTransition,
      delay: getStaggerDelay(index),
    },
  };
}
