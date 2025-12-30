#!/usr/bin/env python3
"""
Consolidate expense report markdown files into a single CSV for CacheWarming import.

Expected output format:
Date | Description | Vendor | Amount | GL Code | Department
"""

import re
import csv
from pathlib import Path
from typing import NamedTuple

class ExpenseLine(NamedTuple):
    date: str
    description: str
    vendor: str
    amount: float
    gl_code: str
    department: str
    source_file: str

# Vendor extraction patterns - order matters (more specific first)
VENDOR_PATTERNS = [
    # Airlines
    (r'\bDelta\b|\bDELTA AIR\b', 'Delta Airlines'),
    (r'\bAmerican\b|\bAMERICAN AIR', 'American Airlines'),
    (r'\bSouthwest\b', 'Southwest Airlines'),
    (r'\bUnited\b', 'United Airlines'),

    # Hotels
    (r'\bHampton Inn\b|\bHAMPTON INN\b', 'Hampton Inn'),
    (r'\bHilton\b|\bHILTON\b', 'Hilton'),
    (r'\bMarriott\b|\bMARRIOTT\b', 'Marriott'),
    (r'\bDoubleTree\b|\bDOUBLETREE\b', 'DoubleTree'),
    (r'\bTribute Portfolio\b', 'Tribute Portfolio Hotel'),

    # Car Rental
    (r'\bHertz\b|\bHERTZ\b', 'Hertz'),
    (r'\bEnterprise\b', 'Enterprise'),

    # Parking
    (r'\bRDU\b.*\bParking\b|\bRDUAA\b', 'RDU Airport Parking'),
    (r'\bParking\b', 'Parking'),

    # Ride Share
    (r'\bLyft\b|\bLYFT\b', 'Lyft'),
    (r'\bUber\b|\bUBER\b', 'Uber'),

    # Software/Subscriptions
    (r'\bChatGPT\b|\bOpenAI\b|\bOPENAI\b', 'OpenAI'),
    (r'\bClaude\b|\bCLAUDE\.AI\b', 'Anthropic Claude'),
    (r'\bCursor\b|\bCURSOR\b', 'Cursor AI'),
    (r'\bTwilio\b|\bTWILIO\b', 'Twilio'),
    (r'\bFoxit\b|\bFOXIT\b', 'Foxit'),
    (r'\bFireflies\b|\bFIREFLIES\b', 'Fireflies.AI'),
    (r'\bGoDaddy\b|\bGODADDY\b', 'GoDaddy'),
    (r'\bSuperhuman\b|\bSUPERHUMAN\b', 'Superhuman'),
    (r'\bElevenLabs\b|\bELEVENLABS\b', 'ElevenLabs'),
    (r'\bAddigy\b|\bADIGY\b', 'Addigy'),
    (r'\bPyCharm\b', 'JetBrains PyCharm'),
    (r'\bGamma\b', 'Gamma'),
    (r'\bStarlink\b|\bSTARLINK\b', 'Starlink'),
    (r'\bCrystal Reports\b', 'SAP Crystal Reports'),

    # Utilities
    (r'\bAT&T\b|\bATT\b|\bMobile Telephone\b', 'AT&T'),
    (r'\bGFiber\b|\bGoogle Fiber\b|\bUtilities.*Internet\b', 'Google Fiber'),
    (r'\bVerizon\b|\bVZW\b', 'Verizon'),

    # Retail/Hardware
    (r'\bAmazon\b|\bAMAZON\b', 'Amazon'),
    (r'\bBest Buy\b|\bBEST BUY\b', 'Best Buy'),
    (r'\bWalmart\b|\bWALMART\b', 'Walmart'),
    (r'\bDell\b|\bDELL\b', 'Dell'),
    (r'\bApple\b|\bAPPLE\b', 'Apple'),
    (r'\bOwl Labs\b', 'Owl Labs'),
    (r'\bUPS Store\b|\bUPS\b.*Shipping', 'UPS'),

    # Restaurants/Meals
    (r'\bChick-fil-A\b|\bChickfila\b', 'Chick-fil-A'),
    (r'\bChilis\b|\bChili\'?s\b', 'Chilis'),
    (r'\bFive Guys\b', 'Five Guys'),
    (r'\bBuffalo Wild Wings\b', 'Buffalo Wild Wings'),
    (r'\bDoorDash\b', 'DoorDash'),
    (r'\bStarbucks\b', 'Starbucks'),

    # Generic travel/meal patterns
    (r'\bFlight\b', 'Flight'),
    (r'\bHotel\b', 'Hotel'),
    (r'\bCar Rental\b|\bRental Car\b', 'Car Rental'),
    (r'\bBreakfast\b', 'Meal - Breakfast'),
    (r'\bLunch\b', 'Meal - Lunch'),
    (r'\bDinner\b', 'Meal - Dinner'),
    (r'\bMeal\b', 'Meal'),
]

