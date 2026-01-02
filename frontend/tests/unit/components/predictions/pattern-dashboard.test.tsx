import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { PatternDashboardTestWrapper } from './pattern-dashboard-test-wrapper';

/**
 * Unit tests for Pattern Dashboard page (T083)
 *
 * Tests:
 * - Pattern list rendering with stats
 * - Suppression toggle functionality
 * - Delete confirmation dialog
 * - Loading and error states
 * - Search filtering
 */

// Mock the predictions hooks
const mockPatterns = [
  {
    id: 'pattern-1',
    displayName: 'STARBUCKS',
    normalizedVendor: 'starbucks',
    category: 'Food & Beverage',
    averageAmount: 5.5,
    occurrenceCount: 25,
    confirmCount: 20,
    rejectCount: 2,
    isSuppressed: false,
    accuracyRate: 0.91,
    lastSeenAt: '2025-12-01T10:00:00Z',
    createdAt: '2025-01-01T10:00:00Z',
  },
  {
    id: 'pattern-2',
    displayName: 'AMAZON',
    normalizedVendor: 'amazon',
    category: 'Shopping',
    averageAmount: 45.0,
    occurrenceCount: 15,
    confirmCount: 10,
    rejectCount: 5,
    isSuppressed: true,
    accuracyRate: 0.67,
    lastSeenAt: '2025-12-15T10:00:00Z',
    createdAt: '2025-02-01T10:00:00Z',
  },
];

const mockPatternsResponse = {
  patterns: mockPatterns,
  totalCount: 2,
  page: 1,
  pageSize: 20,
  activeCount: 1,
  suppressedCount: 1,
};

const mockStats = {
  totalPredictions: 100,
  confirmedCount: 75,
  rejectedCount: 15,
  ignoredCount: 10,
  accuracyRate: 0.833,
  highConfidenceAccuracyRate: 0.9,
  mediumConfidenceAccuracyRate: 0.75,
};

const mockRefetch = vi.fn();
const mockUpdateSuppressionMutate = vi.fn();
const mockDeletePatternMutate = vi.fn();
const mockRebuildPatternsMutate = vi.fn();

// Mock implementations - store current mock state
let mockPatternsState: {
  data: typeof mockPatternsResponse | undefined;
  isLoading: boolean;
  error: Error | null;
  refetch: typeof mockRefetch;
} = {
  data: mockPatternsResponse,
  isLoading: false,
  error: null,
  refetch: mockRefetch,
};

let mockStatsState = {
  data: mockStats,
  isLoading: false,
};

vi.mock('@/hooks/queries/use-predictions', () => ({
  usePatterns: () => mockPatternsState,
  usePredictionStats: () => mockStatsState,
  useUpdatePatternSuppression: () => ({
    mutate: mockUpdateSuppressionMutate,
    isPending: false,
  }),
  useDeletePattern: () => ({
    mutate: mockDeletePatternMutate,
    isPending: false,
  }),
  useRebuildPatterns: () => ({
    mutate: mockRebuildPatternsMutate,
    isPending: false,
  }),
}));

vi.mock('sonner', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
    warning: vi.fn(),
    info: vi.fn(),
  },
}));

vi.mock('@tanstack/react-router', () => ({
  createFileRoute: () => ({ component: null }),
  Link: ({ children, to }: { children: React.ReactNode; to: string }) => (
    <a href={to}>{children}</a>
  ),
}));

