import { EuiProvider } from '@elastic/eui'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import type { ReactElement } from 'react'

/** A fresh client per test, with retries off so failures surface immediately. */
export const createTestQueryClient = () =>
    new QueryClient({
        defaultOptions: {
            queries: { retry: false },
            mutations: { retry: false },
        },
    })

export const renderWithQueryClient = (ui: ReactElement) =>
    render(
        <QueryClientProvider client={createTestQueryClient()}>
            {ui}
        </QueryClientProvider>
    )

/** Like `renderWithQueryClient`, and wrapped in the EUI provider the components expect; `rerender` keeps both. */
export const renderWithProviders = (ui: ReactElement) => {
    const client = createTestQueryClient()
    return render(ui, {
        wrapper: ({ children }) => (
            <EuiProvider
                colorMode="light"
                globalStyles={false}
                utilityClasses={false}
            >
                <QueryClientProvider client={client}>
                    {children}
                </QueryClientProvider>
            </EuiProvider>
        ),
    })
}
