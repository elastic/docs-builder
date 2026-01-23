import { QueryClient } from '@tanstack/react-query'

/**
 * Shared QueryClient instance for all web components.
 * This ensures that queries with the same key share the same cache,
 * preventing duplicate requests across different components.
 */
export const sharedQueryClient = new QueryClient({
    defaultOptions: {
        queries: {
            staleTime: 60 * 60 * 1000, // 60 minutes default stale time
            retry: false,
        },
    },
})
