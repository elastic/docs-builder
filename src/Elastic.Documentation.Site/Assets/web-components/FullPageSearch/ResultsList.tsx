import { AIAnswerPanel } from './AIAnswerPanel'
import { ResultCard } from './ResultCard'
import type { FullPageSearchFilters, SortBy } from './fullPageSearch.store'
import type { SearchResultItem } from './useFullPageSearchQuery'
import {
    EuiBadge,
    EuiButtonIcon,
    EuiFlexGroup,
    EuiFlexItem,
    EuiSkeletonText,
    EuiPanel,
    EuiPagination,
    EuiSelect,
    EuiSpacer,
    EuiText,
    useEuiTheme,
} from '@elastic/eui'
import { css } from '@emotion/react'

const ResultSkeleton = () => {
    const { euiTheme } = useEuiTheme()

    return (
        <EuiPanel paddingSize="l" hasBorder>
            <EuiSkeletonText lines={4} />
            <EuiSpacer size="s" />
            <EuiFlexGroup gutterSize="s">
                <EuiFlexItem grow={false}>
                    <div
                        css={css`
                            width: 60px;
                            height: 20px;
                            background: ${euiTheme.colors.lightestShade};
                            border-radius: ${euiTheme.border.radius.small};
                        `}
                    />
                </EuiFlexItem>
                <EuiFlexItem grow={false}>
                    <div
                        css={css`
                            width: 80px;
                            height: 20px;
                            background: ${euiTheme.colors.lightestShade};
                            border-radius: ${euiTheme.border.radius.small};
                        `}
                    />
                </EuiFlexItem>
            </EuiFlexGroup>
        </EuiPanel>
    )
}

interface ActiveFiltersProps {
    filters: FullPageSearchFilters
    version: string
    onRemoveFilter: (key: keyof FullPageSearchFilters, value: string) => void
    onClearAll: () => void
    onResetVersion: () => void
}

const ActiveFilters = ({
    filters,
    version,
    onRemoveFilter,
    onClearAll,
    onResetVersion,
}: ActiveFiltersProps) => {
    const { euiTheme } = useEuiTheme()

    const allFilters: { key: keyof FullPageSearchFilters; value: string }[] = []
    Object.entries(filters).forEach(([key, values]) => {
        ;(values as string[]).forEach((value: string) => {
            allFilters.push({
                key: key as keyof FullPageSearchFilters,
                value,
            })
        })
    })

    const hasFilters = allFilters.length > 0 || version !== '9.0+'

    if (!hasFilters) return null

    return (
        <EuiFlexGroup
            alignItems="center"
            gutterSize="s"
            wrap
            css={css`
                margin-bottom: ${euiTheme.size.m};
            `}
        >
            <EuiFlexItem grow={false}>
                <EuiText size="xs" color="subdued">
                    Active:
                </EuiText>
            </EuiFlexItem>
            {version !== '9.0+' && (
                <EuiFlexItem grow={false}>
                    <EuiBadge
                        color="primary"
                        iconType="cross"
                        iconSide="right"
                        iconOnClick={onResetVersion}
                        iconOnClickAriaLabel="Remove version filter"
                    >
                        {version}
                    </EuiBadge>
                </EuiFlexItem>
            )}
            {allFilters.map((filter, idx) => (
                <EuiFlexItem key={idx} grow={false}>
                    <EuiBadge
                        color="primary"
                        iconType="cross"
                        iconSide="right"
                        iconOnClick={() =>
                            onRemoveFilter(filter.key, filter.value)
                        }
                        iconOnClickAriaLabel={`Remove ${filter.value} filter`}
                        css={css`
                            text-transform: capitalize;
                        `}
                    >
                        {filter.value}
                    </EuiBadge>
                </EuiFlexItem>
            ))}
            <EuiFlexItem grow={false}>
                <button
                    onClick={onClearAll}
                    css={css`
                        background: none;
                        border: none;
                        color: ${euiTheme.colors.subduedText};
                        cursor: pointer;
                        font-size: ${euiTheme.size.m};
                        display: flex;
                        align-items: center;
                        gap: ${euiTheme.size.xs};

                        &:hover {
                            color: ${euiTheme.colors.text};
                        }
                    `}
                >
                    <EuiButtonIcon
                        iconType="refresh"
                        aria-label="Clear all filters"
                        size="xs"
                    />
                    Clear all
                </button>
            </EuiFlexItem>
        </EuiFlexGroup>
    )
}

