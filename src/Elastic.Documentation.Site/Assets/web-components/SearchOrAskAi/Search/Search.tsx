/** @jsxImportSource @emotion/react */
import { useChatActions } from '../AskAi/chat.store'
import { useIsAskAiCooldownActive } from '../AskAi/useAskAiCooldown'
import { SearchOrAskAiErrorCallout } from '../SearchOrAskAiErrorCallout'
import { useModalActions } from '../modal.store'
import { SearchResults } from './SearchResults'
import { useSearchActions, useSearchTerm } from './search.store'
import { useIsSearchCooldownActive } from './useSearchCooldown'
import { useSearchQuery } from './useSearchQuery'
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
    const inputRef = useRef<HTMLInputElement>(null)
    const [inputValue, setInputValue] = useState(searchTerm)
    const { isLoading, isFetching, cancelQuery, refetch } = useSearchQuery({
        searchTerm,
    })
    const [isButtonVisible, setIsButtonVisible] = useState(false)
    const [isAnimatingOut, setIsAnimatingOut] = useState(false)

    const triggerSearch = useCallback(() => {
        refetch()
    }, [refetch])

    const handleSearch = useCallback(
        (e?: React.ChangeEvent<HTMLInputElement>) => {
            const newValue = e?.target.value ?? inputRef.current?.value ?? ''
            setInputValue(newValue)
            setSearchTerm(newValue)
        },
        [
            searchTerm,
            isSearchCooldownActive,
            isAskAiCooldownActive,
            clearChat,
            submitQuestion,
            setModalMode,
        ]
    )
    const handleChat = useCallback(() => {
        if (isAskAiCooldownActive || searchTerm.trim() === '') {
            return
        }
        // Always start a new conversation
        clearChat()
        submitQuestion(searchTerm)
        setModalMode('askAi')
    }, [
        searchTerm,
        isAskAiCooldownActive,
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

    // Handle button visibility and animation
    useEffect(() => {
        const hasSearchTerm = searchTerm.trim() !== ''

        if (hasSearchTerm && !isButtonVisible) {
            // Show button with slide in animation
            setIsButtonVisible(true)
            setIsAnimatingOut(false)
        } else if (!hasSearchTerm && isButtonVisible) {
            // Start exit animation
            setIsAnimatingOut(true)
            // Remove button after animation completes
            const timer = setTimeout(() => {
                setIsButtonVisible(false)
                setIsAnimatingOut(false)
            }, 200) // Match animation duration
            return () => clearTimeout(timer)
        }
    }, [searchTerm, isButtonVisible])

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
                    onChange={handleSearch}
                    onKeyDown={(e) => {
                        if (e.key === 'Enter') {
                            handleChat()
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
                    iconType="search"
                    display={inputValue.trim() ? 'fill' : 'base'}
                    onClick={triggerSearch}
                    disabled={isSearchCooldownActive}
                    isLoading={isLoading || isFetching}
                />
                {isButtonVisible && (
                    <EuiButtonIcon
                        aria-label={
                            isLoading || isFetching
                                ? 'Cancel search'
                                : 'Clear search'
                        }
                        className={
                            isAnimatingOut
                                ? 'slideOutSearchOrAskAiInputAnimation'
                                : 'slideInSearchOrAskAiInputAnimation'
                        }
                        css={css`
                            position: absolute;
                            right: 40px;
                            top: 50%;
                            transform: translateY(-50%);
                            border-radius: 9999px;
                        `}
                        color="accentSecondary"
                        iconType={isLoading || isFetching ? 'cross' : 'trash'}
                        display="fill"
                        onClick={() => {
                            if (isLoading || isFetching) {
                                cancelQuery()
                            } else {
                                setSearchTerm('')
                            }
                        }}
                    />
                )}
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
