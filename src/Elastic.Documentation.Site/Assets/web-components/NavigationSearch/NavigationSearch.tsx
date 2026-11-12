import { availableIcons } from '../../eui-icons-cache'
import { SearchInput } from './SearchInput'
import { SearchResultsList } from './SearchResultsList'
import {
    GO_TO_LABELS,
    groupSearchResults,
    navigableRowCount,
} from './apiPropertyResults'
import {
    useSearchTerm,
    useSearchActions,
    useSelectedIndex,
} from './navigationSearch.store'
import { useIsNavigationSearchCooldownActive } from './useNavigationSearchCooldown'
import { useNavigationSearchKeyboardNavigation } from './useNavigationSearchKeyboardNavigation'
import {
    useNavigationSearchQuery,
    type TypeFilter,
} from './useNavigationSearchQuery'
import { useNavigationSearchTelemetry } from './useNavigationSearchTelemetry'
import { useSlashToFocus } from './useSlashToFocus'
import {
    EuiInputPopover,
    useEuiTheme,
    EuiIcon,
    EuiText,
    useIsWithinMaxBreakpoint,
} from '@elastic/eui'
import { css } from '@emotion/react'
import { useEffect, useRef, useState, useMemo } from 'react'

interface Props {
    placeholder?: string
    size?: 's' | 'm' | 'l'
    typeFilter?: TypeFilter
}

