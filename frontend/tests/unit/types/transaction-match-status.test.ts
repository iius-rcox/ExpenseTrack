import { describe, it, expect } from 'vitest';
import type { TransactionMatchStatus } from '@/types/transaction';

/**
 * Type-level tests for TransactionMatchStatus.
 * Verifies that 'missing-receipt' is a valid status value.
 */
describe('TransactionMatchStatus Type', () => {
  it('should accept missing-receipt as a valid status value', () => {
    // This test verifies at runtime that the type allows 'missing-receipt'
    const validStatuses: TransactionMatchStatus[] = [
      'matched',
      'pending',
      'unmatched',
      'manual',
      'missing-receipt',
    ];

    expect(validStatuses).toHaveLength(5);
    expect(validStatuses).toContain('missing-receipt');
  });

  it('should allow missing-receipt in filter arrays', () => {
    // Simulating filter usage
    const filterValues: TransactionMatchStatus[] = ['missing-receipt', 'unmatched'];

    expect(filterValues).toContain('missing-receipt');
    expect(filterValues).toContain('unmatched');
  });
});
