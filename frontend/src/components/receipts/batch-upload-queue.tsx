'use client';

/**
 * BatchUploadQueue Component (T043)
 *
 * Displays a queue of receipt uploads with:
 * - Individual progress tracking per file
 * - Status indicators (pending, uploading, processing, complete, error)
 * - Cancel/retry actions
 * - Thumbnail previews
 * - Batch progress summary
 */

import { useMemo } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
  FileImage,
  X,
  RotateCcw,
  CheckCircle2,
  AlertCircle,
  Loader2,
  Upload,
  Clock,
  Sparkles,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Progress } from '@/components/ui/progress';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { ScrollArea } from '@/components/ui/scroll-area';
import type { ReceiptUploadState } from '@/types/receipt';

interface BatchUploadQueueProps {
  /** Array of upload states */
  uploads: ReceiptUploadState[];
  /** Callback to cancel an upload */
  onCancel?: (uploadId: string) => void;
  /** Callback to retry a failed upload */
  onRetry?: (uploadId: string) => void;
  /** Callback to remove a completed/failed upload from queue */
  onRemove?: (uploadId: string) => void;
  /** Callback to clear all completed uploads */
  onClearCompleted?: () => void;
  /** Whether to show compact view */
  compact?: boolean;
  /** Maximum height for scroll area */
  maxHeight?: number;
  /** Custom class name */
  className?: string;
}

// Status icon component
function StatusIcon({ status }: { status: ReceiptUploadState['status'] }) {
  switch (status) {
    case 'pending':
      return <Clock className="h-4 w-4 text-muted-foreground" />;
    case 'uploading':
      return <Upload className="h-4 w-4 text-blue-500 animate-pulse" />;
    case 'processing':
      return <Sparkles className="h-4 w-4 text-amber-500 animate-pulse" />;
    case 'complete':
      return <CheckCircle2 className="h-4 w-4 text-green-500" />;
    case 'error':
      return <AlertCircle className="h-4 w-4 text-destructive" />;
    default:
      return <Loader2 className="h-4 w-4 animate-spin" />;
  }
}

// Status badge component
function StatusBadge({ status }: { status: ReceiptUploadState['status'] }) {
  const variants: Record<
    ReceiptUploadState['status'],
    { label: string; variant: 'default' | 'secondary' | 'destructive' | 'outline' }
  > = {
    pending: { label: 'Pending', variant: 'secondary' },
    uploading: { label: 'Uploading', variant: 'default' },
    processing: { label: 'Processing', variant: 'outline' },
    complete: { label: 'Complete', variant: 'outline' },
    error: { label: 'Failed', variant: 'destructive' },
  };

  const { label, variant } = variants[status];

  return (
    <Badge
      variant={variant}
      className={cn(
        'text-xs',
        status === 'complete' && 'text-green-600 border-green-600/30',
        status === 'processing' && 'text-amber-600 border-amber-600/30'
      )}
    >
      {label}
    </Badge>
  );
}

// Individual upload item
function UploadItem({
  upload,
  onCancel,
  onRetry,
  onRemove,
  compact = false,
}: {
  upload: ReceiptUploadState;
  onCancel?: () => void;
  onRetry?: () => void;
  onRemove?: () => void;
  compact?: boolean;
}) {
  const isActive = upload.status === 'uploading' || upload.status === 'processing';
  const canCancel = upload.status === 'pending' || upload.status === 'uploading';
  const canRetry = upload.status === 'error';
  const canRemove = upload.status === 'complete' || upload.status === 'error';

  return (
    <motion.div
      layout
      initial={{ opacity: 0, y: -10, scale: 0.95 }}
      animate={{ opacity: 1, y: 0, scale: 1 }}
      exit={{ opacity: 0, x: -20, scale: 0.95 }}
      transition={{ duration: 0.2 }}
      className={cn(
        'flex items-center gap-3 rounded-lg border p-3 transition-colors',
        upload.status === 'error' && 'border-destructive/50 bg-destructive/5',
        upload.status === 'complete' && 'border-green-500/30 bg-green-50/50 dark:bg-green-950/20',
        isActive && 'border-primary/50 bg-primary/5'
      )}
    >
      {/* Thumbnail */}
      <div className="relative h-12 w-12 shrink-0 rounded-md overflow-hidden bg-muted">
        {upload.preview ? (
          <img
            src={upload.preview}
            alt={upload.file.name}
            className="h-full w-full object-cover"
          />
        ) : (
          <div className="flex h-full w-full items-center justify-center">
            <FileImage className="h-6 w-6 text-muted-foreground" />
          </div>
        )}

        {/* Status overlay for active states */}
        {isActive && (
          <div className="absolute inset-0 flex items-center justify-center bg-black/40">
            <Loader2 className="h-5 w-5 text-white animate-spin" />
          </div>
        )}
      </div>

      {/* File info */}
      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-2">
          <span className="truncate text-sm font-medium">{upload.file.name}</span>
          {!compact && <StatusBadge status={upload.status} />}
        </div>

        <div className="mt-1 flex items-center gap-2 text-xs text-muted-foreground">
          <span>{(upload.file.size / 1024).toFixed(1)} KB</span>
          {upload.status === 'error' && upload.error && (
            <span className="text-destructive truncate">{upload.error}</span>
          )}
        </div>

        {/* Progress bar for active uploads */}
        {isActive && (
          <div className="mt-2">
            <Progress value={upload.progress} className="h-1" />
          </div>
        )}
      </div>

      {/* Status icon for compact mode */}
      {compact && (
        <div className="shrink-0">
          <StatusIcon status={upload.status} />
        </div>
      )}

      {/* Actions */}
      <div className="flex shrink-0 items-center gap-1">
        {canCancel && (
          <Button
            variant="ghost"
            size="icon"
            className="h-11 w-11 md:h-7 md:w-7"
            onClick={onCancel}
            title="Cancel upload"
          >
            <X className="h-5 w-5 md:h-4 md:w-4" />
          </Button>
        )}

        {canRetry && (
          <Button
            variant="ghost"
            size="icon"
            className="h-11 w-11 md:h-7 md:w-7 text-destructive"
            onClick={onRetry}
            title="Retry upload"
          >
            <RotateCcw className="h-5 w-5 md:h-4 md:w-4" />
          </Button>
        )}

        {canRemove && (
          <Button
            variant="ghost"
            size="icon"
            className="h-11 w-11 md:h-7 md:w-7"
            onClick={onRemove}
            title="Remove from queue"
          >
            <X className="h-5 w-5 md:h-4 md:w-4" />
          </Button>
        )}
      </div>
    </motion.div>
  );
}

