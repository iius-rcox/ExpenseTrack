/**
 * Design Tokens for the Refined Intelligence Design System
 *
 * This file defines the core visual language for ExpenseFlow:
 * - Colors: Slate scale + copper accents + confidence indicators
 * - Typography: Serif (display), Sans (body), Mono (numbers)
 * - Animation: Timing and easing for consistent motion
 */

export interface ColorTokens {
  slate: {
    950: string;
    900: string;
    800: string;
    700: string;
    600: string;
    500: string;
    400: string;
    300: string;
    200: string;
    100: string;
    50: string;
  };
  accent: {
    copper: string;
    copperLight: string;
    copperDark: string;
    emerald: string;
    amber: string;
    rose: string;
  };
  confidence: {
    high: string;
    medium: string;
    low: string;
  };
}

export interface TypographyTokens {
  fontFamily: {
    serif: string;
    sans: string;
    mono: string;
  };
  fontSize: {
    xs: string;
    sm: string;
    base: string;
    lg: string;
    xl: string;
    '2xl': string;
    '3xl': string;
    '4xl': string;
  };
  fontWeight: {
    light: number;
    normal: number;
    medium: number;
    semibold: number;
    bold: number;
  };
}

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

export interface DesignTokens {
  colors: ColorTokens;
  typography: TypographyTokens;
  animation: AnimationTokens;
}

// Refined Intelligence Color Palette
export const colors: ColorTokens = {
  slate: {
    950: '#020617',
    900: '#0f172a',
    800: '#1e293b',
    700: '#334155',
    600: '#475569',
    500: '#64748b',
    400: '#94a3b8',
    300: '#cbd5e1',
    200: '#e2e8f0',
    100: '#f1f5f9',
    50: '#f8fafc',
  },
  accent: {
    copper: '#b87333',
    copperLight: '#d4a574',
    copperDark: '#8b5a2b',
    emerald: '#10b981',
    amber: '#f59e0b',
    rose: '#f43f5e',
  },
  confidence: {
    high: '#10b981',    // Emerald - AI is confident
    medium: '#f59e0b',  // Amber - Needs review
    low: '#f43f5e',     // Rose - Uncertain
  },
};

export const typography: TypographyTokens = {
  fontFamily: {
    serif: "'Playfair Display', Georgia, serif",
    sans: "'Plus Jakarta Sans', system-ui, sans-serif",
    mono: "'JetBrains Mono', monospace",
  },
  fontSize: {
    xs: '0.75rem',   // 12px
    sm: '0.875rem',  // 14px
    base: '1rem',    // 16px
    lg: '1.125rem',  // 18px
    xl: '1.25rem',   // 20px
    '2xl': '1.5rem', // 24px
    '3xl': '1.875rem', // 30px
    '4xl': '2.25rem',  // 36px
  },
  fontWeight: {
    light: 300,
    normal: 400,
    medium: 500,
    semibold: 600,
    bold: 700,
  },
};

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

export const tokens: DesignTokens = {
  colors,
  typography,
  animation,
};

// Confidence level thresholds (for helper functions)
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

export function getConfidenceColor(score: number): string {
  const level = getConfidenceLevel(score);
  return colors.confidence[level];
}
