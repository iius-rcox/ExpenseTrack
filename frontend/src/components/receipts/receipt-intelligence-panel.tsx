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
 */

import { useState, useCallback, useMemo } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
  ZoomIn,
  ZoomOut,
  RotateCw,
  Maximize2,
  RotateCcw,
  Save,
  X,
  AlertTriangle,
  CheckCircle2,
  Loader2,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Separator } from '@/components/ui/separator';
import { ScrollArea } from '@/components/ui/scroll-area';
import { ExtractedField, ExtractedFieldSkeleton } from './extracted-field';
import { useUndo } from '@/hooks/ui/use-undo';
import type {
  ReceiptPreview,
  ExtractedField as ExtractedFieldType,
  ExtractedFieldKey,
} from '@/types/receipt';

interface ReceiptIntelligencePanelProps {
  /** Receipt data with extracted fields */
  receipt: ReceiptPreview;
  /** Callback when a field is updated */
  onFieldUpdate?: (
    field: ExtractedFieldKey,
    value: string | number | null,
    previousValue: string | number | null
  ) => void;
  /** Callback when all changes are saved */
  onSaveAll?: () => void;
  /** Callback when changes are discarded */
  onDiscard?: () => void;
  /** Whether any save operation is in progress */
  isSaving?: boolean;
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
  onSaveAll,
  onDiscard,
  isSaving = false,
  isLoading = false,
  className,
}: ReceiptIntelligencePanelProps) {
  // Image viewer state
  const [zoom, setZoom] = useState(1);
  const [rotation, setRotation] = useState(0);
  const [highlightedField] = useState<ExtractedFieldKey | null>(null);

  // Track field edits with undo support
  const {
    current: editedFields,
    push: setEditedFields,
    undo,
    canUndo,
    reset: clearEdits,
  } = useUndo<Map<ExtractedFieldKey, FieldEditState>>(new Map());

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

  // Handle save all
  const handleSaveAll = () => {
    onSaveAll?.();
    clearEdits(new Map());
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

  // Image controls
  const handleZoomIn = () => setZoom((z) => Math.min(z + 0.25, 3));
  const handleZoomOut = () => setZoom((z) => Math.max(z - 0.25, 0.5));
  const handleRotate = () => setRotation((r) => (r + 90) % 360);
  const handleReset = () => {
    setZoom(1);
    setRotation(0);
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
    <div className={cn('flex gap-4 h-full', className)}>
      {/* Left: Image Viewer */}
      <Card className="flex-1 flex flex-col min-w-0">
        <CardHeader className="pb-2 shrink-0">
          <div className="flex items-center justify-between">
            <CardTitle className="text-base">Receipt Image</CardTitle>
            <div className="flex items-center gap-1">
              <Button
                variant="ghost"
                size="icon"
                className="h-11 w-11 md:h-8 md:w-8"
                onClick={handleZoomOut}
                disabled={zoom <= 0.5}
              >
                <ZoomOut className="h-5 w-5 md:h-4 md:w-4" />
              </Button>
              <span className="text-xs text-muted-foreground w-12 text-center">
                {Math.round(zoom * 100)}%
              </span>
              <Button
                variant="ghost"
                size="icon"
                className="h-11 w-11 md:h-8 md:w-8"
                onClick={handleZoomIn}
                disabled={zoom >= 3}
              >
                <ZoomIn className="h-5 w-5 md:h-4 md:w-4" />
              </Button>
              <Separator orientation="vertical" className="h-4 mx-1 hidden md:block" />
              <Button
                variant="ghost"
                size="icon"
                className="h-11 w-11 md:h-8 md:w-8"
                onClick={handleRotate}
              >
                <RotateCw className="h-5 w-5 md:h-4 md:w-4" />
              </Button>
              <Button
                variant="ghost"
                size="icon"
                className="h-11 w-11 md:h-8 md:w-8"
                onClick={handleReset}
              >
                <Maximize2 className="h-5 w-5 md:h-4 md:w-4" />
              </Button>
            </div>
          </div>
        </CardHeader>
        <CardContent className="flex-1 overflow-hidden p-2">
          <div className="relative h-full w-full overflow-auto rounded-lg bg-muted/50">
            <div
              className="min-h-full min-w-full flex items-center justify-center p-4"
              style={{
                transform: `scale(${zoom}) rotate(${rotation}deg)`,
                transformOrigin: 'center center',
                transition: 'transform 0.2s ease',
              }}
            >
              {receipt.imageUrl ? (
                <img
                  src={receipt.imageUrl}
                  alt="Receipt"
                  className="max-w-full max-h-full object-contain shadow-lg rounded"
                  draggable={false}
                />
              ) : (
                <div className="flex flex-col items-center justify-center text-muted-foreground">
                  <AlertTriangle className="h-12 w-12 mb-2" />
                  <span>Image not available</span>
                </div>
              )}

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
          </div>
        </CardContent>
      </Card>

      {/* Right: Extracted Fields */}
      <Card className="w-96 flex flex-col shrink-0">
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
            {receipt.status === 'complete' && (
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
            {receipt.extractedFields.map((field) => (
              <ExtractedField
                key={field.key}
                field={getFieldWithEdits(field)}
                onUpdate={(key, value) => handleFieldUpdate(key, value)}
                onUndo={() => handleFieldUndo(field.key)}
                canUndo={editedFields.has(field.key)}
                isSaving={isSaving}
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
    <div className="flex gap-4 h-full">
      {/* Image skeleton */}
      <Card className="flex-1">
        <CardHeader className="pb-2">
          <div className="h-5 w-24 rounded bg-muted animate-pulse" />
        </CardHeader>
        <CardContent className="p-2">
          <div className="h-full w-full rounded-lg bg-muted animate-pulse min-h-[400px]" />
        </CardContent>
      </Card>

      {/* Fields skeleton */}
      <Card className="w-96">
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
