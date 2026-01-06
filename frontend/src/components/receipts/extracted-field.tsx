'use client';

/**
 * ExtractedField Component (T041)
 *
 * Displays an AI-extracted receipt field with:
 * - Confidence indicator showing extraction accuracy
 * - Inline editing capability
 * - Edit history tracking for undo support
 * - Field validation feedback
 */

import { useState, useRef, useEffect, useCallback } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Check, X, Pencil, RotateCcw, AlertCircle } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { ConfidenceIndicator } from '@/components/design-system/confidence-indicator';
import type {
  ExtractedField as ExtractedFieldType,
  ExtractedFieldKey,
} from '@/types/receipt';
import { getFieldLabel, validateField } from '@/types/receipt';

interface ExtractedFieldProps {
  /** The extracted field data */
  field: ExtractedFieldType;
  /** Callback when field value is updated */
  onUpdate?: (field: ExtractedFieldKey, value: string | number | null) => void;
  /** Callback when undo is requested */
  onUndo?: () => void;
  /** Whether undo is available */
  canUndo?: boolean;
  /** Whether the field is currently being saved */
  isSaving?: boolean;
  /** Whether to show the confidence indicator */
  showConfidence?: boolean;
  /** Whether the field is read-only */
  readOnly?: boolean;
  /** Custom label override */
  label?: string;
  /** Size variant */
  size?: 'sm' | 'md' | 'lg';
}

