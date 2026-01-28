-- Backfill expense patterns from historical transaction classifications
-- This creates/updates patterns based on Confirmed (Business) and Rejected (Personal) predictions
-- Run this in the expenseflow_staging database

-- First, let's see what we're working with
SELECT
    tp.status,
    COUNT(*) as count
FROM transaction_predictions tp
WHERE tp.status IN ('Confirmed', 'Rejected')
GROUP BY tp.status;

-- Create temporary table with aggregated classification data
CREATE TEMP TABLE pattern_backfill AS
WITH normalized_classifications AS (
    SELECT
        tp.user_id,
        tp.status,
        t.id AS transaction_id,
        t.amount,
        t.transaction_date,
        t.original_description,
        t.description,
        -- Normalize vendor: use vendor_alias canonical_name if exists, otherwise uppercase+trim
        COALESCE(
            va.canonical_name,
            UPPER(TRIM(COALESCE(t.original_description, t.description)))
        ) AS normalized_vendor,
        COALESCE(t.original_description, t.description) AS display_name
    FROM transaction_predictions tp
    INNER JOIN transactions t ON t.id = tp.transaction_id
    LEFT JOIN vendor_aliases va ON (
        -- Match against vendor_aliases patterns
        UPPER(TRIM(COALESCE(t.original_description, t.description))) LIKE UPPER(va.pattern) || '%'
        OR UPPER(TRIM(COALESCE(t.original_description, t.description))) LIKE '%' || UPPER(va.pattern) || '%'
    )
    WHERE tp.status IN ('Confirmed', 'Rejected')
)
SELECT
    user_id,
    normalized_vendor,
    -- Use the most common display name
    (ARRAY_AGG(display_name ORDER BY transaction_date DESC))[1] AS display_name,
    COUNT(*) AS occurrence_count,
    COUNT(*) FILTER (WHERE status = 'Confirmed') AS confirm_count,
    COUNT(*) FILTER (WHERE status = 'Rejected') AS reject_count,
    AVG(amount) AS average_amount,
    MIN(amount) AS min_amount,
    MAX(amount) AS max_amount,
    MAX(transaction_date) AS last_seen_date
FROM normalized_classifications
GROUP BY user_id, normalized_vendor;

-- Show what we're about to insert/update
SELECT
    pb.user_id,
    pb.normalized_vendor,
    pb.display_name,
    pb.confirm_count,
    pb.reject_count,
    pb.occurrence_count,
    CASE
        WHEN pb.confirm_count::decimal / NULLIF(pb.confirm_count + pb.reject_count, 0) >= 0.5 THEN 'Business'
        WHEN pb.reject_count >= 4 AND pb.reject_count::decimal / NULLIF(pb.confirm_count + pb.reject_count, 0) >= 0.75 THEN 'Personal'
        ELSE 'Unknown'
    END AS classification,
    ep.id AS existing_pattern_id
FROM pattern_backfill pb
LEFT JOIN expense_patterns ep ON ep.user_id = pb.user_id AND ep.normalized_vendor = pb.normalized_vendor
ORDER BY pb.occurrence_count DESC;

-- Perform the upsert: insert new patterns or update existing ones
INSERT INTO expense_patterns (
    id,
    user_id,
    normalized_vendor,
    display_name,
    category,
    average_amount,
    min_amount,
    max_amount,
    occurrence_count,
    last_seen_at,
    default_gl_code,
    default_department,
    confirm_count,
    reject_count,
    is_suppressed,
    requires_receipt_match,
    created_at,
    updated_at
)
SELECT
    gen_random_uuid(),
    pb.user_id,
    pb.normalized_vendor,
    pb.display_name,
    NULL, -- category
    pb.average_amount,
    pb.min_amount,
    pb.max_amount,
    pb.occurrence_count,
    pb.last_seen_date::timestamp,
    NULL, -- default_gl_code
    NULL, -- default_department
    pb.confirm_count,
    pb.reject_count,
    false, -- is_suppressed
    false, -- requires_receipt_match
    NOW(),
    NOW()
FROM pattern_backfill pb
ON CONFLICT (user_id, normalized_vendor)
DO UPDATE SET
    confirm_count = expense_patterns.confirm_count + EXCLUDED.confirm_count,
    reject_count = expense_patterns.reject_count + EXCLUDED.reject_count,
    occurrence_count = expense_patterns.occurrence_count + EXCLUDED.occurrence_count,
    average_amount = EXCLUDED.average_amount,
    min_amount = LEAST(expense_patterns.min_amount, EXCLUDED.min_amount),
    max_amount = GREATEST(expense_patterns.max_amount, EXCLUDED.max_amount),
    last_seen_at = GREATEST(expense_patterns.last_seen_at, EXCLUDED.last_seen_at),
    updated_at = NOW();

-- Show summary of what was done
SELECT
    'Patterns Created/Updated' AS action,
    COUNT(*) AS count
FROM pattern_backfill;

-- Show the resulting patterns with their classifications
SELECT
    ep.normalized_vendor,
    ep.display_name,
    ep.confirm_count,
    ep.reject_count,
    ep.occurrence_count,
    CASE
        WHEN ep.confirm_count::decimal / NULLIF(ep.confirm_count + ep.reject_count, 0) >= 0.5 THEN 'Business'
        WHEN ep.reject_count >= 4 AND ep.reject_count::decimal / NULLIF(ep.confirm_count + ep.reject_count, 0) >= 0.75 THEN 'Personal'
        ELSE 'Unknown'
    END AS active_classification
FROM expense_patterns ep
WHERE ep.confirm_count > 0 OR ep.reject_count > 0
ORDER BY ep.occurrence_count DESC;

-- Cleanup
DROP TABLE IF EXISTS pattern_backfill;
