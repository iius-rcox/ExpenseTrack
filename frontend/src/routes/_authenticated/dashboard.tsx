/**
 * Dashboard Route (T032)
 *
 * Main dashboard page using the new "Refined Intelligence" design system.
 * Provides a command center view of expense management activity.
 *
 * Features:
 * - Real-time metrics with 30-second polling
 * - Activity stream showing recent expense events
 * - Priority-sorted action queue for pending items
 * - Category breakdown visualization
 *
 * The layout is responsive:
 * - Desktop: Multi-column grid layout
 * - Tablet: Stacked sections
 * - Mobile: Compact summary bar with scrollable sections
 */

import { createFileRoute, Link } from '@tanstack/react-router';
import { Button } from '@/components/ui/button';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { DashboardLayout } from '@/components/dashboard';
import { AlertCircle, Upload } from 'lucide-react';
import { useDashboardMetrics } from '@/hooks/queries/use-dashboard';

export const Route = createFileRoute('/_authenticated/dashboard')({
  component: DashboardPage,
});

function DashboardPage() {
  // Check for critical errors that should block the whole page
  const { error: metricsError } = useDashboardMetrics();

  // If there's a critical auth error, show alert
  if (metricsError?.message?.includes('401') || metricsError?.message?.includes('Unauthorized')) {
    return (
      <div className="container mx-auto max-w-7xl space-y-6 p-6">
        <Alert variant="destructive">
          <AlertCircle className="h-4 w-4" />
          <AlertTitle>Authentication Error</AlertTitle>
          <AlertDescription>
            Your session may have expired. Please refresh the page or sign in again.
          </AlertDescription>
        </Alert>
      </div>
    );
  }

  return (
    <div className="container mx-auto max-w-7xl p-6">
      {/* Quick Action FAB for mobile (fixed bottom right) */}
      <div className="fixed bottom-6 right-6 z-50 md:hidden">
        <Button size="lg" className="h-14 w-14 rounded-full shadow-lg" asChild>
          <Link to="/receipts">
            <Upload className="h-6 w-6" />
            <span className="sr-only">Upload Receipt</span>
          </Link>
        </Button>
      </div>

      {/* Main Dashboard Content */}
      <DashboardLayout />
    </div>
  );
}

export default DashboardPage;
