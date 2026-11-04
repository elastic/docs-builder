import { config } from '../../config'
import { JumpToIndicator } from './JumpToIndicator'
import { PropertyResultRow } from './PropertyResultRow'
import { RouteLine } from './RouteLine'
import { SanitizedHtmlContent } from './SanitizedHtmlContent'
import {
    GO_TO_LABELS,
    fieldSearchTerms,
    primaryRoute,
    type ApiPropertyResult,
    type ResultGroup,
} from './apiPropertyResults'
import {
    useSelectedIndex,
    useSearchActions,
    useSearchTerm,
} from './navigationSearch.store'
import { SearchResultItem, type TypeFilter } from './useNavigationSearchQuery'
import { useNavigationSearchTelemetry } from './useNavigationSearchTelemetry'
import {
    EuiLoadingSpinner,
    useEuiTheme,
    useEuiOverflowScroll,
    useIsWithinMaxBreakpoint,
} from '@elastic/eui'
import { css } from '@emotion/react'
import htmx from 'htmx.org'
import { useRef, useMemo, MutableRefObject, useEffect } from 'react'
import type { Instance } from 'tippy.js'

const RESULTS_MAX_HEIGHT = 465
const BREADCRUMB_SEPARATOR = ' / '

export const formatApiProductCrumb = (title: string) => {
    if (/ API$/i.test(title)) {
        return title
    }

    const displayName = title
        .split(/[-_]/)
        .map((word) => word.charAt(0).toUpperCase() + word.slice(1))
        .join(' ')
    return `${displayName} API`
}

export const navigationSearchBreadcrumbs = (
    parents: SearchResultItem['parents'],
    typeFilter: TypeFilter,
    buildType: string
) => {
    if (typeFilter === 'api') {
        const [, product, ...rest] = parents
        return [
            'API',
            ...(product ? [formatApiProductCrumb(product.title)] : []),
            ...rest.map((parent) => parent.title),
        ]
    }

    if (buildType === 'codex') {
        return parents.map((parent) => parent.title)
    }

    return ['Docs', ...parents.slice(1).map((parent) => parent.title)]
}

/** A Ctrl, Cmd or non-primary click opens the link in a new tab, so the search stays where it was. htmx treats Shift-click as a normal navigation. */
const opensElsewhere = (event: React.MouseEvent) =>
    event.metaKey || event.ctrlKey || event.button !== 0

export const emptyStateCopy = (typeFilter: TypeFilter) =>
    typeFilter === 'api'
        ? "We couldn't find an API that matches your search"
        : "We couldn't find a page that matches your search"

export interface SearchResultsListProps {
    results: ResultGroup[]
    isLoading: boolean
    isKeyboardNavigating: MutableRefObject<boolean>
    onMouseMove: () => void
    onResultClick: (opensElsewhere: boolean) => void
    typeFilter?: TypeFilter
}

