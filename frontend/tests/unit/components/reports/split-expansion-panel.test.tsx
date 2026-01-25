import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { SplitExpansionPanel } from '@/components/reports/split-expansion-panel';
import type { SplitAllocation } from '@/types/report-editor';

const createAllocation = (overrides: Partial<SplitAllocation> = {}): SplitAllocation => ({
  id: crypto.randomUUID(),
  glCode: 'GL-1234',
  departmentCode: 'DEPT-001',
  percentage: 50,
  amount: 50,
  entryMode: 'amount',
  ...overrides,
});

describe('SplitExpansionPanel Tab Navigation', () => {
  const defaultProps = {
    parentId: 'parent-1',
    parentAmount: 100,
    allocations: [
      createAllocation({ id: 'alloc-1', percentage: 50, amount: 50 }),
      createAllocation({ id: 'alloc-2', percentage: 50, amount: 50 }),
    ],
    onAddAllocation: vi.fn(),
    onRemoveAllocation: vi.fn(),
    onUpdateAllocation: vi.fn(),
    onToggleEntryMode: vi.fn(),
    onBulkPaste: vi.fn(),
    onApply: vi.fn(),
    onCancel: vi.fn(),
  };

  it('should render allocation rows with editable cells', () => {
    render(<SplitExpansionPanel {...defaultProps} />);

    // Should have GL Code cells (click-to-edit divs with role="button")
    const glCodeCells = screen.getAllByRole('button', { name: /edit glcode/i });
    expect(glCodeCells).toHaveLength(2);

    // Should have Department cells
    const deptCells = screen.getAllByRole('button', { name: /edit department/i });
    expect(deptCells).toHaveLength(2);

    // Should have Amount inputs (native number inputs)
    const amountInputs = screen.getAllByTestId(/amount-input/);
    expect(amountInputs).toHaveLength(2);
  });

  it('should navigate Tab from GL code to next GL code (vertical navigation)', async () => {
    const user = userEvent.setup();

    render(<SplitExpansionPanel {...defaultProps} />);

    const glCodeCells = screen.getAllByRole('button', { name: /edit glcode/i });

    // Focus first GL code cell
    await user.click(glCodeCells[0]);
    glCodeCells[0].focus();

    // Press Tab - should go to next GL code (vertical)
    await user.keyboard('{Tab}');

    // The second GL code cell should now be focused
    expect(glCodeCells[1]).toHaveFocus();
  });

  it('should navigate Tab from department to next department (vertical navigation)', async () => {
    const user = userEvent.setup();

    render(<SplitExpansionPanel {...defaultProps} />);

    const deptCells = screen.getAllByRole('button', { name: /edit department/i });

    // Focus first department cell
    deptCells[0].focus();

    // Press Tab - should go to next department (vertical)
    await user.keyboard('{Tab}');

    // The second department cell should now be focused
    expect(deptCells[1]).toHaveFocus();
  });

  it('should navigate Tab from amount to next amount (vertical navigation)', async () => {
    const user = userEvent.setup();

    render(<SplitExpansionPanel {...defaultProps} />);

    const amountInputs = screen.getAllByTestId(/amount-input/);

    // Focus first amount input
    (amountInputs[0] as HTMLInputElement).focus();

    // Press Tab - should go to next amount (vertical)
    await user.keyboard('{Tab}');

    // The second amount input should now be focused
    expect(amountInputs[1]).toHaveFocus();
  });

  it('should navigate Shift+Tab from second department to first department', async () => {
    const user = userEvent.setup();

    render(<SplitExpansionPanel {...defaultProps} />);

    const deptCells = screen.getAllByRole('button', { name: /edit department/i });

    // Focus second department cell
    deptCells[1].focus();

    // Press Shift+Tab - should go to previous department (vertical)
    await user.keyboard('{Shift>}{Tab}{/Shift}');

    // The first department cell should now be focused
    expect(deptCells[0]).toHaveFocus();
  });

  it('should fall through to next column when at last row', async () => {
    const user = userEvent.setup();

    render(<SplitExpansionPanel {...defaultProps} />);

    const deptCells = screen.getAllByRole('button', { name: /edit department/i });
    const amountInputs = screen.getAllByTestId(/amount-input/);

    // Focus last department cell (row 2)
    deptCells[1].focus();

    // Press Tab - at last row, should move to first amount (next column)
    await user.keyboard('{Tab}');

    // Should move to first amount input (next column)
    expect(amountInputs[0]).toHaveFocus();
  });

  it('should work with three allocations', async () => {
    const user = userEvent.setup();

    const propsWithThree = {
      ...defaultProps,
        allocations: [
        createAllocation({ id: 'alloc-1', percentage: 33.33, amount: 33.33 }),
        createAllocation({ id: 'alloc-2', percentage: 33.33, amount: 33.33 }),
        createAllocation({ id: 'alloc-3', percentage: 33.34, amount: 33.34 }),
      ],
    };

    render(<SplitExpansionPanel {...propsWithThree} />);

    const deptCells = screen.getAllByRole('button', { name: /edit department/i });
    expect(deptCells).toHaveLength(3);

    // Focus first department cell
    deptCells[0].focus();

    // Tab twice should reach third department
    await user.keyboard('{Tab}');
    await user.keyboard('{Tab}');

    expect(deptCells[2]).toHaveFocus();
  });
});

describe('SplitExpansionPanel GL Code Display', () => {
  const defaultProps = {
    parentId: 'parent-1',
    parentAmount: 100,
    allocations: [
      createAllocation({ id: 'alloc-1', glCode: 'GL-1234' }),
      createAllocation({ id: 'alloc-2', glCode: 'GL-1234' }),
    ],
    onAddAllocation: vi.fn(),
    onRemoveAllocation: vi.fn(),
    onUpdateAllocation: vi.fn(),
    onToggleEntryMode: vi.fn(),
    onBulkPaste: vi.fn(),
    onApply: vi.fn(),
    onCancel: vi.fn(),
  };

  it('should display inherited GL codes in allocation rows', () => {
    render(<SplitExpansionPanel {...defaultProps} />);

    // GL codes should be displayed in the cells (as text content)
    const glCodeText = screen.getAllByText('GL-1234');
    expect(glCodeText).toHaveLength(2);
  });

  it('should allow editing GL code by clicking the cell', async () => {
    const onUpdateAllocation = vi.fn();

    render(
      <SplitExpansionPanel
        {...defaultProps}
        onUpdateAllocation={onUpdateAllocation}
      />
    );

    const glCodeCells = screen.getAllByRole('button', { name: /edit glcode/i });

    // Click to enter edit mode
    await userEvent.click(glCodeCells[0]);

    // Now an input should appear
    const input = screen.getByRole('textbox');
    expect(input).toBeInTheDocument();

    // Clear and type new value, then blur to save
    await userEvent.clear(input);
    await userEvent.type(input, 'GL-NEW');

    // Blur to trigger save
    input.blur();

    // Should have called update with the new value
    expect(onUpdateAllocation).toHaveBeenCalledWith('alloc-1', 'glCode', 'GL-NEW');
  });
});
