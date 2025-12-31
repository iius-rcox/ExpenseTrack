# Manual Subscription Entry

Add subscriptions that aren't automatically detected.

## Overview

While ExpenseFlow [detects most subscriptions](./detection.md) automatically, some may need to be added manually. This includes new services, infrequent billing cycles, or subscriptions paid through alternative methods.

## When to Add Manually

| Scenario | Reason |
|----------|--------|
| **New subscription** | Not enough history for detection |
| **Annual billing** | Too infrequent to detect quickly |
| **Variable amounts** | Usage-based services |
| **Different payment method** | Paid outside tracked accounts |
| **Pre-paid services** | Paid upfront, needs tracking |

## Creating a Subscription

### Accessing the Form

1. Go to **Subscriptions** tab
2. Click **Add Subscription** or **+**
3. The creation dialog opens

![Create subscription dialog](../../images/subscriptions/create-subscription.png)
*Caption: Manual subscription creation form*

### Required Information

Fill in these fields:

| Field | Description | Example |
|-------|-------------|---------|
| **Vendor/Service** | Who you're paying | "Slack Technologies" |
| **Amount** | Regular charge amount | $12.50 |
| **Frequency** | Billing cycle | Monthly |
| **Category** | Expense category | Software |

### Optional Information

Enhance tracking with:

| Field | Description | Example |
|-------|-------------|---------|
| **Next billing date** | Expected charge date | Jan 15, 2025 |
| **Description** | What it's for | "Team messaging (10 seats)" |
| **Contract end** | Commitment period end | Dec 31, 2025 |
| **Auto-renew** | Does it renew automatically? | Yes |
| **Notes** | Additional context | "Negotiated rate" |

### Saving the Subscription

1. Review all entered information
2. Click **Save** or **Create Subscription**
3. Subscription appears in your list
4. Alerts will be configured based on settings

## Frequency Options

### Standard Frequencies

| Frequency | Days Between |
|-----------|--------------|
| **Weekly** | 7 |
| **Bi-weekly** | 14 |
| **Monthly** | 30-31 |
| **Quarterly** | 90-92 |
| **Semi-annual** | 180-183 |
| **Annual** | 365-366 |

### Custom Frequency

For non-standard cycles:

1. Select **Custom**
2. Enter number of days
3. Or specify exact billing dates

## Linking to Expenses

### Auto-Linking

When a charge matches your subscription:

1. ExpenseFlow proposes the link
2. Review and confirm
3. Charge is associated with subscription

### Manual Linking

To link a past expense:

1. Open the subscription
2. Click **Link Expense**
3. Search for the transaction
4. Select and confirm

### Viewing Linked Expenses

On each subscription:

- **Payment history**: All linked charges
- **Amount variations**: Changes over time
- **Missed payments**: Expected but not found

## Managing Manual Subscriptions

### Editing Details

1. Click subscription to open details
2. Click **Edit**
3. Modify any fields
4. Save changes

### Pausing a Subscription

If temporarily suspended:

1. Open subscription details
2. Click **Pause**
3. Select reason (optional)
4. Alerts are suppressed while paused

### Cancelling a Subscription

When you've ended a subscription:

1. Open subscription details
2. Click **Mark as Cancelled**
3. Enter cancellation date
4. Subscription moves to cancelled list
5. No more renewal alerts

### Reactivating a Subscription

If a cancelled subscription resumes:

1. Find in cancelled list
2. Click **Reactivate**
3. Update billing details if needed
4. Alerts resume

## Best Practices

### Complete Information

Add all details you know:

- Accurate amounts (including tax)
- Correct billing dates
- Contract terms
- Auto-renewal status

### Regular Verification

Periodically check:

- Manual entries still accurate
- No duplicates with auto-detection
- Contract dates current
- Amounts haven't changed

### Use Descriptive Names

| Less Helpful | More Helpful |
|--------------|--------------|
| "Microsoft" | "Microsoft 365 Business (5 users)" |
| "AWS" | "AWS EC2 + S3 (Production)" |
| "Zoom" | "Zoom Pro (monthly)" |

## Troubleshooting

### "Duplicate Subscription Warning"

System detects a similar subscription:

1. Review the existing entry
2. Confirm if it's truly a duplicate
3. Either merge or keep separate

### "Amount Doesn't Match Charges"

If linked charges differ:

1. Check if amount changed
2. Update subscription amount
3. Or note as variable-amount service

### "Missing from Reports"

If subscription isn't appearing:

1. Verify status is Active
2. Check category assignment
3. Ensure expenses are linked

## What's Next

After adding subscriptions:

- [Subscription Detection](./detection.md) - Understand auto-detection
- [Subscription Alerts](./alerts.md) - Configure notifications
- [Category Breakdown](../analytics/categories.md) - See subscription impact

