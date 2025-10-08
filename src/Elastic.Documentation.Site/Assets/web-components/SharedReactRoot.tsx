import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import * as React from 'react'
import { StrictMode } from 'react'

// Singleton QueryClient shared across all components
const queryClient = new QueryClient()

/**
 * Shared React root provider for all web components
 * This ensures all components share the same React instance and providers
 */
export const SharedReactRoot = ({ children }: { children: React.ReactNode }) => {
    return (
        <StrictMode>
            <QueryClientProvider client={queryClient}>
                {children}
            </QueryClientProvider>
        </StrictMode>
    )
}

export { queryClient }
