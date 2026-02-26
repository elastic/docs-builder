import '../../eui-icons-cache'
import { ElasticAiAssistantButton } from '../AskAi/ElasticAiAssistantButton'
import { InfoBanner } from '../AskAi/InfoBanner'
import { KeyboardShortcutsFooter } from '../AskAi/KeyboardShortcutsFooter'
import { LegalDisclaimer } from '../AskAi/LegalDisclaimer'
import { useIsNavigationSearchCooldownActive } from '../NavigationSearch/useNavigationSearchCooldown'
import { ErrorCallout } from '../shared/ErrorCallout'
import { ModalSearchResultsList } from './ModalSearchResultsList'
import {
    useSearchTerm,
    useModalSearchActions,
    useModalIsOpen,
    useSelectedIndex,
} from './modalSearch.store'
import { useModalSearchKeyboardNavigation } from './useModalSearchKeyboardNavigation'
import { useModalSearchQuery } from './useModalSearchQuery'
import { useModalSearchTelemetry } from './useModalSearchTelemetry'
import {
    EuiPortal,
    EuiPanel,
    EuiFieldText,
    EuiButtonIcon,
    EuiHorizontalRule,
    EuiSpacer,
    EuiText,
    EuiLoadingSpinner,
    EuiIcon,
    euiShadow,
    useEuiTheme,
    useEuiFontSize,
} from '@elastic/eui'
import { css } from '@emotion/react'
import { useEffect, useCallback, useRef } from 'react'

const SEARCH_KEYBOARD_SHORTCUTS = [
    { keys: ['returnKey'], label: 'Select' },
    { keys: ['sortUp', 'sortDown'], label: 'Navigate' },
    { keys: ['Esc'], label: 'Close' },
]

interface ModalSearchContentProps {
    placeholder?: string
    onClose: () => void
}

