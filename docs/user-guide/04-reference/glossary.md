# Glossary

← [Back to Reference](./README.md)

Definitions for ExpenseFlow-specific terminology. Terms are listed alphabetically.

---

### Action Queue

A prioritized list of pending tasks displayed on your dashboard. Items are sorted by urgency (high/medium/low) and include tasks like reviewing match proposals, correcting extractions, and approving reports.

**Example**: "You have 5 items in your action queue, including 3 match proposals to review."

**See also**: [Activity Feed](#activity-feed), [Dashboard Overview](../01-getting-started/dashboard-overview.md)

---

### Activity Feed

A chronological list of recent expense events shown on the dashboard. Also called the "Expense Stream," it displays uploads, matches, categorizations, and report changes as they occur.

**Example**: "The activity feed shows that a receipt was uploaded 2 hours ago and matched 30 minutes ago."

**See also**: [Action Queue](#action-queue)

---

### AI Extraction

The automated process of reading receipt images to identify the vendor name, transaction date, total amount, and other details. ExpenseFlow uses AI to analyze uploaded receipts and extract key fields.

**Example**: "AI extraction identified the vendor as 'Starbucks' with 95% confidence."

**See also**: [Confidence Score](#confidence-score)

---

### Batch Approval

The process of confirming multiple match proposals at once using a confidence threshold. Instead of reviewing each match individually, you can approve all matches above a certain confidence level.

**Example**: "Use batch approval to confirm all matches with 85% confidence or higher."

**See also**: [Match Proposal](#match-proposal), [Confidence Score](#confidence-score)

---

### Column Mapping

The process of assigning columns from your bank statement file (CSV or Excel) to standard transaction fields like Date, Amount, and Description. This tells ExpenseFlow how to interpret your bank's specific format.

**Example**: "During column mapping, assign the 'Trans Date' column to the Date field."

**See also**: [Fingerprint](#fingerprint)

---

### Confidence Score

A percentage (0-100%) indicating how certain the AI is about extracted data or a proposed match. Displayed with color coding:
- **Green (90%+)**: High confidence, likely correct
- **Amber (70-89%)**: Medium confidence, review recommended
- **Red (below 70%)**: Low confidence, manual review required

**Example**: "The vendor name has a 75% confidence score (amber), so you should verify it."

**See also**: [AI Extraction](#ai-extraction), [Match Proposal](#match-proposal)

---

### Cost Center

A department or project to which expenses are allocated for accounting purposes. Expenses can be assigned to one cost center or split across multiple cost centers.

**Example**: "Allocate this expense to the Marketing cost center."

**See also**: [Department](#department), [Project](#project), [Split Pattern](#split-pattern)

---

### Department

An organizational unit synced from your company's Vista ERP system. Departments are used for expense allocation and reporting.

**Example**: "Select 'Engineering' as the department for this software subscription."

**See also**: [Project](#project), [Cost Center](#cost-center)

---

### Expense Report

A collection of matched expenses grouped together for submission and approval. Reports are typically created monthly and contain all expenses for that period.

**Example**: "Your December 2025 expense report contains 47 matched expenses totaling $3,450."

**See also**: [Match](#match)

---

### Fingerprint

A saved column mapping template for repeated statement imports from the same bank or credit card. Once you map columns for a bank's format, save it as a fingerprint to auto-apply the mapping on future imports.

**Example**: "Your 'Chase Business Visa' fingerprint automatically maps the date, amount, and description columns."

**See also**: [Column Mapping](#column-mapping)

---

### GL Code

General Ledger account code used for expense categorization and accounting. GL codes determine how expenses are classified in your organization's financial records.

**Example**: "ExpenseFlow suggests GL code '6420 - Travel Meals' for this restaurant receipt."

**See also**: [Categorization](../02-daily-use/categorization/gl-suggestions.md)

---

### List Mode

A card-based interface showing all match proposals at once, allowing you to scan multiple matches quickly. Contrast with Review Mode, which shows one match at a time in a split-pane view.

**Example**: "Switch to List Mode when you want to see all pending matches on one screen."

**See also**: [Review Mode](#review-mode)

---

### Manual Matching

The process of creating a link between a receipt and a transaction yourself, rather than using AI-proposed matches. Use manual matching when the AI doesn't suggest the correct pairing.

**Example**: "Use manual matching to link this receipt to the correct transaction from last week."

**See also**: [Match](#match), [Match Proposal](#match-proposal)

---

### Match

A confirmed link between a receipt and a bank transaction. Once matched, the receipt image and transaction data are connected for reporting.

**Example**: "This coffee shop receipt is matched to the $4.75 charge on your credit card."

**See also**: [Match Proposal](#match-proposal), [Manual Matching](#manual-matching)

---

### Match Proposal

An AI-suggested link between a receipt and transaction that awaits your confirmation. The system proposes matches based on amount, date proximity, vendor similarity, and other factors.

**Example**: "ExpenseFlow proposes matching this $52.30 receipt to a $52.30 transaction with 92% confidence."

**See also**: [Match](#match), [Confidence Score](#confidence-score)

---

### Project

A work assignment synced from your company's Vista ERP system. Projects are used for expense allocation, especially for job-costed or contract-based work.

**Example**: "Allocate this expense to Project 2025-0042 (Office Renovation)."

**See also**: [Department](#department), [Cost Center](#cost-center)

---

### Receipt

A digital image or PDF of a purchase document uploaded to ExpenseFlow for expense tracking. Receipts contain vendor information, dates, and amounts that the AI extracts automatically.

**Example**: "Upload a receipt by dragging the image onto the upload zone."

**See also**: [AI Extraction](#ai-extraction), [Transaction](#transaction)

---

### Review Mode

A keyboard-driven interface for efficiently reviewing match proposals one at a time. Shows a split-pane with the receipt on one side and transaction details on the other.

**Example**: "In Review Mode, press 'A' to approve or 'R' to reject each match."

**See also**: [List Mode](#list-mode), [Keyboard Shortcuts](./keyboard-shortcuts.md)

---

### Split Pattern

A reusable allocation template for dividing expenses across multiple cost centers. Save commonly used splits (like 50/50 between two departments) as patterns for quick application.

**Example**: "Apply the 'Marketing/Sales Split' pattern to divide this trade show expense evenly."

**See also**: [Cost Center](#cost-center), [Expense Splitting](../02-daily-use/splitting/expense-splitting.md)

---

### Subscription

A recurring charge that is detected or manually tracked in the system. ExpenseFlow monitors your transactions for regular patterns like monthly software fees.

**Example**: "ExpenseFlow detected a $14.99 monthly subscription to Spotify."

**See also**: [Subscription Alert](#subscription-alert)

---

### Subscription Alert

A notification about a subscription that requires attention. Alerts are triggered when:
- An expected monthly charge is missing
- An unexpected new recurring charge is detected
- A subscription amount has changed

**Example**: "Alert: Your expected $9.99 Netflix charge is missing this month."

**See also**: [Subscription](#subscription)

---

### Swipe Action

A mobile gesture that reveals action buttons on transaction or receipt rows. Swipe left to see delete/reject options; swipe right to see edit/approve options.

**Example**: "Swipe right on a match proposal to quickly approve it."

**See also**: [Mobile](./mobile.md)

---

### Transaction

A financial record imported from a bank or credit card statement. Transactions contain date, amount, and description information from your bank.

**Example**: "Your statement import added 145 transactions from October."

**See also**: [Receipt](#receipt), [Match](#match)

---

### Travel Period

A defined date range grouping expenses from a business trip. Travel periods help organize trip-related expenses for clearer reporting.

**Example**: "Create a travel period for your Chicago conference from Dec 3-5."

**See also**: [Travel Periods](../03-monthly-close/travel/periods.md)

---

### Undo Stack

The history of field edits that allows you to reverse corrections on AI-extracted fields. You can undo up to 10 previous edits on a single receipt.

**Example**: "Use Ctrl+Z to undo your last edit to the vendor name."

**See also**: [AI Extraction](#ai-extraction)

---

### Vista ERP

Viewpoint Vista, the enterprise resource planning system that provides reference data for ExpenseFlow. Department and project information is synced from Vista.

**Example**: "Departments are synced daily from Vista ERP."

**See also**: [Department](#department), [Project](#project)

---

## Quick Reference

| Term | One-Line Definition |
|------|---------------------|
| Action Queue | Prioritized list of pending tasks on dashboard |
| Activity Feed | Chronological list of recent expense events |
| AI Extraction | Automated reading of receipt images |
| Batch Approval | Approve multiple matches by threshold |
| Column Mapping | Assign bank columns to standard fields |
| Confidence Score | AI certainty percentage (0-100%) |
| Cost Center | Allocation target (department or project) |
| Department | Organizational unit from Vista ERP |
| Expense Report | Collection of matched expenses for submission |
| Fingerprint | Saved column mapping template |
| GL Code | General Ledger categorization code |
| List Mode | Card-based view of all matches |
| Manual Matching | User-created receipt-transaction link |
| Match | Confirmed receipt-transaction link |
| Match Proposal | AI-suggested receipt-transaction link |
| Project | Work assignment from Vista ERP |
| Receipt | Uploaded purchase document image |
| Review Mode | One-at-a-time match review interface |
| Split Pattern | Reusable allocation template |
| Subscription | Recurring charge tracking |
| Subscription Alert | Notification about subscription issues |
| Swipe Action | Mobile gesture revealing actions |
| Transaction | Imported bank record |
| Travel Period | Date range for trip expenses |
| Undo Stack | Edit history for reverting changes |
| Vista ERP | Source system for reference data |
