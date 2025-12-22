import { cn } from '@/lib/utils';
import { getConfidenceLevel, type ConfidenceLevel } from '@/lib/design-tokens';

/**
 * ConfidenceIndicator - Signature Visual Element
 *
 * The confidence indicator is the visual language of the Refined Intelligence
 * design system. It communicates AI confidence through:
 * - A 5-dot scale (0-5 filled dots based on score)
 * - Color coding (emerald=high, amber=medium, rose=low)
 * - Optional percentage label
 *
 * This component appears throughout the application wherever AI
 * has made a prediction: extracted receipt fields, transaction
 * categorization, match suggestions, etc.
 */

export interface ConfidenceIndicatorProps {
  /** Confidence score from 0 to 1 */
  score: number;
  /** Whether to show the percentage label */
  showLabel?: boolean;
  /** Size variant */
  size?: 'sm' | 'md' | 'lg';
  /** Additional CSS classes */
  className?: string;
  /** Optional aria-label override */
  ariaLabel?: string;
}

const sizeConfig = {
  sm: {
    dot: 'w-1 h-1',
    gap: 'gap-0.5',
    label: 'text-xs',
  },
  md: {
    dot: 'w-1.5 h-1.5',
    gap: 'gap-1',
    label: 'text-xs',
  },
  lg: {
    dot: 'w-2 h-2',
    gap: 'gap-1',
    label: 'text-sm',
  },
};

const levelColors: Record<ConfidenceLevel, string> = {
  high: 'bg-confidence-high',
  medium: 'bg-confidence-medium',
  low: 'bg-confidence-low',
};

const levelLabels: Record<ConfidenceLevel, string> = {
  high: 'High confidence',
  medium: 'Medium confidence',
  low: 'Low confidence',
};

export function ConfidenceIndicator({
  score,
  showLabel = false,
  size = 'md',
  className,
  ariaLabel,
}: ConfidenceIndicatorProps) {
  // Clamp score to 0-1 range
  const clampedScore = Math.max(0, Math.min(1, score));
  const level = getConfidenceLevel(clampedScore);
  const filledDots = Math.round(clampedScore * 5);
  const percentage = Math.round(clampedScore * 100);
  const config = sizeConfig[size];

  const label = ariaLabel || `${levelLabels[level]}: ${percentage}%`;

  return (
    <div
      className={cn('flex items-center', config.gap, className)}
      role="meter"
      aria-label={label}
      aria-valuenow={percentage}
      aria-valuemin={0}
      aria-valuemax={100}
    >
      <div className={cn('flex', config.gap)}>
        {[...Array(5)].map((_, index) => (
          <div
            key={index}
            className={cn(
              'rounded-full transition-colors duration-200',
              config.dot,
              index < filledDots ? levelColors[level] : 'bg-slate-700'
            )}
          />
        ))}
      </div>
      {showLabel && (
        <span className={cn('text-slate-500 font-mono', config.label)}>
          {percentage}%
        </span>
      )}
    </div>
  );
}

/**
 * Inline variant for use within text or compact spaces
 */
export function ConfidenceInline({
  score,
  className,
}: {
  score: number;
  className?: string;
}) {
  const level = getConfidenceLevel(score);
  const percentage = Math.round(score * 100);

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 font-mono text-xs',
        className
      )}
    >
      <span
        className={cn('w-1.5 h-1.5 rounded-full', levelColors[level])}
        aria-hidden="true"
      />
      <span className="text-slate-500">{percentage}%</span>
    </span>
  );
}

/**
 * Badge variant for prominent display
 */
export function ConfidenceBadge({
  score,
  className,
}: {
  score: number;
  className?: string;
}) {
  const level = getConfidenceLevel(score);
  const percentage = Math.round(score * 100);

  const badgeColors: Record<ConfidenceLevel, string> = {
    high: 'bg-confidence-high/10 text-confidence-high border-confidence-high/20',
    medium:
      'bg-confidence-medium/10 text-confidence-medium border-confidence-medium/20',
    low: 'bg-confidence-low/10 text-confidence-low border-confidence-low/20',
  };

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-xs font-medium border',
        badgeColors[level],
        className
      )}
    >
      <ConfidenceIndicator score={score} size="sm" />
      <span className="font-mono">{percentage}%</span>
    </span>
  );
}
