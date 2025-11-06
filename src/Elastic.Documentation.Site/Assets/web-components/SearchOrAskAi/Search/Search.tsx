/** @jsxImportSource @emotion/react */
import { useChatActions } from '../AskAi/chat.store'
import { SearchOrAskAiErrorCallout } from '../SearchOrAskAiErrorCallout'
import { useModalActions } from '../modal.store'
import { useIsAskAiCooldownActive } from '../AskAi/useAskAiCooldown'
import {
    useIsSearchCooldownActive,
    useSearchCooldown,
    useSearchCooldownActions,
} from './useSearchCooldown'
import { useCooldown } from '../useCooldown'
import { SearchResults } from './SearchResults'
import { useSearchActions, useSearchTerm } from './search.store'
import { EuiFieldText, EuiSpacer, EuiButton, EuiButtonIcon } from '@elastic/eui'
import { css } from '@emotion/react'
import { useCallback, useRef, useState, useEffect } from 'react'

const askAiButtonStyles = css`
    font-weight: bold;
`

export const Search = () => {
    const searchTerm = useSearchTerm()
    const { setSearchTerm } = useSearchActions()
    const { submitQuestion, clearChat } = useChatActions()
    const { setModalMode } = useModalActions()
    const isSearchCooldownActive = useIsSearchCooldownActive()
    const isAskAiCooldownActive = useIsAskAiCooldownActive()
    const searchCooldown = useSearchCooldown()
    const { notifyCooldownFinished } = useSearchCooldownActions()
    const inputRef = useRef<HTMLInputElement>(null)
    const [inputValue, setInputValue] = useState(searchTerm)

    // Manage search cooldown countdown
    useCooldown({
        domain: 'search',
        cooldown: searchCooldown,
        onCooldownFinished: () => notifyCooldownFinished(),
    })

    const handleSearch = useCallback(() => {
        if (searchTerm.trim()) {
            // Prevent submission during countdown
            if (isSearchCooldownActive) {
                return
            }
            // Always start a new conversation
            clearChat()
            submitQuestion(searchTerm)
            setModalMode('askAi')
        }
    }, [
        searchTerm,
        isSearchCooldownActive,
        clearChat,
        submitQuestion,
        setModalMode,
    ])

    // Sync inputValue with searchTerm from store (when cleared externally)
    useEffect(() => {
        if (searchTerm === '' && inputValue !== '') {
            setInputValue('')
        }
    }, [searchTerm, inputValue])

    return (
        <>
            <EuiSpacer size="m" />
            {!searchTerm.trim() && (
                <SearchOrAskAiErrorCallout error={null} domain="search" />
            )}
            <div
                css={css`
                    position: relative;
                `}
            >
                <EuiFieldText
                    autoFocus
                    inputRef={inputRef}
                    fullWidth
                    placeholder="Search the docs as you type"
                    value={inputValue}
                    onChange={(e) => {
                        const newValue = e.target.value
                        setInputValue(newValue)
                        setSearchTerm(newValue)
                    }}
                    onKeyDown={(e) => {
                        if (e.key === 'Enter') {
                            handleSearch()
                        }
                    }}
                    disabled={isSearchCooldownActive}
                />
                <EuiButtonIcon
                    aria-label="Search"
                    css={css`
                        position: absolute;
                        right: 8px;
                        top: 50%;
                        transform: translateY(-50%);
                        border-radius: 9999px;
                    `}
                    color="primary"
                    iconType="sortUp"
                    display={inputValue.trim() ? 'fill' : 'base'}
                    onClick={handleSearch}
                    disabled={isSearchCooldownActive}
                />
            </div>
            {searchTerm && (
                <>
                    <EuiSpacer size="s" />
                    <AskAiButton
                        term={searchTerm}
                        onAsk={() => {
                            // Prevent submission during countdown
                            if (isAskAiCooldownActive) {
                                return
                            }
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
    const isAskAiCooldownActive = useIsAskAiCooldownActive()
    return (
        <EuiButton
            iconType="newChat"
            fullWidth
            onClick={onAsk}
            disabled={isAskAiCooldownActive}
        >
            Ask AI about <span css={askAiButtonStyles}>"{term}"</span>
        </EuiButton>
    )
}
