/**
 * CreateGroupDialog Component (Feature 028)
 *
 * Dialog for creating a new transaction group from selected transactions.
 * Features:
 * - Preview of selected transactions
 * - Auto-generated name (editable)
 * - Optional date override picker
 * - Validation (minimum 2 transactions)
 *
 * @see transaction.ts for CreateGroupDialogProps
 */

import { useState, useEffect, useMemo, useCallback } from 'react';
import { motion } from 'framer-motion';
import { Layers, Calendar, AlertCircle } from 'lucide-react';
import { cn, safeDisplayString } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover';
import { Calendar as CalendarPicker } from '@/components/ui/calendar';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Badge } from '@/components/ui/badge';
import type { CreateGroupDialogProps, TransactionView } from '@/types/transaction';

/**
 * Format currency amount for display
 */
function formatAmount(amount: number): string {
  const isNegative = amount < 0;
  const formatted = new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(Math.abs(amount));

  return isNegative ? `-${formatted}` : formatted;
}

/**
 * Format date for display
 */
function formatDate(date: Date): string {
  return date.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });
}

/**
 * Generate a suggested group name from transactions
 */
function generateGroupName(transactions: TransactionView[]): string {
  if (transactions.length === 0) return '';

  // Try to find a common merchant/description prefix
  const firstMerchant = safeDisplayString(transactions[0].merchant) || safeDisplayString(transactions[0].description);

  // Clean up the merchant name (remove numbers, special chars at end)
  const cleanName = firstMerchant
    .replace(/[#*0-9]+$/g, '')
    .replace(/\s+$/, '')
    .trim();

  // Use first word or two if the name is very long
  const shortName = cleanName.length > 20
    ? cleanName.split(/\s+/).slice(0, 2).join(' ')
    : cleanName;

  return `${shortName} (${transactions.length} charges)`;
}

/**
 * Get the maximum date from transactions
 */
function getMaxDate(transactions: TransactionView[]): Date {
  if (transactions.length === 0) return new Date();

  return transactions.reduce((max, tx) => {
    return tx.date > max ? tx.date : max;
  }, transactions[0].date);
}

/**
 * Calculate total amount from transactions
 */
function getTotalAmount(transactions: TransactionView[]): number {
  return transactions.reduce((sum, tx) => sum + tx.amount, 0);
}

/**
 * Dialog for creating a transaction group
 */
export function CreateGroupDialog({
  open,
  onOpenChange,
  transactions,
  onCreateGroup,
  isCreating = false,
}: CreateGroupDialogProps) {
  // Form state
  const [name, setName] = useState('');
  const [useDateOverride, setUseDateOverride] = useState(false);
  const [dateOverride, setDateOverride] = useState<Date | undefined>(undefined);
  const [isDatePickerOpen, setIsDatePickerOpen] = useState(false);

  // Computed values
  const suggestedName = useMemo(() => generateGroupName(transactions), [transactions]);
  const maxDate = useMemo(() => getMaxDate(transactions), [transactions]);
  const totalAmount = useMemo(() => getTotalAmount(transactions), [transactions]);

  // Reset form when dialog opens with new transactions
  useEffect(() => {
    if (open) {
      setName(suggestedName);
      setUseDateOverride(false);
      setDateOverride(maxDate);
    }
  }, [open, suggestedName, maxDate]);

  // Check for already-grouped transactions
  const alreadyGroupedTransactions = useMemo(() => {
    return transactions.filter((tx) => tx.groupId);
  }, [transactions]);

  // Validation
  const isValid = useMemo(() => {
    return (
      transactions.length >= 2 &&
      name.trim().length > 0 &&
      name.trim().length <= 100 &&
      alreadyGroupedTransactions.length === 0
    );
  }, [transactions.length, name, alreadyGroupedTransactions.length]);

  // Handle create
  const handleCreate = useCallback(() => {
    if (!isValid) return;

    const finalDate = useDateOverride && dateOverride ? dateOverride : undefined;
    onCreateGroup(name.trim(), finalDate);
  }, [isValid, name, useDateOverride, dateOverride, onCreateGroup]);

  // Handle date selection
  const handleDateSelect = useCallback((date: Date | undefined) => {
    setDateOverride(date);
    setIsDatePickerOpen(false);
  }, []);

  // Validation errors
  const validationErrors: string[] = [];
  if (transactions.length < 2) {
    validationErrors.push('At least 2 transactions are required to create a group');
  }
  if (alreadyGroupedTransactions.length > 0) {
    const count = alreadyGroupedTransactions.length;
    validationErrors.push(
      `${count} transaction${count === 1 ? ' is' : 's are'} already in a group. Remove ${count === 1 ? 'it' : 'them'} from selection.`
    );
  }
  if (name.trim().length > 100) {
    validationErrors.push('Group name cannot exceed 100 characters');
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Layers className="h-5 w-5" />
            Create Transaction Group
          </DialogTitle>
          <DialogDescription>
            Group {transactions.length} transactions into a single unit for receipt matching.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-4">
          {/* Validation Errors */}
          {validationErrors.length > 0 && (
            <Alert variant="destructive">
              <AlertCircle className="h-4 w-4" />
              <AlertDescription>
                {validationErrors.join('. ')}
              </AlertDescription>
            </Alert>
          )}

          {/* Transaction Preview */}
          <div className="space-y-2">
            <Label className="text-sm font-medium">Selected Transactions</Label>
            <div className="border rounded-md max-h-[200px] overflow-y-auto">
              {transactions.map((tx, index) => {
                const isAlreadyGrouped = !!tx.groupId;
                return (
                  <motion.div
                    key={tx.id}
                    initial={{ opacity: 0, y: -5 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ delay: index * 0.03 }}
                    className={cn(
                      'flex items-center justify-between px-3 py-2 text-sm',
                      index !== transactions.length - 1 && 'border-b',
                      isAlreadyGrouped && 'bg-destructive/10'
                    )}
                  >
                    <div className="flex-1 min-w-0">
                      <div className="font-medium truncate flex items-center gap-2">
                        {safeDisplayString(tx.merchant) || safeDisplayString(tx.description)}
                        {isAlreadyGrouped && (
                          <Badge variant="destructive" className="text-[10px] px-1.5 py-0">
                            In Group
                          </Badge>
                        )}
                      </div>
                      <div className="text-xs text-muted-foreground">
                        {formatDate(tx.date)}
                      </div>
                    </div>
                    <div className="font-medium tabular-nums ml-4">
                      {formatAmount(tx.amount)}
                    </div>
                  </motion.div>
                );
              })}
            </div>

            {/* Summary */}
            <div className="flex justify-between text-sm font-medium pt-1">
              <span className="text-muted-foreground">
                Total ({transactions.length} items)
              </span>
              <span>{formatAmount(totalAmount)}</span>
            </div>
          </div>

          {/* Group Name */}
          <div className="space-y-2">
            <Label htmlFor="group-name">Group Name</Label>
            <Input
              id="group-name"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Enter group name"
              disabled={isCreating}
            />
            <p className="text-xs text-muted-foreground">
              This name will appear in your transaction list
            </p>
          </div>

          {/* Date Override */}
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label>Override Display Date</Label>
                <p className="text-xs text-muted-foreground">
                  Default: {formatDate(maxDate)} (latest transaction)
                </p>
              </div>
              <Switch
                checked={useDateOverride}
                onCheckedChange={setUseDateOverride}
                disabled={isCreating}
              />
            </div>

            {useDateOverride && (
              <motion.div
                initial={{ opacity: 0, height: 0 }}
                animate={{ opacity: 1, height: 'auto' }}
                exit={{ opacity: 0, height: 0 }}
              >
                <Popover open={isDatePickerOpen} onOpenChange={setIsDatePickerOpen}>
                  <PopoverTrigger asChild>
                    <Button
                      variant="outline"
                      className={cn(
                        'w-full justify-start text-left font-normal',
                        !dateOverride && 'text-muted-foreground'
                      )}
                      disabled={isCreating}
                    >
                      <Calendar className="mr-2 h-4 w-4" />
                      {dateOverride ? formatDate(dateOverride) : 'Pick a date'}
                    </Button>
                  </PopoverTrigger>
                  <PopoverContent className="w-auto p-0" align="start">
                    <CalendarPicker
                      mode="single"
                      selected={dateOverride}
                      onSelect={handleDateSelect}
                      initialFocus
                    />
                  </PopoverContent>
                </Popover>
              </motion.div>
            )}
          </div>
        </div>

        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={isCreating}
          >
            Cancel
          </Button>
          <Button
            onClick={handleCreate}
            disabled={!isValid || isCreating}
          >
            {isCreating ? (
              <>
                <motion.div
                  className="h-4 w-4 border-2 border-current border-t-transparent rounded-full mr-2"
                  animate={{ rotate: 360 }}
                  transition={{ duration: 1, repeat: Infinity, ease: 'linear' }}
                />
                Creating...
              </>
            ) : (
              <>
                <Layers className="h-4 w-4 mr-2" />
                Create Group
              </>
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

export default CreateGroupDialog;
