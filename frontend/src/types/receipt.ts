/**
 * Receipt Intelligence Types (T036)
 *
 * UI-specific types for receipt processing, extraction, and editing.
 * These types extend API types with client-side state management.
 */

import type { ReceiptDetail, ReceiptStatus } from './api';

/**
 * Field keys for extracted receipt data.
 * Maps to fields extracted by AI/OCR processing.
 */
export type ExtractedFieldKey =
  | 'merchant'
  | 'amount'
  | 'date'
  | 'category'
  | 'taxAmount'
  | 'tip'
  | 'subtotal'
  | 'paymentMethod'
  | 'currency';

/**
 * Represents a single extracted field with confidence and edit tracking.
 * Used for inline editing with undo support.
 */
export interface ExtractedField {
  /** Field identifier */
  key: ExtractedFieldKey;
  /** Current value (string for display, can be parsed as needed) */
  value: string | number | null;
  /** AI confidence score (0-1) */
  confidence: number;
  /** Bounding box coordinates on the receipt image */
  boundingBox?: {
    x: number;
    y: number;
    width: number;
    height: number;
  };
  /** Whether this field has been manually edited */
  isEdited: boolean;
  /** Original value before any edits (for undo) */
  originalValue?: string | number | null;
}

/**
 * Receipt preview for list/grid display.
 * Extends API ReceiptDetail with UI-specific fields.
 */
export interface ReceiptPreview {
  id: string;
  imageUrl: string;
  thumbnailUrl: string | null;
  uploadedAt: string; // ISO date string
  status: ReceiptProcessingStatus;
  processingProgress?: number; // 0-100
  extractedFields: ExtractedField[];
  matchedTransactionId?: string | null;
  /** Original filename for display */
  filename?: string;
  /** AI-suggested category */
  suggestedCategory?: string;
  /** Overall extraction confidence */
  overallConfidence?: number;
  /** Concurrency token for optimistic locking (Feature 024) */
  rowVersion: number;
}

/**
 * Processing status for receipts in the UI.
 * Maps to API ReceiptStatus but optimized for UI state machine.
 */
export type ReceiptProcessingStatus =
  | 'uploading'
  | 'processing'
  | 'complete'
  | 'error'
  | 'review_required';

/**
 * Maps API ReceiptStatus to UI ReceiptProcessingStatus.
 */
export function mapReceiptStatus(apiStatus: ReceiptStatus): ReceiptProcessingStatus {
  switch (apiStatus) {
    case 'Uploaded':
    case 'Processing':
      return 'processing';
    case 'Ready':
    case 'Matched':
      return 'complete';
    case 'ReviewRequired':
    case 'Unmatched':
      return 'review_required';
    case 'Error':
      return 'error';
    default:
      return 'processing';
  }
}

/**
 * Client-side upload state for tracking file uploads.
 * Used in batch upload queue management.
 */
export interface ReceiptUploadState {
  /** Unique ID for this upload instance */
  uploadId: string;
  /** The file being uploaded */
  file: File;
  /** Object URL for local preview */
  preview: string;
  /** Upload/processing progress (0-100) */
  progress: number;
  /** Current status */
  status: 'pending' | 'uploading' | 'processing' | 'complete' | 'error';
  /** Error message if status is 'error' */
  error?: string;
  /** Receipt ID assigned after successful upload */
  receiptId?: string;
  /** Timestamp when upload started */
  startedAt?: string;
}

/**
 * Converts API ReceiptDetail to UI ReceiptPreview.
 */
export function toReceiptPreview(detail: ReceiptDetail): ReceiptPreview {
  const extractedFields: ExtractedField[] = [];

  // Convert API fields to ExtractedField format
  if (detail.vendor !== null) {
    extractedFields.push({
      key: 'merchant',
      value: detail.vendor,
      confidence: detail.confidenceScores?.vendor ?? 0.9,
      isEdited: false,
    });
  }

  if (detail.amount !== null) {
    extractedFields.push({
      key: 'amount',
      value: detail.amount,
      confidence: detail.confidenceScores?.amount ?? 0.9,
      isEdited: false,
    });
  }

  if (detail.date !== null) {
    extractedFields.push({
      key: 'date',
      value: detail.date,
      confidence: detail.confidenceScores?.date ?? 0.9,
      isEdited: false,
    });
  }

  if (detail.tax !== null) {
    extractedFields.push({
      key: 'taxAmount',
      value: detail.tax,
      confidence: detail.confidenceScores?.tax ?? 0.9,
      isEdited: false,
    });
  }

  // Calculate overall confidence as average
  const confidences = Object.values(detail.confidenceScores || {});
  const overallConfidence =
    confidences.length > 0
      ? confidences.reduce((a, b) => a + b, 0) / confidences.length
      : undefined;

  return {
    id: detail.id,
    imageUrl: detail.blobUrl,
    thumbnailUrl: detail.thumbnailUrl,
    uploadedAt: detail.createdAt,
    status: mapReceiptStatus(detail.status),
    extractedFields,
    matchedTransactionId: null, // Would come from matching data
    filename: detail.originalFilename,
    overallConfidence,
    rowVersion: detail.rowVersion,
  };
}

/**
 * Field edit history entry for undo support.
 */
export interface FieldEditHistoryEntry {
  /** Receipt ID */
  receiptId: string;
  /** Field that was edited */
  field: ExtractedFieldKey;
  /** Previous value */
  previousValue: string | number | null;
  /** New value */
  newValue: string | number | null;
  /** Timestamp of edit */
  timestamp: string;
}

/**
 * Upload configuration for receipt dropzone.
 */
