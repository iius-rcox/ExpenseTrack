/**
 * CategoryBreakdown Component (T030)
 *
 * Visualizes spending breakdown by category using Recharts.
 * Supports multiple visualization modes:
 * - Pie chart (default): For overall distribution
 * - Bar chart: For comparison between categories
 * - List view: For detailed category information
 */

import { useMemo } from 'react';
import { motion } from 'framer-motion';
import { Link } from '@tanstack/react-router';
import {
  PieChart,
  Pie,
  Cell,
  ResponsiveContainer,
  Tooltip,
  Legend,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
} from 'recharts';
import {
  ShoppingCart,
  Utensils,
  Car,
  Home,
  Briefcase,
  Heart,
  Plane,
  MoreHorizontal,
} from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { CategoryBreakdownSkeleton } from '@/components/design-system/loading-skeleton';
import { cn, formatCurrency } from '@/lib/utils';
import { fadeIn } from '@/lib/animations';
import type { CategoryBreakdownData, CategoryBreakdownProps } from '@/types/dashboard';

/**
 * DEFENSIVE HELPER: Safely convert any value to a displayable string.
 * Guards against React Error #301 where empty objects {} might be in cached data.
 */
function safeDisplayString(value: unknown, fallback = ''): string {
  if (value === null || value === undefined) return fallback;
  if (typeof value === 'object' && !Array.isArray(value) && !(value instanceof Date)) {
    const keys = Object.keys(value as object);
    if (keys.length === 0) return fallback;
    return fallback;
  }
  return String(value);
}

/**
 * Default category colors for the design system.
 * These work well in both light and dark modes.
 */
const CATEGORY_COLORS = [
  '#2d5f4f', // Emerald (primary in light mode)
  '#10b981', // Emerald (confidence-high)
  '#8b5cf6', // Violet
  '#f59e0b', // Amber
  '#ec4899', // Pink
  '#06b6d4', // Cyan
  '#84cc16', // Lime
  '#6366f1', // Indigo
];

/**
 * Category icon mapping (for list view).
 */
const categoryIcons: Record<string, React.ElementType> = {
  shopping: ShoppingCart,
  'food & dining': Utensils,
  food: Utensils,
  transportation: Car,
  travel: Plane,
  housing: Home,
  business: Briefcase,
  health: Heart,
  healthcare: Heart,
};

/**
 * Get icon for category (case-insensitive).
 */
function getCategoryIcon(category: string): React.ElementType {
  const normalized = category.toLowerCase();
  for (const [key, icon] of Object.entries(categoryIcons)) {
    if (normalized.includes(key)) return icon;
  }
  return MoreHorizontal;
}

/**
 * Custom tooltip for charts.
 */
function CustomTooltip({
  active,
  payload,
}: {
  active?: boolean;
  payload?: Array<{ payload: CategoryBreakdownData }>;
}) {
  if (!active || !payload?.[0]) return null;

  const data = payload[0].payload;
  return (
    <div className="rounded-lg border bg-popover px-3 py-2 shadow-lg">
      <p className="font-medium">{data.category}</p>
      <p className="text-sm text-muted-foreground">
        {formatCurrency(data.amount)} ({data.percentage}%)
      </p>
      <p className="text-xs text-muted-foreground">
        {data.transactionCount} transactions
      </p>
    </div>
  );
}

/**
 * Pie chart visualization.
 */
