import {
    EuiButtonIcon,
    EuiEmptyPrompt,
    useEuiTheme,
} from '@elastic/eui'
import { css } from '@emotion/react'
import { useState, useCallback } from 'react'
import { LandingPage } from './LandingPage'
import { SearchHeader } from './SearchHeader'
import { FilterSidebar } from './FilterSidebar'
import { ResultsList } from './ResultsList'
import { AIAnswerPanel } from './AIAnswerPanel'
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
import {
    useFullPageSearch,
    isSemanticQuery,
} from './useFullPageSearchQuery'

const NoResultsState = ({
    query,
    onClearFilters,
}: {
    query: string
    onClearFilters: () => void
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

export const FullPageSearch = () => {
    const { euiTheme } = useEuiTheme()

    // Animation state for search box transition
    const [isAnimatingSearchToHeader, setIsAnimatingSearchToHeader] = useState(false)

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
    const { data, isLoading, isFetching } = useFullPageSearch()

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

    const handleSearch = useCallback((searchQuery: string) => {
        if (!searchQuery.trim()) return

        // Start the animation
        setIsAnimatingSearchToHeader(true)

        // After animation starts, submit the search
        setTimeout(() => {
            actions.submitSearch(searchQuery)
            // Reset animation state after transition completes
            setTimeout(() => setIsAnimatingSearchToHeader(false), 50)
        }, 200)
    }, [actions])

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

            {hasSearched && (
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
                {!hasSearched ? (
                    <>
                        <div />
                        <LandingPage
                            query={query}
                            isAnimatingOut={isAnimatingSearchToHeader}
                            onQueryChange={actions.setQuery}
                            onSearch={handleSearch}
                        />
                        <div />
                    </>
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
                            {showAIAnswer && (
                                <AIAnswerPanel
                                    query={query}
                                    results={results}
                                    visible={showAIAnswer}
                                />
                            )}
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