export interface ReceiptUploadConfig {
  /** Maximum number of files per upload */
  maxFiles: number;
  /** Maximum file size in bytes (20MB per spec) */
  maxSize: number;
  /** Accepted MIME types */
  acceptedTypes: string[];
}

/**
 * Default upload configuration matching spec requirements.
 */
export const DEFAULT_UPLOAD_CONFIG: ReceiptUploadConfig = {
  maxFiles: 10,
  maxSize: 20 * 1024 * 1024, // 20MB
  acceptedTypes: [
    'image/jpeg',
    'image/png',
    'image/heic',
    'image/heif',
    'application/pdf',
  ],
};

/**
 * Field validation result for extracted fields.
 */
export interface FieldValidationResult {
  isValid: boolean;
  error?: string;
  suggestions?: string[];
}

/**
 * Validates an extracted field value based on its type.
 */
export function validateField(
  key: ExtractedFieldKey,
  value: string | number | null
): FieldValidationResult {
  if (value === null || value === '') {
    return { isValid: true }; // Null values are allowed
  }

  switch (key) {
    case 'amount':
    case 'taxAmount':
    case 'tip':
    case 'subtotal': {
      const numValue = typeof value === 'number' ? value : parseFloat(value);
      if (isNaN(numValue)) {
        return { isValid: false, error: 'Must be a valid number' };
      }
      if (numValue < 0) {
        return { isValid: false, error: 'Cannot be negative' };
      }
      return { isValid: true };
    }

    case 'date': {
      const strValue = String(value);
      const date = new Date(strValue);
      if (isNaN(date.getTime())) {
        return { isValid: false, error: 'Must be a valid date' };
      }
      if (date > new Date()) {
        return { isValid: false, error: 'Date cannot be in the future' };
      }
      return { isValid: true };
    }

    case 'merchant':
    case 'category':
    case 'paymentMethod':
    case 'currency': {
      const strValue = String(value).trim();
      if (strValue.length === 0) {
        return { isValid: false, error: 'Cannot be empty' };
      }
      if (strValue.length > 200) {
        return { isValid: false, error: 'Too long (max 200 characters)' };
      }
      return { isValid: true };
    }

    default:
      return { isValid: true };
  }
}

/**
 * Display labels for extracted field keys.
 */
export const FIELD_LABELS: Record<ExtractedFieldKey, string> = {
  merchant: 'Merchant',
  amount: 'Total Amount',
  date: 'Date',
  category: 'Category',
  taxAmount: 'Tax',
  tip: 'Tip',
  subtotal: 'Subtotal',
  paymentMethod: 'Payment Method',
  currency: 'Currency',
};

/**
 * Gets human-readable label for a field key.
 */
export function getFieldLabel(key: ExtractedFieldKey): string {
  return FIELD_LABELS[key] || key;
}

/**
 * Receipt batch operation for bulk actions.
 */
export interface ReceiptBatchOperation {
  receiptIds: string[];
  action: 'delete' | 'categorize' | 'match' | 'export';
  params?: Record<string, unknown>;
}

/**
 * Receipt filter options for list views.
 */
export interface ReceiptFilters {
  status?: ReceiptProcessingStatus[];
  dateRange?: {
    start: string | null;
    end: string | null;
  };
  amountRange?: {
    min: number | null;
    max: number | null;
  };
  hasMatch?: boolean;
  search?: string;
}

/**
 * Sort options for receipt lists.
 */
export interface ReceiptSortConfig {
  field: 'uploadedAt' | 'amount' | 'merchant' | 'date';
  direction: 'asc' | 'desc';
}

/**
 * Training feedback metadata for a single field correction.
 * Captures original AI-extracted value for model improvement.
 * Feature 024: Extraction Editor Training
 */
export interface CorrectionMetadata {
  /** Name of the corrected field (vendor, amount, date, tax, currency, line_item) */
  fieldName: 'vendor' | 'amount' | 'date' | 'tax' | 'currency' | 'line_item';
  /** Original AI-extracted value before user correction */
  originalValue: string;
  /** For line_item corrections, the index of the item */
  lineItemIndex?: number;
  /** For line_item corrections, which field was corrected */
  lineItemField?: 'description' | 'quantity' | 'unitPrice' | 'totalPrice';
}

/**
 * Request DTO for updating receipt data with training feedback.
 * Feature 024: Extraction Editor Training
 */
export interface ReceiptUpdateRequest {
  /** Vendor/merchant name */
  vendor?: string | null;
  /** Transaction date (ISO format) */
  date?: string | null;
  /** Total amount */
  amount?: number | null;
  /** Tax amount */
  tax?: number | null;
  /** Currency code (e.g., USD, EUR) */
  currency?: string | null;
  /** Line items on the receipt */
  lineItems?: Array<{
    description: string;
    quantity?: number | null;
    unitPrice?: number | null;
    totalPrice?: number | null;
    confidence?: number | null;
  }>;
  /** Concurrency token for optimistic locking */
  rowVersion?: number;
  /** Training feedback for corrected fields */
  corrections?: CorrectionMetadata[];
}

/**
 * Pending correction state for tracking unsaved edits.
 * Used in ReceiptIntelligencePanel for batch submission.
 */
export interface PendingCorrection {
  /** Field being corrected */
  fieldName: CorrectionMetadata['fieldName'];
  /** Original value (for training feedback) */
  originalValue: string;
  /** New value entered by user */
  newValue: string | number | null;
  /** For line item fields */
  lineItemIndex?: number;
  lineItemField?: CorrectionMetadata['lineItemField'];
}