function PieChartView({
  categories,
  onCategoryClick,
}: {
  categories: CategoryBreakdownData[];
  onCategoryClick?: (category: CategoryBreakdownData) => void;
}) {
  // Convert to recharts-compatible format with index signature
  const chartData = categories.map((cat) => ({
    ...cat,
    name: cat.category,
  }));

  return (
    <ResponsiveContainer width="100%" height={300}>
      <PieChart>
        <Pie
          data={chartData}
          cx="50%"
          cy="50%"
          innerRadius={60}
          outerRadius={100}
          paddingAngle={2}
          dataKey="amount"
          nameKey="name"
          onClick={(_, index) => onCategoryClick?.(categories[index])}
          style={{ cursor: 'pointer' }}
        >
          {chartData.map((entry, index) => (
            <Cell
              key={`cell-${index}`}
              fill={entry.color || CATEGORY_COLORS[index % CATEGORY_COLORS.length]}
              stroke="transparent"
            />
          ))}
        </Pie>
        <Tooltip content={<CustomTooltip />} />
        <Legend
          layout="horizontal"
          verticalAlign="bottom"
          align="center"
          formatter={(value) => (
            <span className="text-xs text-foreground">{value}</span>
          )}
        />
      </PieChart>
    </ResponsiveContainer>
  );
}

/**
 * Bar chart visualization.
 */
function BarChartView({
  categories,
  onCategoryClick,
}: {
  categories: CategoryBreakdownData[];
  onCategoryClick?: (category: CategoryBreakdownData) => void;
}) {
  // Convert to recharts-compatible format
  const chartData = categories.map((cat) => ({
    ...cat,
    name: cat.category,
  }));

  return (
    <ResponsiveContainer width="100%" height={300}>
      <BarChart
        data={chartData}
        layout="vertical"
        margin={{ top: 5, right: 30, left: 20, bottom: 5 }}
      >
        <CartesianGrid strokeDasharray="3 3" stroke="var(--muted)" />
        <XAxis
          type="number"
          tickFormatter={(value) => formatCurrency(value)}
          style={{ fontSize: '12px' }}
        />
        <YAxis
          type="category"
          dataKey="category"
          width={100}
          style={{ fontSize: '12px' }}
        />
        <Tooltip content={<CustomTooltip />} />
        <Bar
          dataKey="amount"
          radius={[0, 4, 4, 0]}
          onClick={(_, index) => onCategoryClick?.(categories[index])}
          style={{ cursor: 'pointer' }}
        >
          {chartData.map((entry, index) => (
            <Cell
              key={`cell-${index}`}
              fill={entry.color || CATEGORY_COLORS[index % CATEGORY_COLORS.length]}
            />
          ))}
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  );
}

/**
 * List view visualization.
 */
function ListView({
  categories,
  onCategoryClick,
}: {
  categories: CategoryBreakdownData[];
  onCategoryClick?: (category: CategoryBreakdownData) => void;
}) {
  const total = useMemo(
    () => categories.reduce((sum, cat) => sum + cat.amount, 0),
    [categories]
  );

  return (
    <div className="space-y-3">
      {categories.map((category, index) => {
        const Icon = getCategoryIcon(safeDisplayString(category.category));
        const color = category.color || CATEGORY_COLORS[index % CATEGORY_COLORS.length];
        const percentage = total > 0 ? (category.amount / total) * 100 : 0;

        return (
          <div
            key={safeDisplayString(category.category, `cat-${index}`)}
            className={cn(
              'group flex items-center gap-3 rounded-lg p-2 transition-colors',
              'hover:bg-muted/50 cursor-pointer'
            )}
            onClick={() => onCategoryClick?.(category)}
          >
            {/* Icon */}
            <div
              className="flex h-9 w-9 items-center justify-center rounded-lg"
              style={{ backgroundColor: `${color}20` }}
            >
              <Icon className="h-4 w-4" style={{ color }} />
            </div>

            {/* Content */}
            <div className="min-w-0 flex-1">
              <div className="flex items-center justify-between">
                <span className="font-medium text-sm truncate">
                  {safeDisplayString(category.category, 'Unknown')}
                </span>
                <span className="shrink-0 font-semibold tabular-nums">
                  {formatCurrency(category.amount)}
                </span>
              </div>

              {/* Progress bar */}
              <div className="mt-1.5 flex items-center gap-2">
                <div className="h-1.5 flex-1 overflow-hidden rounded-full bg-muted">
                  <motion.div
                    className="h-full rounded-full"
                    style={{ backgroundColor: color }}
                    initial={{ width: 0 }}
                    animate={{ width: `${percentage}%` }}
                    transition={{ duration: 0.5, delay: index * 0.05 }}
                  />
                </div>
                <span className="shrink-0 text-xs tabular-nums text-muted-foreground">
                  {percentage.toFixed(1)}%
                </span>
              </div>

              {/* Transaction count */}
              <p className="mt-0.5 text-xs text-muted-foreground">
                {category.transactionCount} transactions
              </p>
            </div>
          </div>
        );
      })}
    </div>
  );
}