export const NavigationSearch = ({
    placeholder = 'Jump to page',
    size = 'm',
    typeFilter = 'all',
}: Props) => {
    const { euiTheme } = useEuiTheme()
    const isMobile = useIsWithinMaxBreakpoint('s')
    const [isPopoverOpen, setIsPopoverOpen] = useState(false)
    const popoverContentRef = useRef<HTMLDivElement>(null)
    const searchTerm = useSearchTerm()
    const selectedIndex = useSelectedIndex()
    const { setSearchTerm } = useSearchActions()
    const isSearchCooldownActive = useIsNavigationSearchCooldownActive()
    const { isLoading, isFetching, data } = useNavigationSearchQuery(typeFilter)
    const { trackOpened, trackClosed } =
        useNavigationSearchTelemetry(typeFilter)

    const groups = useMemo(
        () => groupSearchResults(data?.results ?? []),
        [data]
    )
    const hasContent = !!searchTerm.trim()
    const isSearching = isLoading || isFetching

    // Mirrors isPopoverOpen so a blur that fires before React re-renders sees the popover already closed.
    const isOpenRef = useRef(false)
    const setOpen = (open: boolean) => {
        isOpenRef.current = open
        setIsPopoverOpen(open)
    }

    const close = (reason: 'navigate' | 'escape' | 'blur') => {
        if (isOpenRef.current) {
            trackClosed({
                reason,
                query: searchTerm,
                hadResults: groups.length > 0,
                hadSelection: selectedIndex >= 0,
            })
        }
        setOpen(false)
    }

    // The result link whose htmx request is in flight. The dropdown stays up until that request ends, so the reader
    // is not left on the old page with no cue. htmx sends from its listener on the link itself, ahead of React's
    // delegated onClick, so the click handler already sees it. beforeSend, not beforeRequest: preloads and the
    // new-tab and full-page-load paths in htmx-hooks cancel beforeRequest and never send.
    const pendingRef = useRef<Element | null>(null)
    const [isNavigating, setIsNavigating] = useState(false)
    const setPending = (link: Element | null) => {
        pendingRef.current = link
        setIsNavigating(!!link)
    }

    const finishNavigation = () => {
        setPending(null)
        close('navigate')
        inputRef.current?.blur()
    }

    const cancelNavigation = () => {
        const link = pendingRef.current
        if (!link) return
        // Cleared first, so the afterRequest the abort fires is not taken for the end of a navigation.
        setPending(null)
        link.dispatchEvent(
            new CustomEvent('htmx:abort', {
                bubbles: true,
                detail: { elt: link },
            })
        )
    }

    const handleResultClick = (opensElsewhere = false) => {
        if (opensElsewhere) return
        if (!pendingRef.current) finishNavigation()
    }

    const finishNavigationRef = useRef(finishNavigation)
    finishNavigationRef.current = finishNavigation

    useEffect(() => {
        const onBeforeSend = (event: Event) => {
            const link = (event as CustomEvent<{ elt?: Element }>).detail?.elt
            if (
                link?.hasAttribute('data-search-result-index') &&
                popoverContentRef.current?.contains(link)
            ) {
                setPending(link)
            }
        }
        // Fires once the response is swapped in, and on an error, abort or timeout as well.
        const onAfterRequest = (event: Event) => {
            const link = (event as CustomEvent<{ elt?: Element }>).detail?.elt
            if (link && link === pendingRef.current)
                finishNavigationRef.current()
        }
        document.addEventListener('htmx:beforeSend', onBeforeSend)
        document.addEventListener('htmx:afterRequest', onAfterRequest)
        return () => {
            document.removeEventListener('htmx:beforeSend', onBeforeSend)
            document.removeEventListener('htmx:afterRequest', onAfterRequest)
        }
    }, [])

    const {
        inputRef,
        isKeyboardNavigating,
        handleInputKeyDown,
        handleMouseMove,
    } = useNavigationSearchKeyboardNavigation({
        resultsCount: navigableRowCount(groups),
        isLoading: isSearching,
        onClose: () => setOpen(false),
        typeFilter,
    })

    const isApi = typeFilter === 'api'
    // The sidebar is off-canvas on small screens, so there is nothing to focus.
    const slashFocusEnabled = isApi && !isMobile
    useSlashToFocus({ enabled: slashFocusEnabled, inputRef })

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const value = e.target.value
        cancelNavigation()
        setSearchTerm(value)
        setOpen(!!value.trim())
    }

    const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
        if (e.key === 'Escape') {
            e.preventDefault()
            cancelNavigation()
            close('escape')
            setSearchTerm('')
            // After the re-render, so handleBlur doesn't log a second close for the same Escape.
            if (isApi) {
                queueMicrotask(() => inputRef.current?.blur())
            }
            return
        }
        if (e.key === 'Enter' && pendingRef.current) {
            e.preventDefault()
            return
        }
        handleInputKeyDown(e)
    }

    const handleBlur = (e: React.FocusEvent) => {
        if (pendingRef.current) return
        // Check if focus is moving to something inside the popover
        const relatedTarget = e.relatedTarget as Node | null
        if (
            relatedTarget &&
            popoverContentRef.current?.contains(relatedTarget)
        ) {
            // Focus is moving inside the popover, don't close
            return
        }
        close('blur')
    }

    return (
        <EuiInputPopover
            isOpen={hasContent}
            closePopover={() => setOpen(false)}
            ownFocus={false}
            anchorPosition="downLeft"
            // The panel stays mounted while hidden; an anchor jump scrolls the page under it.
            repositionOnScroll
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
                        size={size}
                        placeholder={placeholder}
                        inputRef={inputRef}
                        value={searchTerm}
                        onChange={handleChange}
                        onFocus={() => {
                            trackOpened('focus')
                            if (hasContent) {
                                setOpen(true)
                            }
                        }}
                        onBlur={handleBlur}
                        onKeyDown={handleKeyDown}
                        disabled={isSearchCooldownActive}
                        isLoading={isSearching || isNavigating}
                        keyShortcuts={slashFocusEnabled ? '/ s' : undefined}
                        shortcutHint={slashFocusEnabled ? '/' : undefined}
                        ariaLabel={isApi ? 'Explore APIs' : undefined}
                        title={
                            isApi
                                ? 'Explore API operations. Type / to focus.'
                                : undefined
                        }
                    />
                </>
            }
        >
            {hasContent && (
                <div ref={popoverContentRef}>
                    <SearchResultsList
                        results={groups}
                        isLoading={isSearching}
                        isNavigating={isNavigating}
                        isKeyboardNavigating={isKeyboardNavigating}
                        onMouseMove={handleMouseMove}
                        onResultClick={handleResultClick}
                        typeFilter={typeFilter}
                    />
                    <SearchDropdownFooter />
                </div>
            )}
        </EuiInputPopover>
    )
}

const KEYBOARD_SHORTCUTS = [
    { keys: ['returnKey'], label: GO_TO_LABELS.page },
    { keys: ['sortUp', 'sortDown'], label: 'Navigate' },
    { keys: ['Esc'], label: 'Close' },
]

const SearchDropdownFooter = () => {
    const { euiTheme } = useEuiTheme()
    const isMobile = useIsWithinMaxBreakpoint('s')

    if (isMobile) {
        return null
    }

    return (
        <div
            css={css`
                display: flex;
                align-items: center;
                justify-content: flex-end;
                min-height: 40px;
                box-sizing: content-box;
                border-top: 1px solid ${euiTheme.colors.borderBaseSubdued};
                background-color: ${euiTheme.colors.backgroundBasePlain};
                border-bottom-right-radius: ${euiTheme.size.s};
                border-bottom-left-radius: ${euiTheme.size.s};
                padding-inline: ${euiTheme.size.base};
                padding-block: ${euiTheme.size.xs};
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
