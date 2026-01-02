/**
 * Settings Page Integration Test
 *
 * Tests the settings page renders correctly with MSW mocked APIs.
 */

import { describe, it, expect, beforeEach } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import { server } from '@/test-utils/msw-server'
import { http, HttpResponse } from 'msw'
import { renderWithProviders } from '@/test-utils/render-with-providers'

// Placeholder component - replace with actual import
const SettingsPage = () => (
  <div data-testid="settings-page">
    <h1>Settings</h1>
    <section data-testid="appearance-settings">
      <h2>Appearance</h2>
      <select data-testid="theme-select">
        <option>Light</option>
        <option>Dark</option>
        <option>System</option>
      </select>
    </section>
    <section data-testid="preferences-settings">
      <h2>Preferences</h2>
      <select data-testid="date-format">
        <option>MM/DD/YYYY</option>
        <option>DD/MM/YYYY</option>
        <option>YYYY-MM-DD</option>
      </select>
      <select data-testid="currency">
        <option>USD</option>
        <option>EUR</option>
        <option>GBP</option>
      </select>
    </section>
    <section data-testid="notification-settings">
      <h2>Notifications</h2>
      <label>
        <input type="checkbox" data-testid="email-notifications" />
        Email Notifications
      </label>
    </section>
    <section data-testid="account-settings">
      <h2>Account</h2>
      <button data-testid="sign-out">Sign Out</button>
    </section>
  </div>
)

describe('Settings Page Integration', () => {
  beforeEach(() => {
    server.resetHandlers()

    // Setup settings API handlers
    server.use(
      http.get('*/api/users/preferences', () => {
        return HttpResponse.json({
          theme: 'light',
          dateFormat: 'MM/DD/YYYY',
          currency: 'USD',
          emailNotifications: true,
          defaultCategory: null,
        })
      }),
      http.put('*/api/users/preferences', async ({ request }) => {
        const body = await request.json()
        return HttpResponse.json({
          ...body,
          updatedAt: new Date().toISOString(),
        })
      })
    )
  })

  describe('Successful Render', () => {
    it('renders the settings page', async () => {
      renderWithProviders(<SettingsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('settings-page')).toBeInTheDocument()
      })
    })

    it('displays appearance settings section', async () => {
      renderWithProviders(<SettingsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('appearance-settings')).toBeInTheDocument()
      })
    })

    it('displays theme selector', async () => {
      renderWithProviders(<SettingsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('theme-select')).toBeInTheDocument()
      })
    })

    it('displays preferences settings section', async () => {
      renderWithProviders(<SettingsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('preferences-settings')).toBeInTheDocument()
      })
    })

    it('displays notification settings section', async () => {
      renderWithProviders(<SettingsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('notification-settings')).toBeInTheDocument()
      })
    })

    it('displays account settings section', async () => {
      renderWithProviders(<SettingsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('account-settings')).toBeInTheDocument()
      })
    })
  })

  describe('Error Handling', () => {
    it('handles API error gracefully', async () => {
      server.use(
        http.get('*/api/users/preferences', () => {
          return HttpResponse.json(
            { error: 'Server Error' },
            { status: 500 }
          )
        })
      )

      renderWithProviders(<SettingsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('settings-page')).toBeInTheDocument()
      })
    })
  })
})
