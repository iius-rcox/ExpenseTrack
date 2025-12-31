# Subscription Alerts

Get notified about subscription changes, renewals, and anomalies.

## Overview

Subscription alerts help you stay on top of recurring expenses. Get notified before charges occur, when prices change, or when unexpected activity is detected.

## Alert Types

### Renewal Reminders

Alerts before subscriptions renew:

| Type | When You're Notified |
|------|---------------------|
| **Annual renewal** | 14 days before charge |
| **Monthly renewal** | Day before charge |
| **Large amounts** | 7 days before (configurable) |

### Price Change Alerts

When subscription amounts change:

- **Increase detected**: New charge higher than usual
- **Decrease detected**: New charge lower (potential downgrade)
- **Significant variance**: Amount outside normal range

![Alert badge](../../images/subscriptions/alert-badge.png)
*Caption: Alert badge showing pending subscription notifications*

### Missing Payment Alerts

When expected charges don't appear:

- **Overdue**: Expected charge date passed
- **Possible cancellation**: No charge for 2 cycles
- **Service risk**: May indicate payment failure

### New Detection Alerts

When new subscriptions are identified:

- **New recurring expense**: First-time detection
- **Review suggested**: Verify it's a valid subscription

## Viewing Alerts

### Alert Badge

The navigation shows:

- Red badge with count on Subscriptions menu
- Badge on Dashboard if alerts pending
- Notification bell shows recent alerts

### Alert List

1. Go to **Subscriptions** → **Alerts**
2. View all pending alerts
3. Filter by alert type or status

### Alert Details

Each alert shows:

| Field | Information |
|-------|-------------|
| **Type** | What kind of alert |
| **Subscription** | Which service/vendor |
| **Details** | Specific information |
| **Date** | When alert was created |
| **Action needed** | What you should do |

## Responding to Alerts

### Acknowledging Alerts

After reviewing:

1. Click **Acknowledge** or **Dismiss**
2. Alert moves to history
3. Won't show again for same event

### Snoozing Alerts

If you need to handle later:

1. Click **Snooze**
2. Select snooze period:
   - 1 day
   - 3 days
   - 1 week
   - Until next month
3. Alert reappears after snooze period

### Taking Action

For actionable alerts:

| Alert Type | Possible Actions |
|------------|------------------|
| **Renewal** | Review service, cancel if unneeded |
| **Price increase** | Verify with vendor, consider alternatives |
| **Missing charge** | Check payment method, contact vendor |
| **New detection** | Confirm or dismiss detection |

## Configuring Alerts

### Alert Preferences

1. Go to **Settings** → **Notifications** → **Subscriptions**
2. Configure each alert type:

| Setting | Options |
|---------|---------|
| **Renewal reminders** | On/Off, days before |
| **Price change alerts** | On/Off, threshold % |
| **Missing payment** | On/Off, days overdue |
| **New detection** | On/Off |

### Notification Channels

Choose how to receive alerts:

| Channel | Description |
|---------|-------------|
| **In-app** | Badge and notification center |
| **Email** | Sent to your account email |
| **Daily digest** | Combined daily summary |

### Amount Thresholds

Set when to be alerted:

- **Minimum amount**: Only alert for subscriptions above $X
- **Change threshold**: Alert when change exceeds X%
- **Annual reminder threshold**: Alert earlier for amounts above $X

## Alert History

### Viewing Past Alerts

1. Go to **Subscriptions** → **Alerts** → **History**
2. See all acknowledged/dismissed alerts
3. Filter by date range or type

### Alert Analytics

Track alert patterns:

- How many alerts per month
- Most common alert types
- Response time to alerts
- Actions taken

## Best Practices

### Regular Review

| Frequency | Review |
|-----------|--------|
| **Daily** | Check badge, handle urgent alerts |
| **Weekly** | Clear alert backlog |
| **Monthly** | Review all subscriptions before close |

### Proactive Management

Use renewal alerts to:

1. Evaluate if you still need the service
2. Check for better pricing/alternatives
3. Update budget forecasts
4. Negotiate renewals

### Don't Ignore Price Changes

Even small increases add up:

- $5/month increase = $60/year
- Across 10 subscriptions = $600/year
- Review all price change alerts

## Troubleshooting

### "Too Many Alerts"

Reduce alert volume:

1. Increase minimum amount threshold
2. Reduce reminder lead time
3. Disable less critical alert types

### "Missing Expected Alert"

If you didn't get an alert:

1. Check alert preferences are enabled
2. Verify subscription is tracked (not dismissed)
3. Check spam folder for email alerts

### "Alert for Cancelled Service"

1. Mark subscription as cancelled
2. Dismiss the alert
3. It won't trigger again for that subscription

## What's Next

After understanding alerts:

- [Subscription Detection](./detection.md) - How subscriptions are found
- [Manual Entry](./manual-entry.md) - Add subscriptions manually
- [Dashboard Overview](../../01-getting-started/dashboard-overview.md) - See alerts on dashboard

