import { availableIcons } from '../../eui-icons-cache'
import { SearchInput } from './SearchInput'
import { SearchResultsList } from './SearchResultsList'
import { useSearchTerm, useSearchActions } from './navigationSearch.store'
import { useGlobalKeyboardShortcut } from './useGlobalKeyboardShortcut'
import { useIsNavigationSearchCooldownActive } from './useNavigationSearchCooldown'
import { useNavigationSearchKeyboardNavigation } from './useNavigationSearchKeyboardNavigation'
import { useNavigationSearchQuery } from './useNavigationSearchQuery'
import {
    EuiInputPopover,
    useEuiTheme,
    useEuiFontSize,
    EuiBetaBadge,
    EuiLink,
    EuiIcon,
    EuiText,
    EuiHorizontalRule,
    useIsWithinMaxBreakpoint,
} from '@elastic/eui'
import { css } from '@emotion/react'
import { useRef, useState, useEffect } from 'react'

export const NavigationSearch = () => {
    const { euiTheme } = useEuiTheme()
    const isMobile = useIsWithinMaxBreakpoint('s')
    const [isPopoverOpen, setIsPopoverOpen] = useState(false)
    const popoverContentRef = useRef<HTMLDivElement>(null)
    const searchTerm = useSearchTerm()
    const { setSearchTerm } = useSearchActions()
    const isSearchCooldownActive = useIsNavigationSearchCooldownActive()
    const { isLoading, isFetching, data } = useNavigationSearchQuery()

    const results = data?.results ?? []
    const hasContent = !!searchTerm.trim()
    const isSearching = isLoading || isFetching

    const handleResultClick = () => {
        // Handled by htmx event listeners
    }

    const {
        inputRef,
        isKeyboardNavigating,
        handleInputKeyDown,
        handleMouseMove,
    } = useNavigationSearchKeyboardNavigation({
        resultsCount: results.length,
        isLoading: isSearching,
        onClose: () => setIsPopoverOpen(false),
        onNavigate: handleResultClick,
    })

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const value = e.target.value
        setSearchTerm(value)
        setIsPopoverOpen(!!value.trim())
    }

    const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
        if (e.key === 'Escape') {
            e.preventDefault()
            setSearchTerm('')
            setIsPopoverOpen(false)
            return
        }
        handleInputKeyDown(e)
    }

    const handleBlur = (e: React.FocusEvent) => {
        // Check if focus is moving to something inside the popover
        const relatedTarget = e.relatedTarget as Node | null
        if (
            relatedTarget &&
            popoverContentRef.current?.contains(relatedTarget)
        ) {
            // Focus is moving inside the popover, don't close
            return
        }
        setIsPopoverOpen(false)
    }

    useGlobalKeyboardShortcut('k', () => {
        inputRef.current?.focus()
        inputRef.current?.select()
    })

    // Close popover and blur input when htmx navigation starts from a search result
    useEffect(() => {
        const handleBeforeSend = (event: CustomEvent) => {
            const trigger = event.detail?.elt as HTMLElement | undefined
            if (trigger?.hasAttribute('data-search-result-index')) {
                setIsPopoverOpen(false)
                inputRef.current?.blur()
            }
        }

        document.addEventListener(
            'htmx:beforeSend',
            handleBeforeSend as EventListener
        )
        return () => {
            document.removeEventListener(
                'htmx:beforeSend',
                handleBeforeSend as EventListener
            )
        }
    }, [inputRef])

    return (
        <div
            className="sticky top-0"
            css={css`
                padding-top: ${euiTheme.size.base};
                padding-right: ${euiTheme.size.base};
            `}
        >
            <EuiInputPopover
                isOpen={hasContent}
                closePopover={() => setIsPopoverOpen(false)}
                ownFocus={false}
                disableFocusTrap={true}
                panelMinWidth={isMobile ? undefined : 640}
                panelPaddingSize="none"
                offset={12}
                panelProps={{
                    css: css`
                        border-radius: ${euiTheme.size.s};
                        visibility: ${isPopoverOpen ? 'visible' : 'hidden'};
                        opacity: ${isPopoverOpen ? 1 : 0};
                        pointer-events: ${isPopoverOpen ? 'auto' : 'none'};
                    `,
                    onMouseDown: (e: React.MouseEvent) => {
                        // Prevent input blur when clicking anywhere inside the popover panel
                        e.preventDefault()
                    },
                }}
                input={
                    <>
                        <SearchInput
                            inputRef={inputRef}
                            value={searchTerm}
                            onChange={handleChange}
                            onFocus={() => {
                                if (hasContent) {
                                    setIsPopoverOpen(true)
                                }
                            }}
                            onBlur={handleBlur}
                            onKeyDown={handleKeyDown}
                            disabled={isSearchCooldownActive}
                            isLoading={isSearching}
                        />
                    </>
                }
            >
                {hasContent && (
                    <div ref={popoverContentRef}>
                        <SearchDropdownContent
                            isKeyboardNavigating={isKeyboardNavigating}
                            onMouseMove={handleMouseMove}
                            onResultClick={handleResultClick}
                        />
                    </div>
                )}
            </EuiInputPopover>
            <EuiHorizontalRule
                margin="none"
                css={css`
                    margin-top: ${euiTheme.size.base};
                `}
            />
        </div>
    )
}