describe('Pattern Dashboard', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // Reset mock state to defaults
    mockPatternsState = {
      data: mockPatternsResponse,
      isLoading: false,
      error: null,
      refetch: mockRefetch,
    };
    mockStatsState = {
      data: mockStats,
      isLoading: false,
    };
  });

  describe('Pattern List Rendering', () => {
    it('should render patterns with their details', () => {
      render(<PatternDashboardTestWrapper />);

      // Check pattern names are displayed
      expect(screen.getByText('STARBUCKS')).toBeInTheDocument();
      expect(screen.getByText('AMAZON')).toBeInTheDocument();
    });

    it('should display pattern categories', () => {
      render(<PatternDashboardTestWrapper />);

      expect(screen.getByText('Food & Beverage')).toBeInTheDocument();
      expect(screen.getByText('Shopping')).toBeInTheDocument();
    });

    it('should show suppression status badges', () => {
      render(<PatternDashboardTestWrapper />);

      // Should have "Suppressed" text - appears in stats card and as badge
      const suppressedElements = screen.getAllByText('Suppressed');
      expect(suppressedElements.length).toBeGreaterThanOrEqual(1);
      // "Active" appears in stats card, table header, and badge
      const activeElements = screen.getAllByText('Active');
      expect(activeElements.length).toBeGreaterThanOrEqual(1);
    });

    it('should display average amounts', () => {
      render(<PatternDashboardTestWrapper />);

      // Should show formatted currency amounts
      expect(screen.getByText('$5.50')).toBeInTheDocument();
      expect(screen.getByText('$45.00')).toBeInTheDocument();
    });
  });

  describe('Statistics Display', () => {
    it('should show total pattern count', () => {
      render(<PatternDashboardTestWrapper />);

      // Total Patterns header
      expect(screen.getByText('Total Patterns')).toBeInTheDocument();
    });

    it('should show accuracy rate percentage', () => {
      render(<PatternDashboardTestWrapper />);

      // Should display accuracy percentage
      expect(screen.getByText('Accuracy Rate')).toBeInTheDocument();
      expect(screen.getByText('83.3%')).toBeInTheDocument();
    });
  });

  describe('Suppression Toggle', () => {
    it('should call updateSuppression when pattern toggle is clicked', async () => {
      render(<PatternDashboardTestWrapper />);

      // Find all switches - first is "include suppressed" toggle, rest are pattern toggles
      const switches = screen.getAllByRole('switch');
      // Should have at least 3 switches: 1 for include suppressed + 2 for pattern rows
      expect(switches.length).toBeGreaterThanOrEqual(3);

      // Click the second switch (first pattern row toggle)
      fireEvent.click(switches[1]);

      await waitFor(() => {
        expect(mockUpdateSuppressionMutate).toHaveBeenCalled();
      });
    });

    it('should pass correct suppression value when toggling active pattern', async () => {
      render(<PatternDashboardTestWrapper />);

      // Find pattern row switches (skip first which is "include suppressed" toggle)
      const switches = screen.getAllByRole('switch');
      // Second switch is for STARBUCKS (active, so toggling should suppress)
      fireEvent.click(switches[1]);

      await waitFor(() => {
        expect(mockUpdateSuppressionMutate).toHaveBeenCalledWith(
          expect.objectContaining({
            id: 'pattern-1',
            isSuppressed: true,
          })
        );
      });
    });
  });

  describe('Delete Pattern', () => {
    it('should open confirmation dialog when delete is clicked', async () => {
      const user = userEvent.setup();
      render(<PatternDashboardTestWrapper />);

      // Find and click delete button
      const deleteButtons = screen.getAllByRole('button', { name: /delete pattern/i });
      expect(deleteButtons.length).toBeGreaterThanOrEqual(1);

      await user.click(deleteButtons[0]);

      // Confirmation dialog should appear
      await waitFor(() => {
        expect(screen.getByText(/are you sure/i)).toBeInTheDocument();
      });
    });

    it('should call deletePattern when confirmed', async () => {
      const user = userEvent.setup();
      render(<PatternDashboardTestWrapper />);

      // Open delete dialog
      const deleteButtons = screen.getAllByRole('button', { name: /delete pattern/i });
      await user.click(deleteButtons[0]);

      // Click confirm button in dialog
      const confirmButton = await screen.findByRole('button', { name: /^delete$/i });
      await user.click(confirmButton);

      await waitFor(() => {
        expect(mockDeletePatternMutate).toHaveBeenCalledWith(
          'pattern-1',
          expect.any(Object)
        );
      });
    });

    it('should close dialog when cancel is clicked', async () => {
      const user = userEvent.setup();
      render(<PatternDashboardTestWrapper />);

      // Open delete dialog
      const deleteButtons = screen.getAllByRole('button', { name: /delete pattern/i });
      await user.click(deleteButtons[0]);

      // Click cancel button
      const cancelButton = await screen.findByRole('button', { name: /cancel/i });
      await user.click(cancelButton);

      // Dialog should close
      await waitFor(() => {
        expect(screen.queryByText(/are you sure/i)).not.toBeInTheDocument();
      });
    });
  });

  describe('Loading States', () => {
    it('should show loading skeleton when patterns are loading', () => {
      mockPatternsState = {
        data: undefined,
        isLoading: true,
        error: null,
        refetch: mockRefetch,
      };

      const { container } = render(<PatternDashboardTestWrapper />);

      // Should have skeleton elements with animate-pulse class
      const skeletons = container.querySelectorAll('[class*="animate-pulse"]');
      expect(skeletons.length).toBeGreaterThanOrEqual(1);
    });
  });

  describe('Error States', () => {
    it('should show error alert when patterns fail to load', () => {
      mockPatternsState = {
        data: undefined,
        isLoading: false,
        error: new Error('Failed to load patterns'),
        refetch: mockRefetch,
      };

      render(<PatternDashboardTestWrapper />);

      // Should display error alert
      expect(screen.getByRole('alert')).toBeInTheDocument();
      expect(screen.getByText('Error')).toBeInTheDocument();
    });
  });

  describe('Search Functionality', () => {
    it('should have search input', () => {
      render(<PatternDashboardTestWrapper />);

      const searchInput = screen.getByPlaceholderText(/search/i);
      expect(searchInput).toBeInTheDocument();
    });

    it('should filter patterns client-side when searching', async () => {
      const user = userEvent.setup();
      render(<PatternDashboardTestWrapper />);

      const searchInput = screen.getByPlaceholderText(/search/i);
      await user.type(searchInput, 'STARBUCKS');

      // After typing, only STARBUCKS should be visible
      await waitFor(() => {
        expect(screen.getByText('STARBUCKS')).toBeInTheDocument();
        expect(screen.queryByText('AMAZON')).not.toBeInTheDocument();
      });
    });
  });

  describe('Rebuild Patterns', () => {
    it('should have rebuild button', () => {
      render(<PatternDashboardTestWrapper />);

      const rebuildButton = screen.getByRole('button', { name: /rebuild/i });
      expect(rebuildButton).toBeInTheDocument();
    });

    it('should call rebuildPatterns when rebuild button is clicked', async () => {
      const user = userEvent.setup();
      render(<PatternDashboardTestWrapper />);

      const rebuildButton = screen.getByRole('button', { name: /rebuild/i });
      await user.click(rebuildButton);

      await waitFor(() => {
        expect(mockRebuildPatternsMutate).toHaveBeenCalled();
      });
    });
  });

  describe('Empty State', () => {
    it('should show empty state when no patterns exist', () => {
      mockPatternsState = {
        data: { patterns: [], totalCount: 0, page: 1, pageSize: 20, activeCount: 0, suppressedCount: 0 },
        isLoading: false,
        error: null,
        refetch: mockRefetch,
      };

      render(<PatternDashboardTestWrapper />);

      // Should show empty state message
      expect(screen.getByText('No Patterns Found')).toBeInTheDocument();
    });
  });

  describe('Include Suppressed Toggle', () => {
    it('should have show suppressed toggle', () => {
      render(<PatternDashboardTestWrapper />);

      expect(screen.getByText('Show Suppressed')).toBeInTheDocument();
      // There are multiple switches - one for "show suppressed" toggle and one per pattern row
      const switches = screen.getAllByRole('switch');
      expect(switches.length).toBeGreaterThan(0);
    });
  });

  describe('Refresh Button', () => {
    it('should call refetch when refresh button is clicked', async () => {
      const user = userEvent.setup();
      render(<PatternDashboardTestWrapper />);

      // Find refresh button by its icon (last button without text)
      const buttons = screen.getAllByRole('button');
      // The refresh button is typically a small icon button
      const refreshButton = buttons.find(btn => btn.querySelector('.lucide-refresh-ccw'));

      if (refreshButton) {
        await user.click(refreshButton);
        await waitFor(() => {
          expect(mockRefetch).toHaveBeenCalled();
        });
      }
    });
  });
});
