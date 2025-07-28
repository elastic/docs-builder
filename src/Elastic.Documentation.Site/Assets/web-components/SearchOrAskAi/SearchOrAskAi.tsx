import { SearchOrAskAiButton } from './SearchOrAskAiButton'
import r2wc from '@r2wc/react-to-web-component'
import * as React from 'react'
import { StrictMode } from 'react'

const SearchOrAskAi = () => {
    return (
        <StrictMode>
            <SearchOrAskAiButton />
        </StrictMode>
    )
}

customElements.define('search-or-ask-ai', r2wc(SearchOrAskAi))
