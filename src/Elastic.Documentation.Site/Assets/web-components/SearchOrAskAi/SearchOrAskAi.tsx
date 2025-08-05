import { SearchOrAskAiButton } from './SearchOrAskAiButton'
import r2wc from '@r2wc/react-to-web-component'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import * as React from 'react'
import { StrictMode } from 'react'

const SearchOrAskAi = () => {
    const queryClient = new QueryClient()
    return (
        <StrictMode>
            <QueryClientProvider client={queryClient}>
                <SearchOrAskAiButton />
            </QueryClientProvider>
        </StrictMode>
    )
}

customElements.define('search-or-ask-ai', r2wc(SearchOrAskAi))
