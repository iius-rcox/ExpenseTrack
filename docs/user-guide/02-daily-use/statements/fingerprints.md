# Fingerprints

Save column mappings as reusable templates for faster statement imports.

## Overview

A [Fingerprint](../../04-reference/glossary.md#fingerprint) is a saved column mapping configuration. When you import statements from the same bank or credit card, you can apply the fingerprint to skip the mapping step entirely.

## Why Use Fingerprints

| Without Fingerprints | With Fingerprints |
|---------------------|-------------------|
| Map columns every import | One-click import |
| Risk of mapping errors | Consistent configuration |
| 2-3 minutes per import | 30 seconds per import |

## Creating a Fingerprint

### During Import

1. Complete the [column mapping](./column-mapping.md) step
2. Before clicking **Import**, look for **Save as Template**
3. Click the checkbox or button to enable saving
4. Enter a descriptive name:
   - Good: "Chase Business Visa", "Bank of America Checking"
   - Avoid: "My Bank", "Statement 1"
5. Complete the import
6. The fingerprint is saved automatically

![Fingerprint save dialog](../../images/statements/fingerprint-save.png)
*Caption: Save your column mapping as a reusable template*

### Naming Best Practices

Use names that identify:
- **Bank name**: Chase, Wells Fargo, Amex
- **Account type**: Checking, Credit Card, Business
- **Account identifier**: Last 4 digits or account nickname

Examples:
- "Chase Sapphire **4521"
- "Capital One Business Checking"
- "Corporate Amex Platinum"

## Using a Fingerprint

### When Importing

1. Start the import process (Upload file)
2. On the **Column Mapping** step, look for **Use Template**
3. Select your saved fingerprint from the dropdown
4. Column mappings auto-populate
5. Review the sample data to confirm
6. Click **Continue** to proceed

### Auto-Detection

ExpenseFlow may automatically suggest a fingerprint:
- When file structure matches a saved template
- Based on column headers similarity
- From file naming patterns

Accept the suggestion or choose a different fingerprint manually.

## Managing Fingerprints

### View All Fingerprints

1. Go to **Settings**
2. Select **Import Templates** or **Fingerprints**
3. See all saved fingerprints with:
   - Template name
   - Date created
   - Last used date
   - Source bank (if detected)

### Edit a Fingerprint

To modify an existing fingerprint:

1. Open Settings > Import Templates
2. Find the fingerprint to edit
3. Click **Edit** (pencil icon)
4. Modify the name or description
5. Click **Save**

> **Note**: Column mapping changes require creating a new fingerprint. You cannot modify the actual column configuration.

### Rename a Fingerprint

1. Open Settings > Import Templates
2. Find the fingerprint
3. Click **Rename**
4. Enter the new name
5. Click **Save**

### Delete a Fingerprint

1. Open Settings > Import Templates
2. Find the fingerprint
3. Click **Delete** (trash icon)
4. Confirm deletion

> **Warning**: Deleted fingerprints cannot be recovered. Create a new one if needed.

## Fingerprint Troubleshooting

### Fingerprint Doesn't Apply Correctly

If mappings don't match after applying:

1. Your bank may have changed their export format
2. Check if column names or order changed
3. Update the mapping manually
4. Save as a new fingerprint

### "No Matching Template" Message

If no fingerprint is suggested:

1. This is normal for new file formats
2. Map columns manually
3. Save as a new fingerprint for next time

### Duplicate Fingerprints

If you have multiple similar fingerprints:

1. Review which one is most accurate
2. Delete outdated versions
3. Keep one well-named template per account

## Multiple Bank Accounts

### Organizing Fingerprints

For multiple accounts:

- Create one fingerprint per account
- Use clear naming conventions
- Group by bank name for easy finding

Example organization:
```
Bank of America
├── BoA Checking **1234
├── BoA Savings **5678

Chase
├── Chase Sapphire **4521
├── Chase Business **9012

American Express
├── Personal Platinum
└── Corporate Card
```

### Shared Formats

Some banks use identical formats across accounts:
- You may only need one fingerprint per bank
- Name it generically: "Chase Credit Cards"
- Works for any Chase card export

## Best Practices

### When to Create New Fingerprints

Create a new fingerprint when:
- Importing from a new bank or account
- Your bank changes their export format
- You want different mapping options (e.g., different date column)

### Maintenance

- Review fingerprints quarterly
- Delete unused templates
- Update names if accounts change
- Test after bank website updates

### Security Note

Fingerprints store:
- Column position mappings
- Configuration settings (sign convention, etc.)
- Template name

Fingerprints do NOT store:
- Actual transaction data
- Account numbers
- Bank credentials

## What's Next

After setting up fingerprints:

- [Importing Statements](./importing.md) - Use fingerprints for fast imports
- [Filtering](../transactions/filtering.md) - Work with imported data
- [Settings](../../04-reference/settings.md) - Manage all templates
