/**
 * Prediction Feedback Hook (T065)
 *
 * Convenience hook that combines confirm/reject mutations
 * with shared state for prediction feedback interactions.
 *
 * Features:
 * - Combined processing state for disabling UI
 * - Individual mutation states for loading indicators
 * - Pre-bound handlers for easy component integration
 * - Type-safe with proper inference
 *
 * @example
 * ```tsx
 * function PredictionRow({ prediction }) {
 *   const feedback = usePredictionFeedback();
 *
 *   return (
 *     <PredictionFeedback
 *       predictionId={prediction.id}
 *       onConfirm={feedback.confirm}
 *       onReject={feedback.reject}
 *       isConfirming={feedback.isConfirmingId === prediction.id}
 *       isRejecting={feedback.isRejectingId === prediction.id}
 *       disabled={feedback.isProcessing}
 *     />
 *   );
 * }
 * ```
 */

import { useState, useCallback, useMemo } from 'react';
import { useConfirmPrediction, useRejectPrediction } from './use-predictions';

export interface UsePredictionFeedbackOptions {
  /** Callback after successful confirm */
  onConfirmSuccess?: (predictionId: string) => void;
  /** Callback after successful reject */
  onRejectSuccess?: (predictionId: string, patternSuppressed: boolean) => void;
  /** Callback on any error */
  onError?: (error: Error, action: 'confirm' | 'reject') => void;
}

export interface UsePredictionFeedbackReturn {
  /** Confirm a prediction */
  confirm: (predictionId: string, glCodeOverride?: string, departmentOverride?: string) => void;
  /** Reject a prediction */
  reject: (predictionId: string) => void;
  /** Whether any action is processing (for disabling UI) */
  isProcessing: boolean;
  /** Whether a confirm is in progress */
  isConfirming: boolean;
  /** ID of prediction currently being confirmed (null if none) */
  isConfirmingId: string | null;
  /** Whether a reject is in progress */
  isRejecting: boolean;
  /** ID of prediction currently being rejected (null if none) */
  isRejectingId: string | null;
  /** Most recent error if any */
  error: Error | null;
  /** Reset error state */
  clearError: () => void;
}

/**
 * Hook for prediction feedback actions with state management.
 *
 * Provides confirm/reject handlers with tracking of which
 * specific prediction is being acted on, enabling proper
 * loading states in list UIs.
 */
export function usePredictionFeedback(
  options: UsePredictionFeedbackOptions = {}
): UsePredictionFeedbackReturn {
  const { onConfirmSuccess, onRejectSuccess, onError } = options;

  // Track which prediction ID is being processed
  const [confirmingId, setConfirmingId] = useState<string | null>(null);
  const [rejectingId, setRejectingId] = useState<string | null>(null);
  const [error, setError] = useState<Error | null>(null);

  const confirmMutation = useConfirmPrediction();
  const rejectMutation = useRejectPrediction();

  const confirm = useCallback(
    (predictionId: string, glCodeOverride?: string, departmentOverride?: string) => {
      setConfirmingId(predictionId);
      setError(null);

      confirmMutation.mutate(
        { predictionId, glCodeOverride, departmentOverride },
        {
          onSuccess: () => {
            setConfirmingId(null);
            onConfirmSuccess?.(predictionId);
          },
          onError: (err) => {
            setConfirmingId(null);
            const error = err instanceof Error ? err : new Error(String(err));
            setError(error);
            onError?.(error, 'confirm');
          },
        }
      );
    },
    [confirmMutation, onConfirmSuccess, onError]
  );

  const reject = useCallback(
    (predictionId: string) => {
      setRejectingId(predictionId);
      setError(null);

      rejectMutation.mutate(
        { predictionId },
        {
          onSuccess: (response) => {
            setRejectingId(null);
            onRejectSuccess?.(predictionId, response.patternSuppressed ?? false);
          },
          onError: (err) => {
            setRejectingId(null);
            const error = err instanceof Error ? err : new Error(String(err));
            setError(error);
            onError?.(error, 'reject');
          },
        }
      );
    },
    [rejectMutation, onRejectSuccess, onError]
  );

  const clearError = useCallback(() => {
    setError(null);
  }, []);

  // Memoize the return value to prevent unnecessary re-renders
  return useMemo(
    () => ({
      confirm,
      reject,
      isProcessing: confirmMutation.isPending || rejectMutation.isPending,
      isConfirming: confirmMutation.isPending,
      isConfirmingId: confirmingId,
      isRejecting: rejectMutation.isPending,
      isRejectingId: rejectingId,
      error,
      clearError,
    }),
    [
      confirm,
      reject,
      confirmMutation.isPending,
      rejectMutation.isPending,
      confirmingId,
      rejectingId,
      error,
      clearError,
    ]
  );
}

export default usePredictionFeedback;
