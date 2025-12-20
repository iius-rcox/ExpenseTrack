# ExpenseTrack Frontend Review

## Critical Bug

### Sidebar Overlaps Main Content

**Impact:** Blocker | **Effort:** Low

The left navigation menu overlaps the main body content. The main content area needs proper margin or padding to account for the sidebar width.

**Recommended Fix: Fixed sidebar with margin offset**

This is the best approach for ExpenseTrack because:
- Sidebar stays visible while scrolling long transaction lists
- Main content scrolls independently (expected UX for dashboard apps)
- Simpler responsive handling (hide sidebar, replace with drawer on mobile)

**Step 1: Add CSS variable**

```css
/* globals.css */
:root {
  --sidebar-width: 16rem;
}
```

**Step 2: Apply layout**

```tsx
<aside className="fixed left-0 top-0 h-screen w-[var(--sidebar-width)] border-r bg-background">
  {/* nav items */}
</aside>
<main className="ml-[var(--sidebar-width)] min-h-screen p-6">
  {/* content */}
</main>
```

**Why CSS variable?** If you later add a collapsible sidebar (icon-only mode at 4rem), update one variable and both values stay in sync.

**Alternative (not recommended for this app):** Flex layout where sidebar scrolls with page. Better for document-style sites, not data-heavy apps where users live in the main content area.
```

---

## Recommendations (Most to Least Impactful)

---

### 1. TanStack Query for Server State

**Impact:** Critical | **Effort:** Medium

Replace `useEffect` + `useState` fetching patterns with TanStack Query. This is foundational for the matching workflow.

**Benefits:**
- Optimistic updates make matching feel instant
- Smart caching prevents redundant API calls during review workflow
- Built-in loading/error states
- Automatic background refetching

**Implementation:**

```tsx
// Transactions with smart caching
const { data: transactions, isLoading } = useQuery({
  queryKey: ['transactions', { status: 'unmatched', month }],
  queryFn: () => fetchTransactions({ status: 'unmatched', month }),
  staleTime: 30_000, // 30s before refetch
});

// Optimistic match updates
const matchMutation = useMutation({
  mutationFn: matchTransactionToReceipt,
  onMutate: async ({ transactionId, receiptId }) => {
    await queryClient.cancelQueries({ queryKey: ['transactions'] });
    // Optimistically remove from unmatched list
    queryClient.setQueryData(['transactions', { status: 'unmatched' }], 
      old => old?.filter(t => t.id !== transactionId)
    );
  },
  onSettled: () => queryClient.invalidateQueries({ queryKey: ['transactions'] }),
});
```

---

### 2. Skeleton Loading States

**Impact:** High | **Effort:** Low

Users scan long transaction lists. Proper loading states prevent layout shift and feel polished.

**Implementation:**

```tsx
function TransactionSkeleton() {
  return (
    <div className="flex items-center gap-4 p-4 border-b animate-pulse">
      <Skeleton className="h-10 w-10 rounded-full" />
      <div className="flex-1 space-y-2">
        <Skeleton className="h-4 w-3/4" />
        <Skeleton className="h-3 w-1/2" />
      </div>
      <Skeleton className="h-6 w-20" />
    </div>
  );
}