export const SearchResultsList = ({
    results,
    isLoading,
    isKeyboardNavigating,
    onMouseMove,
    onResultClick,
    typeFilter = 'all',
}: SearchResultsListProps) => {
    const { euiTheme } = useEuiTheme()
    const selectedIndex = useSelectedIndex()
    const { setSelectedIndex } = useSearchActions()
    const containerRef = useRef<HTMLDivElement>(null)
    const searchTerm = useSearchTerm()
    const { trackResultClicked } = useNavigationSearchTelemetry(typeFilter)
    const propertySearch = useMemo(
        () => fieldSearchTerms(searchTerm),
        [searchTerm]
    )

    const isInitialLoading = isLoading && results.length === 0

    // Scroll to top when search term changes
    useEffect(() => {
        if (containerRef.current) {
            containerRef.current.scrollTop = 0
        }
    }, [searchTerm])

    // React renders the rows after htmx's own pass over the page, so they need their own to inherit hx-boost.
    useEffect(() => {
        if (containerRef.current) htmx.process(containerRef.current)
    }, [results])

    // The page's tippy delegate mounts tooltips on the body and only drops them on an htmx swap, so a tooltip shown
    // over a row would outlive it. Captured now: by cleanup time React has already detached the rows it removed.
    useEffect(() => {
        const tooltipped = Array.from(
            containerRef.current?.querySelectorAll('[data-tippy-content]') ?? []
        )
        return () =>
            tooltipped.forEach((el) =>
                (el as Element & { _tippy?: Instance })._tippy?.destroy()
            )
    }, [results])

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
                <div css={emptyStateStyles}>{emptyStateCopy(typeFilter)}</div>
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

    const handleResultClick = (
        event: React.MouseEvent,
        result: SearchResultItem,
        rank: number,
        property?: ApiPropertyResult
    ) => {
        trackResultClicked({
            query: searchTerm,
            position: rank,
            url: property?.url ?? result.url,
            score: result.score,
            anchor: property?.anchor,
        })
        onResultClick(opensElsewhere(event))
    }

    return (
        <div ref={containerRef} css={containerStyles}>
            {results.map(({ result, index, rank, properties }) => (
                <div
                    key={result.url}
                    css={css`
                        margin: ${euiTheme.size.s};
                        border: 1px solid ${euiTheme.colors.borderBaseSubdued};
                        border-radius: ${euiTheme.size.s};
                        overflow: hidden;
                    `}
                >
                    <SearchResultRow
                        index={index}
                        result={result}
                        isSelected={index === selectedIndex}
                        isKeyboardNavigating={isKeyboardNavigating.current}
                        onMouseEnter={() => handleMouseEnter(index)}
                        onMouseMove={() => handleItemMouseMove(index)}
                        onClick={(event) =>
                            handleResultClick(event, result, rank)
                        }
                        typeFilter={typeFilter}
                        searchTerm={searchTerm}
                    />
                    {properties.map(({ property, index: propertyIndex }) => (
                        <PropertyResultRow
                            key={property.url}
                            index={propertyIndex}
                            property={property}
                            propertySearch={propertySearch}
                            isSelected={propertyIndex === selectedIndex}
                            isKeyboardNavigating={isKeyboardNavigating.current}
                            onMouseEnter={() => handleMouseEnter(propertyIndex)}
                            onMouseMove={() =>
                                handleItemMouseMove(propertyIndex)
                            }
                            onClick={(event) =>
                                handleResultClick(event, result, rank, property)
                            }
                        />
                    ))}
                </div>
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
    onClick: (event: React.MouseEvent) => void
    typeFilter: TypeFilter
    searchTerm: string
}

const SearchResultRow = ({
    index,
    result,
    isSelected,
    isKeyboardNavigating,
    onMouseEnter,
    onMouseMove,
    onClick,
    typeFilter,
    searchTerm,
}: SearchResultRowProps) => {
    const { euiTheme } = useEuiTheme()
    const isMobile = useIsWithinMaxBreakpoint('s')
    const { route, tokens } = primaryRoute(result.routes ?? [], searchTerm)
    const breadcrumbItems = useMemo(
        () =>
            navigationSearchBreadcrumbs(
                result.parents,
                typeFilter,
                config.buildType
            ),
        [result.parents, typeFilter]
    )

    return (
        <a
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
                background: ${
                    isSelected
                        ? euiTheme.colors.backgroundBaseSubdued
                        : 'transparent'
                };

                &:last-child {
                    border-bottom: none;
                }

                &:hover:not([data-keyboard-navigating='true']) {
                    background: ${
                        isSelected
                            ? euiTheme.colors.backgroundBaseSubdued
                            : euiTheme.colors.backgroundBaseHighlighted
                    };

                    .title-text {
                        color: ${euiTheme.colors.link};
                        text-decoration: underline;

                        mark {
                            color: ${euiTheme.colors.textParagraph};
                            text-decoration: underline;
                        }
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
                {route ? (
                    <RouteLine route={route} tokens={tokens} />
                ) : (
                    result.description && (
                        <Description text={result.description} />
                    )
                )}
            </div>
            {!isMobile && (
                <JumpToIndicator
                    label={
                        result.type === 'api'
                            ? GO_TO_LABELS.operation
                            : GO_TO_LABELS.page
                    }
                />
            )}
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
                    background-color: var(--search-highlight-tint);
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
                    background-color: var(--search-highlight-tint);
                    font-weight: ${euiTheme.font.weight.bold};
                }
            `}
        >
            <SanitizedHtmlContent htmlContent={text} ellipsis />
        </div>
    )
}
