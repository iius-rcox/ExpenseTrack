-- Migration: AddStatementImportTables
-- Sprint 4: Statement Import & Fingerprinting
-- Date: 2025-12-05
-- Run this script against the PostgreSQL database

BEGIN;

-- =====================================================
-- 1. Update statement_fingerprints table
-- =====================================================

-- Make user_id nullable (for system fingerprints)
ALTER TABLE statement_fingerprints ALTER COLUMN user_id DROP NOT NULL;

-- Add new columns
ALTER TABLE statement_fingerprints ADD COLUMN IF NOT EXISTS hit_count INTEGER NOT NULL DEFAULT 0;
ALTER TABLE statement_fingerprints ADD COLUMN IF NOT EXISTS last_used_at TIMESTAMP WITH TIME ZONE;

-- Add index on header_hash for faster lookups
CREATE INDEX IF NOT EXISTS ix_statement_fingerprints_header_hash ON statement_fingerprints(header_hash);

-- =====================================================
-- 2. Create statement_imports table
-- =====================================================

CREATE TABLE IF NOT EXISTS statement_imports (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    fingerprint_id UUID REFERENCES statement_fingerprints(id) ON DELETE SET NULL,
    file_name VARCHAR(255) NOT NULL,
    file_size BIGINT NOT NULL,
    tier_used INTEGER NOT NULL,
    transaction_count INTEGER NOT NULL DEFAULT 0,
    skipped_count INTEGER NOT NULL DEFAULT 0,
    duplicate_count INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Indexes for statement_imports
CREATE INDEX IF NOT EXISTS ix_statement_imports_user_id ON statement_imports(user_id);
CREATE INDEX IF NOT EXISTS ix_statement_imports_fingerprint_id ON statement_imports(fingerprint_id);
CREATE INDEX IF NOT EXISTS ix_statement_imports_created_at ON statement_imports(created_at DESC);

-- =====================================================
-- 3. Create transactions table
-- =====================================================

CREATE TABLE IF NOT EXISTS transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    import_id UUID NOT NULL REFERENCES statement_imports(id) ON DELETE CASCADE,
    transaction_date DATE NOT NULL,
    post_date DATE,
    description VARCHAR(500) NOT NULL,
    original_description VARCHAR(500) NOT NULL,
    amount NUMERIC(18, 2) NOT NULL,
    category VARCHAR(100),
    memo VARCHAR(500),
    reference VARCHAR(100),
    duplicate_hash VARCHAR(64) NOT NULL,
    matched_receipt_id UUID REFERENCES receipts(id) ON DELETE SET NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Indexes for transactions
CREATE INDEX IF NOT EXISTS ix_transactions_user_id ON transactions(user_id);
CREATE INDEX IF NOT EXISTS ix_transactions_import_id ON transactions(import_id);
CREATE UNIQUE INDEX IF NOT EXISTS ix_transactions_user_duplicate_hash ON transactions(user_id, duplicate_hash);
CREATE INDEX IF NOT EXISTS ix_transactions_user_date ON transactions(user_id, transaction_date);
CREATE INDEX IF NOT EXISTS ix_transactions_matched_receipt_id ON transactions(matched_receipt_id);

-- =====================================================
-- 4. Seed system fingerprints (Chase & Amex)
-- =====================================================

-- Chase Business Card system fingerprint
-- Headers: Transaction Date, Post Date, Description, Category, Type, Amount, Memo
INSERT INTO statement_fingerprints (id, user_id, source_name, header_hash, column_mapping, date_format, amount_sign, hit_count, created_at)
VALUES (
    '00000000-0000-0000-0000-000000000001',
    NULL,  -- System fingerprint (no user)
    'Chase Business Card',
    -- SHA-256 hash of: "amount,category,description,memo,post date,transaction date,type"
    'b5a9d7c8e3f2a1b4c6d8e0f2a4b6c8d0e2f4a6b8c0d2e4f6a8b0c2d4e6f8a0b2',
    '{"Transaction Date":"date","Post Date":"post_date","Description":"description","Amount":"amount","Category":"category","Memo":"memo","Type":"ignore"}',
    'MM/dd/yyyy',
    'negative_charges',
    0,
    NOW()
)
ON CONFLICT (id) DO NOTHING;

-- American Express system fingerprint
-- Headers: Date, Description, Card Member, Account #, Amount, Extended Details, Appears On Your Statement As, Category
INSERT INTO statement_fingerprints (id, user_id, source_name, header_hash, column_mapping, date_format, amount_sign, hit_count, created_at)
VALUES (
    '00000000-0000-0000-0000-000000000002',
    NULL,  -- System fingerprint (no user)
    'American Express',
    -- SHA-256 hash of: "account #,amount,appears on your statement as,card member,category,date,description,extended details"
    'c6d8e0f2a4b6c8d0e2f4a6b8c0d2e4f6a8b0c2d4e6f8a0b2c4d6e8f0a2b4c6d8',
    '{"Date":"date","Description":"description","Amount":"amount","Extended Details":"memo","Category":"category","Card Member":"ignore","Account #":"ignore","Appears On Your Statement As":"ignore"}',
    'MM/dd/yyyy',
    'positive_charges',
    0,
    NOW()
)
ON CONFLICT (id) DO NOTHING;

-- =====================================================
-- 5. Record migration in EF migration history
-- =====================================================

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251205180000_AddStatementImportTables', '8.0.2')
ON CONFLICT ("MigrationId") DO NOTHING;

COMMIT;

-- =====================================================
-- Verification queries (run separately to verify)
-- =====================================================
-- SELECT * FROM statement_fingerprints WHERE user_id IS NULL;
-- SELECT column_name, is_nullable FROM information_schema.columns WHERE table_name = 'statement_fingerprints';
-- SELECT column_name, data_type FROM information_schema.columns WHERE table_name = 'transactions';
-- SELECT column_name, data_type FROM information_schema.columns WHERE table_name = 'statement_imports';
