import { cn } from '@/lib/utils';
import { type ReactNode } from 'react';
import { motion } from 'framer-motion';
import { fadeIn } from '@/lib/animations';
import {
  FileText,
  Receipt,
  Wallet,
  TrendingUp,
  Search,
  Upload,
  type LucideIcon,
} from 'lucide-react';
import { Button } from '@/components/ui/button';

/**
 * EmptyState - Zero-Data Scenarios
 *
 * Provides helpful, actionable empty states when there's no data to display.
 * Per FR-031: "Display helpful empty states with clear actions".
 *
 * Design philosophy:
 * - Never leave the user wondering "what now?"
 * - Provide a clear primary action
 * - Use contextual illustrations/icons
 */

export interface EmptyStateProps {
  /** Icon to display */
  icon?: LucideIcon | ReactNode;
  /** Main heading */
  title: string;
  /** Descriptive text */
  description?: string;
  /** Primary action button */
  action?: {
    label: string;
    onClick: () => void;
  };
  /** Secondary action */
  secondaryAction?: {
    label: string;
    onClick: () => void;
  };
  /** Additional CSS classes */
  className?: string;
}

export function EmptyState({
  icon: Icon,
  title,
  description,
  action,
  secondaryAction,
  className,
}: EmptyStateProps) {
  // If Icon is a LucideIcon (function or forwardRef), render it; otherwise use it directly as ReactNode
  const renderIcon = () => {
    if (!Icon) return null;

    // Check if Icon is a component type (function or forwardRef)
    // forwardRef components have typeof === 'object' with a render function
    const isComponentType =
      typeof Icon === 'function' ||
      (typeof Icon === 'object' &&
        Icon !== null &&
        typeof (Icon as { render?: unknown }).render === 'function');

    if (isComponentType) {
      const IconComponent = Icon as LucideIcon;
      return (
        <div className="rounded-full bg-muted p-4">
          <IconComponent className="h-8 w-8 text-muted-foreground" />
        </div>
      );
    }

    // Already a ReactNode (JSX element), return directly
    return Icon;
  };

  return (
    <motion.div
      initial="hidden"
      animate="visible"
      variants={fadeIn}
      className={cn(
        'flex flex-col items-center justify-center py-12 px-4 text-center',
        className
      )}
    >
      {renderIcon()}
      <h3 className="mt-4 text-lg font-semibold font-serif">{title}</h3>
      {description && (
        <p className="mt-2 max-w-sm text-sm text-muted-foreground">
          {description}
        </p>
      )}
      {(action || secondaryAction) && (
        <div className="mt-6 flex gap-3">
          {action && <Button onClick={action.onClick}>{action.label}</Button>}
          {secondaryAction && (
            <Button variant="outline" onClick={secondaryAction.onClick}>
              {secondaryAction.label}
            </Button>
          )}
        </div>
      )}
    </motion.div>
  );
}

// ============================================================================
// Pre-configured Empty States for Common Scenarios
// ============================================================================

export function EmptyReceipts({ onUpload }: { onUpload: () => void }) {
  return (
    <EmptyState
      icon={Receipt}
      title="No receipts yet"
      description="Upload your first receipt to start tracking expenses. We'll extract the details automatically."
      action={{
        label: 'Upload Receipt',
        onClick: onUpload,
      }}
    />
  );
}

export function EmptyTransactions({
  onImport,
}: {
  onImport: () => void;
}) {
  return (
    <EmptyState
      icon={Wallet}
      title="No transactions found"
      description="Import your bank statements to see your transactions here."
      action={{
        label: 'Import Statement',
        onClick: onImport,
      }}
    />
  );
}

export function EmptySearchResults({
  searchTerm,
  onClear,
}: {
  searchTerm: string;
  onClear: () => void;
}) {
  return (
    <EmptyState
      icon={Search}
      title="No results found"
      description={`We couldn't find anything matching "${searchTerm}". Try adjusting your search or filters.`}
      action={{
        label: 'Clear Search',
        onClick: onClear,
      }}
    />
  );
}

export function EmptyReports({ onGenerate }: { onGenerate: () => void }) {
  return (
    <EmptyState
      icon={FileText}
      title="No reports generated"
      description="Generate your first expense report to summarize your spending."
      action={{
        label: 'Generate Report',
        onClick: onGenerate,
      }}
    />
  );
}

export function EmptyDashboard({
  onUploadReceipt,
  onImportStatement,
}: {
  onUploadReceipt: () => void;
  onImportStatement: () => void;
}) {
  return (
    <EmptyState
      icon={TrendingUp}
      title="Welcome to ExpenseFlow"
      description="Get started by uploading receipts or importing bank statements. Your spending insights will appear here."
      action={{
        label: 'Upload Receipt',
        onClick: onUploadReceipt,
      }}
      secondaryAction={{
        label: 'Import Statement',
        onClick: onImportStatement,
      }}
    />
  );
}

export function EmptyPendingMatches() {
  return (
    <EmptyState
      icon={Upload}
      title="All caught up!"
      description="You have no pending matches to review. Upload more receipts or import statements to continue."
    />
  );
}
