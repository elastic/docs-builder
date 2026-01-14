import { FilterSidebar } from './FilterSidebar'
import { LandingPage } from './LandingPage'
import { ResultsList } from './ResultsList'
import { SearchHeader } from './SearchHeader'
import {
    useFullPageSearchQuery,
    useHasSearched,
    usePage,
    usePageSize,
    useSortBy,
    useVersion,
    useFilters,
    useRecentSearches,
    useFullPageSearchActions,
    FullPageSearchFilters,
} from './fullPageSearch.store'
import { useFullPageSearch, isSemanticQuery } from './useFullPageSearchQuery'
import { useSearchAvailability } from './useSearchAvailability'
import {
    EuiBadge,
    EuiButtonIcon,
    EuiEmptyPrompt,
    useEuiTheme,
} from '@elastic/eui'
import { css } from '@emotion/react'
import { useState, useCallback, useEffect, useRef } from 'react'

const NoResultsState = ({
    query,
    onClearFilters,
}: {
    query: string
    onClearFilters: () => void
}) => {
    return (
        <div
            css={css`
                max-width: 600px;
                margin: 0 auto;
            `}
        >
            <EuiEmptyPrompt
                iconType="search"
                title={<h2>No results for &quot;{query}&quot;</h2>}
                body={
                    <p>
                        Try adjusting your filters or search terms. You can also
                        try a different query or clear all filters.
                    </p>
                }
                actions={[
                    <EuiButtonIcon
                        key="clear"
                        iconType="refresh"
                        aria-label="Clear filters"
                        onClick={onClearFilters}
                    >
                        Clear filters and search again
                    </EuiButtonIcon>,
                ]}
            />
        </div>
    )
}

const ErrorState = ({
    query,
    onGoToLanding,
}: {
    query: string
    onGoToLanding: () => void
}) => {
    const { euiTheme } = useEuiTheme()

    return (
        <div
            css={css`
                max-width: 600px;
                margin: 0 auto;
            `}
        >
            <EuiEmptyPrompt
                iconType="search"
                title={
                    <h2>
                        <EuiBadge color="danger">Error</EuiBadge> searching for
                        &quot;{query}&quot;
                    </h2>
                }
                body={
                    <p>
                        Something went wrong while processing your search.
                        Please try again later.
                    </p>
                }
                actions={[
                    <button
                        key="back"
                        onClick={onGoToLanding}
                        css={css`
                            background: ${euiTheme.colors.primary};
                            color: ${euiTheme.colors.ghost};
                            border: none;
                            border-radius: ${euiTheme.border.radius.medium};
                            padding: ${euiTheme.size.s} ${euiTheme.size.l};
                            font-size: ${euiTheme.size.m};
                            font-weight: ${euiTheme.font.weight.medium};
                            cursor: pointer;
                            transition: background 0.2s ease;

                            &:hover {
                                background: ${euiTheme.colors.primaryText};
                            }
                        `}
                    >
                        Back to search
                    </button>,
                ]}
            />
        </div>
    )
}