def extract_vendor(description: str) -> str:
    """Extract vendor name from description using pattern matching."""
    for pattern, vendor in VENDOR_PATTERNS:
        if re.search(pattern, description, re.IGNORECASE):
            return vendor
    # If no pattern matches, return first significant word(s)
    words = description.split()
    if words:
        # Remove common prefixes
        for skip in ['PAYPAL', 'DNH*', 'PY', 'DMI*', 'IAH', 'ATL', 'MSY', 'DFW', 'RDU']:
            if words[0].startswith(skip):
                words = words[1:]
                break
        return ' '.join(words[:2]) if len(words) >= 2 else words[0] if words else 'Unknown'
    return 'Unknown'

def parse_amount(amount_str: str) -> float:
    """Parse amount string like '$1,234.56' to float."""
    clean = amount_str.replace('$', '').replace(',', '').strip()
    try:
        return float(clean)
    except ValueError:
        return 0.0

def parse_markdown_file(filepath: Path) -> list[ExpenseLine]:
    """Parse a markdown expense report and extract expense lines."""
    expenses = []
    content = filepath.read_text()

    # Find the expenses table
    # Pattern: | Date | GL Acct/Job | Dept/Phase | Description | Amount |
    lines = content.split('\n')
    in_table = False
    header_found = False

    for line in lines:
        line = line.strip()

        # Skip empty lines
        if not line:
            continue

        # Check for table start
        if '| Date |' in line and 'GL Acct' in line:
            in_table = True
            header_found = True
            continue

        # Skip separator line
        if in_table and line.startswith('|---'):
            continue

        # Check for table end (total line or empty)
        if in_table and ('**Total' in line or not line.startswith('|')):
            in_table = False
            continue

        # Parse expense line
        if in_table and line.startswith('|'):
            parts = [p.strip() for p in line.split('|')]
            # Remove empty first/last elements from split
            parts = [p for p in parts if p]

            if len(parts) >= 5:
                date = parts[0]
                gl_code = parts[1].replace('.', '')  # Remove trailing dots
                department = parts[2]
                description = parts[3]
                amount_str = parts[4]

                # Extract vendor from description
                vendor = extract_vendor(description)
                amount = parse_amount(amount_str)

                expenses.append(ExpenseLine(
                    date=date,
                    description=description,
                    vendor=vendor,
                    amount=amount,
                    gl_code=gl_code,
                    department=department,
                    source_file=filepath.name
                ))

    return expenses

def main():
    # Find all markdown files
    md_folder = Path('/Users/rogercox/ExpenseTrack/example-data/expense-reports-md')
    md_files = sorted(md_folder.glob('expense-report-*.md'))

    print(f"Found {len(md_files)} markdown files")

    all_expenses = []
    for md_file in md_files:
        expenses = parse_markdown_file(md_file)
        print(f"  {md_file.name}: {len(expenses)} expenses")
        all_expenses.extend(expenses)

    print(f"\nTotal expenses: {len(all_expenses)}")

    # Write CSV output
    output_csv = md_folder / 'historical-expenses-consolidated.csv'
    with open(output_csv, 'w', newline='') as f:
        writer = csv.writer(f)
        writer.writerow(['Date', 'Description', 'Vendor', 'Amount', 'GL Code', 'Department'])
        for exp in all_expenses:
            writer.writerow([exp.date, exp.description, exp.vendor, exp.amount, exp.gl_code, exp.department])

    print(f"\nWritten to: {output_csv}")

    # Summary stats
    vendors = set(exp.vendor for exp in all_expenses)
    gl_codes = set(exp.gl_code for exp in all_expenses)
    departments = set(exp.department for exp in all_expenses)

    print(f"\n=== Summary ===")
    print(f"Unique vendors: {len(vendors)}")
    print(f"Unique GL codes: {len(gl_codes)}")
    print(f"Unique departments: {len(departments)}")
    print(f"Total amount: ${sum(exp.amount for exp in all_expenses):,.2f}")

    # Top vendors by count
    vendor_counts = {}
    for exp in all_expenses:
        vendor_counts[exp.vendor] = vendor_counts.get(exp.vendor, 0) + 1

    print(f"\n=== Top 15 Vendors by Frequency ===")
    for vendor, count in sorted(vendor_counts.items(), key=lambda x: -x[1])[:15]:
        print(f"  {vendor}: {count}")

    # GL code breakdown
    gl_totals = {}
    for exp in all_expenses:
        gl_totals[exp.gl_code] = gl_totals.get(exp.gl_code, 0) + exp.amount

    print(f"\n=== GL Code Totals ===")
    for gl, total in sorted(gl_totals.items(), key=lambda x: -x[1]):
        print(f"  {gl}: ${total:,.2f}")

if __name__ == '__main__':
    main()