export function BatchUploadQueue({
  uploads,
  onCancel,
  onRetry,
  onRemove,
  onClearCompleted,
  compact = false,
  maxHeight = 400,
  className,
}: BatchUploadQueueProps) {
  // Calculate summary stats
  const stats = useMemo(() => {
    return {
      total: uploads.length,
      pending: uploads.filter((u) => u.status === 'pending').length,
      uploading: uploads.filter((u) => u.status === 'uploading').length,
      processing: uploads.filter((u) => u.status === 'processing').length,
      complete: uploads.filter((u) => u.status === 'complete').length,
      error: uploads.filter((u) => u.status === 'error').length,
    };
  }, [uploads]);

  // Calculate overall progress
  const overallProgress = useMemo(() => {
    if (uploads.length === 0) return 0;
    const totalProgress = uploads.reduce((sum, u) => {
      if (u.status === 'complete') return sum + 100;
      if (u.status === 'error') return sum + 0;
      return sum + u.progress;
    }, 0);
    return totalProgress / uploads.length;
  }, [uploads]);

  const hasActiveUploads = stats.uploading > 0 || stats.processing > 0;
  const hasCompletedUploads = stats.complete > 0 || stats.error > 0;

  if (uploads.length === 0) {
    return null;
  }

  return (
    <Card className={className}>
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between">
          <CardTitle className="text-base flex items-center gap-2">
            Upload Queue
            {hasActiveUploads && (
              <Loader2 className="h-4 w-4 animate-spin text-primary" />
            )}
          </CardTitle>

          <div className="flex items-center gap-2">
            {/* Summary badges */}
            {stats.complete > 0 && (
              <Badge variant="outline" className="text-green-600 border-green-600/30">
                <CheckCircle2 className="h-3 w-3 mr-1" />
                {stats.complete}
              </Badge>
            )}
            {stats.error > 0 && (
              <Badge variant="destructive">
                <AlertCircle className="h-3 w-3 mr-1" />
                {stats.error}
              </Badge>
            )}
            {stats.pending + stats.uploading + stats.processing > 0 && (
              <Badge variant="secondary">
                {stats.pending + stats.uploading + stats.processing} pending
              </Badge>
            )}

            {/* Clear completed button */}
            {hasCompletedUploads && onClearCompleted && (
              <Button
                variant="ghost"
                size="sm"
                onClick={onClearCompleted}
                className="text-xs min-h-[44px] md:min-h-0"
              >
                Clear completed
              </Button>
            )}
          </div>
        </div>

        {/* Overall progress */}
        {hasActiveUploads && (
          <div className="mt-2">
            <div className="flex items-center justify-between text-xs text-muted-foreground mb-1">
              <span>
                Processing {stats.uploading + stats.processing} of {stats.total}
              </span>
              <span>{Math.round(overallProgress)}%</span>
            </div>
            <Progress value={overallProgress} className="h-2" />
          </div>
        )}
      </CardHeader>

      <CardContent className="pt-0">
        <ScrollArea style={{ maxHeight }}>
          <AnimatePresence mode="popLayout">
            <div className="space-y-2">
              {uploads.map((upload) => (
                <UploadItem
                  key={upload.uploadId}
                  upload={upload}
                  onCancel={onCancel ? () => onCancel(upload.uploadId) : undefined}
                  onRetry={onRetry ? () => onRetry(upload.uploadId) : undefined}
                  onRemove={onRemove ? () => onRemove(upload.uploadId) : undefined}
                  compact={compact}
                />
              ))}
            </div>
          </AnimatePresence>
        </ScrollArea>
      </CardContent>
    </Card>
  );
}

/**
 * Compact inline queue for embedding in other components
 */
export function BatchUploadQueueInline({
  uploads,
  onCancel,
  className,
}: {
  uploads: ReceiptUploadState[];
  onCancel?: (uploadId: string) => void;
  className?: string;
}) {
  const activeUploads = uploads.filter(
    (u) => u.status === 'uploading' || u.status === 'processing'
  );

  if (activeUploads.length === 0) return null;

  return (
    <div className={cn('flex items-center gap-2 text-sm', className)}>
      <Loader2 className="h-4 w-4 animate-spin text-primary" />
      <span className="text-muted-foreground">
        Uploading {activeUploads.length} file{activeUploads.length !== 1 ? 's' : ''}...
      </span>
      {onCancel && (
        <Button
          variant="ghost"
          size="sm"
          className="h-11 md:h-6 text-xs"
          onClick={() => activeUploads.forEach((u) => onCancel(u.uploadId))}
        >
          Cancel
        </Button>
      )}
    </div>
  );
}