/**
 * CategoryBreakdown visualizes spending by category.
 */
export function CategoryBreakdown({
  categories,
  isLoading,
  variant = 'pie',
  onCategoryClick,
  className,
}: CategoryBreakdownProps) {
  // Sort categories by amount (highest first)
  const sortedCategories = useMemo(() => {
    if (!categories) return [];
    return [...categories].sort((a, b) => b.amount - a.amount);
  }, [categories]);

  // Calculate total
  const total = useMemo(
    () => sortedCategories.reduce((sum, cat) => sum + cat.amount, 0),
    [sortedCategories]
  );

  if (isLoading) {
    return <CategoryBreakdownSkeleton />;
  }

  if (!sortedCategories.length) {
    return (
      <Card className={className}>
        <CardHeader>
          <CardTitle className="text-base">Spending by Category</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex flex-col items-center justify-center py-12 text-center">
            <div className="rounded-full bg-muted p-4">
              <ShoppingCart className="h-6 w-6 text-muted-foreground" />
            </div>
            <p className="mt-3 text-sm text-muted-foreground">
              No spending data yet
            </p>
            <p className="text-xs text-muted-foreground/70">
              Categories will appear as you add expenses
            </p>
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card className={cn('overflow-hidden', className)}>
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <div>
          <CardTitle className="text-base">Spending by Category</CardTitle>
          <p className="text-sm text-muted-foreground">
            Total: {formatCurrency(total)}
          </p>
        </div>
        <Link
          to="/analytics"
          className="text-sm text-primary hover:underline"
        >
          View details
        </Link>
      </CardHeader>
      <CardContent>
        <motion.div
          variants={fadeIn}
          initial="hidden"
          animate="visible"
        >
          {variant === 'pie' && (
            <PieChartView
              categories={sortedCategories}
              onCategoryClick={onCategoryClick}
            />
          )}
          {variant === 'bar' && (
            <BarChartView
              categories={sortedCategories}
              onCategoryClick={onCategoryClick}
            />
          )}
          {variant === 'list' && (
            <ListView
              categories={sortedCategories}
              onCategoryClick={onCategoryClick}
            />
          )}
        </motion.div>
      </CardContent>
    </Card>
  );
}

/**
 * Compact category list for dashboard sidebar.
 */
export function CategoryBreakdownCompact({
  categories,
  maxItems = 5,
}: {
  categories?: CategoryBreakdownData[];
  maxItems?: number;
}) {
  const topCategories = useMemo(() => {
    if (!categories) return [];
    return [...categories]
      .sort((a, b) => b.amount - a.amount)
      .slice(0, maxItems);
  }, [categories, maxItems]);

  if (!topCategories.length) {
    return null;
  }

  return (
    <div className="space-y-2">
      {topCategories.map((category, index) => {
        const color = category.color || CATEGORY_COLORS[index % CATEGORY_COLORS.length];
        return (
          <div key={safeDisplayString(category.category, `legend-${index}`)} className="flex items-center gap-2">
            <div
              className="h-2 w-2 rounded-full"
              style={{ backgroundColor: color }}
            />
            <span className="flex-1 truncate text-sm">{safeDisplayString(category.category, 'Unknown')}</span>
            <span className="shrink-0 text-sm tabular-nums text-muted-foreground">
              {formatCurrency(category.amount)}
            </span>
          </div>
        );
      })}
    </div>
  );
}

export default CategoryBreakdown;
