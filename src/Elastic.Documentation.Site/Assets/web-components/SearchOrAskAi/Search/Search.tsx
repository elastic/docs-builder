/** @jsxImportSource @emotion/react */
import { useChatActions } from '../AskAi/chat.store'
import { useModalActions } from '../modal.store'
import { SearchResults } from './SearchResults'
import { useSearchActions, useSearchTerm } from './search.store'
import { EuiFieldSearch, EuiSpacer, EuiButton } from '@elastic/eui'
import { css } from '@emotion/react'
import * as React from 'react'

const askAiButtonStyles = css`
    font-weight: bold;
`

export const Search = () => {
    const searchTerm = useSearchTerm()
    const { setSearchTerm } = useSearchActions()
    const { submitQuestion, clearChat } = useChatActions()
    const { setModalMode } = useModalActions()

    return (
        <>
            <EuiSpacer size="m" />
            <EuiFieldSearch
                autoFocus
                fullWidth
                placeholder="Search the docs as you type"
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                onSearch={(e) => {
                    if (e.trim()) {
                        // Always start a new conversation
                        clearChat()
                        submitQuestion(e)
                        setModalMode('askAi')
                    }
                }}
                isClearable
            />
            {searchTerm && (
                <>
                    <EuiSpacer size="s" />
                    <AskAiButton
                        term={searchTerm}
                        onAsk={() => {
                            clearChat()
                            if (searchTerm.trim()) submitQuestion(searchTerm)
                            setModalMode('askAi')
                        }}
                    />
                </>
            )}

            <EuiSpacer size="m" />
            <SearchResults />
        </>
    )
}

const AskAiButton = ({ term, onAsk }: { term: string; onAsk: () => void }) => {
    return (
        <EuiButton iconType="newChat" fullWidth onClick={onAsk}>
            Ask AI about <span css={askAiButtonStyles}>"{term}"</span>
        </EuiButton>
    )
}
