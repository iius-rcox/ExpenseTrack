#!/usr/bin/env node
/**
 * Script to import expense patterns from CSV data.
 *
 * Usage:
 *   node scripts/import-patterns.js --token <bearer_token>
 *
 * Or with environment variable:
 *   export API_TOKEN=<bearer_token>
 *   node scripts/import-patterns.js
 */

const fs = require('fs');
const path = require('path');
const https = require('https');

// Configuration
const API_BASE = process.env.API_BASE || 'https://staging.expense.ii-us.com';
const CSV_PATH = path.join(__dirname, '../example-data/expense-reports-md/historical-expenses-consolidated.csv');

// Parse command line args
const args = process.argv.slice(2);
let token = process.env.API_TOKEN || '';

for (let i = 0; i < args.length; i++) {
    if (args[i] === '--token' && args[i + 1]) {
        token = args[i + 1];
        i++;
    }
}

// Parse CSV
function parseCSV(content) {
    const lines = content.trim().split('\n');
    const headers = lines[0].split(',').map(h => h.trim());

    return lines.slice(1).map(line => {
        // Handle quoted fields with commas
        const values = [];
        let current = '';
        let inQuotes = false;

        for (const char of line) {
            if (char === '"') {
                inQuotes = !inQuotes;
            } else if (char === ',' && !inQuotes) {
                values.push(current.trim());
                current = '';
            } else {
                current += char;
            }
        }
        values.push(current.trim());

        const obj = {};
        headers.forEach((header, i) => {
            obj[header] = values[i] || '';
        });
        return obj;
    });
}

// Convert CSV row to import DTO
function toImportDto(row) {
    return {
        vendor: row.Vendor,
        displayName: row.Vendor,
        category: row.Description,
        amount: parseFloat(row.Amount) || 0,
        glCode: row['GL Code'],
        department: row.Department,
        date: new Date(row.Date).toISOString()
    };
}

// Make API request
function importPatterns(entries, token) {
    return new Promise((resolve, reject) => {
        const data = JSON.stringify({ entries });

        const url = new URL('/api/predictions/patterns/import', API_BASE);

        const options = {
            hostname: url.hostname,
            port: 443,
            path: url.pathname,
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Content-Length': data.length,
                ...(token ? { 'Authorization': `Bearer ${token}` } : {})
            }
        };

        const req = https.request(options, (res) => {
            let body = '';
            res.on('data', chunk => body += chunk);
            res.on('end', () => {
                if (res.statusCode >= 200 && res.statusCode < 300) {
                    try {
                        resolve(JSON.parse(body));
                    } catch {
                        resolve(body);
                    }
                } else {
                    reject(new Error(`HTTP ${res.statusCode}: ${body}`));
                }
            });
        });

        req.on('error', reject);
        req.write(data);
        req.end();
    });
}

// Main
async function main() {
    console.log('Reading CSV file...');
    const csvContent = fs.readFileSync(CSV_PATH, 'utf-8');
    const rows = parseCSV(csvContent);

    console.log(`Parsed ${rows.length} expense entries`);

    // Convert to import DTOs
    const entries = rows.map(toImportDto);

    // Show vendor summary
    const vendors = [...new Set(entries.map(e => e.vendor))];
    console.log(`Found ${vendors.length} unique vendors:`);
    vendors.slice(0, 10).forEach(v => console.log(`  - ${v}`));
    if (vendors.length > 10) {
        console.log(`  ... and ${vendors.length - 10} more`);
    }

    if (!token) {
        console.log('\nNo token provided. Outputting JSON payload to stdout:');
        console.log(JSON.stringify({ entries }, null, 2));
        console.log('\nTo import, run with --token <bearer_token> or set API_TOKEN env var');
        return;
    }

    console.log('\nImporting patterns to API...');
    try {
        const result = await importPatterns(entries, token);
        console.log('Import successful!');
        console.log(`  Created: ${result.createdCount} patterns`);
        console.log(`  Updated: ${result.updatedCount} patterns`);
        console.log(`  Total processed: ${result.totalProcessed} entries`);
        console.log(`  Message: ${result.message}`);
    } catch (error) {
        console.error('Import failed:', error.message);
        process.exit(1);
    }
}

main().catch(console.error);