const FEEDBACK_URL =
    'https://github.com/elastic/docs-eng-team/issues/new?template=search-or-ask-ai-feedback.yml'

const KEYBOARD_SHORTCUTS = [
    { keys: ['returnKey'], label: 'Jump to' },
    { keys: ['sortUp', 'sortDown'], label: 'Navigate' },
    { keys: ['Esc'], label: 'Close' },
]

interface SearchDropdownContentProps {
    isKeyboardNavigating: React.MutableRefObject<boolean>
    onMouseMove: () => void
    onResultClick: () => void
}

const SearchDropdownContent = ({
    isKeyboardNavigating,
    onMouseMove,
    onResultClick,
}: SearchDropdownContentProps) => {
    return (
        <>
            <SearchResultsList
                isKeyboardNavigating={isKeyboardNavigating}
                onMouseMove={onMouseMove}
                onResultClick={onResultClick}
            />
            <SearchDropdownFooter />
        </>
    )
}

const SearchDropdownFooter = () => {
    const { euiTheme } = useEuiTheme()
    const { fontSize: sFontsize, lineHeight: sLineHeight } = useEuiFontSize('s')
    const isMobile = useIsWithinMaxBreakpoint('s')

    return (
        <div
            css={css`
                display: flex;
                align-items: center;
                justify-content: space-between;
                min-height: 40px;
                box-sizing: content-box;
                border-top: 1px solid ${euiTheme.colors.borderBaseSubdued};
                background-color: ${euiTheme.colors.backgroundBasePlain};
                border-bottom-right-radius: ${euiTheme.size.s};
                border-bottom-left-radius: ${euiTheme.size.s};
                padding-inline: ${euiTheme.size.base};
                padding-block: ${euiTheme.size.xs};
            `}
        >
            <div
                css={css`
                    display: flex;
                    align-items: center;
                    gap: ${euiTheme.size.s};
                `}
            >
                <EuiBetaBadge
                    color="accent"
                    label="ALPHA"
                    size="s"
                    anchorProps={{
                        css: css`
                            display: inline-flex;
                            align-items: center;
                        `,
                    }}
                />
                <span
                    css={css`
                        font-size: ${euiTheme.size.m};
                        color: ${euiTheme.colors.textDisabled};
                    `}
                >
                    ·
                </span>
                <EuiLink
                    href={FEEDBACK_URL}
                    target="_blank"
                    external
                    css={css`
                        font-size: ${sFontsize};
                        line-height: ${sLineHeight};
                    `}
                >
                    Give feedback
                </EuiLink>
            </div>
            {!isMobile && (
                <div
                    css={css`
                        display: flex;
                        align-items: center;
                        gap: ${euiTheme.size.base};
                    `}
                >
                    {KEYBOARD_SHORTCUTS.map((shortcut, index) => (
                        <KeyboardShortcutItem
                            key={index}
                            keys={shortcut.keys}
                            label={shortcut.label}
                        />
                    ))}
                </div>
            )}
        </div>
    )
}

const KeyboardKey = ({
    children,
    className,
}: {
    children: React.ReactNode
    className?: string
}) => {
    const { euiTheme } = useEuiTheme()
    return (
        <span
            className={className}
            css={css`
                display: inline-flex;
                justify-content: center;
                align-items: center;
                background-color: ${euiTheme.colors.backgroundBaseHighlighted};
                border: 1px solid ${euiTheme.colors.borderBasePlain};
                border-radius: ${euiTheme.border.radius.small};
                padding: 2px 8px;

                &.keyboard-key-icon {
                    padding-inline: 2px;
                }
            `}
        >
            {children}
        </span>
    )
}

const KeyboardIcon = ({ type }: { type: string }) => {
    const { euiTheme } = useEuiTheme()
    const hasIcon = availableIcons.includes(type)
    return (
        <KeyboardKey
            className={hasIcon ? 'keyboard-key-icon' : 'keyboard-key-text'}
        >
            {hasIcon ? (
                <EuiIcon
                    type={type}
                    size="s"
                    css={css`
                        color: ${euiTheme.colors.textSubdued};
                    `}
                />
            ) : (
                <span
                    className="keyboard-key-text"
                    css={css`
                        color: ${euiTheme.colors.textSubdued};
                        font-size: 11px;
                        line-height: 16px;
                        display: inline-block;
                        font-family: ${euiTheme.font.family};
                        font-weight: ${euiTheme.font.weight.regular};
                    `}
                >
                    {type}
                </span>
            )}
        </KeyboardKey>
    )
}

const KeyboardShortcutItem = ({
    keys,
    label,
}: {
    keys: string[]
    label: string
}) => {
    const { euiTheme } = useEuiTheme()
    return (
        <span
            css={css`
                display: flex;
                align-items: center;
                gap: ${euiTheme.size.xs};
            `}
        >
            <span
                css={css`
                    display: flex;
                    gap: ${euiTheme.size.xs};
                `}
            >
                {keys.map((key, index) => (
                    <KeyboardIcon type={key} key={key + index} />
                ))}
            </span>
            <EuiText
                size="xs"
                css={css`
                    color: ${euiTheme.colors.textSubdued};
                `}
            >
                {label}
            </EuiText>
        </span>
    )
}