const ModalSearchContent = ({
    onClose,
    placeholder,
}: ModalSearchContentProps) => {
    const { euiTheme } = useEuiTheme()
    const mFontSize = useEuiFontSize('m').fontSize
    const searchTerm = useSearchTerm()
    const selectedIndex = useSelectedIndex()
    const { setSearchTerm, setSelectedIndex } = useModalSearchActions()
    const isSearchCooldownActive = useIsNavigationSearchCooldownActive()
    const { isLoading, isFetching, data, error } = useModalSearchQuery()
    const { trackClosed } = useModalSearchTelemetry()

    const results = data?.results ?? []
    const isSearching = isLoading || isFetching

    const handleResultClick = () => {
        trackClosed({
            reason: 'navigate',
            query: searchTerm,
            hadResults: results.length > 0,
            hadSelection: selectedIndex >= 0,
        })
    }

    const {
        inputRef,
        panelRef,
        isKeyboardNavigating,
        handleInputKeyDown,
        handleMouseMove,
    } = useModalSearchKeyboardNavigation({
        resultsCount: results.length,
        isLoading: isSearching,
        onClose: () => {
            trackClosed({
                reason: 'escape',
                query: searchTerm,
                hadResults: results.length > 0,
                hadSelection: selectedIndex >= 0,
            })
            onClose()
        },
        onNavigate: handleResultClick,
    })

    const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
        if (
            e.key === 'Tab' &&
            !e.shiftKey &&
            results.length > 0 &&
            selectedIndex < 0
        ) {
            e.preventDefault()
            setSelectedIndex(0)
            return
        }
        handleInputKeyDown(e)
    }

    const handleAskAi = () => {
        const trimmed = searchTerm.trim()
        if (!trimmed) return

        onClose()
        document.dispatchEvent(
            new CustomEvent('ask-ai:ask', {
                detail: `Tell me more about ${trimmed}`,
            })
        )
    }

    useEffect(() => {
        const handleEscape = (e: KeyboardEvent) => {
            if (e.key === 'Escape') {
                e.preventDefault()
                e.stopPropagation()
                trackClosed({
                    reason: 'escape',
                    query: searchTerm,
                    hadResults: results.length > 0,
                    hadSelection: selectedIndex >= 0,
                })
                onClose()
            }
        }
        window.addEventListener('keydown', handleEscape, { capture: true })
        return () =>
            window.removeEventListener('keydown', handleEscape, {
                capture: true,
            })
    }, [trackClosed, searchTerm, results.length, selectedIndex, onClose])

    useEffect(() => {
        inputRef.current?.focus()
    }, [inputRef])

    return (
        <div ref={panelRef}>
            {!searchTerm.trim() && (
                <ErrorCallout
                    error={null}
                    domain="search"
                    title="Error loading search results"
                />
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
                {isSearching ? (
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
                    placeholder={placeholder}
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    onKeyDown={handleKeyDown}
                    disabled={isSearchCooldownActive}
                />
                <EuiButtonIcon
                    aria-label="Close search modal"
                    iconType="cross"
                    color="text"
                    onClick={onClose}
                    tabIndex={-1}
                />
            </div>

            {searchTerm.trim() && (
                <>
                    <ErrorCallout
                        error={error ?? null}
                        domain="search"
                        title="Error loading search results"
                    />

                    <EuiHorizontalRule margin="none" />

                    <ModalSearchResultsList
                        isKeyboardNavigating={isKeyboardNavigating}
                        onMouseMove={handleMouseMove}
                        onResultClick={handleResultClick}
                    />
                </>
            )}

            {<EuiHorizontalRule margin="none" />}

            {searchTerm && (
                <div
                    css={css`
                        padding-inline: ${euiTheme.size.base};
                    `}
                >
                    <EuiSpacer size="m" />
                    <EuiText
                        color="default"
                        size="xs"
                        css={css`
                            font-weight: 500;
                        `}
                    >
                        Ask AI Assistant
                    </EuiText>
                    <EuiSpacer size="s" />
                    <ElasticAiAssistantButton
                        fullWidth
                        size="s"
                        onClick={handleAskAi}
                        css={css`
                            height: 40px;
                            & > span {
                                justify-content: flex-start;
                            }
                        `}
                    >
                        <span>Tell me more about</span>{' '}
                        <span
                            css={css`
                                font-weight: ${euiTheme.font.weight.bold};
                            `}
                        >
                            {searchTerm.trim()}
                        </span>
                    </ElasticAiAssistantButton>
                    <EuiSpacer size="m" />
                    <LegalDisclaimer />
                </div>
            )}

            <InfoBanner />
            <KeyboardShortcutsFooter shortcuts={SEARCH_KEYBOARD_SHORTCUTS} />
        </div>
    )
}

interface ModalSearchProps {
    placeholder?: string
    size?: 's' | 'm' | 'l'
}

const ModalSearchTrigger = ({
    placeholder,
    size,
    onClick,
}: {
    placeholder: string
    size: 's' | 'm' | 'l'
    onClick: () => void
}) => {
    const { euiTheme } = useEuiTheme()
    const isMac =
        typeof navigator !== 'undefined' &&
        /Mac|iPod|iPhone|iPad/.test(navigator.platform)

    return (
        <div
            role="button"
            tabIndex={0}
            onClick={onClick}
            onKeyDown={(e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault()
                    onClick()
                }
            }}
            aria-label="Open search"
            css={css`
                position: relative;
                display: flex;
                align-items: center;
                width: 100%;
                padding: calc(
                        ${size === 's' ? euiTheme.size.xs : euiTheme.size.s} +
                            2px
                    )
                    ${size === 's' ? euiTheme.size.s : euiTheme.size.m};
                padding-left: 34px;
                padding-right: calc(
                    ${euiTheme.size.m} + ${isMac ? '2ch' : '4ch'} +
                        ${euiTheme.size.m}
                );
                border: 1px solid ${euiTheme.colors.borderBasePlain};
                border-radius: ${euiTheme.border.radius.medium};
                background: ${euiTheme.colors.backgroundBaseSubdued};
                font-size: ${euiTheme.font.scale.s * euiTheme.base}px;
                line-height: ${euiTheme.base * 1.25}px;
                color: ${euiTheme.colors.textDisabled};
                cursor: pointer;

                &:hover {
                    border-color: ${euiTheme.colors.borderBasePlain};
                }

                &:focus-visible {
                    outline: 2px solid ${euiTheme.colors.primary};
                    outline-offset: 2px;
                }
            `}
        >
            <EuiIcon
                type="search"
                size="m"
                css={css`
                    position: absolute;
                    left: ${euiTheme.base * 0.75}px;
                    color: ${euiTheme.colors.textDisabled};
                `}
            />
            <span
                css={css`
                    flex: 1;
                    text-align: left;
                `}
            >
                {placeholder}
            </span>
            <span
                css={css`
                    position: absolute;
                    right: ${euiTheme.size.m};
                    color: ${euiTheme.colors.textDisabled};
                    font-size: ${euiTheme.font.scale.s * euiTheme.base}px;
                `}
            >
                {isMac ? '⌘K' : 'Ctrl+K'}
            </span>
        </div>
    )
}

export const ModalSearch = ({
    placeholder = 'Search',
    size = 'm',
}: ModalSearchProps) => {
    const isOpen = useModalIsOpen()
    const { openModal, closeModal } = useModalSearchActions()
    const { trackOpened } = useModalSearchTelemetry()
    const euiThemeContext = useEuiTheme()
    const { euiTheme } = euiThemeContext

    const handleTriggerClick = () => {
        trackOpened('click')
        openModal()
    }

    useEffect(() => {
        const handleKeyDown = (e: KeyboardEvent) => {
            if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
                e.preventDefault()
                trackOpened('keyboard_shortcut')
                openModal()
            }
        }
        window.addEventListener('keydown', handleKeyDown)
        return () => window.removeEventListener('keydown', handleKeyDown)
    }, [openModal, trackOpened])

    useEffect(() => {
        const handleOpenEvent = () => {
            trackOpened('click')
            openModal()
        }
        document.addEventListener('modal-search:open', handleOpenEvent)
        return () =>
            document.removeEventListener('modal-search:open', handleOpenEvent)
    }, [openModal, trackOpened])

    const backdropRef = useRef<HTMLDivElement>(null)

    useEffect(() => {
        if (isOpen) {
            const scrollbarWidth =
                window.innerWidth - document.documentElement.clientWidth
            document.body.style.overflow = 'hidden'
            document.body.style.paddingRight = `${scrollbarWidth}px`
        } else {
            document.body.style.overflow = ''
            document.body.style.paddingRight = ''
        }
        return () => {
            document.body.style.overflow = ''
            document.body.style.paddingRight = ''
        }
    }, [isOpen])

    useEffect(() => {
        if (!isOpen) return

        const handleBeforeSend = (event: CustomEvent) => {
            const trigger = event.detail?.elt as HTMLElement | undefined
            if (trigger?.hasAttribute('data-search-result-index')) {
                if (backdropRef.current) {
                    backdropRef.current.style.display = 'none'
                }
                document.body.style.overflow = ''
                document.body.style.paddingRight = ''
            }
        }

        const handleAfterSwap = (event: CustomEvent) => {
            const trigger = event.detail?.requestConfig?.elt as
                | HTMLElement
                | undefined
            if (trigger?.hasAttribute('data-search-result-index')) {
                closeModal()
            }
        }

        document.addEventListener(
            'htmx:beforeSend',
            handleBeforeSend as EventListener
        )
        document.addEventListener(
            'htmx:afterSwap',
            handleAfterSwap as EventListener
        )
        return () => {
            document.removeEventListener(
                'htmx:beforeSend',
                handleBeforeSend as EventListener
            )
            document.removeEventListener(
                'htmx:afterSwap',
                handleAfterSwap as EventListener
            )
        }
    }, [isOpen, closeModal])

    const handleBackdropClick = useCallback(
        (e: React.MouseEvent<HTMLDivElement>) => {
            if (e.target === e.currentTarget) {
                closeModal()
            }
        },
        [closeModal]
    )

    return (
        <>
            <ModalSearchTrigger
                placeholder={placeholder}
                size={size}
                onClick={handleTriggerClick}
            />
            {isOpen && (
                <EuiPortal>
                    <div
                        ref={backdropRef}
                        onClick={handleBackdropClick}
                        css={css`
                            position: fixed;
                            inset: 0;
                            z-index: ${euiTheme.levels.mask};
                            background-color: rgba(0, 0, 0, 0.5);
                            display: flex;
                            justify-content: center;
                            padding-top: 10vh;
                            padding-inline: ${euiTheme.size.base};
                        `}
                    >
                        <EuiPanel
                            hasBorder={false}
                            hasShadow={false}
                            paddingSize="none"
                            css={css`
                                width: 100%;
                                max-width: 640px;
                                max-height: 80vh;
                                overflow-y: auto;
                                border-radius: ${euiTheme.size.s};
                                align-self: flex-start;
                                ${euiShadow(euiThemeContext, 'xl')};
                            `}
                        >
                            <ModalSearchContent
                                onClose={closeModal}
                                placeholder={placeholder}
                            />
                        </EuiPanel>
                    </div>
                </EuiPortal>
            )}
        </>
    )
}
