#!/usr/bin/env tsx
/**
 * Generate TypeScript Types from OpenAPI Spec
 *
 * This script generates TypeScript type definitions from the OpenAPI spec.
 * It uses openapi-typescript to create strongly-typed interfaces that match
 * the backend API contract.
 *
 * Usage:
 *   pnpm tsx scripts/generate-types.ts
 *   pnpm tsx scripts/generate-types.ts --from-file  # Use local spec file
 *   pnpm tsx scripts/generate-types.ts --from-url   # Fetch from API (default)
 *
 * The generated types can be used to:
 *   1. Type API responses in fetch/axios calls
 *   2. Validate MSW mock handlers match real API shapes
 *   3. Catch contract breaking changes at compile-time
 */

import { exec } from 'child_process'
import { promisify } from 'util'
import fs from 'fs'
import path from 'path'

const execAsync = promisify(exec)

// =============================================================================
// Configuration
// =============================================================================

const DEFAULT_API_URL = 'http://localhost:5000/swagger/v1/swagger.json'
const LOCAL_SPEC_FILE = path.join(process.cwd(), 'src/types/openapi-spec.json')
const OUTPUT_FILE = path.join(process.cwd(), 'src/types/generated-api.d.ts')

// =============================================================================
// CLI Argument Parsing
// =============================================================================

function parseArgs(): { source: 'file' | 'url'; url?: string } {
  const args = process.argv.slice(2)

  if (args.includes('--from-file')) {
    return { source: 'file' }
  }

  let url = process.env.OPENAPI_URL || DEFAULT_API_URL

  for (let i = 0; i < args.length; i++) {
    if (args[i] === '--url' && args[i + 1]) {
      url = args[i + 1]
      i++
    }
  }

  return { source: 'url', url }
}

// =============================================================================
// Type Generation
// =============================================================================

async function generateTypes(): Promise<void> {
  const { source, url } = parseArgs()

  console.log('\n🔧 Generating TypeScript types from OpenAPI spec...\n')

  let inputSource: string

  if (source === 'file') {
    if (!fs.existsSync(LOCAL_SPEC_FILE)) {
      console.error(`❌ Local spec file not found: ${LOCAL_SPEC_FILE}`)
      console.error('\n💡 Hint: Run `pnpm tsx scripts/fetch-openapi.ts` first\n')
      process.exit(1)
    }
    inputSource = LOCAL_SPEC_FILE
    console.log(`📁 Using local spec file: ${LOCAL_SPEC_FILE}`)
  } else {
    inputSource = url!
    console.log(`🌐 Fetching spec from: ${inputSource}`)
  }

  try {
    // Use npx to run openapi-typescript
    const command = `npx openapi-typescript "${inputSource}" -o "${OUTPUT_FILE}"`

    console.log(`\n$ ${command}\n`)

    const { stdout, stderr } = await execAsync(command, {
      cwd: process.cwd(),
    })

    if (stdout) console.log(stdout)
    if (stderr && !stderr.includes('warning')) console.error(stderr)

    if (fs.existsSync(OUTPUT_FILE)) {
      const stats = fs.statSync(OUTPUT_FILE)
      console.log(`\n✅ Types generated successfully!`)
      console.log(`   Output: ${OUTPUT_FILE}`)
      console.log(`   Size: ${(stats.size / 1024).toFixed(2)} KB\n`)

      // Print summary of generated types
      const content = fs.readFileSync(OUTPUT_FILE, 'utf-8')
      const interfaceCount = (content.match(/export interface/g) || []).length
      const typeCount = (content.match(/export type/g) || []).length

      console.log(`   Interfaces: ${interfaceCount}`)
      console.log(`   Type aliases: ${typeCount}\n`)
    } else {
      console.error('❌ Output file was not created')
      process.exit(1)
    }
  } catch (error) {
    if (error instanceof Error) {
      console.error(`\n❌ Type generation failed: ${error.message}`)

      if (error.message.includes('ECONNREFUSED')) {
        console.error('\n💡 Hint: Backend API might not be running.')
        console.error('   Try using --from-file with a cached spec\n')
      }
    } else {
      console.error('\n❌ Type generation failed:', error)
    }

    process.exit(1)
  }
}

// Run the script
generateTypes()
