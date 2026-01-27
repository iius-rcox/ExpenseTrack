-- Migration: Add Recurring Allowances Feature
-- Date: 2026-01-27
-- Description: Creates recurring_allowances table and adds AllowanceId FK to ExpenseLines
-- IMPORTANT: Apply this migration BEFORE deploying the code

-- 1. Create the recurring_allowances table
CREATE TABLE IF NOT EXISTS recurring_allowances (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL,
    vendor_name varchar(100) NOT NULL,
    amount numeric(18,2) NOT NULL,
    frequency integer NOT NULL DEFAULT 1, -- 0=Weekly, 1=Monthly, 2=Quarterly
    gl_code varchar(20),
    gl_name varchar(100),
    department_code varchar(20),
    description varchar(500),
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamp with time zone NOT NULL DEFAULT NOW(),
    updated_at timestamp with time zone,
    CONSTRAINT "PK_recurring_allowances" PRIMARY KEY (id),
    CONSTRAINT "FK_recurring_allowances_users" FOREIGN KEY (user_id)
        REFERENCES "Users" (id) ON DELETE CASCADE
);

-- 2. Create indexes for efficient queries
CREATE INDEX IF NOT EXISTS ix_recurring_allowances_user_id
    ON recurring_allowances (user_id);

CREATE INDEX IF NOT EXISTS ix_recurring_allowances_user_active
    ON recurring_allowances (user_id, is_active);

-- 3. Add AllowanceId column to ExpenseLines
ALTER TABLE "ExpenseLines"
ADD COLUMN IF NOT EXISTS allowance_id uuid;

-- 4. Add foreign key relationship
ALTER TABLE "ExpenseLines"
ADD CONSTRAINT "FK_ExpenseLines_RecurringAllowances"
    FOREIGN KEY (allowance_id) REFERENCES recurring_allowances (id)
    ON DELETE SET NULL;

-- 5. Create index on allowance_id for efficient lookups
CREATE INDEX IF NOT EXISTS ix_expense_lines_allowance
    ON "ExpenseLines" (allowance_id)
    WHERE allowance_id IS NOT NULL;

-- 6. Update check constraint to allow allowance-only lines
-- First drop the existing constraint
ALTER TABLE "ExpenseLines"
DROP CONSTRAINT IF EXISTS "CK_expense_lines_has_source";

-- Then recreate with allowance support
ALTER TABLE "ExpenseLines"
ADD CONSTRAINT "CK_expense_lines_has_source"
    CHECK (receipt_id IS NOT NULL OR transaction_id IS NOT NULL OR allowance_id IS NOT NULL);

-- 7. Record migration in EF history
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260127000000_AddRecurringAllowances', '8.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;

-- Verification query (run after migration to confirm)
-- SELECT
--     (SELECT COUNT(*) FROM recurring_allowances) as allowance_count,
--     (SELECT COUNT(*) FROM information_schema.columns
--      WHERE table_name = 'ExpenseLines' AND column_name = 'allowance_id') as column_exists;
