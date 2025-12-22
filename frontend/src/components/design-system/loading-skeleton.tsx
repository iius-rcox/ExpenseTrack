import { cn } from '@/lib/utils';
import { Skeleton } from '@/components/ui/skeleton';

/**
 * LoadingSkeleton - Async Operation Placeholders
 *
 * Per FR-032: "Display skeleton loaders during async operations".
 * These skeletons match the layout of their corresponding components
 * to provide smooth loading transitions.
 */

// ============================================================================
// Base Skeleton Components
// ============================================================================

export function SkeletonCard({ className }: { className?: string }) {
  return (
    <div
      className={cn(
        'rounded-lg border bg-card p-6 space-y-3',
        className
      )}
    >
      <Skeleton className="h-4 w-24" />
      <Skeleton className="h-8 w-32" />
      <Skeleton className="h-3 w-20" />
    </div>
  );
}

// ============================================================================
// Dashboard Skeletons
// ============================================================================

export function DashboardMetricsSkeleton() {
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
      {[...Array(4)].map((_, i) => (
        <SkeletonCard key={i} />
      ))}
    </div>
  );
}

export function ExpenseStreamSkeleton({ count = 5 }: { count?: number }) {
  return (
    <div className="space-y-3">
      {[...Array(count)].map((_, i) => (
        <div key={i} className="flex items-center gap-3 p-3 rounded-lg border">
          <Skeleton className="h-10 w-10 rounded-full" />
          <div className="flex-1 space-y-2">
            <Skeleton className="h-4 w-3/4" />
            <Skeleton className="h-3 w-1/2" />
          </div>
          <Skeleton className="h-5 w-16" />
        </div>
      ))}
    </div>
  );
}

export function ActionQueueSkeleton({ count = 3 }: { count?: number }) {
  return (
    <div className="space-y-2">
      {[...Array(count)].map((_, i) => (
        <div key={i} className="flex items-center gap-3 p-3 rounded-lg border">
          <Skeleton className="h-2 w-2 rounded-full" />
          <div className="flex-1 space-y-1">
            <Skeleton className="h-4 w-4/5" />
            <Skeleton className="h-3 w-2/3" />
          </div>
        </div>
      ))}
    </div>
  );
}

export function CategoryBreakdownSkeleton() {
  return (
    <div className="space-y-4">
      <Skeleton className="h-6 w-40" />
      <div className="h-64 flex items-end justify-around gap-4">
        {[...Array(6)].map((_, i) => (
          <div key={i} className="flex flex-col items-center gap-2 flex-1">
            <Skeleton
              className="w-full rounded-t"
              style={{ height: `${Math.random() * 60 + 40}%` }}
            />
            <Skeleton className="h-3 w-12" />
          </div>
        ))}
      </div>
    </div>
  );
}

// ============================================================================
// Receipt Skeletons
// ============================================================================

export function ReceiptCardSkeleton() {
  return (
    <div className="rounded-lg border bg-card overflow-hidden">
      <Skeleton className="h-40 w-full" />
      <div className="p-4 space-y-2">
        <Skeleton className="h-4 w-3/4" />
        <Skeleton className="h-3 w-1/2" />
        <div className="flex justify-between items-center pt-2">
          <Skeleton className="h-4 w-16" />
          <Skeleton className="h-5 w-12 rounded-full" />
        </div>
      </div>
    </div>
  );
}

export function ReceiptGridSkeleton({ count = 6 }: { count?: number }) {
  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
      {[...Array(count)].map((_, i) => (
        <ReceiptCardSkeleton key={i} />
      ))}
    </div>
  );
}

export function ReceiptIntelligenceSkeleton() {
  return (
    <div className="grid grid-cols-2 gap-6">
      {/* Image side */}
      <Skeleton className="h-96 rounded-lg" />
      {/* Fields side */}
      <div className="space-y-4">
        <Skeleton className="h-6 w-32" />
        {[...Array(5)].map((_, i) => (
          <div key={i} className="space-y-1">
            <Skeleton className="h-3 w-20" />
            <Skeleton className="h-10 w-full rounded-md" />
          </div>
        ))}
      </div>
    </div>
  );
}

// ============================================================================
// Transaction Skeletons
// ============================================================================

export function TransactionRowSkeleton() {
  return (
    <div className="flex items-center gap-4 p-3 border-b">
      <Skeleton className="h-4 w-4 rounded" />
      <Skeleton className="h-4 w-20" />
      <div className="flex-1 space-y-1">
        <Skeleton className="h-4 w-48" />
        <Skeleton className="h-3 w-32" />
      </div>
      <Skeleton className="h-4 w-16" />
      <Skeleton className="h-6 w-20 rounded-full" />
      <Skeleton className="h-5 w-20" />
    </div>
  );
}

export function TransactionTableSkeleton({ count = 10 }: { count?: number }) {
  return (
    <div className="rounded-lg border">
      {/* Header */}
      <div className="flex items-center gap-4 p-3 bg-muted/50 border-b">
        <Skeleton className="h-4 w-4 rounded" />
        <Skeleton className="h-4 w-16" />
        <Skeleton className="h-4 w-32 flex-1" />
        <Skeleton className="h-4 w-20" />
        <Skeleton className="h-4 w-16" />
        <Skeleton className="h-4 w-16" />
      </div>
      {/* Rows */}
      {[...Array(count)].map((_, i) => (
        <TransactionRowSkeleton key={i} />
      ))}
    </div>
  );
}

// ============================================================================
// Match Review Skeletons
// ============================================================================

export function MatchReviewSkeleton() {
  return (
    <div className="grid grid-cols-2 gap-8">
      {/* Receipt side */}
      <div className="space-y-4">
        <Skeleton className="h-6 w-24" />
        <Skeleton className="h-64 rounded-lg" />
        <div className="space-y-2">
          {[...Array(3)].map((_, i) => (
            <div key={i} className="flex justify-between">
              <Skeleton className="h-4 w-20" />
              <Skeleton className="h-4 w-24" />
            </div>
          ))}
        </div>
      </div>
      {/* Transaction side */}
      <div className="space-y-4">
        <Skeleton className="h-6 w-28" />
        <div className="p-4 rounded-lg border space-y-3">
          <Skeleton className="h-5 w-40" />
          <Skeleton className="h-4 w-32" />
          <Skeleton className="h-6 w-20" />
        </div>
        <div className="space-y-2">
          <Skeleton className="h-4 w-32" />
          {[...Array(3)].map((_, i) => (
            <div key={i} className="flex items-center gap-2">
              <Skeleton className="h-3 w-3 rounded-full" />
              <Skeleton className="h-3 w-full" />
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

// ============================================================================
// Analytics Skeletons
// ============================================================================

export function SpendingChartSkeleton() {
  return (
    <div className="space-y-4">
      <div className="flex justify-between items-center">
        <Skeleton className="h-6 w-40" />
        <Skeleton className="h-8 w-32 rounded-md" />
      </div>
      <Skeleton className="h-72 rounded-lg" />
    </div>
  );
}

export function TopMerchantsListSkeleton({ count = 5 }: { count?: number }) {
  return (
    <div className="space-y-3">
      {[...Array(count)].map((_, i) => (
        <div key={i} className="flex items-center gap-3">
          <Skeleton className="h-5 w-5 rounded" />
          <div className="flex-1 space-y-1">
            <Skeleton className="h-4 w-32" />
            <Skeleton className="h-2 w-full rounded-full" />
          </div>
          <Skeleton className="h-4 w-16" />
        </div>
      ))}
    </div>
  );
}
