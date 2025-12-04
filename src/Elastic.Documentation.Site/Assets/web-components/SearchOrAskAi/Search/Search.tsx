import { InfoBanner } from '../InfoBanner'
import { KeyboardShortcutsFooter } from '../KeyboardShortcutsFooter'
import { SearchOrAskAiErrorCallout } from '../SearchOrAskAiErrorCallout'
import { useModalActions } from '../modal.store'
import { SearchResults } from './SearchResults/SearchResults'
import { TellMeMoreButton } from './TellMeMoreButton'
import { useSearchActions, useSearchTerm } from './search.store'
import { useAskAiFromSearch } from './useAskAiFromSearch'
import { useIsSearchCooldownActive } from './useSearchCooldown'
import { useSearchKeyboardNavigation } from './useSearchKeyboardNavigation'
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
import { useCallback } from 'react'

export const Search = () => {
    const searchTerm = useSearchTerm()
    const { setSearchTerm, setInputFocused, clearSearchTerm } =
        useSearchActions()
    const { closeModal } = useModalActions()
    const isSearchCooldownActive = useIsSearchCooldownActive()
    const { askAi } = useAskAiFromSearch()
    const { isLoading, isFetching } = useSearchQuery()
    const mFontSize = useEuiFontSize('m').fontSize
    const { euiTheme } = useEuiTheme()

    const handleSearchInputChange = (
        e: React.ChangeEvent<HTMLInputElement>
    ) => {
        setSearchTerm(e.target.value)
    }

    const handleCloseModal = () => {
        clearSearchTerm()
        closeModal()
    }

    const handleInputFocus = useCallback(
        () => setInputFocused(true),
        [setInputFocused]
    )
    const handleInputBlur = useCallback(
        () => setInputFocused(false),
        [setInputFocused]
    )

    const {
        inputRef,
        buttonRef,
        handleInputKeyDown,
        handleListItemKeyDown,
        focusLastAvailable,
        setItemRef,
    } = useSearchKeyboardNavigation()

    const showLoadingSpinner = isLoading || isFetching

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
                {showLoadingSpinner ? (
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
                    onFocus={handleInputFocus}
                    onBlur={handleInputBlur}
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
                        onAsk={askAi}
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
