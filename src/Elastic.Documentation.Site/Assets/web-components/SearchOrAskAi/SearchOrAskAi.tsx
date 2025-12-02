import { SearchOrAskAiButton } from './SearchOrAskAiButton'
import { EuiProvider } from '@elastic/eui'
import r2wc from '@r2wc/react-to-web-component'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import * as React from 'react'
import { StrictMode } from 'react'

const queryClient = new QueryClient()

const SearchOrAskAi = () => {
    return (
        <StrictMode>
            <EuiProvider
                colorMode="light"
                globalStyles={false}
                utilityClasses={false}
            >
                <QueryClientProvider client={queryClient}>
                    <SearchOrAskAiButton />
                </QueryClientProvider>
            </EuiProvider>
        </StrictMode>
    )
}

customElements.define('search-or-ask-ai', r2wc(SearchOrAskAi))
