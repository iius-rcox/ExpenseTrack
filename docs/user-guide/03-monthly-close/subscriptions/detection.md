# Subscription Detection

← [Back to Monthly Close](../README.md) | [Subscriptions Section](./alerts.md)

Understand how ExpenseFlow automatically identifies recurring expenses.

## Overview

ExpenseFlow analyzes your expense patterns to detect [subscriptions](../../04-reference/glossary.md#subscription) and recurring charges. This helps you track ongoing costs that might otherwise go unnoticed.

## How Detection Works

### Pattern Recognition

The system looks for:

| Pattern | Example |
|---------|---------|
| **Same vendor, recurring** | Netflix $15.99 every month |
| **Similar amounts** | Adobe $52.99 ± small variations |
| **Regular intervals** | Weekly, monthly, annual |
| **Consistent dates** | Always around the 15th |

### Detection Triggers

Subscriptions are detected when:

1. **Three or more** charges from the same vendor
2. **Within 15%** amount variance
3. **Predictable timing** (weekly, monthly, quarterly, annual)
4. **At least 2 months** of history

> **Note**: New subscriptions may take 2-3 billing cycles to be detected.

## Viewing Detected Subscriptions

### Subscriptions Tab

1. Navigate to **Analytics** or **Monthly Close**
2. Click **Subscriptions** tab
3. View list of detected recurring expenses

![Subscriptions tab](../../images/subscriptions/subscriptions-tab.png)
*Caption: The subscriptions tab showing detected recurring charges*

### Subscription Card Information

Each detected subscription shows:

| Field | Description |
|-------|-------------|
| **Vendor** | Who you're paying |
| **Amount** | Typical charge amount |
| **Frequency** | Monthly, Annual, etc. |
| **Last Charge** | Date of most recent |
| **Next Expected** | Predicted next charge |
| **Annual Cost** | Calculated yearly total |
| **Status** | Active, Paused, or Cancelled |

## Subscription Categories

### By Frequency

| Type | Billing Cycle |
|------|---------------|
| **Weekly** | Every 7 days |
| **Monthly** | Every 30-31 days |
| **Quarterly** | Every 90 days |
| **Semi-Annual** | Every 6 months |
| **Annual** | Every 12 months |

### By Type (Auto-Categorized)

- **Software**: SaaS tools, apps
- **Services**: Cloud services, hosting
- **Memberships**: Professional associations
- **Media**: Streaming, publications
- **Utilities**: Phone, internet (if business)

## Managing Detection

### Confirming a Detection

When a subscription is detected:

1. Review the details
2. Click **Confirm** to validate
3. Confirmed subscriptions get priority tracking

### Dismissing False Positives

If something isn't actually a subscription:

1. Click **Not a Subscription**
2. Provide reason (optional):
   - One-time purchase
   - Cancelled service
   - Misidentified vendor
3. It won't be suggested again

### Adjusting Detection

If details are wrong:

1. Click **Edit** on the subscription
2. Correct:
   - Amount (if it varies)
   - Frequency (if detected wrong)
   - Next expected date
3. Save changes

## Detection Accuracy

### Factors That Help

| Factor | Why It Helps |
|--------|--------------|
| **Consistent vendor names** | Easier to group charges |
| **Same payment method** | Patterns are clearer |
| **Regular imports** | More data points |
| **Clean categorization** | Better grouping |

### Factors That Hinder

| Factor | Why It's Difficult |
|--------|-------------------|
| **Vendor name variations** | "NETFLIX.COM" vs "Netflix Inc" |
| **Variable amounts** | Usage-based services |
| **Irregular timing** | Non-standard billing cycles |
| **Recent signup** | Not enough history |

## Subscription Insights

### Total Recurring Costs

The subscriptions page shows:

- **Monthly total**: Sum of all monthly subscriptions
- **Annual projection**: Yearly cost if all continue
- **Category breakdown**: Recurring costs by type

### Trend Analysis

Compare over time:

- New subscriptions added
- Subscriptions cancelled
- Price changes detected
- Total recurring cost trend

## Integration with Analytics

Detected subscriptions appear in:

- [Category breakdown](../analytics/categories.md) - as recurring portion
- [Trends analysis](../analytics/trends.md) - as baseline spending
- Budget tracking - as committed costs

## What's Next

After understanding detection:

- [Subscription Alerts](./alerts.md) - Get notified of changes
- [Manual Entry](./manual-entry.md) - Add subscriptions manually
- [Trends Analysis](../analytics/trends.md) - See subscription impact

