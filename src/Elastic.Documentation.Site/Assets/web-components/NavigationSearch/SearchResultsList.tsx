import { SanitizedHtmlContent } from './SanitizedHtmlContent'
import { useSelectedIndex, useSearchActions } from './navigationSearch.store'
import { useSearchTerm } from './navigationSearch.store'
import {
    useNavigationSearchQuery,
    SearchResultItem,
} from './useNavigationSearchQuery'
import { useNavigationSearchTelemetry } from './useNavigationSearchTelemetry'
import {
    EuiBadge,
    EuiIcon,
    EuiLoadingSpinner,
    useEuiTheme,
    useEuiOverflowScroll,
    useIsWithinMaxBreakpoint,
} from '@elastic/eui'
import { css } from '@emotion/react'
import htmx from 'htmx.org'
import { useRef, useMemo, MutableRefObject, useEffect, useState } from 'react'

const RESULTS_MAX_HEIGHT = 465
const BREADCRUMB_SEPARATOR = ' / '

/**
 * Gets the first segment from a URL path after removing /docs/ prefix.
 * Matches the pattern from the original SearchSuggestions component.
 */
const getFirstSegment = (path: string): string =>
    path.replace('/docs/', '/').split('/')[1] ?? ''

/**
 * Returns the appropriate hx-select-oob value based on whether
 * the target URL is in the same top-level group as the current URL.
 */
const getHxSelectOob = (targetUrl: string, currentPathname: string): string => {
    const currentSegment = getFirstSegment(currentPathname)
    const targetSegment = getFirstSegment(targetUrl)
    return currentSegment === targetSegment
        ? '#content-container,#toc-nav'
        : '#content-container,#toc-nav,#nav-tree,#nav-dropdown'
}

/**
 * Hook that tracks the current pathname and updates when htmx navigation occurs.
 */
const useCurrentPathname = (): string => {
    const [pathname, setPathname] = useState(window.location.pathname)

    useEffect(() => {
        const handleNavigation = () => {
            setPathname(window.location.pathname)
        }

        // Listen for htmx history updates (htmx navigation)
        document.addEventListener('htmx:pushedIntoHistory', handleNavigation)
        // Listen for browser back/forward navigation
        window.addEventListener('popstate', handleNavigation)

        return () => {
            document.removeEventListener(
                'htmx:pushedIntoHistory',
                handleNavigation
            )
            window.removeEventListener('popstate', handleNavigation)
        }
    }, [])

    return pathname
}

export interface SearchResultsListProps {
    isKeyboardNavigating: MutableRefObject<boolean>
    onMouseMove: () => void
    onResultClick: () => void
}

export const SearchResultsList = ({
    isKeyboardNavigating,
    onMouseMove,
    onResultClick,
}: SearchResultsListProps) => {
    const { euiTheme } = useEuiTheme()
    const selectedIndex = useSelectedIndex()
    const { setSelectedIndex } = useSearchActions()
    const { isLoading, data } = useNavigationSearchQuery()
    const containerRef = useRef<HTMLDivElement>(null)
    const searchTerm = useSearchTerm()
    const { trackResultClicked } = useNavigationSearchTelemetry()

    const results = data?.results ?? []
    const isInitialLoading = isLoading && !data

    // Scroll to top when search term changes
    useEffect(() => {
        if (containerRef.current) {
            containerRef.current.scrollTop = 0
        }
    }, [searchTerm])

    const containerStyles = css`
        max-height: ${RESULTS_MAX_HEIGHT}px;
        overflow-y: auto;
        overflow-x: hidden;
        border-top-left-radius: ${euiTheme.size.s};
        border-top-right-radius: ${euiTheme.size.s};
        ${useEuiOverflowScroll('y', false)}
    `

    const emptyStateStyles = css`
        padding: ${euiTheme.size.xl} ${euiTheme.size.xl} ${euiTheme.size.l}
            ${euiTheme.size.xl};
        text-align: center;
        color: ${euiTheme.colors.textDisabled};
        font-size: ${euiTheme.font.scale.s * euiTheme.base}px;
        line-height: ${euiTheme.base * 1.25}px;
    `

    if (isInitialLoading) {
        return (
            <div css={containerStyles}>
                <div css={emptyStateStyles}>
                    <EuiLoadingSpinner size="xl" />
                </div>
            </div>
        )
    }

    if (results.length === 0) {
        return (
            <div css={containerStyles}>
                <div css={emptyStateStyles}>
                    We couldn't find a page that matches your search
                </div>
            </div>
        )
    }

    const handleMouseEnter = (index: number) => {
        if (!isKeyboardNavigating.current) {
            setSelectedIndex(index)
        }
    }

    const handleItemMouseMove = (index: number) => {
        if (isKeyboardNavigating.current) {
            onMouseMove()
            setSelectedIndex(index)
        }
    }

    const handleResultClick = (result: SearchResultItem, index: number) => {
        trackResultClicked({
            query: searchTerm,
            position: index,
            url: result.url,
            score: result.score,
        })
        onResultClick()
    }

    return (
        <div ref={containerRef} css={containerStyles}>
            {results.map((result, index) => (
                <SearchResultRow
                    key={result.url}
                    index={index}
                    result={result}
                    isSelected={index === selectedIndex}
                    isKeyboardNavigating={isKeyboardNavigating.current}
                    onMouseEnter={() => handleMouseEnter(index)}
                    onMouseMove={() => handleItemMouseMove(index)}
                    onClick={() => handleResultClick(result, index)}
                />
            ))}
        </div>
    )
}

