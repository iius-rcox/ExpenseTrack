/**
 * Test wrapper for Pattern Dashboard (T083)
 *
 * This wrapper provides a testable version of the pattern dashboard
 * without requiring the full TanStack Router context.
 */

import { useState } from 'react';
import {
  usePatterns,
  useUpdatePatternSuppression,
  useDeletePattern,
  useRebuildPatterns,
  usePredictionStats,
} from '@/hooks/queries/use-predictions';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Switch } from '@/components/ui/switch';
import { Skeleton } from '@/components/ui/skeleton';
import { Input } from '@/components/ui/input';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/components/ui/alert-dialog';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { formatCurrency } from '@/lib/utils';
import { toast } from 'sonner';
import {
  Search,
  RefreshCcw,
  Trash2,
  Loader2,
  AlertCircle,
  Store,
} from 'lucide-react';
import type { PatternSummary } from '@/types/prediction';

export function PatternDashboardTestWrapper() {
  const [page, setPage] = useState(1);
  const [includeSuppressed, setIncludeSuppressed] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');

  const { data: patternsData, isLoading, error, refetch } = usePatterns({
    page,
    pageSize: 20,
    includeSuppressed,
  });

  const { data: stats } = usePredictionStats();
  const updateSuppression = useUpdatePatternSuppression();
  const deletePattern = useDeletePattern();
  const rebuildPatterns = useRebuildPatterns();

  // Client-side search filter
  const filteredPatterns = patternsData?.patterns?.filter((pattern) =>
    pattern.displayName.toLowerCase().includes(searchQuery.toLowerCase())
  ) ?? [];

  const handleToggleSuppression = async (pattern: PatternSummary) => {
    updateSuppression.mutate({
      id: pattern.id,
      isSuppressed: !pattern.isSuppressed,
    });
  };

  const handleDeletePattern = (id: string) => {
    deletePattern.mutate(id, {
      onSuccess: () => {
        toast.success('Pattern deleted');
      },
    });
  };

  const handleRebuildPatterns = () => {
    rebuildPatterns.mutate(undefined, {
      onSuccess: (count) => {
        toast.success(`Rebuilt ${count} patterns from approved reports`);
      },
    });
  };

  if (error) {
    return (
      <Alert variant="destructive">
        <AlertCircle className="h-4 w-4" />
        <AlertTitle>Error</AlertTitle>
        <AlertDescription>
          Failed to load patterns. Please try again.
        </AlertDescription>
      </Alert>
    );
  }

  const emptyState = !isLoading && filteredPatterns.length === 0;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Expense Patterns</h1>
          <p className="text-muted-foreground">
            Manage learned expense patterns
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            onClick={handleRebuildPatterns}
            disabled={rebuildPatterns.isPending}
          >
            {rebuildPatterns.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            Rebuild Patterns
          </Button>
          <Button variant="outline" size="icon" onClick={() => refetch()}>
            <RefreshCcw className={isLoading ? 'animate-spin' : ''} />
          </Button>
        </div>
      </div>

      {/* Stats Cards */}
      <div className="grid gap-4 md:grid-cols-4">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">Total Patterns</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{patternsData?.totalCount ?? 0}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">Active</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-emerald-500">
              {patternsData?.activeCount ?? 0}
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">Suppressed</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-amber-500">
              {patternsData?.suppressedCount ?? 0}
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">Accuracy Rate</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {stats ? `${(stats.accuracyRate * 100).toFixed(1)}%` : '—'}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Search */}
      <div className="flex items-center gap-4">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Search patterns..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="pl-9"
          />
        </div>
        <div className="flex items-center gap-2">
          <Switch
            id="include-suppressed"
            checked={includeSuppressed}
            onCheckedChange={setIncludeSuppressed}
          />
          <label htmlFor="include-suppressed" className="text-sm text-muted-foreground">
            Show Suppressed
          </label>
        </div>
      </div>

      {/* Loading State */}
      {isLoading && (
        <div className="space-y-4">
          {Array.from({ length: 5 }).map((_, i) => (
            <Skeleton key={i} className="h-16 w-full animate-pulse" />
          ))}
        </div>
      )}

      {/* Empty State */}
      {emptyState && (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <Store className="h-12 w-12 text-muted-foreground mb-4" />
            <h3 className="text-lg font-semibold">No Patterns Found</h3>
            <p className="text-muted-foreground text-center max-w-md mt-2">
              Start by approving expense reports to help the system learn your expense patterns.
            </p>
          </CardContent>
        </Card>
      )}

      {/* Pattern Table */}
      {!isLoading && filteredPatterns.length > 0 && (
        <Card>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Vendor</TableHead>
                <TableHead>Category</TableHead>
                <TableHead className="text-right">Avg Amount</TableHead>
                <TableHead className="text-right">Occurrences</TableHead>
                <TableHead className="text-right">Accuracy</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Active</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filteredPatterns.map((pattern) => (
                <TableRow key={pattern.id}>
                  <TableCell className="font-medium">{pattern.displayName}</TableCell>
                  <TableCell>{pattern.category}</TableCell>
                  <TableCell className="text-right font-mono">
                    {formatCurrency(pattern.averageAmount)}
                  </TableCell>
                  <TableCell className="text-right">{pattern.occurrenceCount}</TableCell>
                  <TableCell className="text-right">
                    <span className={pattern.accuracyRate >= 0.8 ? 'text-emerald-500' : pattern.accuracyRate >= 0.5 ? 'text-amber-500' : 'text-rose-500'}>
                      {(pattern.accuracyRate * 100).toFixed(0)}%
                    </span>
                  </TableCell>
                  <TableCell>
                    {pattern.isSuppressed ? (
                      <Badge variant="secondary">Suppressed</Badge>
                    ) : (
                      <Badge variant="default">Active</Badge>
                    )}
                  </TableCell>
                  <TableCell>
                    <Switch
                      checked={!pattern.isSuppressed}
                      onCheckedChange={() => handleToggleSuppression(pattern)}
                      aria-label={pattern.isSuppressed ? 'Activate pattern' : 'Suppress pattern'}
                    />
                  </TableCell>
                  <TableCell className="text-right">
                    <AlertDialog>
                      <AlertDialogTrigger asChild>
                        <Button variant="ghost" size="icon" aria-label="Delete pattern">
                          <Trash2 className="h-4 w-4 text-destructive" />
                        </Button>
                      </AlertDialogTrigger>
                      <AlertDialogContent>
                        <AlertDialogHeader>
                          <AlertDialogTitle>Are you sure?</AlertDialogTitle>
                          <AlertDialogDescription>
                            This will permanently delete the pattern for &quot;{pattern.displayName}&quot;.
                            This action cannot be undone.
                          </AlertDialogDescription>
                        </AlertDialogHeader>
                        <AlertDialogFooter>
                          <AlertDialogCancel>Cancel</AlertDialogCancel>
                          <AlertDialogAction
                            onClick={() => handleDeletePattern(pattern.id)}
                            className="bg-destructive text-destructive-foreground"
                          >
                            Delete
                          </AlertDialogAction>
                        </AlertDialogFooter>
                      </AlertDialogContent>
                    </AlertDialog>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Card>
      )}

      {/* Pagination */}
      {patternsData && patternsData.totalCount > 20 && (
        <div className="flex justify-center gap-2">
          <Button
            variant="outline"
            disabled={page === 1}
            onClick={() => setPage(page - 1)}
          >
            Previous
          </Button>
          <span className="flex items-center px-4">
            Page {page} of {Math.ceil(patternsData.totalCount / 20)}
          </span>
          <Button
            variant="outline"
            disabled={page >= Math.ceil(patternsData.totalCount / 20)}
            onClick={() => setPage(page + 1)}
          >
            Next
          </Button>
        </div>
      )}
    </div>
  );
}
