'use client';

import { motion } from 'framer-motion';
import { formatCurrency, formatDate, getStatusVariant, cn } from '@/lib/utils';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { ConfidenceIndicator } from '@/components/design-system/confidence-indicator';
import { ImageOff, Pencil, Trash2, CheckCircle2 } from 'lucide-react';
import type { ReceiptSummary } from '@/types/api';

interface ReceiptCardProps {
  /** Receipt data */
  receipt: ReceiptSummary;
  /** Whether the card is selected (for multi-select) */
  isSelected?: boolean;
  /** Callback when card is selected/deselected */
  onSelect?: (selected: boolean) => void;
  /** Callback when edit is clicked */
  onEdit?: () => void;
  /** Callback when delete is clicked */
  onDelete?: () => void;
  /** Whether to show confidence indicator */
  showConfidence?: boolean;
  /** Confidence score (0-1) to display if showConfidence is true */
  confidence?: number;
  /** Whether to show selection checkbox */
  showCheckbox?: boolean;
  /** Whether the card is in compact mode */
  compact?: boolean;
  /** Click handler (if not using as link) */
  onClick?: () => void;
}

export function ReceiptCard({
  receipt,
  isSelected = false,
  onSelect,
  onEdit,
  onDelete,
  showConfidence = false,
  confidence,
  showCheckbox = false,
  compact = false,
  onClick,
}: ReceiptCardProps) {
  const statusVariant = getStatusVariant(receipt.status);

  const handleClick = (e: React.MouseEvent) => {
    // If clicking action buttons, don't trigger card click
    if ((e.target as HTMLElement).closest('button, [role="checkbox"]')) {
      return;
    }
    onClick?.();
  };

  const handleCheckboxChange = (checked: boolean) => {
    onSelect?.(checked);
  };

  const CardWrapper = onClick ? 'div' : 'a';
  const cardProps = onClick
    ? { onClick: handleClick }
    : { href: `/receipts/${receipt.id}` };

  return (
    <CardWrapper {...cardProps}>
      <Card
        className={cn(
          'group overflow-hidden transition-all cursor-pointer',
          'hover:bg-accent/50 hover:shadow-md',
          isSelected && 'ring-2 ring-primary bg-primary/5',
          compact && 'flex flex-row items-center'
        )}
      >
        {/* Thumbnail */}
        <div
          className={cn(
            'relative bg-muted',
            compact ? 'h-16 w-16 shrink-0' : 'aspect-[4/3]'
          )}
        >
          {receipt.thumbnailUrl ? (
            <img
              src={receipt.thumbnailUrl}
              alt={receipt.originalFilename}
              className="object-cover w-full h-full"
            />
          ) : (
            <div className="flex items-center justify-center w-full h-full">
              <ImageOff
                className={cn(
                  'text-muted-foreground/50',
                  compact ? 'h-6 w-6' : 'h-12 w-12'
                )}
              />
            </div>
          )}

          {/* Selection checkbox */}
          {showCheckbox && (
            <div
              className={cn(
                'absolute top-2 left-2 transition-opacity',
                !isSelected && 'opacity-0 group-hover:opacity-100'
              )}
            >
              <Checkbox
                checked={isSelected}
                onCheckedChange={handleCheckboxChange}
                className="bg-background/80 backdrop-blur-sm"
              />
            </div>
          )}

          {/* Status badge - only show on non-compact */}
          {!compact && (
            <Badge variant={statusVariant} className="absolute top-2 right-2">
              {receipt.status}
            </Badge>
          )}

          {/* Selection overlay */}
          {isSelected && (
            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              className="absolute inset-0 bg-primary/20 flex items-center justify-center"
            >
              <CheckCircle2 className="h-8 w-8 text-primary" />
            </motion.div>
          )}
        </div>

        {/* Content */}
        <CardContent className={cn('p-3', compact && 'flex-1 py-2')}>
          <div className={cn('space-y-1', compact && 'flex items-center gap-3')}>
            <div className={cn(compact && 'flex-1 min-w-0')}>
              <p
                className="text-sm font-medium truncate"
                title={receipt.originalFilename}
              >
                {receipt.vendor || receipt.originalFilename}
              </p>
              <div className="flex items-center justify-between text-xs text-muted-foreground">
                <span>{receipt.date ? formatDate(receipt.date) : 'No date'}</span>
                {!compact && (
                  <span className="font-medium text-foreground">
                    {receipt.amount != null
                      ? formatCurrency(receipt.amount, receipt.currency)
                      : '--'}
                  </span>
                )}
              </div>
            </div>

            {/* Compact mode: amount and status */}
            {compact && (
              <>
                <span className="font-semibold tabular-nums">
                  {receipt.amount != null
                    ? formatCurrency(receipt.amount, receipt.currency)
                    : '--'}
                </span>
                <Badge variant={statusVariant} className="shrink-0">
                  {receipt.status}
                </Badge>
              </>
            )}

            {/* Confidence indicator */}
            {showConfidence && confidence !== undefined && (
              <div className={cn(compact ? 'shrink-0' : 'pt-1')}>
                <ConfidenceIndicator
                  score={confidence}
                  size="sm"
                  showLabel={!compact}
                />
              </div>
            )}

            {/* Action buttons */}
            {(onEdit || onDelete) && (
              <div
                className={cn(
                  'flex items-center gap-1',
                  !compact && 'pt-2 md:opacity-0 md:group-hover:opacity-100 transition-opacity',
                  compact && 'shrink-0'
                )}
              >
                {onEdit && (
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-11 w-11 md:h-7 md:w-7"
                    onClick={(e) => {
                      e.preventDefault();
                      e.stopPropagation();
                      onEdit();
                    }}
                    title="Edit receipt"
                  >
                    <Pencil className="h-4 w-4 md:h-3.5 md:w-3.5" />
                  </Button>
                )}
                {onDelete && (
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-11 w-11 md:h-7 md:w-7 text-destructive hover:text-destructive"
                    onClick={(e) => {
                      e.preventDefault();
                      e.stopPropagation();
                      onDelete();
                    }}
                    title="Delete receipt"
                  >
                    <Trash2 className="h-4 w-4 md:h-3.5 md:w-3.5" />
                  </Button>
                )}
              </div>
            )}
          </div>
        </CardContent>
      </Card>
    </CardWrapper>
  );
}

export function ReceiptCardSkeleton() {
  return (
    <Card className="overflow-hidden">
      <Skeleton className="aspect-[4/3]" />
      <CardContent className="p-3">
        <div className="space-y-2">
          <Skeleton className="h-4 w-3/4" />
          <div className="flex items-center justify-between">
            <Skeleton className="h-3 w-16" />
            <Skeleton className="h-3 w-12" />
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