interface SearchResultRowProps {
    index: number
    result: SearchResultItem
    isSelected: boolean
    isKeyboardNavigating: boolean
    onMouseEnter: () => void
    onMouseMove: () => void
    onClick: () => void
}

const SearchResultRow = ({
    index,
    result,
    isSelected,
    isKeyboardNavigating,
    onMouseEnter,
    onMouseMove,
    onClick,
}: SearchResultRowProps) => {
    const { euiTheme } = useEuiTheme()
    const isMobile = useIsWithinMaxBreakpoint('s')
    const anchorRef = useRef<HTMLAnchorElement | null>(null)
    const currentPathname = useCurrentPathname()

    const breadcrumbItems = useMemo(() => {
        const typePrefix = result.type === 'api' ? 'API' : 'Docs'
        return [typePrefix, ...result.parents.slice(1).map((p) => p.title)]
    }, [result.type, result.parents])

    // Process htmx when element mounts or when pathname changes
    useEffect(() => {
        if (anchorRef.current) {
            const hxSelectOob = getHxSelectOob(result.url, currentPathname)
            anchorRef.current.setAttribute('hx-select-oob', hxSelectOob)
            anchorRef.current.setAttribute('hx-swap', 'none')
            htmx.process(anchorRef.current)
        }
    }, [result.url, currentPathname])

    return (
        <a
            ref={anchorRef}
            href={result.url}
            data-search-result-index={index}
            onClick={onClick}
            onMouseEnter={onMouseEnter}
            onMouseMove={onMouseMove}
            data-selected={isSelected ? 'true' : 'false'}
            data-keyboard-navigating={isKeyboardNavigating ? 'true' : 'false'}
            css={css`
                display: flex;
                align-items: center;
                gap: ${euiTheme.size.s};
                padding-inline: ${euiTheme.size.l};
                padding-block: ${euiTheme.size.base};
                text-decoration: none;
                cursor: pointer;
                border-bottom: 1px solid ${euiTheme.colors.borderBaseSubdued};
                background: ${isSelected
                    ? euiTheme.colors.backgroundBaseSubdued
                    : 'transparent'};

                &:last-child {
                    border-bottom: none;
                }

                &:hover:not([data-keyboard-navigating='true']) {
                    background: ${isSelected
                        ? euiTheme.colors.backgroundBaseSubdued
                        : euiTheme.colors.backgroundBaseHighlighted};

                    .title-text {
                        color: ${euiTheme.colors.link};
                        text-decoration: underline;

                        mark {
                            color: ${euiTheme.colors.textParagraph};
                            text-decoration: underline;
                        }
                    }

                    .jump-to-indicator {
                        visibility: visible;
                    }
                }

                &[data-selected='true'] {
                    .title-text {
                        color: ${euiTheme.colors.link};
                        text-decoration: underline;

                        mark {
                            color: ${euiTheme.colors.textParagraph};
                            text-decoration: underline;
                        }
                    }

                    .jump-to-indicator {
                        visibility: visible;
                    }
                }
            `}
        >
            <div
                css={css`
                    flex: 1;
                    min-width: 0;
                    overflow: hidden;
                `}
            >
                <Breadcrumb items={breadcrumbItems} />
                <Title text={result.title} />
                {result.description && (
                    <Description text={result.description} />
                )}
            </div>
            {!isMobile && <JumpToIndicator />}
        </a>
    )
}

