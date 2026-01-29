-- Fix matched transactions that still have Pending predictions
-- These are historical matches created before the auto-confirm logic was added
-- Run this in the expenseflow_staging database
--
-- match_status values: 0=Unmatched, 1=Proposed, 2=Matched
-- prediction.status values: 'Pending', 'Confirmed', 'Rejected'

-- First, show what we're about to fix (individual transactions with matched receipts)
SELECT
    t.id AS transaction_id,
    t.description,
    t.match_status,
    tp.id AS prediction_id,
    tp.status AS prediction_status,
    tp.confidence_score
FROM transactions t
INNER JOIN transaction_predictions tp ON tp.transaction_id = t.id
WHERE t.match_status = 2  -- Matched
  AND tp.status = 'Pending'
ORDER BY t.transaction_date DESC;

-- Count how many need fixing
SELECT
    'Transactions needing fix' AS description,
    COUNT(*) AS count
FROM transactions t
INNER JOIN transaction_predictions tp ON tp.transaction_id = t.id
WHERE t.match_status = 2
  AND tp.status = 'Pending';

-- Update predictions to Confirmed for matched transactions
UPDATE transaction_predictions tp
SET status = 'Confirmed'
FROM transactions t
WHERE tp.transaction_id = t.id
  AND t.match_status = 2
  AND tp.status = 'Pending';

-- Verify the fix
SELECT
    'Fixed predictions' AS description,
    COUNT(*) AS count
FROM transactions t
INNER JOIN transaction_predictions tp ON tp.transaction_id = t.id
WHERE t.match_status = 2
  AND tp.status = 'Confirmed';

-- Also fix any transactions in groups that have matched receipts
-- These should also be marked as Business
SELECT
    t.id AS transaction_id,
    t.description,
    tg.name AS group_name,
    tg.match_status AS group_match_status,
    tp.status AS prediction_status
FROM transactions t
INNER JOIN transaction_group_memberships tgm ON tgm.transaction_id = t.id
INNER JOIN transaction_groups tg ON tg.id = tgm.transaction_group_id
LEFT JOIN transaction_predictions tp ON tp.transaction_id = t.id
WHERE tg.match_status = 2  -- Matched
  AND (tp.status = 'Pending' OR tp.id IS NULL)
ORDER BY tg.name, t.transaction_date DESC;

-- Update predictions for transactions in matched groups
UPDATE transaction_predictions tp
SET status = 'Confirmed'
FROM transactions t
INNER JOIN transaction_group_memberships tgm ON tgm.transaction_id = t.id
INNER JOIN transaction_groups tg ON tg.id = tgm.transaction_group_id
WHERE tp.transaction_id = t.id
  AND tg.match_status = 2
  AND tp.status = 'Pending';
