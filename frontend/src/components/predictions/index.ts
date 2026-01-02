/**
 * Predictions Components (Feature 023)
 *
 * Components for the expense prediction feature:
 * - ExpenseBadge: Visual indicator on transaction rows with inline actions
 * - PredictionFeedback: Standalone confirm/reject thumb buttons
 * - AutoSuggestedBadge: Indicator for auto-suggested expense lines
 * - AutoSuggestedSummary: Summary of auto-suggested vs manual expenses
 * - (Future) PredictionWorkspace: Management dashboard
 * - (Future) PatternList: Pattern management interface
 */

export {
  ExpenseBadge,
  ExpenseBadgeSkeleton,
  ExpenseBadgeInline,
  type ExpenseBadgeProps,
} from './expense-badge';

export {
  PredictionFeedback,
  PredictionFeedbackSkeleton,
  type PredictionFeedbackProps,
} from './prediction-feedback';

export {
  AutoSuggestedBadge,
  AutoSuggestedBadgeSkeleton,
  AutoSuggestedSummary,
} from './auto-suggested-badge';