interface ResultsListProps {
    results: SearchResultItem[]
    totalResults: number
    pageCount: number
    currentPage: number
    pageSize: number
    sortBy: SortBy
    isLoading: boolean
    filters: FullPageSearchFilters
    version: string
    query: string
    inputQuery: string
    showAIAnswer: boolean
    forceAICollapsed: boolean
    onPageChange: (page: number) => void
    onPageSizeChange: (size: number) => void
    onSortChange: (sort: SortBy) => void
    onRemoveFilter: (key: keyof FullPageSearchFilters, value: string) => void
    onClearAllFilters: () => void
    onResetVersion: () => void
}

export const ResultsList = ({
    results,
    totalResults,
    pageCount,
    currentPage,
    pageSize,
    sortBy,
    isLoading,
    filters,
    version,
    query,
    inputQuery,
    showAIAnswer,
    forceAICollapsed,
    onPageChange,
    onPageSizeChange,
    onSortChange,
    onRemoveFilter,
    onClearAllFilters,
    onResetVersion,
}: ResultsListProps) => {
    const { euiTheme } = useEuiTheme()

    const pageSizeOptions = [
        { value: '10', text: '10 per page' },
        { value: '20', text: '20 per page' },
        { value: '50', text: '50 per page' },
    ]

    const sortOptions = [
        { value: 'relevance', text: 'Most relevant' },
        { value: 'recent', text: 'Recently updated' },
        { value: 'alpha', text: 'Alphabetical' },
    ]

    return (
        <div>
            <EuiFlexGroup
                alignItems="center"
                justifyContent="spaceBetween"
                css={css`
                    margin-bottom: ${euiTheme.size.m};
                `}
            >
                <EuiFlexItem grow={false}>
                    <EuiText size="s">
                        {isLoading && totalResults === 0 ? (
                            <span
                                css={css`
                                    display: inline-flex;
                                    align-items: center;
                                    gap: ${euiTheme.size.s};
                                `}
                            >
                                <span
                                    css={css`
                                        width: 16px;
                                        height: 16px;
                                        border: 2px solid
                                            ${euiTheme.colors.lightShade};
                                        border-top-color: ${euiTheme.colors
                                            .primary};
                                        border-radius: 50%;
                                        animation: spin 0.8s linear infinite;
                                        @keyframes spin {
                                            to {
                                                transform: rotate(360deg);
                                            }
                                        }
                                    `}
                                />
                                Searching...
                            </span>
                        ) : (
                            <>
                                <strong>{totalResults.toLocaleString()}</strong>{' '}
                                results
                            </>
                        )}
                    </EuiText>
                </EuiFlexItem>
                <EuiFlexItem grow={false}>
                    <EuiFlexGroup gutterSize="s">
                        <EuiFlexItem grow={false}>
                            <EuiSelect
                                options={pageSizeOptions}
                                value={pageSize.toString()}
                                onChange={(e) =>
                                    onPageSizeChange(parseInt(e.target.value))
                                }
                                compressed
                            />
                        </EuiFlexItem>
                        <EuiFlexItem grow={false}>
                            <EuiSelect
                                options={sortOptions}
                                value={sortBy}
                                onChange={(e) =>
                                    onSortChange(e.target.value as SortBy)
                                }
                                compressed
                            />
                        </EuiFlexItem>
                    </EuiFlexGroup>
                </EuiFlexItem>
            </EuiFlexGroup>

            <ActiveFilters
                filters={filters}
                version={version}
                onRemoveFilter={onRemoveFilter}
                onClearAll={onClearAllFilters}
                onResetVersion={onResetVersion}
            />

            {showAIAnswer && (
                <AIAnswerPanel
                    query={query}
                    inputQuery={inputQuery}
                    results={results}
                    visible={showAIAnswer}
                    forceCollapsed={forceAICollapsed}
                />
            )}

            {isLoading ? (
                <EuiFlexGroup direction="column" gutterSize="m">
                    {[1, 2, 3].map((i) => (
                        <EuiFlexItem key={i}>
                            <ResultSkeleton />
                        </EuiFlexItem>
                    ))}
                </EuiFlexGroup>
            ) : (
                <>
                    <EuiFlexGroup direction="column" gutterSize="m">
                        {results.map((result) => (
                            <EuiFlexItem key={result.url}>
                                <ResultCard result={result} />
                            </EuiFlexItem>
                        ))}
                    </EuiFlexGroup>

                    {pageCount > 1 && (
                        <EuiFlexGroup
                            justifyContent="center"
                            css={css`
                                margin-top: ${euiTheme.size.xl};
                            `}
                        >
                            <EuiFlexItem grow={false}>
                                <EuiPagination
                                    pageCount={pageCount}
                                    activePage={currentPage - 1}
                                    onPageClick={(page) =>
                                        onPageChange(page + 1)
                                    }
                                />
                            </EuiFlexItem>
                        </EuiFlexGroup>
                    )}
                </>
            )}
        </div>
    )
}
