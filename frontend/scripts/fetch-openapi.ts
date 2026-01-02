#!/usr/bin/env tsx
/**
 * Fetch OpenAPI Spec Script
 *
 * Fetches the OpenAPI specification from the backend API and saves it locally.
 * This enables offline contract validation and consistent test fixtures.
 *
 * Usage:
 *   pnpm tsx scripts/fetch-openapi.ts
 *   pnpm tsx scripts/fetch-openapi.ts --url http://custom-api:5000
 *
 * Environment:
 *   OPENAPI_URL - Base URL for the API (default: http://localhost:5000)
 */

import fs from 'fs'
import path from 'path'

// =============================================================================
// Configuration
// =============================================================================

const DEFAULT_API_URL = 'http://localhost:5000'
const SWAGGER_PATH = '/swagger/v1/swagger.json'
const OUTPUT_DIR = path.join(process.cwd(), 'src/types')
const OUTPUT_FILE = 'openapi-spec.json'

// =============================================================================
// CLI Argument Parsing
// =============================================================================

function parseArgs(): { url: string } {
  const args = process.argv.slice(2)
  let url = process.env.OPENAPI_URL || DEFAULT_API_URL

  for (let i = 0; i < args.length; i++) {
    if (args[i] === '--url' && args[i + 1]) {
      url = args[i + 1]
      i++
    }
  }

  return { url }
}

// =============================================================================
// Main
// =============================================================================

async function fetchOpenApiSpec(): Promise<void> {
  const { url } = parseArgs()
  const fullUrl = `${url}${SWAGGER_PATH}`

  console.log(`\n📡 Fetching OpenAPI spec from: ${fullUrl}\n`)

  try {
    const response = await fetch(fullUrl, {
      headers: {
        Accept: 'application/json',
      },
    })

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`)
    }

    const spec = await response.json()

    // Ensure output directory exists
    if (!fs.existsSync(OUTPUT_DIR)) {
      fs.mkdirSync(OUTPUT_DIR, { recursive: true })
    }

    const outputPath = path.join(OUTPUT_DIR, OUTPUT_FILE)

    // Write spec to file with pretty formatting
    fs.writeFileSync(outputPath, JSON.stringify(spec, null, 2), 'utf-8')

    console.log(`✅ OpenAPI spec saved to: ${outputPath}`)
    console.log(`   API Title: ${spec.info?.title || 'Unknown'}`)
    console.log(`   API Version: ${spec.info?.version || 'Unknown'}`)
    console.log(`   Paths: ${Object.keys(spec.paths || {}).length}`)
    console.log(`   Schemas: ${Object.keys(spec.components?.schemas || {}).length}`)
    console.log('')
  } catch (error) {
    if (error instanceof Error) {
      console.error(`\n❌ Failed to fetch OpenAPI spec: ${error.message}`)

      if (error.message.includes('ECONNREFUSED')) {
        console.error('\n💡 Hint: Make sure the backend API is running.')
        console.error('   Try: cd backend && dotnet run\n')
      }
    } else {
      console.error('\n❌ Failed to fetch OpenAPI spec:', error)
    }

    process.exit(1)
  }
}

// Run the script
fetchOpenApiSpec()
