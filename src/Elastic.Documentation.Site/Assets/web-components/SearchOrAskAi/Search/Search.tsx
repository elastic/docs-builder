import { availableIcons } from '../../../eui-icons-cache'
import { useChatActions } from '../AskAi/chat.store'
import { useIsAskAiCooldownActive } from '../AskAi/useAskAiCooldown'
import { InfoBanner } from '../InfoBanner'
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
    EuiHorizontalRule,
    EuiIcon,
    EuiLoadingSpinner,
    EuiText,
    useEuiTheme,
    useEuiFontSize,
} from '@elastic/eui'
import { css } from '@emotion/react'
import React, { useState } from 'react'

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
                    onFocus={() => setIsInputFocused(true)}
                    onBlur={() => setIsInputFocused(false)}
                    onKeyDown={handleInputKeyDown}
                    disabled={isSearchCooldownActive}
                />
                <EuiButton
                    css={`
                        block-size: 20px;
                        font-size: ${xsFontSize};
                        padding-inline: ${euiTheme.size.s};
                        border-radius: ${euiTheme.border.radius.small};
                    `}
                    size="s"
                    color="text"
                    minWidth={false}
                    onClick={handleCloseModal}
                >
                    Esc
                </EuiButton>
            </div>

            <SearchResults
                onKeyDown={handleListItemKeyDown}
                setItemRef={setItemRef}
            />
            {searchTerm && (
                <div
                    css={css`
                        padding-inline: ${euiTheme.size.base};
                    `}
                >
                    <EuiSpacer size="s" />
                    <EuiText color="subdued" size="xs">
                        Ask AI assistant
                    </EuiText>
                    <EuiSpacer size="m" />
                    <TellMeMoreButton
                        ref={buttonRef}
                        term={searchTerm}
                        isInputFocused={isInputFocused}
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

const SearchFooter = () => {
    const { euiTheme } = useEuiTheme()
    return (
        <>
            <EuiHorizontalRule margin="none" />
            <div
                css={css`
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    gap: ${euiTheme.size.m};
                    background-color: ${euiTheme.colors.backgroundBaseSubdued};
                    border-bottom-right-radius: ${euiTheme.border.radius.small};
                    border-bottom-left-radius: ${euiTheme.border.radius.small};
                    padding-inline: ${euiTheme.size.base};
                    padding-block: ${euiTheme.size.m};
                `}
            >
                <KeyboardIconsWithLabel
                    types={['returnKey']}
                    label="to select"
                />
                <KeyboardIconsWithLabel
                    types={['sortUp', 'sortDown']}
                    label="to navigate"
                />
                <KeyboardIconsWithLabel types={['Esc']} label="to close" />
            </div>
        </>
    )
}

interface KeyboardIconProps {
    type: string
}

const KeyboardKey = ({ children }: { children: React.ReactNode }) => {
    const { euiTheme } = useEuiTheme()
    return (
        <span
            css={css`
                display: inline-flex;
                justify-content: center;
                align-items: center;
                background-color: ${euiTheme.colors.backgroundLightText};
                min-width: ${euiTheme.size.l};
                height: ${euiTheme.size.l};
                border-radius: ${euiTheme.border.radius.small};
                padding-inline: ${euiTheme.size.xs};
            `}
        >
            {children}
        </span>
    )
}

const KeyboardIcon = ({ type }: KeyboardIconProps) => {
    const { euiTheme } = useEuiTheme()
    return (
        <KeyboardKey>
            {availableIcons.includes(type) ? (
                <EuiIcon type={type} size="s" />
            ) : (
                <EuiText
                    size="xs"
                    css={css`
                        margin-inline: ${euiTheme.size.xs};
                    `}
                >
                    {type}
                </EuiText>
            )}
        </KeyboardKey>
    )
}

const KeyboardIconsWithLabel = ({
    types,
    label,
}: {
    types: string[]
    label: string
}) => {
    const { euiTheme } = useEuiTheme()
    return (
        <span
            css={css`
                display: flex;
                gap: ${euiTheme.size.xs};
            `}
        >
            <span
                css={css`
                    display: flex;
                    gap: ${euiTheme.size.xs};
                `}
            >
                {types.map((type, index) => (
                    <KeyboardIcon type={types[index]} key={type + index} />
                ))}
            </span>
            <span>{label}</span>
        </span>
    )
}