// In list component
{isLoading ? (
  Array.from({ length: 8 }).map((_, i) => (
    <TransactionSkeleton key={i} style={{ animationDelay: `${i * 50}ms` }} />
  ))
) : (
  transactions.map(t => <TransactionRow key={t.id} {...t} />)
)}
```

**Key detail:** Stagger animations with `animationDelay` for visual polish.

---

### 3. Composed Match Confirmation Dialog

**Impact:** High | **Effort:** Low

Wrap shadcn's AlertDialog for the matching workflow rather than rebuilding from scratch.

**Implementation:**

```tsx
// components/custom/MatchConfirmDialog.tsx
export function MatchConfirmDialog({ 
  transaction, 
  receipt, 
  onConfirm, 
  open, 
  onOpenChange 
}: MatchConfirmDialogProps) {
  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Confirm Match</AlertDialogTitle>
          <AlertDialogDescription>
            Match <span className="font-medium">{transaction.vendor}</span> 
            ({formatCurrency(transaction.amount)}) to the selected receipt?
          </AlertDialogDescription>
        </AlertDialogHeader>
        
        {/* Side-by-side preview */}
        <div className="grid grid-cols-2 gap-4 py-4">
          <TransactionPreview data={transaction} />
          <ReceiptPreview data={receipt} />
        </div>
        
        <AlertDialogFooter>
          <AlertDialogCancel>Cancel</AlertDialogCancel>
          <AlertDialogAction onClick={onConfirm}>
            Confirm Match
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
```

**Key detail:** Side-by-side preview lets users visually confirm before committing.

---

### 4. Status Badge Component

**Impact:** Medium | **Effort:** Low

Create a reusable badge mapping match status to semantic colors.

**Implementation:**

```tsx
// components/custom/StatusBadge.tsx
import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';

const statusConfig = {
  unmatched: { variant: 'outline', className: 'border-amber-500 text-amber-700' },
  matched: { variant: 'default', className: 'bg-emerald-600' },
  disputed: { variant: 'destructive', className: '' },
  pending: { variant: 'secondary', className: 'animate-pulse' },
} as const;

export function StatusBadge({ status }: { status: keyof typeof statusConfig }) {
  const config = statusConfig[status];
  return (
    <Badge variant={config.variant} className={cn(config.className)}>
      {status.charAt(0).toUpperCase() + status.slice(1)}
    </Badge>
  );
}
```

**Color rationale:**
- Amber for unmatched (needs attention)
- Emerald for matched (success)
- Red/destructive for disputed (problem)
- Muted pulse for pending (in progress)

---

### 5. Keyboard Navigation

**Impact:** Medium | **Effort:** Low

Keyboard shortcuts accelerate workflow when processing many transactions.

**Implementation:**

```tsx
// hooks/useMatchKeyboard.ts
export function useMatchKeyboard({ 
  onMatch, 
  onSkip, 
  onUndo 
}: MatchKeyboardActions) {
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.target instanceof HTMLInputElement) return;
      
      switch (e.key) {
        case 'm': onMatch?.(); break;
        case 's': onSkip?.(); break;
        case 'z': if (e.metaKey || e.ctrlKey) onUndo?.(); break;
      }
    };
    
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [onMatch, onSkip, onUndo]);
}
```

**UI hint:**

```tsx
<p className="text-xs text-muted-foreground">
  Press <kbd className="px-1.5 py-0.5 bg-muted rounded text-[10px]">M</kbd> to match, 
  <kbd className="px-1.5 py-0.5 bg-muted rounded text-[10px]">S</kbd> to skip
</p>
```

---

## Quick Wins

| Item | Effort | Notes |
|------|--------|-------|
| Distinctive typography | Low | Swap Inter for Space Grotesk or similar |
| Row hover states | Low | `hover:bg-muted/50 transition-colors` |
| Toast notifications | Low | Use shadcn `useToast` for match confirmations |
| Focus ring styling | Low | Consistent `focus-visible:ring-2 ring-offset-2` |

---

## Anti-Patterns to Avoid

- **Don't** modify files in `components/ui/` directly
- **Don't** use `useEffect` for data fetching
- **Don't** inline styles; use Tailwind + CSS variables
- **Don't** scatter uncoordinated animations
- **Don't** use generic system fonts

---

## Suggested File Structure

```
src/
├── components/
│   ├── ui/                    # shadcn auto-generated (don't edit)
│   └── custom/
│       ├── MatchConfirmDialog.tsx
│       ├── StatusBadge.tsx
│       ├── TransactionRow.tsx
│       ├── TransactionSkeleton.tsx
│       └── ReceiptPreview.tsx
├── hooks/
│   ├── useTransactions.ts     # TanStack Query wrapper
│   ├── useMatchMutation.ts
│   └── useMatchKeyboard.ts
├── lib/
│   └── utils.ts               # cn() helper
└── styles/
    └── globals.css            # CSS variables, base styles
```
