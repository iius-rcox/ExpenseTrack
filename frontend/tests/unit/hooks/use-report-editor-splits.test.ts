import { describe, it, expect } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useReportEditor } from '@/hooks/use-report-editor';

describe('useReportEditor split allocation GL Code inheritance', () => {
  it('should inherit parent GL code when starting a split', () => {
    const { result } = renderHook(() => useReportEditor('2026-01'));

    // Load a line with a GL code
    act(() => {
      result.current.dispatch({
        type: 'LOAD_PREVIEW',
        lines: [
          {
            id: 'line-1',
            transactionId: 'tx-1',
            amount: 100,
            glCode: 'GL-1234',
            departmentCode: 'DEPT-001',
            vendorName: 'Test Vendor',
            expenseDate: '2026-01-15',
          },
        ],
      });
    });

    // Start a split
    act(() => {
      result.current.dispatch({
        type: 'START_SPLIT',
        id: result.current.state.lines[0].id,
      });
    });

    const line = result.current.state.lines[0];

    // First allocation should inherit parent's GL code
    expect(line.allocations[0].glCode).toBe('GL-1234');

    // Second allocation should ALSO inherit parent's GL code (this is the fix)
    expect(line.allocations[1].glCode).toBe('GL-1234');
  });

  it('should inherit parent GL code when adding a new allocation', () => {
    const { result } = renderHook(() => useReportEditor('2026-01'));

    // Load a line with a GL code
    act(() => {
      result.current.dispatch({
        type: 'LOAD_PREVIEW',
        lines: [
          {
            id: 'line-1',
            transactionId: 'tx-1',
            amount: 100,
            glCode: 'GL-5678',
            departmentCode: 'DEPT-002',
            vendorName: 'Another Vendor',
            expenseDate: '2026-01-20',
          },
        ],
      });
    });

    // Start a split
    act(() => {
      result.current.dispatch({
        type: 'START_SPLIT',
        id: result.current.state.lines[0].id,
      });
    });

    // Add a third allocation
    act(() => {
      result.current.dispatch({
        type: 'ADD_ALLOCATION',
        parentId: result.current.state.lines[0].id,
      });
    });

    const line = result.current.state.lines[0];

    // Should have 3 allocations
    expect(line.allocations).toHaveLength(3);

    // The new (third) allocation should inherit the parent's GL code
    expect(line.allocations[2].glCode).toBe('GL-5678');
  });

  it('should preserve user-entered GL code when updating allocation', () => {
    const { result } = renderHook(() => useReportEditor('2026-01'));

    // Load a line
    act(() => {
      result.current.dispatch({
        type: 'LOAD_PREVIEW',
        lines: [
          {
            id: 'line-1',
            transactionId: 'tx-1',
            amount: 100,
            glCode: 'GL-PARENT',
            departmentCode: 'DEPT-001',
            vendorName: 'Test Vendor',
            expenseDate: '2026-01-15',
          },
        ],
      });
    });

    // Start a split
    act(() => {
      result.current.dispatch({
        type: 'START_SPLIT',
        id: result.current.state.lines[0].id,
      });
    });

    // User changes the GL code on second allocation
    act(() => {
      result.current.dispatch({
        type: 'UPDATE_ALLOCATION',
        parentId: result.current.state.lines[0].id,
        allocationId: result.current.state.lines[0].allocations[1].id,
        field: 'glCode',
        value: 'GL-CUSTOM',
      });
    });

    const line = result.current.state.lines[0];

    // First allocation should still have parent GL code
    expect(line.allocations[0].glCode).toBe('GL-PARENT');

    // Second allocation should have user's custom GL code
    expect(line.allocations[1].glCode).toBe('GL-CUSTOM');
  });

  it('should work with empty parent GL code', () => {
    const { result } = renderHook(() => useReportEditor('2026-01'));

    // Load a line WITHOUT a GL code
    act(() => {
      result.current.dispatch({
        type: 'LOAD_PREVIEW',
        lines: [
          {
            id: 'line-1',
            transactionId: 'tx-1',
            amount: 100,
            glCode: '', // Empty GL code
            departmentCode: 'DEPT-001',
            vendorName: 'Test Vendor',
            expenseDate: '2026-01-15',
          },
        ],
      });
    });

    // Start a split
    act(() => {
      result.current.dispatch({
        type: 'START_SPLIT',
        id: result.current.state.lines[0].id,
      });
    });

    const line = result.current.state.lines[0];

    // Allocations should inherit empty GL code (no errors)
    expect(line.allocations[0].glCode).toBe('');
    expect(line.allocations[1].glCode).toBe('');
  });
});
