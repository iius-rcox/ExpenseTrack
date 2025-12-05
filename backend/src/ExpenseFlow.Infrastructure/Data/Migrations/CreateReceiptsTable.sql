-- Migration: CreateReceiptsTable
-- Sprint 3: Receipt Upload Pipeline
-- Run this script against the PostgreSQL database if dotnet ef is unavailable

-- Create the receipts table
CREATE TABLE IF NOT EXISTS receipts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    blob_url VARCHAR(500) NOT NULL,
    thumbnail_url VARCHAR(500),
    original_filename VARCHAR(255) NOT NULL,
    content_type VARCHAR(100) NOT NULL,
    file_size BIGINT NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'Uploaded',
    vendor_extracted VARCHAR(255),
    date_extracted TIMESTAMP WITH TIME ZONE,
    amount_extracted DECIMAL(12,2),
    tax_extracted DECIMAL(12,2),
    currency VARCHAR(3) DEFAULT 'USD',
    line_items JSONB,
    confidence_scores JSONB,
    error_message TEXT,
    retry_count INTEGER NOT NULL DEFAULT 0,
    page_count INTEGER NOT NULL DEFAULT 1,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    processed_at TIMESTAMP WITH TIME ZONE,

    CONSTRAINT fk_receipts_users
        FOREIGN KEY (user_id)
        REFERENCES users(id)
        ON DELETE CASCADE
);

-- Create indexes for efficient querying
CREATE INDEX IF NOT EXISTS ix_receipts_user_id ON receipts(user_id);
CREATE INDEX IF NOT EXISTS ix_receipts_status ON receipts(status);
CREATE INDEX IF NOT EXISTS ix_receipts_user_status ON receipts(user_id, status);
CREATE INDEX IF NOT EXISTS ix_receipts_created_at ON receipts(created_at DESC);

-- Partial index for unmatched receipts (optimization for Phase 5)
CREATE INDEX IF NOT EXISTS ix_receipts_unmatched
    ON receipts(user_id, created_at DESC)
    WHERE status = 'Unmatched';

-- Add to EF migrations history (so EF knows this migration was applied)
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251205120000_CreateReceiptsTable', '8.0.2')
ON CONFLICT DO NOTHING;

-- Verify table creation
SELECT
    table_name,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_name = 'receipts'
ORDER BY ordinal_position;
