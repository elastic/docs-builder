import {
    EuiButtonIcon,
    EuiEmptyPrompt,
    EuiFlexGroup,
    EuiFlexItem,
    EuiIcon,
    EuiText,
    useEuiTheme,
} from '@elastic/eui'
import { css } from '@emotion/react'
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
    const isSemanticSearch = data?.isSemanticQuery ?? false

    // Show AI answer for semantic queries
    const showAIAnswer =
        hasSearched &&
        isSemanticQuery(query) &&
        results.length > 0 &&
        !isLoading

    const handleSearch = (searchQuery: string) => {
        actions.submitSearch(searchQuery)
    }

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

    return (
        <div
            css={css`
                min-height: 100vh;
                background: linear-gradient(
                    180deg,
                    ${euiTheme.colors.lightestShade} 0%,
                    ${euiTheme.colors.emptyShade} 100%
                );
            `}
        >
            <SearchHeader
                query={query}
                recentSearches={recentSearches}
                onQueryChange={actions.setQuery}
                onSearch={handleSearch}
                onClearRecent={actions.clearRecentSearches}
            />

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
                        <LandingPage onSearch={handleSearch} />
                        <div />
                    </>
                ) : totalResults === 0 && !isLoading ? (
                    <NoResultsState
                        query={query}
                        onClearFilters={actions.clearAllFilters}
                    />
                ) : (
                    <EuiFlexGroup gutterSize="xl">
                        <EuiFlexItem
                            grow={false}
                            css={css`
                                width: 280px;
                                flex-shrink: 0;

                                @media (max-width: 768px) {
                                    display: none;
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
                        </EuiFlexItem>
                        <EuiFlexItem>
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
                        </EuiFlexItem>
                    </EuiFlexGroup>
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
