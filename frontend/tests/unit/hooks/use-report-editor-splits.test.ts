import { describe, it, expect } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useReportEditor } from '@/hooks/use-report-editor';
import { parseExcelPaste } from '@/lib/parse-excel-paste';

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

describe('useReportEditor bulk paste allocations (Excel copy/paste)', () => {
  it('should apply bulk pasted allocations from Excel data', () => {
    const { result } = renderHook(() => useReportEditor('2026-01'));

    // Load a line with amount of 475.02 (matching user example total)
    act(() => {
      result.current.dispatch({
        type: 'LOAD_PREVIEW',
        lines: [
          {
            id: 'line-1',
            transactionId: 'tx-1',
            amount: 475.02,
            glCode: 'GL-1234',
            departmentCode: 'DEPT-001',
            vendorName: 'Test Vendor',
            expenseDate: '2026-01-15',
          },
        ],
      });
    });

    // Parse Excel data (user's example format: Dept, Amount)
    const excelData = `07\t348.34
11\t31.67
22\t31.67
43\t31.67
44\t31.67`;

    const parsed = parseExcelPaste(excelData);
    expect(parsed.success).toBe(true);

    // Apply bulk paste
    act(() => {
      result.current.dispatch({
        type: 'BULK_PASTE_ALLOCATIONS',
        parentId: result.current.state.lines[0].id,
        allocations: parsed.allocations,
      });
    });

    const line = result.current.state.lines[0];

    // Should have 5 allocations
    expect(line.allocations).toHaveLength(5);
    expect(line.isExpanded).toBe(true);

    // Verify first allocation
    expect(line.allocations[0].departmentCode).toBe('07');
    expect(line.allocations[0].amount).toBe(348.34);
    expect(line.allocations[0].glCode).toBe('GL-1234'); // Inherited from parent

    // Verify last allocation
    expect(line.allocations[4].departmentCode).toBe('44');
    expect(line.allocations[4].amount).toBe(31.67);

    // Verify percentages are calculated
    expect(line.allocations[0].percentage).toBeCloseTo(73.33, 1); // 348.34 / 475.02
  });

  it('should use pasted GL codes when 3-column format is provided', () => {
    const { result } = renderHook(() => useReportEditor('2026-01'));

    act(() => {
      result.current.dispatch({
        type: 'LOAD_PREVIEW',
        lines: [
          {
            id: 'line-1',
            transactionId: 'tx-1',
            amount: 300,
            glCode: 'GL-PARENT',
            departmentCode: 'DEPT-001',
            vendorName: 'Test Vendor',
            expenseDate: '2026-01-15',
          },
        ],
      });
    });

    // Parse Excel data with GL codes (3-column format)
    const excelData = `5000\t07\t100.00
5100\t11\t100.00
5200\t22\t100.00`;

    const parsed = parseExcelPaste(excelData);
    expect(parsed.success).toBe(true);
    expect(parsed.format).toBe('3-column');

    // Apply bulk paste
    act(() => {
      result.current.dispatch({
        type: 'BULK_PASTE_ALLOCATIONS',
        parentId: result.current.state.lines[0].id,
        allocations: parsed.allocations,
      });
    });

    const line = result.current.state.lines[0];

    // Should use pasted GL codes, not parent's
    expect(line.allocations[0].glCode).toBe('5000');
    expect(line.allocations[1].glCode).toBe('5100');
    expect(line.allocations[2].glCode).toBe('5200');
  });

  it('should replace existing allocations when pasting', () => {
    const { result } = renderHook(() => useReportEditor('2026-01'));

    act(() => {
      result.current.dispatch({
        type: 'LOAD_PREVIEW',
        lines: [
          {
            id: 'line-1',
            transactionId: 'tx-1',
            amount: 200,
            glCode: 'GL-1234',
            departmentCode: 'DEPT-001',
            vendorName: 'Test Vendor',
            expenseDate: '2026-01-15',
          },
        ],
      });
    });

    // Start a manual split first
    act(() => {
      result.current.dispatch({
        type: 'START_SPLIT',
        id: result.current.state.lines[0].id,
      });
    });

    // Should have 2 allocations from manual split
    expect(result.current.state.lines[0].allocations).toHaveLength(2);

    // Now paste new data (should replace existing)
    const excelData = `10\t50.00
20\t50.00
30\t100.00`;

    const parsed = parseExcelPaste(excelData);

    act(() => {
      result.current.dispatch({
        type: 'BULK_PASTE_ALLOCATIONS',
        parentId: result.current.state.lines[0].id,
        allocations: parsed.allocations,
      });
    });

    const line = result.current.state.lines[0];

    // Should now have 3 allocations (replaced the 2)
    expect(line.allocations).toHaveLength(3);
    expect(line.allocations[0].departmentCode).toBe('10');
    expect(line.allocations[1].departmentCode).toBe('20');
    expect(line.allocations[2].departmentCode).toBe('30');
  });
});