export const FullPageSearch = () => {
    const { euiTheme } = useEuiTheme()

    // Check search service availability
    const { isAvailable, isChecking } = useSearchAvailability()

    // Animation state for search box transition
    const [isAnimatingSearchToHeader, setIsAnimatingSearchToHeader] =
        useState(false)

    // Track when to force collapse the AI panel (when filters/page/sort change)
    const [forceAICollapsed, setForceAICollapsed] = useState(false)

    // Store state
    const query = useFullPageSearchQuery()
    const hasSearched = useHasSearched()
    const page = usePage()
    const pageSize = usePageSize()
    const sortBy = useSortBy()
    const version = useVersion()
    const filters = useFilters()
    const recentSearches = useRecentSearches()
    const actions = useFullPageSearchActions()

    // API query
    const { data, isLoading, isFetching, error } = useFullPageSearch()

    const results = data?.results ?? []
    const totalResults = data?.totalResults ?? 0
    const pageCount = data?.pageCount ?? 0
    const aggregations = data?.aggregations

    // Show AI answer for semantic queries
    const showAIAnswer =
        hasSearched &&
        isSemanticQuery(query) &&
        results.length > 0 &&
        !isLoading

    // Track previous values to detect changes
    const prevFiltersRef = useRef(filters)
    const prevVersionRef = useRef(version)
    const prevPageRef = useRef(page)
    const prevPageSizeRef = useRef(pageSize)
    const prevSortByRef = useRef(sortBy)

    // Force collapse AI panel when filters, page, sort, or version change
    useEffect(() => {
        const filtersChanged =
            JSON.stringify(filters) !== JSON.stringify(prevFiltersRef.current)
        const versionChanged = version !== prevVersionRef.current
        const pageChanged = page !== prevPageRef.current
        const pageSizeChanged = pageSize !== prevPageSizeRef.current
        const sortByChanged = sortBy !== prevSortByRef.current

        if (
            filtersChanged ||
            versionChanged ||
            pageChanged ||
            pageSizeChanged ||
            sortByChanged
        ) {
            setForceAICollapsed(true)
            // Reset after a tick so component can react to the change
            setTimeout(() => setForceAICollapsed(false), 0)
        }

        prevFiltersRef.current = filters
        prevVersionRef.current = version
        prevPageRef.current = page
        prevPageSizeRef.current = pageSize
        prevSortByRef.current = sortBy
    }, [filters, version, page, pageSize, sortBy])

    const handleSearch = useCallback(
        (searchQuery: string) => {
            if (!searchQuery.trim()) return

            // Start the animation
            setIsAnimatingSearchToHeader(true)

            // After animation starts, submit the search
            setTimeout(() => {
                actions.submitSearch(searchQuery)
                // Reset animation state after transition completes
                setTimeout(() => setIsAnimatingSearchToHeader(false), 50)
            }, 200)
        },
        [actions]
    )

    const handleFilterChange = (
        key: keyof FullPageSearchFilters,
        value: string
    ) => {
        actions.toggleFilter(key, value)
    }

    const handleRemoveFilter = (
        key: keyof FullPageSearchFilters,
        value: string
    ) => {
        actions.removeFilter(key, value)
    }

    const handleResetVersion = () => {
        actions.setVersion('9.0+')
    }

    // Show header search input only after search has been performed
    const showHeaderSearchInput = hasSearched

    return (
        <div
            css={css`
                min-height: 100vh;
            `}
        >
            {/* Full-width gradient background */}
            <div
                css={css`
                    position: fixed;
                    top: 0;
                    left: 0;
                    right: 0;
                    bottom: 0;
                    background: linear-gradient(
                        180deg,
                        ${euiTheme.colors.lightestShade} 0%,
                        ${euiTheme.colors.emptyShade} 100%
                    );
                    z-index: -1;
                `}
            />

            {hasSearched && isAvailable && (
                <SearchHeader
                    query={query}
                    recentSearches={recentSearches}
                    showSearchInput={showHeaderSearchInput}
                    onQueryChange={actions.setQuery}
                    onSearch={(q) => actions.submitSearch(q)}
                    onClearRecent={actions.clearRecentSearches}
                    onLogoClick={actions.goToLanding}
                />
            )}

            <main
                css={css`
                    max-width: var(--max-layout-width);
                    width: 100%;
                    height: 100%;
                    margin: 0 auto;
                    display: grid;
                    padding: ${hasSearched ? euiTheme.size.l : '0'};

                    ${hasSearched
                        ? `
                        grid-template-columns: 1fr;
                    `
                        : `
                        grid-template-columns: 1fr;

                        @media (min-width: 768px) {
                            grid-template-columns: 1fr minmax(800px, auto) 1fr;
                        }
                    `}
                `}
            >
                {!hasSearched || (!isAvailable && !isChecking) ? (
                    <>
                        <div />
                        <LandingPage
                            query={query}
                            isAnimatingOut={isAnimatingSearchToHeader}
                            onQueryChange={actions.setQuery}
                            onSearch={handleSearch}
                            disabled={isChecking}
                            unavailable={!isAvailable && !isChecking}
                        />
                        <div />
                    </>
                ) : error ? (
                    <ErrorState
                        query={query}
                        onGoToLanding={actions.goToLanding}
                    />
                ) : totalResults === 0 && !isLoading ? (
                    <NoResultsState
                        query={query}
                        onClearFilters={actions.clearAllFilters}
                    />
                ) : (
                    <div
                        css={css`
                            display: flex;
                            gap: ${euiTheme.size.xl};
                            height: calc(100vh - var(--offset-top, 0px) - 80px);
                        `}
                    >
                        <div
                            css={css`
                                width: 280px;
                                flex-shrink: 0;
                                overflow-y: auto;
                                padding-right: ${euiTheme.size.s};

                                @media (max-width: 768px) {
                                    display: none;
                                }

                                /* Custom scrollbar styling */
                                &::-webkit-scrollbar {
                                    width: 6px;
                                }
                                &::-webkit-scrollbar-track {
                                    background: transparent;
                                }
                                &::-webkit-scrollbar-thumb {
                                    background: ${euiTheme.colors.lightShade};
                                    border-radius: 3px;
                                }
                                &::-webkit-scrollbar-thumb:hover {
                                    background: ${euiTheme.colors.mediumShade};
                                }
                            `}
                        >
                            <FilterSidebar
                                aggregations={aggregations}
                                version={version}
                                filters={filters}
                                onVersionChange={actions.setVersion}
                                onFilterChange={handleFilterChange}
                            />
                        </div>
                        <div
                            css={css`
                                flex: 1;
                                overflow-y: auto;
                                padding-right: ${euiTheme.size.s};

                                /* Custom scrollbar styling */
                                &::-webkit-scrollbar {
                                    width: 6px;
                                }
                                &::-webkit-scrollbar-track {
                                    background: transparent;
                                }
                                &::-webkit-scrollbar-thumb {
                                    background: ${euiTheme.colors.lightShade};
                                    border-radius: 3px;
                                }
                                &::-webkit-scrollbar-thumb:hover {
                                    background: ${euiTheme.colors.mediumShade};
                                }
                            `}
                        >
                            <ResultsList
                                results={results}
                                totalResults={totalResults}
                                pageCount={pageCount}
                                currentPage={page}
                                pageSize={pageSize}
                                sortBy={sortBy}
                                isLoading={isLoading || isFetching}
                                filters={filters}
                                version={version}
                                query={query}
                                showAIAnswer={showAIAnswer}
                                forceAICollapsed={forceAICollapsed}
                                onPageChange={actions.setPage}
                                onPageSizeChange={actions.setPageSize}
                                onSortChange={actions.setSortBy}
                                onRemoveFilter={handleRemoveFilter}
                                onClearAllFilters={actions.clearAllFilters}
                                onResetVersion={handleResetVersion}
                            />
                        </div>
                    </div>
                )}
            </main>

            <EuiButtonIcon
                iconType="arrowUp"
                aria-label="Scroll to top"
                onClick={() => window.scrollTo({ top: 0, behavior: 'smooth' })}
                css={css`
                    position: fixed;
                    bottom: ${euiTheme.size.l};
                    right: ${euiTheme.size.l};
                    background: ${euiTheme.colors.darkestShade};
                    color: ${euiTheme.colors.ghost};
                    border-radius: 50%;
                    width: 48px;
                    height: 48px;
                    box-shadow: ${euiTheme.levels.menu};

                    &:hover {
                        background: ${euiTheme.colors.darkShade};
                    }
                `}
            />
        </div>
    )
}
