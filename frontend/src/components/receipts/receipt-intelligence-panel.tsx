'use client';

/**
 * ReceiptIntelligencePanel Component (T042)
 *
 * Side-by-side layout showing:
 * - Left: Receipt image with zoom/pan capabilities
 * - Right: AI-extracted fields with inline editing
 *
 * Features:
 * - Undo support via useUndo hook
 * - Field highlighting on image (when coordinates available)
 * - Overall confidence display
 * - Save/discard all changes
 * - Training feedback collection (Feature 024)
 * - Optimistic concurrency with rowVersion
 */

import { useState, useCallback, useMemo } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
  RotateCcw,
  Save,
  X,
  AlertTriangle,
  CheckCircle2,
  Loader2,
  Lock,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Separator } from '@/components/ui/separator';
import { ScrollArea } from '@/components/ui/scroll-area';
import { DocumentViewer } from '@/components/ui/document-viewer';
import { ExtractedField, ExtractedFieldSkeleton } from './extracted-field';
import { useUndo } from '@/hooks/ui/use-undo';
import { useUpdateReceipt } from '@/hooks/queries/use-receipts';
import type {
  ReceiptPreview,
  ExtractedField as ExtractedFieldType,
  ExtractedFieldKey,
  CorrectionMetadata,
} from '@/types/receipt';

interface ReceiptIntelligencePanelProps {
  /** Receipt data with extracted fields */
  receipt: ReceiptPreview;
  /** Callback when a field is updated (for external tracking) */
  onFieldUpdate?: (
    field: ExtractedFieldKey,
    value: string | number | null,
    previousValue: string | number | null
  ) => void;
  /** Callback after all changes are saved */
  onSaveComplete?: () => void;
  /** Callback when changes are discarded */
  onDiscard?: () => void;
  /** Loading state */
  isLoading?: boolean;
  /** Custom class name */
  className?: string;
}

interface FieldEditState {
  field: ExtractedFieldKey;
  originalValue: string | number | null;
  currentValue: string | number | null;
}

