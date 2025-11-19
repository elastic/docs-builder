import { useChatActions } from '../AskAi/chat.store'
import { useIsAskAiCooldownActive } from '../AskAi/useAskAiCooldown'
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
    EuiButton,
    EuiIcon,
    EuiLoadingSpinner,
    EuiText,
    useEuiTheme,
    useEuiFontSize,
} from '@elastic/eui'
import { css } from '@emotion/react'
import { useState } from 'react'

export const Search = () => {
    const searchTerm = useSearchTerm()
    const { setSearchTerm, clearSearchTerm } = useSearchActions()
    const { submitQuestion, clearChat } = useChatActions()
    const { setModalMode, closeModal } = useModalActions()
    const isSearchCooldownActive = useIsSearchCooldownActive()
    const isAskAiCooldownActive = useIsAskAiCooldownActive()
    const [isInputFocused, setIsInputFocused] = useState(false)
    const { isLoading, isFetching } = useSearchQuery()
    const xsFontSize = useEuiFontSize('xs').fontSize
    const { euiTheme } = useEuiTheme()

    const handleSearch = (e: React.ChangeEvent<HTMLInputElement>) => {
        setSearchTerm(e.target.value)
    }

    const handleAskAi = () => {
        if (isAskAiCooldownActive || searchTerm.trim() === '') {
            return
        }
        // Always start a new conversation
        clearChat()
        submitQuestion(searchTerm)
        setModalMode('askAi')
    }

    const {
        inputRef,
        buttonRef,
        handleInputKeyDown,
        handleListItemKeyDown,
        focusLastAvailable,
        setItemRef,
    } = useKeyboardNavigation(handleAskAi)

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
                    css={css`
                        padding-inline-end: 60px;
                    `}
                    autoFocus
                    icon="empty"
                    inputRef={inputRef}
                    fullWidth
                    placeholder="Search in Docs"
                    value={searchTerm}
                    onChange={handleSearch}
                    onFocus={() => setIsInputFocused(true)}
                    onBlur={() => setIsInputFocused(false)}
                    onKeyDown={handleInputKeyDown}
                    disabled={isSearchCooldownActive}
                />
                {isLoading || isFetching ? (
                    <div
                        css={css`
                            position: absolute;
                            display: flex;
                            left: 12px;
                            top: 50%;
                            transform: translateY(-50%);
                        `}
                    >
                        <EuiLoadingSpinner size="m" />
                    </div>
                ) : (
                    <EuiIcon
                        type="search"
                        css={css`
                            position: absolute;
                            left: 12px;
                            top: 50%;
                            transform: translateY(-50%);
                        `}
                    />
                )}
                <EuiButton
                    css={`
                        position: absolute;
                        right: ${euiTheme.size.m};
                        top: 50%;
                        transform: translateY(-50%);
                        block-size: 20px;
                        font-size: ${xsFontSize};
                        padding-inline: ${euiTheme.size.s};
                        border-radius: ${euiTheme.border.radius.small};
                    `}
                    size="s"
                    color="text"
                    minWidth={false}
                    onClick={() => {
                        clearSearchTerm()
                        closeModal()
                    }}
                >
                    Esc
                </EuiButton>
            </div>
            <SearchResults
                onKeyDown={handleListItemKeyDown}
                setItemRef={setItemRef}
            />
            {searchTerm && (
                <>
                    <EuiSpacer size="s" />
                    <EuiText color="subdued" size="xs">
                        Ask AI assistant
                    </EuiText>
                    <EuiSpacer size="m" />
                    <TellMeMoreButton
                        ref={buttonRef}
                        term={searchTerm}
                        isInputFocused={isInputFocused}
                        onAsk={handleAskAi}
                        onArrowUp={focusLastAvailable}
                    />
                </>
            )}
        </>
    )
}
