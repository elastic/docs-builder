import { useChatActions } from '../AskAi/chat.store'
import { useIsAskAiCooldownActive } from '../AskAi/useAskAiCooldown'
import { InfoBanner } from '../InfoBanner'
import { KeyboardShortcutsFooter } from '../KeyboardShortcutsFooter'
import { SearchOrAskAiErrorCallout } from '../SearchOrAskAiErrorCallout'
import { useModalActions } from '../modal.store'
import { SearchResults } from './SearchResults/SearchResults'
import { TellMeMoreButton } from './TellMeMoreButton'
import { useSearchActions, useSearchTerm } from './search.store'
import { useKeyboardNavigation } from './useKeyboardNavigation'
import { useIsSearchCooldownActive } from './useSearchCooldown'
import { useSearchQuery } from './useSearchQuery'
import {
    EuiFieldText,
    EuiSpacer,
    EuiHorizontalRule,
    EuiIcon,
    EuiLoadingSpinner,
    EuiText,
    useEuiTheme,
    useEuiFontSize,
    EuiButtonIcon,
} from '@elastic/eui'
import { css } from '@emotion/react'
import React from 'react'

export const Search = () => {
    const searchTerm = useSearchTerm()
    const { setSearchTerm, clearSearchTerm } = useSearchActions()
    const { submitQuestion, clearChat } = useChatActions()
    const { setModalMode, closeModal } = useModalActions()
    const isSearchCooldownActive = useIsSearchCooldownActive()
    const isAskAiCooldownActive = useIsAskAiCooldownActive()
    const { isLoading, isFetching } = useSearchQuery()
    const mFontSize = useEuiFontSize('m').fontSize
    const { euiTheme } = useEuiTheme()

    const handleSearchInputChange = (
        e: React.ChangeEvent<HTMLInputElement>
    ) => {
        setSearchTerm(e.target.value)
    }

    const handleAskAiClick = () => {
        const trimmedSearchTerm = searchTerm.trim()
        if (isAskAiCooldownActive || trimmedSearchTerm === '') {
            return
        }
        clearChat()
        submitQuestion(trimmedSearchTerm)
        setModalMode('askAi')
    }

    const handleCloseModal = () => {
        clearSearchTerm()
        closeModal()
    }

    const {
        inputRef,
        buttonRef,
        handleInputKeyDown,
        handleListItemKeyDown,
        focusLastAvailable,
        setItemRef,
    } = useKeyboardNavigation(handleAskAiClick)

    return (
        <>
            {!searchTerm.trim() && (
                <SearchOrAskAiErrorCallout error={null} domain="search" />
            )}
            <div
                css={css`
                    display: grid;
                    grid-template-columns: auto 1fr auto;
                    gap: ${euiTheme.size.m};
                    align-items: center;
                    height: 56px;
                    padding-inline: ${euiTheme.size.base};
                `}
            >
                {isLoading || isFetching ? (
                    <EuiLoadingSpinner size="m" />
                ) : (
                    <EuiIcon type="search" size="m" />
                )}
                <EuiFieldText
                    css={css`
                        box-shadow: none !important;
                        outline: none !important;
                        font-size: ${mFontSize};
                        padding: 0;
                    `}
                    autoFocus
                    inputRef={inputRef}
                    fullWidth
                    placeholder="Search in Docs"
                    value={searchTerm}
                    onChange={handleSearchInputChange}
                    onKeyDown={handleInputKeyDown}
                    disabled={isSearchCooldownActive}
                />
                <EuiButtonIcon
                    aria-label="Close search modal"
                    iconType="cross"
                    color="text"
                    onClick={handleCloseModal}
                />
            </div>

            <SearchResults
                onKeyDown={handleListItemKeyDown}
                setItemRef={setItemRef}
            />
            <EuiHorizontalRule margin="none" />
            {searchTerm && (
                <div
                    css={css`
                        padding-inline: ${euiTheme.size.base};
                    `}
                >
                    <EuiSpacer size="m" />
                    <EuiText color="subdued" size="xs">
                        Ask AI assistant
                    </EuiText>
                    <EuiSpacer size="s" />
                    <TellMeMoreButton
                        ref={buttonRef}
                        term={searchTerm}
                        onAsk={handleAskAiClick}
                        onArrowUp={focusLastAvailable}
                    />
                </div>
            )}

            <InfoBanner />
            <SearchFooter />
        </>
    )
}

const SEARCH_KEYBOARD_SHORTCUTS = [
    { keys: ['returnKey'], label: 'to select' },
    { keys: ['sortUp', 'sortDown'], label: 'to navigate' },
    { keys: ['Esc'], label: 'to close' },
]

const SearchFooter = () => (
    <KeyboardShortcutsFooter shortcuts={SEARCH_KEYBOARD_SHORTCUTS} />
)