export function ReceiptIntelligencePanel({
  receipt,
  onFieldUpdate,
  onSaveComplete,
  onDiscard,
  isLoading = false,
  className,
}: ReceiptIntelligencePanelProps) {
  // Field highlight state (for future bounding box feature)
  const [highlightedField] = useState<ExtractedFieldKey | null>(null);

  // Use the update receipt mutation hook (Feature 024)
  const { mutate: updateReceipt, isPending: isSaving } = useUpdateReceipt();

  // Track field edits with undo support
  const {
    current: editedFields,
    push: setEditedFields,
    undo,
    canUndo,
    reset: clearEdits,
  } = useUndo<Map<ExtractedFieldKey, FieldEditState>>(new Map());

  // Check if receipt is still processing (cannot edit)
  const isProcessing = receipt.status === 'processing';

  // Calculate overall confidence
  const overallConfidence = useMemo(() => {
    if (!receipt.extractedFields.length) return 0;
    const sum = receipt.extractedFields.reduce((acc, f) => acc + f.confidence, 0);
    return sum / receipt.extractedFields.length;
  }, [receipt.extractedFields]);

  // Get field with current edit state
  const getFieldWithEdits = useCallback(
    (field: ExtractedFieldType): ExtractedFieldType => {
      const edit = editedFields.get(field.key);
      if (!edit) return field;

      return {
        ...field,
        value: edit.currentValue,
        isEdited: true,
        originalValue: edit.originalValue ?? field.originalValue,
      };
    },
    [editedFields]
  );

  // Handle field update
  const handleFieldUpdate = (key: ExtractedFieldKey, newValue: string | number | null) => {
    const originalField = receipt.extractedFields.find((f) => f.key === key);
    const originalValue = originalField?.value ?? null;

    // Track the edit
    const newEdits = new Map(editedFields);
    const existingEdit = newEdits.get(key);

    // Preserve the original value from the first edit
    const preservedOriginal = existingEdit
      ? existingEdit.originalValue
      : originalValue;

    newEdits.set(key, {
      field: key,
      originalValue: preservedOriginal,
      currentValue: newValue,
    });

    setEditedFields(newEdits);
    onFieldUpdate?.(key, newValue, originalValue);
  };

  // Handle field undo
  const handleFieldUndo = (key: ExtractedFieldKey) => {
    const edit = editedFields.get(key);
    if (!edit) return;

    const newEdits = new Map(editedFields);
    newEdits.delete(key);
    setEditedFields(newEdits);

    onFieldUpdate?.(key, edit.originalValue, edit.currentValue);
  };

  // Map ExtractedFieldKey to CorrectionMetadata fieldName
  const mapFieldKeyToCorrectionField = (
    key: ExtractedFieldKey
  ): CorrectionMetadata['fieldName'] | null => {
    switch (key) {
      case 'merchant':
        return 'vendor';
      case 'amount':
        return 'amount';
      case 'date':
        return 'date';
      case 'taxAmount':
        return 'tax';
      case 'currency':
        return 'currency';
      default:
        return null; // Fields like tip, subtotal, etc. don't have correction mapping yet
    }
  };

  // Handle save all - sends update with corrections for training feedback
  const handleSaveAll = () => {
    if (editedFields.size === 0) return;

    // Build the request with all edited fields
    const request: {
      vendor?: string | null;
      amount?: number | null;
      date?: string | null;
      tax?: number | null;
      currency?: string | null;
      rowVersion: number;
      corrections: CorrectionMetadata[];
    } = {
      rowVersion: receipt.rowVersion,
      corrections: [],
    };

    // Collect all edits and build corrections array
    editedFields.forEach((edit: FieldEditState) => {
      // Map the edit to the request field
      switch (edit.field) {
        case 'merchant':
          request.vendor = edit.currentValue as string | null;
          break;
        case 'amount':
          request.amount = edit.currentValue as number | null;
          break;
        case 'date':
          request.date = edit.currentValue as string | null;
          break;
        case 'taxAmount':
          request.tax = edit.currentValue as number | null;
          break;
        case 'currency':
          request.currency = edit.currentValue as string | null;
          break;
      }

      // Create correction metadata for training feedback
      const correctionField = mapFieldKeyToCorrectionField(edit.field);
      if (correctionField && edit.originalValue !== edit.currentValue) {
        request.corrections.push({
          fieldName: correctionField,
          originalValue: String(edit.originalValue ?? ''),
        });
      }
    });

    // Submit the update
    updateReceipt(
      { receiptId: receipt.id, request },
      {
        onSuccess: () => {
          clearEdits(new Map());
          onSaveComplete?.();
        },
      }
    );
  };

  // Handle discard
  const handleDiscard = () => {
    // Revert all edits
    editedFields.forEach((edit: FieldEditState, key: ExtractedFieldKey) => {
      onFieldUpdate?.(key, edit.originalValue, edit.currentValue);
    });
    clearEdits(new Map());
    onDiscard?.();
  };

  // Check if there are unsaved changes
  const hasChanges = editedFields.size > 0;

  // Get confidence level for styling
  const getConfidenceLevel = (score: number): 'high' | 'medium' | 'low' => {
    if (score >= 0.9) return 'high';
    if (score >= 0.7) return 'medium';
    return 'low';
  };

  if (isLoading) {
    return <ReceiptIntelligencePanelSkeleton />;
  }

  return (
    <div className={cn('flex flex-col lg:flex-row gap-4 h-full', className)}>
      {/* Left: Document Viewer */}
      <Card className="flex-1 flex flex-col min-w-0 min-h-[300px] lg:min-h-0">
        <CardHeader className="pb-2 shrink-0">
          <CardTitle className="text-base">Receipt Document</CardTitle>
        </CardHeader>
        <CardContent className="flex-1 overflow-hidden p-2">
          <div className="relative h-full w-full rounded-lg bg-muted/50">
            <DocumentViewer
              src={receipt.imageUrl}
              filename={receipt.filename}
              alt="Receipt"
              showControls={true}
              className="h-full"
            />

            {/* Field highlight overlay (when bounding boxes available) */}
            {highlightedField && (
              <div className="absolute inset-0 pointer-events-none">
                {receipt.extractedFields
                  .filter((f) => f.key === highlightedField && f.boundingBox)
                  .map((field) => (
                    <motion.div
                      key={field.key}
                      className="absolute border-2 border-primary bg-primary/10 rounded"
                      style={{
                        left: `${field.boundingBox!.x}%`,
                        top: `${field.boundingBox!.y}%`,
                        width: `${field.boundingBox!.width}%`,
                        height: `${field.boundingBox!.height}%`,
                      }}
                      initial={{ opacity: 0, scale: 0.95 }}
                      animate={{ opacity: 1, scale: 1 }}
                      exit={{ opacity: 0 }}
                    />
                  ))}
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Right: Extracted Fields */}
      <Card className="w-full lg:w-96 flex flex-col shrink-0">
        <CardHeader className="pb-2 shrink-0">
          <div className="flex items-center justify-between">
            <div className="space-y-1">
              <CardTitle className="text-base">Extracted Data</CardTitle>
              <p className="text-xs text-muted-foreground">
                AI-extracted with{' '}
                <span
                  className={cn(
                    'font-medium',
                    getConfidenceLevel(overallConfidence) === 'high' &&
                      'text-green-600',
                    getConfidenceLevel(overallConfidence) === 'medium' &&
                      'text-amber-600',
                    getConfidenceLevel(overallConfidence) === 'low' && 'text-red-600'
                  )}
                >
                  {Math.round(overallConfidence * 100)}% confidence
                </span>
              </p>
            </div>
            {isProcessing && (
              <Badge variant="outline" className="text-blue-600 border-blue-600/30">
                <Loader2 className="h-3 w-3 mr-1 animate-spin" />
                Processing
              </Badge>
            )}
            {receipt.status === 'complete' && !isProcessing && (
              <Badge variant="outline" className="text-green-600 border-green-600/30">
                <CheckCircle2 className="h-3 w-3 mr-1" />
                Complete
              </Badge>
            )}
            {receipt.status === 'review_required' && (
              <Badge variant="outline" className="text-amber-600 border-amber-600/30">
                <AlertTriangle className="h-3 w-3 mr-1" />
                Review
              </Badge>
            )}
          </div>
        </CardHeader>

        <Separator />

        <ScrollArea className="flex-1">
          <CardContent className="p-4 space-y-3">
            {/* Processing lock message */}
            {isProcessing && (
              <div className="flex items-center gap-2 p-3 rounded-lg bg-blue-50 dark:bg-blue-950/30 text-blue-600 dark:text-blue-400 text-sm mb-4">
                <Lock className="h-4 w-4 flex-shrink-0" />
                <span>Fields are locked while the receipt is being processed.</span>
              </div>
            )}

            {receipt.extractedFields.map((field) => (
              <ExtractedField
                key={field.key}
                field={getFieldWithEdits(field)}
                onUpdate={(key, value) => handleFieldUpdate(key, value)}
                onUndo={() => handleFieldUndo(field.key)}
                canUndo={editedFields.has(field.key)}
                isSaving={isSaving}
                readOnly={isProcessing}
                showConfidence
                size="md"
              />
            ))}

            {receipt.extractedFields.length === 0 && (
              <div className="text-center py-8 text-muted-foreground">
                <AlertTriangle className="h-8 w-8 mx-auto mb-2 opacity-50" />
                <p className="text-sm">No fields extracted yet</p>
              </div>
            )}
          </CardContent>
        </ScrollArea>

        {/* Action buttons */}
        <AnimatePresence>
          {hasChanges && (
            <motion.div
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: 20 }}
              className="shrink-0 border-t p-4 bg-muted/30"
            >
              <div className="flex items-center gap-2 flex-wrap md:flex-nowrap">
                <Button
                  variant="ghost"
                  size="sm"
                  className="min-h-[44px] md:min-h-0"
                  onClick={undo}
                  disabled={!canUndo || isSaving}
                >
                  <RotateCcw className="h-4 w-4 mr-1" />
                  Undo
                </Button>

                <div className="flex-1 hidden md:block" />

                <Button
                  variant="outline"
                  size="sm"
                  className="min-h-[44px] flex-1 md:flex-none md:min-h-0"
                  onClick={handleDiscard}
                  disabled={isSaving}
                >
                  <X className="h-4 w-4 mr-1" />
                  Discard
                </Button>

                <Button
                  size="sm"
                  className="min-h-[44px] flex-1 md:flex-none md:min-h-0"
                  onClick={handleSaveAll}
                  disabled={isSaving}
                >
                  {isSaving ? (
                    <Loader2 className="h-4 w-4 mr-1 animate-spin" />
                  ) : (
                    <Save className="h-4 w-4 mr-1" />
                  )}
                  Save All
                </Button>
              </div>

              <p className="text-xs text-muted-foreground mt-2">
                {editedFields.size} field{editedFields.size !== 1 ? 's' : ''} modified
              </p>
            </motion.div>
          )}
        </AnimatePresence>
      </Card>
    </div>
  );
}

/**
 * Skeleton loader for ReceiptIntelligencePanel
 */
export function ReceiptIntelligencePanelSkeleton() {
  return (
    <div className="flex flex-col lg:flex-row gap-4 h-full">
      {/* Image skeleton */}
      <Card className="flex-1 min-h-[300px] lg:min-h-0">
        <CardHeader className="pb-2">
          <div className="h-5 w-24 rounded bg-muted animate-pulse" />
        </CardHeader>
        <CardContent className="p-2">
          <div className="h-full w-full rounded-lg bg-muted animate-pulse min-h-[400px]" />
        </CardContent>
      </Card>

      {/* Fields skeleton */}
      <Card className="w-full lg:w-96">
        <CardHeader className="pb-2">
          <div className="h-5 w-32 rounded bg-muted animate-pulse" />
        </CardHeader>
        <Separator />
        <CardContent className="p-4 space-y-3">
          {[...Array(5)].map((_, i) => (
            <ExtractedFieldSkeleton key={i} />
          ))}
        </CardContent>
      </Card>
    </div>
  );
}
