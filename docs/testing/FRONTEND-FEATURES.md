# ExpenseFlow Frontend Features Documentation

This document provides a comprehensive reference for all ExpenseFlow frontend features and routes. It is designed to support testing, integration, and future automation efforts.

**Technology Stack**:
- TypeScript 5.7+ with React 18.3+
- TanStack Router (file-based routing)
- TanStack Query (data fetching)
- Tailwind CSS 4.x with shadcn/ui components
- Framer Motion (animations)
- Microsoft Entra ID (authentication)

---

## Table of Contents

1. [Application Structure](#application-structure)
2. [Routes Overview](#routes-overview)
3. [Dashboard](#dashboard)
4. [Receipts](#receipts)
5. [Transactions](#transactions)
6. [Statements](#statements)
7. [Matching](#matching)
8. [Reports](#reports)
9. [Analytics](#analytics)
10. [Settings](#settings)
11. [Design System](#design-system)
12. [Theme System](#theme-system)

---

## Application Structure

### Root Layout (`__root.tsx`)

The root route provides:
- **ThemeProvider**: Dual theme system (light/dark with system detection)
- **ErrorBoundary**: Global error handling with graceful fallback
- **Toaster**: Toast notifications via Sonner
- **DevTools**: TanStack Router and Query devtools (development only)

### Authenticated Layout (`_authenticated.tsx`)

Protected routes that require authentication:
- Redirects unauthenticated users to `/login`
- Provides the AppShell layout (sidebar navigation, header)
- Preserves return URL for post-login redirect

---

## Routes Overview

| Route | Component | Description |
|-------|-----------|-------------|
| `/` | Index | Landing/redirect based on auth state |
| `/login` | Login | Microsoft Entra ID authentication |
| `/dashboard` | Dashboard | Main command center view |
| `/receipts` | Receipts | Receipt list and upload |
| `/receipts/:receiptId` | Receipt Detail | Single receipt view |
| `/transactions` | Transactions | Transaction list |
| `/transactions/:transactionId` | Transaction Detail | Single transaction view |
| `/statements` | Statements | Statement import wizard |
| `/matching` | Matching | Receipt-transaction matching |
| `/reports` | Reports | Expense report list |
| `/reports/:reportId` | Report Detail | Single report view |
| `/analytics` | Analytics | Spending analytics dashboard |
| `/settings` | Settings | User preferences |

---

## Dashboard

**Route**: `/_authenticated/dashboard`
**Task ID**: T032

### Features

- **Real-time Metrics**: 30-second auto-refresh polling
  - Pending receipts count
  - Unmatched transactions count
  - Pending matches count
  - Draft reports count
  - Monthly spending with trend

- **Activity Stream**: Recent expense events
  - Receipt uploads
  - Transaction imports
  - Match confirmations
  - Report submissions

- **Action Queue**: Priority-sorted pending items
  - Failed receipts requiring retry
  - Proposed matches awaiting confirmation
  - Reports needing review

- **Category Breakdown**: Visual spending distribution

### Responsive Behavior

| Breakpoint | Layout |
|------------|--------|
| Desktop (lg+) | Multi-column grid |
| Tablet (md) | Stacked sections |
| Mobile (sm) | Compact summary bar + scrollable sections + FAB |

### Mobile-Specific Features

- **Quick Action FAB**: Fixed bottom-right button for receipt upload
- **Swipeable Cards**: Touch-friendly interactions

---

## Receipts

**Route**: `/_authenticated/receipts`
**Detail Route**: `/_authenticated/receipts/:receiptId`

### List View Features

- **Upload Area**: Drag-and-drop or click to upload
  - Max 20 files per batch
  - Max 25MB per file
  - Formats: JPEG, PNG, HEIC, PDF

- **Receipt Grid/List**: Toggle between views
- **Status Filters**: Uploaded, Processing, Processed, Error, Matched, Unmatched
- **Date Range Filter**: From/to date selection
- **Pagination**: 20 items per page (configurable to 100)

### Detail View Features

- **Full-size Image**: Zoomable receipt image with SAS URL
- **Extracted Data**: Vendor, date, amount, currency
- **Line Items**: Individual items extracted via OCR
- **Confidence Scores**: Per-field extraction confidence
- **Actions**:
  - Edit extracted data
  - Retry processing (for failed receipts)
  - Delete receipt
  - Download original

### Status States

| Status | Badge Color | Description |
|--------|-------------|-------------|
| Uploaded | Blue | Awaiting processing |
| Processing | Yellow | OCR in progress |
| Processed | Green | Ready for matching |
| Error | Red | Processing failed |
| Matched | Purple | Linked to transaction |
| Unmatched | Gray | No matching transaction |

---

## Transactions

**Route**: `/_authenticated/transactions`
**Detail Route**: `/_authenticated/transactions/:transactionId`

### List View Features

- **Transaction Table**: Sortable columns
- **Search**: Full-text search on description
- **Filters**:
  - Date range (start/end)
  - Matched status (true/false)
  - Import batch
- **Pagination**: 50 items per page (configurable to 200)

### Detail View Features

- **Transaction Info**: Date, amount, description
- **Original Description**: Raw bank/card description
- **Import Source**: Statement filename and batch
- **Linked Receipt**: If matched
- **Categorization**: GL code and department

---

## Statements

**Route**: `/_authenticated/statements`

### Import Wizard

A multi-step wizard for importing bank/credit card statements:

#### Step 1: Upload
- Drag-and-drop or click to upload
- Supported formats: CSV, XLSX, XLS
- Max size: 10MB

#### Step 2: Column Mapping
- Automatic mapping via fingerprint matching (Tier 1)
- AI-powered inference for new formats (Tier 3)
- Manual column assignment
- Sample row preview

#### Step 3: Review
- Preview of parsed transactions
- Date format selection
- Amount sign convention (positive/negative charges)
- Fingerprint naming (optional)

#### Step 4: Import
- Progress indicator
- Import statistics:
  - Imported count
  - Skipped count
  - Duplicate count
- Transaction preview

### Import History

- List of past imports
- Source name (fingerprint or "AI Detected")
- Tier used (1, 2, or 3)
- Transaction count
- Import date

---

## Matching

**Route**: `/_authenticated/matching`

### Features

- **Auto-Match**: Trigger matching algorithm
- **Proposal Queue**: Review suggested matches
- **Manual Match**: Link receipt to transaction manually
- **Confidence Scores**: Visual score breakdown
  - Amount match score
  - Date match score
  - Vendor match score

### Match Proposal Card

- Side-by-side receipt and transaction
- Confidence meter with score breakdown
- Actions: Confirm, Reject
- Vendor alias configuration (optional)

### Matching Statistics

- Matched count
- Proposed count
- Unmatched receipts
- Unmatched transactions
- Auto-match rate
- Average confidence

---

## Reports

**Route**: `/_authenticated/reports`
**Detail Route**: `/_authenticated/reports/:reportId`

### List View Features

- **Status Filters**: Draft, Submitted, Approved, Rejected
- **Period Filter**: Month selection
- **Pagination**: 20 items per page

### Detail View Features

- **Report Summary**: Period, total, line count
- **Expense Lines Table**: Editable line items
  - Date, description, amount
  - GL code selection
  - Department selection
  - Receipt link
  - Justification text

- **Export Actions**:
  - Download as Excel
  - Download consolidated PDF (all receipts)

### Status States

| Status | Badge Color | Actions |
|--------|-------------|---------|
| Draft | Yellow | Edit, Delete |
| Submitted | Blue | View only |
| Approved | Green | View, Export |
| Rejected | Red | View, Edit, Resubmit |

---

## Analytics

**Route**: `/_authenticated/analytics`
**Task ID**: T084

### Overview Tab

- **Summary Metrics**: Current/previous period, change %, subscription count
- **Spending Trend Chart**: Line/area chart with granularity options
- **Category Distribution**: Donut chart with breakdown
- **Top Merchants**: Ranked list with amounts
- **Cache Performance**: Tier usage and cost savings

### Categories Tab

- **Full Category Breakdown**: Up to 12 categories
- **Comparison View**: Optional period comparison
- **Pie/Bar/Donut Options**: Visual toggle

### Merchants Tab

- **Top 20 Merchants**: With trend indicators
- **New Merchants**: Vendors first seen this period
- **Spending Patterns**: Amount and frequency

### Subscriptions Tab

- **Detected Subscriptions**: AI-identified recurring payments
- **Confidence Levels**: High, Medium, Low
- **Frequency Detection**: Monthly, Weekly, Yearly
- **Category Grouping**: Organized by expense type
- **Acknowledge Action**: Mark as reviewed

### Date Range Controls

- **Quick Presets**: This Month, Last Month, Last 30/60/90 Days, This Year
- **Custom Range**: Date picker with max 365 days
- **Comparison Toggle**: Enable period-over-period comparison
- **Granularity**: Day, Week, Month

### Monthly Changes

- **New Vendors**: First-time vendors this period
- **Missing Recurring**: Expected vendors not seen
- **Significant Changes**: Notable spending changes (>20%)

---

## Settings

**Route**: `/_authenticated/settings`

### User Preferences

- **Theme Selection**: Light, Dark, System (auto-detect)
- **Default Department**: Pre-selected department for new expenses
- **Default Project**: Pre-selected project for new expenses

### Account Info (Read-only)

- Email
- Display name
- Member since

---

## Design System

ExpenseFlow uses a "Refined Intelligence" design system with shadcn/ui components.

### Core Components

| Component | Usage |
|-----------|-------|
| Button | Primary/secondary/outline/ghost variants |
| Card | Content containers with header/content/footer |
| Table | Data tables with sorting and selection |
| Form | Form inputs with validation |
| Dialog | Modal dialogs for actions |
| Sheet | Slide-out panels (mobile navigation) |
| Tabs | Content organization |
| Badge | Status indicators |
| Skeleton | Loading placeholders |

### Animation Library

Framer Motion provides smooth transitions:

```typescript
// Fade in animation
const fadeIn = {
  hidden: { opacity: 0 },
  visible: { opacity: 1, transition: { duration: 0.3 } }
}

// Stagger children
const staggerContainer = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: { staggerChildren: 0.1 }
  }
}

const staggerChild = {
  hidden: { opacity: 0, y: 20 },
  visible: { opacity: 1, y: 0 }
}
```

### Color System

| Color | CSS Variable | Usage |
|-------|--------------|-------|
| Primary | `--primary` | Buttons, links, focus |
| Secondary | `--secondary` | Secondary actions |
| Destructive | `--destructive` | Errors, delete actions |
| Muted | `--muted` | Subtle backgrounds |
| Accent | `--accent` | Highlights |

---

## Theme System

ExpenseFlow supports a dual theme system (Feature 015).

### Implementation

- **next-themes**: Provider and hook integration
- **CSS Variables**: Dynamic color switching
- **Local Storage**: Preference persistence

### Theme Values

| Value | Description |
|-------|-------------|
| `light` | Light mode colors |
| `dark` | Dark mode colors |
| `system` | Auto-detect from OS preference |

### Usage

```typescript
import { useTheme } from '@/providers/theme-provider'

function ThemeToggle() {
  const { theme, setTheme } = useTheme()

  return (
    <Select value={theme} onValueChange={setTheme}>
      <SelectItem value="light">Light</SelectItem>
      <SelectItem value="dark">Dark</SelectItem>
      <SelectItem value="system">System</SelectItem>
    </Select>
  )
}
```

---

## Testing Considerations

### Component Testing

1. **Responsive Behavior**: Test at sm, md, lg, xl breakpoints
2. **Theme Switching**: Verify both light and dark modes
3. **Loading States**: Check skeleton placeholders
4. **Error States**: Verify error boundaries and fallbacks
5. **Empty States**: Test with no data scenarios

### E2E Testing Scenarios

| Feature | Test Case |
|---------|-----------|
| Receipt Upload | Upload single/multiple files |
| Statement Import | Complete wizard flow |
| Matching | Auto-match and manual match |
| Analytics | Date range selection, tab switching |
| Reports | Create draft, edit lines, export |
| Settings | Theme change, preference save |

### Accessibility Testing

- Keyboard navigation
- Screen reader compatibility
- Color contrast (WCAG AA)
- Focus indicators
- ARIA labels

---

## Version History

| Date | Version | Changes |
|------|---------|---------|
| 2025-12-31 | 1.16.0 | Added Analytics Dashboard (Feature 019) |
| 2025-12-29 | 1.15.0 | Added User Preferences (Feature 016) |
| 2025-12-27 | 1.14.0 | Added Dual Theme System (Feature 015) |
| 2025-12-25 | 1.13.0 | Added Frontend Redesign (Feature 013) |
| 2025-12-23 | 1.12.0 | Initial Unified Frontend (Feature 011) |
