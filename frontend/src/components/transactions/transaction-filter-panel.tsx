/**
 * TransactionFilterPanel Component (T054)
 *
 * Filter panel for the transaction grid with:
 * - Full-text search with debounce
 * - Date range picker
 * - Category multi-select
 * - Amount range inputs
 * - Match status filter
 * - Clear all filters button
 *
 * @see data-model.md Section 4.2 for props specification
 */

import { useState, useCallback, useEffect, useRef } from 'react';
import { useDebounce } from 'use-debounce';
import { motion, AnimatePresence } from 'framer-motion';
import {
  Search,
  Calendar,
  Tag,
  DollarSign,
  Link2,
  X,
  ChevronDown,
  ChevronUp,
  Filter,
  Lightbulb,
  Briefcase,
} from 'lucide-react';
import { FilterPresetSelector } from './filter-preset-selector';
import { QuickFilterChips } from './quick-filter-chips';
import { DEFAULT_TRANSACTION_FILTERS } from '@/types/transaction';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Label } from '@/components/ui/label';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { Checkbox } from '@/components/ui/checkbox';
import type {
  TransactionFilterPanelProps,
  TransactionMatchStatus,
  TransactionReimbursability,
} from '@/types/transaction';
import { getActiveFilterCount } from '@/hooks/queries/use-transactions';
import { safeDisplayString } from '@/lib/utils';

// Match status options for the filter
const MATCH_STATUS_OPTIONS: { value: TransactionMatchStatus; label: string }[] = [
  { value: 'matched', label: 'Matched' },
  { value: 'pending', label: 'Pending Review' },
  { value: 'unmatched', label: 'Unmatched' },
  { value: 'manual', label: 'Manual Match' },
];

// Reimbursability status options for the filter
const REIMBURSABILITY_OPTIONS: { value: TransactionReimbursability; label: string }[] = [
  { value: 'business', label: 'Business' },
  { value: 'personal', label: 'Personal' },
  { value: 'uncategorized', label: 'Uncategorized' },
];

/**
 * Date range preset options
 */
const DATE_PRESETS = [
  { label: 'Last 7 days', days: 7 },
  { label: 'Last 30 days', days: 30 },
  { label: 'Last 90 days', days: 90 },
  { label: 'This month', days: 'month' as const },
  { label: 'Last month', days: 'lastMonth' as const },
] as const;

function getPresetDateRange(preset: typeof DATE_PRESETS[number]['days']): {
  start: Date;
  end: Date;
} {
  const end = new Date();
  const start = new Date();

  if (preset === 'month') {
    start.setDate(1);
  } else if (preset === 'lastMonth') {
    start.setMonth(start.getMonth() - 1);
    start.setDate(1);
    end.setDate(0); // Last day of previous month
  } else {
    start.setDate(start.getDate() - preset);
  }

  return { start, end };
}

