import { cn } from '@/lib/utils';
import { Card, CardContent } from '@/components/ui/card';
import { TrendingUp, TrendingDown, Minus } from 'lucide-react';
import { type ReactNode } from 'react';

/**
 * StatCard - Dashboard Metrics Display
 *
 * Displays key metrics with trend indicators. Used in the dashboard
 * to show monthly spending, pending reviews, matching percentage, etc.
 *
 * Design notes:
 * - Monospace font for numerical values (visual alignment)
 * - Color-coded trend indicators (green up, red down, neutral gray)
 * - Optional highlight state for emphasis (e.g., pending items needing attention)
 */

export interface StatCardProps {
  /** The metric label */
  label: string;
  /** The metric value (can be formatted string or number) */
  value: string | number;
  /** Percentage change from previous period */
  trend?: number;
  /** Direction of trend (can override automatic detection) */
  trendDirection?: 'up' | 'down' | 'neutral';
  /** Whether this card should be highlighted (e.g., for attention) */
  highlight?: boolean;
  /** Icon to display */
  icon?: ReactNode;
  /** Additional classes */
  className?: string;
  /** Format the value as currency */
  isCurrency?: boolean;
  /** Format the value as percentage */
  isPercentage?: boolean;
}

export function StatCard({
  label,
  value,
  trend,
  trendDirection,
  highlight = false,
  icon,
  className,
  isCurrency = false,
  isPercentage = false,
}: StatCardProps) {
  // Format the value
  let displayValue: string;
  if (typeof value === 'number') {
    if (isCurrency) {
      displayValue = new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: 'USD',
        minimumFractionDigits: 0,
        maximumFractionDigits: 2,
      }).format(value);
    } else if (isPercentage) {
      displayValue = `${value}%`;
    } else {
      displayValue = new Intl.NumberFormat('en-US').format(value);
    }
  } else {
    displayValue = value;
  }

  // Determine trend direction
  const direction =
    trendDirection ??
    (trend !== undefined
      ? trend > 0
        ? 'up'
        : trend < 0
          ? 'down'
          : 'neutral'
      : 'neutral');

  const trendConfig = {
    up: {
      icon: TrendingUp,
      color: 'text-confidence-high',
      bg: 'bg-confidence-high/10',
    },
    down: {
      icon: TrendingDown,
      color: 'text-confidence-low',
      bg: 'bg-confidence-low/10',
    },
    neutral: {
      icon: Minus,
      color: 'text-slate-500',
      bg: 'bg-slate-500/10',
    },
  };

  const trendStyles = trendConfig[direction];
  const TrendIcon = trendStyles.icon;

  return (
    <Card
      className={cn(
        'transition-all duration-200',
        highlight && 'ring-2 ring-accent-copper ring-offset-2',
        className
      )}
    >
      <CardContent className="p-6">
        <div className="flex items-start justify-between">
          <div className="flex-1">
            <p className="text-sm font-medium text-muted-foreground">{label}</p>
            <p className="mt-2 text-3xl font-semibold font-mono tracking-tight">
              {displayValue}
            </p>
            {trend !== undefined && (
              <div className="mt-2 flex items-center gap-1">
                <span
                  className={cn(
                    'inline-flex items-center gap-0.5 rounded-full px-1.5 py-0.5 text-xs font-medium',
                    trendStyles.bg,
                    trendStyles.color
                  )}
                >
                  <TrendIcon className="h-3 w-3" />
                  <span className="font-mono">{Math.abs(trend)}%</span>
                </span>
                <span className="text-xs text-muted-foreground">
                  vs last month
                </span>
              </div>
            )}
          </div>
          {icon && (
            <div className="rounded-full bg-muted p-2.5 text-muted-foreground">
              {icon}
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  );
}

/**
 * Compact variant for smaller spaces
 */
export function StatCardCompact({
  label,
  value,
  className,
}: Pick<StatCardProps, 'label' | 'value' | 'className'>) {
  return (
    <div className={cn('flex flex-col gap-1', className)}>
      <span className="text-xs font-medium text-muted-foreground">{label}</span>
      <span className="text-lg font-semibold font-mono">{value}</span>
    </div>
  );
}
