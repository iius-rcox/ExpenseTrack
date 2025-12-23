/**
 * Design Tokens for the ExpenseFlow Dual Theme System
 *
 * Light Mode: Luxury Minimalist (Emerald #2d5f4f)
 * Dark Mode: Dark Cyber (Cyan #00bcd4)
 *
 * Note: Color values are now defined in CSS variables (globals.css).
 * This file provides TypeScript utilities for confidence levels and animations.
 */

export type Theme = 'light' | 'dark' | 'system';

export interface AnimationTokens {
  duration: {
    instant: number;
    fast: number;
    normal: number;
    slow: number;
  };
  easing: {
    default: string;
    spring: string;
    bounce: string;
  };
}

export interface ConfidenceColors {
  high: string;
  medium: string;
  low: string;
}

// Confidence level thresholds
export const CONFIDENCE_THRESHOLDS = {
  HIGH: 0.9,
  MEDIUM: 0.7,
} as const;

export type ConfidenceLevel = 'high' | 'medium' | 'low';

export function getConfidenceLevel(score: number): ConfidenceLevel {
  if (score >= CONFIDENCE_THRESHOLDS.HIGH) return 'high';
  if (score >= CONFIDENCE_THRESHOLDS.MEDIUM) return 'medium';
  return 'low';
}

// Animation tokens (shared between themes)
export const animation: AnimationTokens = {
  duration: {
    instant: 0,
    fast: 150,
    normal: 300,
    slow: 500,
  },
  easing: {
    default: 'cubic-bezier(0.4, 0, 0.2, 1)',
    spring: 'cubic-bezier(0.34, 1.56, 0.64, 1)',
    bounce: 'cubic-bezier(0.68, -0.55, 0.265, 1.55)',
  },
};

// Confidence colors (semantic, theme-independent)
export const confidenceColors: ConfidenceColors = {
  high: '#10b981',
  medium: '#f59e0b',
  low: '#f43f5e',
};

export function getConfidenceColor(score: number): string {
  const level = getConfidenceLevel(score);
  return confidenceColors[level];
}