export function TransactionFilterPanel({
  filters,
  categories,
  tags,
  onChange,
  onReset,
}: TransactionFilterPanelProps) {
  const [showAdvanced, setShowAdvanced] = useState(false);
  const [localSearch, setLocalSearch] = useState(filters.search);

  // Debounce search input - useDebounce returns [debouncedValue, ...]
  const [debouncedSearch] = useDebounce(localSearch, 300);

  // Sync local search state when filters change from parent (e.g., preset load, URL change)
  useEffect(() => {
    if (filters.search !== localSearch) {
      setLocalSearch(filters.search);
    }
  }, [filters.search]); // eslint-disable-line react-hooks/exhaustive-deps

  // Update parent filters when debounced search changes
  // Use a ref to get current filters without adding to dependencies (avoids infinite loop)
  const filtersRef = useRef(filters);
  filtersRef.current = filters;

  useEffect(() => {
    if (debouncedSearch !== filtersRef.current.search) {
      onChange({ ...filtersRef.current, search: debouncedSearch });
    }
  }, [debouncedSearch, onChange]);

  const activeFilterCount = getActiveFilterCount(filters);

  // Handle search input change
  const handleSearchChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      setLocalSearch(e.target.value);
    },
    []
  );

  // Handle date range change
  const handleDatePreset = useCallback(
    (preset: typeof DATE_PRESETS[number]['days']) => {
      const { start, end } = getPresetDateRange(preset);
      onChange({
        ...filters,
        dateRange: { start, end },
      });
    },
    [filters, onChange]
  );

  // Handle category toggle
  const handleCategoryToggle = useCallback(
    (categoryId: string) => {
      const newCategories = filters.categories.includes(categoryId)
        ? filters.categories.filter((c) => c !== categoryId)
        : [...filters.categories, categoryId];
      onChange({ ...filters, categories: newCategories });
    },
    [filters, onChange]
  );

  // Handle match status toggle
  const handleMatchStatusToggle = useCallback(
    (status: TransactionMatchStatus) => {
      const newStatus = filters.matchStatus.includes(status)
        ? filters.matchStatus.filter((s) => s !== status)
        : [...filters.matchStatus, status];
      onChange({ ...filters, matchStatus: newStatus });
    },
    [filters, onChange]
  );

  // Handle reimbursability toggle
  const handleReimbursabilityToggle = useCallback(
    (status: TransactionReimbursability) => {
      const newStatus = filters.reimbursability.includes(status)
        ? filters.reimbursability.filter((s) => s !== status)
        : [...filters.reimbursability, status];
      onChange({ ...filters, reimbursability: newStatus });
    },
    [filters, onChange]
  );

  // Handle amount range change
  const handleAmountChange = useCallback(
    (field: 'min' | 'max', value: string) => {
      const numValue = value === '' ? null : parseFloat(value);
      onChange({
        ...filters,
        amountRange: {
          ...filters.amountRange,
          [field]: numValue,
        },
      });
    },
    [filters, onChange]
  );

  // Clear date range
  const handleClearDateRange = useCallback(() => {
    onChange({
      ...filters,
      dateRange: { start: null, end: null },
    });
  }, [filters, onChange]);

  // Toggle predictions filter
  const handlePredictionToggle = useCallback(() => {
    onChange({
      ...filters,
      hasPendingPrediction: !filters.hasPendingPrediction,
    });
  }, [filters, onChange]);

  return (
    <div className="space-y-4 w-full max-w-full overflow-hidden">
      {/* Search Bar Row */}
      <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-3">
        {/* Search Input */}
        <div className="relative flex-1 min-w-0">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            type="text"
            placeholder="Search transactions..."
            value={localSearch}
            onChange={handleSearchChange}
            className="pl-10"
          />
          {localSearch && (
            <button
              onClick={() => {
                setLocalSearch('');
                onChange({ ...filters, search: '' });
              }}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
            >
              <X className="h-4 w-4" />
            </button>
          )}
        </div>

        {/* Quick Filters */}
        <div className="flex items-center gap-2 flex-wrap">
          {/* Pending Predictions Filter (Feature 023) */}
          <Button
            variant={filters.hasPendingPrediction ? 'default' : 'outline'}
            size="sm"
            onClick={handlePredictionToggle}
            className="gap-2"
            title="Show transactions with pending expense predictions awaiting review"
          >
            <Lightbulb className="h-4 w-4" />
            <span className="hidden sm:inline">Pending Review</span>
          </Button>

          {/* Date Range Dropdown */}
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant={filters.dateRange.start ? 'secondary' : 'outline'}
                size="sm"
                className="gap-2"
              >
                <Calendar className="h-4 w-4" />
                <span className="hidden sm:inline">Date</span>
                {filters.dateRange.start && (
                  <Badge variant="secondary" className="ml-1">
                    1
                  </Badge>
                )}
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent className="w-56" align="end">
              <div className="space-y-3 p-2">
                <div className="font-medium">Date Range</div>
                <div className="space-y-1">
                  {DATE_PRESETS.map((preset) => (
                    <Button
                      key={preset.label}
                      variant="ghost"
                      size="sm"
                      className="w-full justify-start"
                      onClick={() => handleDatePreset(preset.days)}
                    >
                      {preset.label}
                    </Button>
                  ))}
                </div>
                {filters.dateRange.start && (
                  <Button
                    variant="outline"
                    size="sm"
                    className="w-full"
                    onClick={handleClearDateRange}
                  >
                    Clear dates
                  </Button>
                )}
              </div>
            </DropdownMenuContent>
          </DropdownMenu>

          {/* Category Filter Dropdown */}
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant={filters.categories.length > 0 ? 'secondary' : 'outline'}
                size="sm"
                className="gap-2"
              >
                <Tag className="h-4 w-4" />
                <span className="hidden sm:inline">Category</span>
                {filters.categories.length > 0 && (
                  <Badge variant="secondary" className="ml-1">
                    {filters.categories.length}
                  </Badge>
                )}
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent className="w-56" align="end">
              <div className="space-y-3 p-2">
                <div className="font-medium">Categories</div>
                <div className="max-h-48 space-y-2 overflow-y-auto">
                  {categories.map((category) => (
                    <label
                      key={category.id}
                      className="flex cursor-pointer items-center gap-2"
                    >
                      <Checkbox
                        checked={filters.categories.includes(category.id)}
                        onCheckedChange={() => handleCategoryToggle(category.id)}
                      />
                      <span className="text-sm">{safeDisplayString(category.name, 'Unknown')}</span>
                    </label>
                  ))}
                </div>
              </div>
            </DropdownMenuContent>
          </DropdownMenu>

          {/* Match Status Filter */}
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant={filters.matchStatus.length > 0 ? 'secondary' : 'outline'}
                size="sm"
                className="gap-2"
              >
                <Link2 className="h-4 w-4" />
                <span className="hidden sm:inline">Status</span>
                {filters.matchStatus.length > 0 && (
                  <Badge variant="secondary" className="ml-1">
                    {filters.matchStatus.length}
                  </Badge>
                )}
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent className="w-48" align="end">
              <div className="space-y-3 p-2">
                <div className="font-medium">Match Status</div>
                <div className="space-y-2">
                  {MATCH_STATUS_OPTIONS.map((option) => (
                    <label
                      key={option.value}
                      className="flex cursor-pointer items-center gap-2"
                    >
                      <Checkbox
                        checked={filters.matchStatus.includes(option.value)}
                        onCheckedChange={() =>
                          handleMatchStatusToggle(option.value)
                        }
                      />
                      <span className="text-sm">{option.label}</span>
                    </label>
                  ))}
                </div>
              </div>
            </DropdownMenuContent>
          </DropdownMenu>

          {/* Reimbursability Filter */}
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant={filters.reimbursability.length > 0 ? 'secondary' : 'outline'}
                size="sm"
                className="gap-2"
              >
                <Briefcase className="h-4 w-4" />
                <span className="hidden sm:inline">Type</span>
                {filters.reimbursability.length > 0 && (
                  <Badge variant="secondary" className="ml-1">
                    {filters.reimbursability.length}
                  </Badge>
                )}
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent className="w-48" align="end">
              <div className="space-y-3 p-2">
                <div className="font-medium">Type</div>
                <div className="space-y-2">
                  {REIMBURSABILITY_OPTIONS.map((option) => (
                    <label
                      key={option.value}
                      className="flex cursor-pointer items-center gap-2"
                    >
                      <Checkbox
                        checked={filters.reimbursability.includes(option.value)}
                        onCheckedChange={() =>
                          handleReimbursabilityToggle(option.value)
                        }
                      />
                      <span className="text-sm">{option.label}</span>
                    </label>
                  ))}
                </div>
              </div>
            </DropdownMenuContent>
          </DropdownMenu>

          {/* Advanced Filters Toggle */}
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setShowAdvanced(!showAdvanced)}
            className="gap-2"
          >
            <Filter className="h-4 w-4" />
            {showAdvanced ? (
              <ChevronUp className="h-4 w-4" />
            ) : (
              <ChevronDown className="h-4 w-4" />
            )}
          </Button>

          {/* Filter Presets (save, load, delete filter configurations) */}
          <FilterPresetSelector
            filters={filters}
            onLoadPreset={onChange}
          />

          {/* Clear All Filters */}
          {activeFilterCount > 0 && (
            <Button
              variant="ghost"
              size="sm"
              onClick={onReset}
              className="gap-2 text-destructive hover:text-destructive"
            >
              <X className="h-4 w-4" />
              Clear ({activeFilterCount})
            </Button>
          )}
        </div>
      </div>

      {/* Quick Filter Chips */}
      <QuickFilterChips
        filters={filters}
        onApplyFilter={onChange}
        defaultFilters={DEFAULT_TRANSACTION_FILTERS}
      />

      {/* Advanced Filters Panel */}
      <AnimatePresence>
        {showAdvanced && (
          <motion.div
            initial={{ height: 0, opacity: 0 }}
            animate={{ height: 'auto', opacity: 1 }}
            exit={{ height: 0, opacity: 0 }}
            transition={{ duration: 0.2 }}
            className="overflow-hidden"
          >
            <div className="rounded-lg border bg-muted/30 p-4">
              <div className="grid gap-4 sm:grid-cols-2 md:grid-cols-4">
                {/* Amount Range */}
                <div className="space-y-2">
                  <Label className="text-sm font-medium">Amount Range</Label>
                  <div className="flex items-center gap-2">
                    <div className="relative flex-1">
                      <DollarSign className="absolute left-2 top-1/2 h-3 w-3 -translate-y-1/2 text-muted-foreground" />
                      <Input
                        type="number"
                        placeholder="Min"
                        value={filters.amountRange.min ?? ''}
                        onChange={(e) => handleAmountChange('min', e.target.value)}
                        className="h-9 pl-6"
                      />
                    </div>
                    <span className="text-muted-foreground">-</span>
                    <div className="relative flex-1">
                      <DollarSign className="absolute left-2 top-1/2 h-3 w-3 -translate-y-1/2 text-muted-foreground" />
                      <Input
                        type="number"
                        placeholder="Max"
                        value={filters.amountRange.max ?? ''}
                        onChange={(e) => handleAmountChange('max', e.target.value)}
                        className="h-9 pl-6"
                      />
                    </div>
                  </div>
                </div>

                {/* Custom Date Range */}
                <div className="space-y-2">
                  <Label className="text-sm font-medium">Start Date</Label>
                  <Input
                    type="date"
                    value={
                      filters.dateRange.start
                        ? filters.dateRange.start.toISOString().split('T')[0]
                        : ''
                    }
                    onChange={(e) => {
                      const date = e.target.value ? new Date(e.target.value) : null;
                      onChange({
                        ...filters,
                        dateRange: { ...filters.dateRange, start: date },
                      });
                    }}
                    className="h-9"
                  />
                </div>

                <div className="space-y-2">
                  <Label className="text-sm font-medium">End Date</Label>
                  <Input
                    type="date"
                    value={
                      filters.dateRange.end
                        ? filters.dateRange.end.toISOString().split('T')[0]
                        : ''
                    }
                    onChange={(e) => {
                      const date = e.target.value ? new Date(e.target.value) : null;
                      onChange({
                        ...filters,
                        dateRange: { ...filters.dateRange, end: date },
                      });
                    }}
                    className="h-9"
                  />
                </div>

                {/* Tags (if available) */}
                {tags.length > 0 && (
                  <div className="space-y-2">
                    <Label className="text-sm font-medium">Tags</Label>
                    <div className="flex flex-wrap gap-1">
                      {tags.slice(0, 5).map((tag) => (
                        <Button
                          key={safeDisplayString(tag, `tag-${Math.random()}`)}
                          variant={filters.tags.includes(tag) ? 'secondary' : 'outline'}
                          size="sm"
                          className="h-7 text-xs"
                          onClick={() => {
                            onChange({
                              ...filters,
                              tags: filters.tags.includes(tag)
                                ? filters.tags.filter((t) => t !== tag)
                                : [...filters.tags, tag],
                            });
                          }}
                        >
                          {safeDisplayString(tag, 'tag')}
                        </Button>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            </div>
          </motion.div>
        )}
      </AnimatePresence>

      {/* Active Filters Display */}
      {activeFilterCount > 0 && (
        <div className="flex flex-wrap gap-2">
          {filters.hasPendingPrediction && (
            <Badge variant="default" className="gap-1 bg-amber-500">
              <Lightbulb className="h-3 w-3" />
              Pending Review
              <button
                onClick={handlePredictionToggle}
                className="ml-1 hover:text-white/80"
              >
                <X className="h-3 w-3" />
              </button>
            </Badge>
          )}
          {safeDisplayString(filters.search) && (
            <Badge variant="secondary" className="gap-1">
              Search: &quot;{safeDisplayString(filters.search)}&quot;
              <button
                onClick={() => {
                  setLocalSearch('');
                  onChange({ ...filters, search: '' });
                }}
                className="ml-1 hover:text-destructive"
              >
                <X className="h-3 w-3" />
              </button>
            </Badge>
          )}
          {filters.dateRange.start && (
            <Badge variant="secondary" className="gap-1">
              Date: {filters.dateRange.start.toLocaleDateString()} -{' '}
              {filters.dateRange.end?.toLocaleDateString() || 'Now'}
              <button
                onClick={handleClearDateRange}
                className="ml-1 hover:text-destructive"
              >
                <X className="h-3 w-3" />
              </button>
            </Badge>
          )}
          {filters.categories.map((categoryId) => {
            const category = categories.find((c) => c.id === categoryId);
            return (
              <Badge key={categoryId} variant="secondary" className="gap-1">
                {safeDisplayString(category?.name) || categoryId}
                <button
                  onClick={() => handleCategoryToggle(categoryId)}
                  className="ml-1 hover:text-destructive"
                >
                  <X className="h-3 w-3" />
                </button>
              </Badge>
            );
          })}
          {filters.matchStatus.map((status) => {
            const option = MATCH_STATUS_OPTIONS.find((o) => o.value === status);
            return (
              <Badge key={status} variant="secondary" className="gap-1">
                {option?.label || status}
                <button
                  onClick={() => handleMatchStatusToggle(status)}
                  className="ml-1 hover:text-destructive"
                >
                  <X className="h-3 w-3" />
                </button>
              </Badge>
            );
          })}
          {filters.reimbursability.map((status) => {
            const option = REIMBURSABILITY_OPTIONS.find((o) => o.value === status);
            return (
              <Badge key={status} variant="secondary" className="gap-1">
                <Briefcase className="h-3 w-3" />
                {option?.label || status}
                <button
                  onClick={() => handleReimbursabilityToggle(status)}
                  className="ml-1 hover:text-destructive"
                >
                  <X className="h-3 w-3" />
                </button>
              </Badge>
            );
          })}
        </div>
      )}
    </div>
  );
}

export default TransactionFilterPanel;
