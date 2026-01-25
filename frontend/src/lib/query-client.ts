import { QueryClient } from '@tanstack/react-query'

/**
 * Deep sanitize data to remove empty objects that cause React Error #301.
 *
 * TanStack Query's `placeholderData: keepPreviousData` can cache data with
 * empty objects `{}` that, when rendered as React children, cause:
 * "Objects are not valid as a React child (found: object with keys {})"
 *
 * This sanitizer runs on ALL query results to ensure no empty objects
 * leak through to React rendering.
 */
function deepSanitizeEmptyObjects<T>(data: T): T {
  // Handle null/undefined
  if (data === null || data === undefined) {
    return data;
  }

  // Handle primitive types
  if (typeof data !== 'object') {
    return data;
  }

  // Handle Date objects (don't sanitize)
  if (data instanceof Date) {
    return data;
  }

  // Handle arrays
  if (Array.isArray(data)) {
    return data.map((item) => deepSanitizeEmptyObjects(item)) as T;
  }

  // Handle objects
  const obj = data as Record<string, unknown>;
  const keys = Object.keys(obj);

  // Convert empty objects to null (defensive against malformed API responses)
  if (keys.length === 0) {
    return null as T;
  }

  // Recursively sanitize all properties
  const sanitized: Record<string, unknown> = {};
  for (const key of keys) {
    const value = obj[key];
    // For non-null values, recursively sanitize
    // For null/undefined, keep as-is
    if (value !== null && value !== undefined && typeof value === 'object') {
      const sanitizedValue = deepSanitizeEmptyObjects(value);
      // If an empty object was sanitized to null, we need to handle it
      // For string fields that became null, they should stay null
      sanitized[key] = sanitizedValue;
    } else {
      sanitized[key] = value;
    }
  }

  return sanitized as T;
}

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000, // 30 seconds
      gcTime: 5 * 60 * 1000, // 5 minutes (garbage collection time)
      retry: 1,
      refetchOnWindowFocus: true,
      // Sanitize all query data to prevent React Error #301
      // This catches empty objects before they reach React rendering
      structuralSharing: (_oldData, newData) => {
        // Perform deep sanitization on incoming data
        const sanitized = deepSanitizeEmptyObjects(newData);
        // Return sanitized data - TanStack Query will handle structural sharing
        return sanitized;
      },
    },
  },
})