/**
 * Breadcrumb with 3-part layout:
 * - First: always visible
 * - Middle: truncates with ellipsis
 * - Last: always visible
 */
const Breadcrumb = ({ items }: { items: string[] }) => {
    const { euiTheme } = useEuiTheme()

    if (items.length === 0) return null

    const baseStyles = css`
        font-size: ${euiTheme.size.m};
        line-height: ${euiTheme.size.base};
        color: ${euiTheme.colors.textDisabled};
        margin-bottom: 0;
    `

    if (items.length === 1) {
        return <div css={baseStyles}>{items[0]}</div>
    }

    const first = items[0]
    const middle = items.slice(1, -1)
    const last = items[items.length - 1]

    return (
        <div
            css={css`
                ${baseStyles}
                display: flex;
                white-space: nowrap;
                overflow: hidden;
            `}
        >
            <span
                css={css`
                    flex-shrink: 0;
                `}
            >
                {first}
            </span>

            {middle.length > 0 && (
                <span
                    css={css`
                        flex-shrink: 1;
                        min-width: 0;
                        overflow: hidden;
                        text-overflow: ellipsis;
                        margin-left: 0.5ch;
                    `}
                >
                    {BREADCRUMB_SEPARATOR}
                    {middle.join(BREADCRUMB_SEPARATOR)}
                </span>
            )}

            <span
                css={css`
                    flex-shrink: 0;
                    margin-left: 0.5ch;
                `}
            >
                {BREADCRUMB_SEPARATOR}
                {last}
            </span>
        </div>
    )
}

const Title = ({ text }: { text: string }) => {
    const { euiTheme } = useEuiTheme()

    return (
        <div
            className="title-text"
            css={css`
                font-size: ${euiTheme.font.scale.s * euiTheme.base}px;
                line-height: ${euiTheme.size.l};
                font-weight: ${euiTheme.font.weight.bold};
                color: ${euiTheme.colors.textParagraph};
                margin-bottom: calc(${euiTheme.size.xs} / 2);

                mark {
                    background-color: ${euiTheme.colors.backgroundLightPrimary};
                    font-weight: ${euiTheme.font.weight.bold};
                    color: ${euiTheme.colors.textParagraph};
                }
            `}
        >
            <SanitizedHtmlContent htmlContent={text} />
        </div>
    )
}

const Description = ({ text }: { text: string }) => {
    const { euiTheme } = useEuiTheme()

    return (
        <div
            css={css`
                font-size: ${euiTheme.size.m};
                line-height: ${euiTheme.size.base};
                color: ${euiTheme.colors.textSubdued};
                white-space: nowrap;
                overflow: hidden;
                text-overflow: ellipsis;

                mark {
                    background-color: ${euiTheme.colors.backgroundLightPrimary};
                    font-weight: ${euiTheme.font.weight.bold};
                }
            `}
        >
            <SanitizedHtmlContent htmlContent={text} ellipsis />
        </div>
    )
}

const JumpToIndicator = () => {
    const { euiTheme } = useEuiTheme()

    return (
        <div
            className="jump-to-indicator"
            css={css`
                flex-shrink: 0;
                color: ${euiTheme.colors.textSubdued};
                margin-left: ${euiTheme.size.xxxxl};
                visibility: hidden;
            `}
        >
            <EuiBadge color="hollow">
                Jump to <EuiIcon type="returnKey" size="s" />
            </EuiBadge>
        </div>
    )
}
