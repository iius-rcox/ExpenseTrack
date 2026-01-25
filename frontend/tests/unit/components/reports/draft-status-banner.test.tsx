import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { DraftStatusBanner } from '@/components/reports/draft-status-banner'

describe('DraftStatusBanner', () => {
  const defaultProps = {
    useDraft: true,
    reportId: 'test-report-123',
    lastSaved: null,
    isSaving: false,
    hasPendingChanges: false,
    onCreateDraft: vi.fn(),
    onDiscardDraft: vi.fn(),
    onRegenerateDraft: vi.fn(),
    onSave: vi.fn(),
  }

  describe('Save Button Visibility', () => {
    it('shows Save button when useDraft, reportId, and hasPendingChanges are all true', () => {
      render(
        <DraftStatusBanner
          {...defaultProps}
          useDraft={true}
          reportId="test-report-123"
          hasPendingChanges={true}
        />
      )

      expect(screen.getByTestId('save-report-btn')).toBeInTheDocument()
    })

    it('hides Save button when not in draft mode', () => {
      render(
        <DraftStatusBanner
          {...defaultProps}
          useDraft={false}
          reportId="test-report-123"
          hasPendingChanges={true}
        />
      )

      expect(screen.queryByTestId('save-report-btn')).not.toBeInTheDocument()
    })

    it('hides Save button when no reportId', () => {
      render(
        <DraftStatusBanner
          {...defaultProps}
          useDraft={true}
          reportId={null}
          hasPendingChanges={true}
        />
      )

      expect(screen.queryByTestId('save-report-btn')).not.toBeInTheDocument()
    })

    it('hides Save button when no pending changes', () => {
      render(
        <DraftStatusBanner
          {...defaultProps}
          useDraft={true}
          reportId="test-report-123"
          hasPendingChanges={false}
        />
      )

      expect(screen.queryByTestId('save-report-btn')).not.toBeInTheDocument()
    })

    it('hides Save button when onSave is not provided', () => {
      render(
        <DraftStatusBanner
          {...defaultProps}
          useDraft={true}
          reportId="test-report-123"
          hasPendingChanges={true}
          onSave={undefined}
        />
      )

      expect(screen.queryByTestId('save-report-btn')).not.toBeInTheDocument()
    })
  })

  describe('Save Button Behavior', () => {
    it('calls onSave when Save button is clicked', () => {
      const onSave = vi.fn()
      render(
        <DraftStatusBanner
          {...defaultProps}
          useDraft={true}
          reportId="test-report-123"
          hasPendingChanges={true}
          onSave={onSave}
        />
      )

      fireEvent.click(screen.getByTestId('save-report-btn'))

      expect(onSave).toHaveBeenCalledTimes(1)
    })

    it('disables Save button while saving', () => {
      render(
        <DraftStatusBanner
          {...defaultProps}
          useDraft={true}
          reportId="test-report-123"
          hasPendingChanges={true}
          isSaving={true}
        />
      )

      const saveButton = screen.getByTestId('save-report-btn')
      expect(saveButton).toBeDisabled()
    })

    it('shows loading spinner while saving', () => {
      render(
        <DraftStatusBanner
          {...defaultProps}
          useDraft={true}
          reportId="test-report-123"
          hasPendingChanges={true}
          isSaving={true}
        />
      )

      // Check for spinner (Loader2 icon with animate-spin class)
      const saveButton = screen.getByTestId('save-report-btn')
      const spinner = saveButton.querySelector('svg.animate-spin')
      expect(spinner).toBeInTheDocument()
    })
  })

  describe('Status Display', () => {
    it('shows "Unsaved changes" when hasPendingChanges is true and not saved yet', () => {
      render(
        <DraftStatusBanner
          {...defaultProps}
          useDraft={true}
          reportId="test-report-123"
          hasPendingChanges={true}
          lastSaved={null}
        />
      )

      expect(screen.getByText('Unsaved changes')).toBeInTheDocument()
    })

    it('shows "No changes" when hasPendingChanges is false and not saved yet', () => {
      render(
        <DraftStatusBanner
          {...defaultProps}
          useDraft={true}
          reportId="test-report-123"
          hasPendingChanges={false}
          lastSaved={null}
        />
      )

      expect(screen.getByText('No changes')).toBeInTheDocument()
    })

    it('shows "Saved" with time ago when lastSaved is set', () => {
      const fiveMinutesAgo = new Date(Date.now() - 5 * 60 * 1000)
      render(
        <DraftStatusBanner
          {...defaultProps}
          useDraft={true}
          reportId="test-report-123"
          hasPendingChanges={false}
          lastSaved={fiveMinutesAgo}
        />
      )

      expect(screen.getByText(/Saved 5 min ago/)).toBeInTheDocument()
    })

    it('shows "Saving..." when isSaving is true', () => {
      render(
        <DraftStatusBanner
          {...defaultProps}
          useDraft={true}
          reportId="test-report-123"
          hasPendingChanges={true}
          isSaving={true}
        />
      )

      expect(screen.getByText('Saving...')).toBeInTheDocument()
    })
  })

  describe('Preview Mode', () => {
    it('shows "Save as Draft" button in preview mode', () => {
      render(
        <DraftStatusBanner
          {...defaultProps}
          useDraft={false}
        />
      )

      expect(screen.getByRole('button', { name: 'Save as Draft' })).toBeInTheDocument()
    })

    it('does not show Save button in preview mode', () => {
      render(
        <DraftStatusBanner
          {...defaultProps}
          useDraft={false}
          hasPendingChanges={true}
        />
      )

      expect(screen.queryByTestId('save-report-btn')).not.toBeInTheDocument()
    })
  })

  describe('Draft Mode Actions', () => {
    it('shows Discard Draft button in draft mode', () => {
      render(
        <DraftStatusBanner
          {...defaultProps}
          useDraft={true}
          reportId="test-report-123"
        />
      )

      expect(screen.getByRole('button', { name: 'Discard Draft' })).toBeInTheDocument()
    })

    it('shows Refresh button when onRegenerateDraft is provided', () => {
      render(
        <DraftStatusBanner
          {...defaultProps}
          useDraft={true}
          reportId="test-report-123"
          onRegenerateDraft={vi.fn()}
        />
      )

      expect(screen.getByRole('button', { name: /Refresh/ })).toBeInTheDocument()
    })

    it('disables all action buttons while saving', () => {
      render(
        <DraftStatusBanner
          {...defaultProps}
          useDraft={true}
          reportId="test-report-123"
          hasPendingChanges={true}
          isSaving={true}
        />
      )

      expect(screen.getByRole('button', { name: /Refresh/ })).toBeDisabled()
      expect(screen.getByRole('button', { name: 'Discard Draft' })).toBeDisabled()
    })
  })
})
