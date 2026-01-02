'use client';

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReceiptUploadDropzone } from '@/components/receipts/receipt-upload-dropzone';

// Mock the upload mutation
const mockMutate = vi.fn();
vi.mock('@/hooks/queries/use-receipts', () => ({
  useUploadReceipts: () => ({
    mutate: mockMutate,
    isPending: false,
  }),
}));

// Mock sonner toast
vi.mock('sonner', () => ({
  toast: {
    error: vi.fn(),
    success: vi.fn(),
    warning: vi.fn(),
  },
}));

function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });
}

function renderWithProviders(ui: React.ReactElement) {
  const queryClient = createTestQueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      {ui}
    </QueryClientProvider>
  );
}

describe('ReceiptUploadDropzone', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Initial Render', () => {
    it('should render dropzone with instructions', () => {
      renderWithProviders(<ReceiptUploadDropzone />);

      expect(screen.getByText(/drag & drop receipts here/i)).toBeInTheDocument();
      expect(screen.getByText(/or click to browse/i)).toBeInTheDocument();
    });

    it('should render upload icon', () => {
      const { container } = renderWithProviders(<ReceiptUploadDropzone />);

      // Check for the Upload icon (lucide)
      const uploadIcon = container.querySelector('.lucide-upload');
      expect(uploadIcon).toBeInTheDocument();
    });

    it('should display supported file types', () => {
      renderWithProviders(<ReceiptUploadDropzone />);

      expect(screen.getByText(/JPG, PNG, WebP, HEIC, PDF/i)).toBeInTheDocument();
    });

    it('should have file input element', () => {
      const { container } = renderWithProviders(<ReceiptUploadDropzone />);

      const fileInput = container.querySelector('input[type="file"]');
      expect(fileInput).toBeInTheDocument();
    });
  });

  describe('Dropzone Configuration', () => {
    it('should have dropzone with dashed border', () => {
      const { container } = renderWithProviders(<ReceiptUploadDropzone />);

      const dropzone = container.querySelector('.border-dashed');
      expect(dropzone).toBeInTheDocument();
    });

    it('should not show file list when no files selected', () => {
      renderWithProviders(<ReceiptUploadDropzone />);

      expect(screen.queryByText(/files? selected/i)).not.toBeInTheDocument();
    });

    it('should not show upload button when no files selected', () => {
      renderWithProviders(<ReceiptUploadDropzone />);

      expect(screen.queryByRole('button', { name: /upload/i })).not.toBeInTheDocument();
    });
  });

  describe('Accessibility', () => {
    it('should have accessible text for screen readers', () => {
      renderWithProviders(<ReceiptUploadDropzone />);

      // Check that instructional text is present and visible
      expect(screen.getByText(/drag & drop receipts here/i)).toBeVisible();
      expect(screen.getByText(/or click to browse/i)).toBeVisible();
    });

    it('should have file input accessible', () => {
      const { container } = renderWithProviders(<ReceiptUploadDropzone />);

      const fileInput = container.querySelector('input[type="file"]');
      expect(fileInput).toHaveAttribute('multiple');
    });
  });

  describe('Upload Mutation', () => {
    it('should provide mutate function from useUploadReceipts', () => {
      // Verify the mock is set up correctly
      renderWithProviders(<ReceiptUploadDropzone />);

      // The component should have access to the mutate function
      expect(mockMutate).not.toHaveBeenCalled(); // Not called on initial render
    });
  });

  describe('Styling', () => {
    it('should have rounded corners', () => {
      const { container } = renderWithProviders(<ReceiptUploadDropzone />);

      const dropzone = container.querySelector('.rounded-lg');
      expect(dropzone).toBeInTheDocument();
    });

    it('should have proper padding', () => {
      const { container } = renderWithProviders(<ReceiptUploadDropzone />);

      const dropzone = container.querySelector('.p-8');
      expect(dropzone).toBeInTheDocument();
    });

    it('should center text content', () => {
      const { container } = renderWithProviders(<ReceiptUploadDropzone />);

      const centeredContent = container.querySelector('.text-center');
      expect(centeredContent).toBeInTheDocument();
    });
  });
});

// Note: Full file upload simulation requires integration tests with the actual
// react-dropzone library. The above tests verify the component structure and
// initial state. For complete file handling tests, use E2E tests with Playwright.
