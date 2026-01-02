/**
 * MSW Server Configuration
 *
 * Sets up the Mock Service Worker server for Node.js testing environment.
 * This file should be imported in test setup to enable API mocking.
 */

import { setupServer } from 'msw/node'
import { handlers } from './msw-handlers'

/**
 * MSW server instance for Node.js testing
 * Import and use in test setup:
 *
 * @example
 * ```ts
 * import { server } from '@/test-utils/msw-server'
 *
 * beforeAll(() => server.listen())
 * afterEach(() => server.resetHandlers())
 * afterAll(() => server.close())
 * ```
 */
export const server = setupServer(...handlers)

export default server