export function ExtractedField({
  field,
  onUpdate,
  onUndo,
  canUndo = false,
  isSaving = false,
  showConfidence = true,
  readOnly = false,
  label,
  size = 'md',
}: ExtractedFieldProps) {
  const [isEditing, setIsEditing] = useState(false);
  const [editValue, setEditValue] = useState<string>('');
  const [validationError, setValidationError] = useState<string | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  // Format display value based on field type
  const formatDisplayValue = useCallback(
    (value: string | number | null): string => {
      if (value === null || value === undefined) return '—';

      switch (field.key) {
        case 'amount':
        case 'taxAmount':
        case 'tip':
        case 'subtotal':
          return typeof value === 'number'
            ? new Intl.NumberFormat('en-US', {
                style: 'currency',
                currency: 'USD',
              }).format(value)
            : `$${value}`;

        case 'date':
          try {
            return new Date(String(value)).toLocaleDateString();
          } catch {
            return String(value);
          }

        default:
          return String(value);
      }
    },
    [field.key]
  );

  // Start editing
  const handleStartEdit = () => {
    if (readOnly) return;

    // Set initial edit value
    const value = field.value;
    if (value === null || value === undefined) {
      setEditValue('');
    } else if (field.key === 'date' && typeof value === 'string') {
      // Convert to YYYY-MM-DD for date input
      try {
        const date = new Date(value);
        setEditValue(date.toISOString().split('T')[0]);
      } catch {
        setEditValue(String(value));
      }
    } else {
      setEditValue(String(value));
    }

    setValidationError(null);
    setIsEditing(true);
  };

  // Focus input when editing starts
  useEffect(() => {
    if (isEditing && inputRef.current) {
      inputRef.current.focus();
      inputRef.current.select();
    }
  }, [isEditing]);

  // Save edit
  const handleSave = () => {
    // Parse and validate value
    let parsedValue: string | number | null = editValue.trim() || null;

    if (
      parsedValue !== null &&
      ['amount', 'taxAmount', 'tip', 'subtotal'].includes(field.key)
    ) {
      // Remove currency symbols and parse as number
      const numStr = String(parsedValue).replace(/[$,]/g, '');
      parsedValue = parseFloat(numStr);
      if (isNaN(parsedValue)) {
        setValidationError('Invalid number');
        return;
      }
    }

    // Validate
    const validation = validateField(field.key, parsedValue);
    if (!validation.isValid) {
      setValidationError(validation.error || 'Invalid value');
      return;
    }

    onUpdate?.(field.key, parsedValue);
    setIsEditing(false);
    setValidationError(null);
  };

  // Cancel edit
  const handleCancel = () => {
    setIsEditing(false);
    setValidationError(null);
    setEditValue('');
  };

  // Handle keyboard shortcuts
  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      handleSave();
    } else if (e.key === 'Escape') {
      e.preventDefault();
      handleCancel();
    }
  };

  // Size-based styles
  const sizeStyles = {
    sm: {
      container: 'py-1.5 px-2',
      label: 'text-xs',
      value: 'text-sm',
      input: 'h-7 text-sm',
    },
    md: {
      container: 'py-2 px-3',
      label: 'text-xs',
      value: 'text-base',
      input: 'h-8',
    },
    lg: {
      container: 'py-3 px-4',
      label: 'text-sm',
      value: 'text-lg',
      input: 'h-10 text-lg',
    },
  };

  const styles = sizeStyles[size];
  const displayLabel = label || getFieldLabel(field.key);

  return (
    <div
      className={cn(
        'group relative rounded-lg border transition-colors',
        field.isEdited && 'border-amber-500/50 bg-amber-50/50 dark:bg-amber-950/20',
        !field.isEdited && 'border-border bg-card',
        isEditing && 'ring-2 ring-primary',
        styles.container
      )}
    >
      {/* Label row */}
      <div className="flex items-center justify-between mb-1">
        <span className={cn('font-medium text-muted-foreground', styles.label)}>
          {displayLabel}
          {field.isEdited && (
            <span className="ml-1.5 text-amber-600 dark:text-amber-400">(edited)</span>
          )}
        </span>

        {showConfidence && !isEditing && (
          <ConfidenceIndicator
            score={field.confidence}
            size={size === 'lg' ? 'md' : 'sm'}
            showLabel={size !== 'sm'}
          />
        )}
      </div>

      {/* Value/Edit row */}
      <AnimatePresence mode="wait">
        {isEditing ? (
          <motion.div
            key="edit"
            initial={{ opacity: 0, y: -4 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: 4 }}
            className="space-y-2"
          >
            <div className="flex items-center gap-2">
              <Input
                ref={inputRef}
                type={
                  ['amount', 'taxAmount', 'tip', 'subtotal'].includes(field.key)
                    ? 'number'
                    : field.key === 'date'
                      ? 'date'
                      : 'text'
                }
                step={
                  ['amount', 'taxAmount', 'tip', 'subtotal'].includes(field.key)
                    ? '0.01'
                    : undefined
                }
                value={editValue}
                onChange={(e) => {
                  setEditValue(e.target.value);
                  setValidationError(null);
                }}
                onKeyDown={handleKeyDown}
                className={cn(styles.input, validationError && 'border-destructive')}
                disabled={isSaving}
              />

              <Button
                size="icon"
                variant="ghost"
                className="h-11 w-11 md:h-8 md:w-8 text-green-600 hover:text-green-700 hover:bg-green-100"
                onClick={handleSave}
                disabled={isSaving}
              >
                <Check className="h-5 w-5 md:h-4 md:w-4" />
              </Button>

              <Button
                size="icon"
                variant="ghost"
                className="h-11 w-11 md:h-8 md:w-8 text-muted-foreground hover:text-foreground"
                onClick={handleCancel}
                disabled={isSaving}
              >
                <X className="h-5 w-5 md:h-4 md:w-4" />
              </Button>
            </div>

            {validationError && (
              <div className="flex items-center gap-1 text-xs text-destructive">
                <AlertCircle className="h-3 w-3" />
                {validationError}
              </div>
            )}
          </motion.div>
        ) : (
          <motion.div
            key="display"
            initial={{ opacity: 0, y: 4 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -4 }}
            className="flex items-center justify-between gap-2"
          >
            <span className={cn('font-semibold', styles.value)}>
              {formatDisplayValue(field.value)}
            </span>

            {!readOnly && (
              <div className="flex items-center gap-1 opacity-50 hover:opacity-100 focus-within:opacity-100 transition-opacity">
                {canUndo && field.isEdited && (
                  <Button
                    size="icon"
                    variant="ghost"
                    className="h-11 w-11 md:h-7 md:w-7"
                    onClick={onUndo}
                    title="Undo edit"
                  >
                    <RotateCcw className="h-4 w-4 md:h-3.5 md:w-3.5" />
                  </Button>
                )}

                <Button
                  size="icon"
                  variant="ghost"
                  className="h-11 w-11 md:h-7 md:w-7"
                  onClick={handleStartEdit}
                  title="Edit field"
                >
                  <Pencil className="h-4 w-4 md:h-3.5 md:w-3.5" />
                </Button>
              </div>
            )}
          </motion.div>
        )}
      </AnimatePresence>

      {/* Bounding box indicator (for image highlighting) */}
      {field.boundingBox && !isEditing && (
        <div className="absolute -right-1 -top-1">
          <div className="h-2 w-2 rounded-full bg-primary animate-pulse" />
        </div>
      )}
    </div>
  );
}

/**
 * Skeleton loader for ExtractedField
 */
export function ExtractedFieldSkeleton({ size = 'md' }: { size?: 'sm' | 'md' | 'lg' }) {
  const sizeStyles = {
    sm: 'h-12',
    md: 'h-14',
    lg: 'h-16',
  };

  return (
    <div
      className={cn(
        'rounded-lg border border-border bg-card animate-pulse',
        sizeStyles[size]
      )}
    >
      <div className="p-3 space-y-2">
        <div className="h-3 w-16 rounded bg-muted" />
        <div className="h-4 w-24 rounded bg-muted" />
      </div>
    </div>
  );
}
