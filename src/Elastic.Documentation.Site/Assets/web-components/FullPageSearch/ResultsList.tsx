import { AIAnswerPanel } from './AIAnswerPanel'
import { ResultCard } from './ResultCard'
import type { FullPageSearchFilters, SortBy } from './fullPageSearch.store'
import type {
    SearchResultItem,
    SearchAggregations,
} from './useFullPageSearchQuery'
import {
    EuiButtonIcon,
    EuiFlexGroup,
    EuiFlexItem,
    EuiIcon,
    EuiSkeletonText,
    EuiPanel,
    EuiPagination,
    EuiSelect,
    EuiSpacer,
    EuiText,
    useEuiTheme,
} from '@elastic/eui'
import { css } from '@emotion/react'
import { ReactNode } from 'react'

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

// Facet icons (smaller versions for active filters)
const TypeIcon = () => (
    <svg
        xmlns="http://www.w3.org/2000/svg"
        width="12"
        height="12"
        viewBox="0 0 16 16"
        fill="currentColor"
    >
        <path d="M6 2H2v4h4V2ZM1 1h6v6H1V1ZM14 2h-4v4h4V2Zm-5-1h6v6H9V1ZM6 10H2v4h4v-4ZM1 9h6v6H1V9ZM14 10h-4v4h4v-4Zm-5-1h6v6H9V9Z" />
    </svg>
)

const LayersIcon = () => (
    <svg
        xmlns="http://www.w3.org/2000/svg"
        width="12"
        height="12"
        viewBox="0 0 16 16"
        fill="currentColor"
    >
        <path
            fillRule="evenodd"
            d="M1.553 4.106a1 1 0 0 0 0 1.788l6 3a1 1 0 0 0 .894 0l6-3a1 1 0 0 0 0-1.788l-6-3a1 1 0 0 0-.894 0l-6 3ZM14 5 8 8 2 5l6-3 6 3Z"
            clipRule="evenodd"
        />
        <path d="m8 11 6.894-3.447S15 7.843 15 8a1 1 0 0 1-.553.895l-6 3a1 1 0 0 1-.894 0l-6-3A1 1 0 0 1 1 8c0-.158.106-.447.106-.447L8 11Z" />
        <path d="m8 14 6.894-3.447s.106.29.106.447a1 1 0 0 1-.553.895l-6 3a1 1 0 0 1-.894 0l-6-3A1 1 0 0 1 1 11c0-.158.106-.447.106-.447L8 14Z" />
    </svg>
)

const CloudIcon = () => (
    <svg
        xmlns="http://www.w3.org/2000/svg"
        width="12"
        height="12"
        viewBox="0 0 16 16"
        fill="currentColor"
    >
        <path d="M10.7458178,5.00539515 C13.6693111,5.13397889 16,7.54480866 16,10.5 C16,13.5375661 13.5375661,16 10.5,16 L4,16 C1.790861,16 0,14.209139 0,12 C0,10.3632968 0.983004846,8.95618668 2.39082809,8.33685609 C2.20209747,7.91526722 2.07902056,7.46524082 2.02752078,7 L0.5,7 C0.223857625,7 0,6.77614237 0,6.5 C0,6.22385763 0.223857625,6 0.5,6 L2.02746439,6 C2.1234088,5.13207349 2.46618907,4.33854089 2.98405031,3.69115709 L1.64644661,2.35355339 C1.45118446,2.15829124 1.45118446,1.84170876 1.64644661,1.64644661 C1.84170876,1.45118446 2.15829124,1.45118446 2.35355339,1.64644661 L3.69115709,2.98405031 C4.33854089,2.46618907 5.13207349,2.1234088 6,2.02746439 L6,0.5 C6,0.223857625 6.22385763,8.8817842e-16 6.5,8.8817842e-16 C6.77614237,8.8817842e-16 7,0.223857625 7,0.5 L7,2.02763291 C7.86199316,2.12340881 8.65776738,2.46398198 9.30889273,2.98400049 L10.6464466,1.64644661 C10.8417088,1.45118446 11.1582912,1.45118446 11.3535534,1.64644661 C11.5488155,1.84170876 11.5488155,2.15829124 11.3535534,2.35355339 L10.0163882,3.69071859 C10.3273281,4.07929253 10.5759739,4.52210317 10.7458178,5.00539515 Z M4,15 L10.5,15 C12.9852814,15 15,12.9852814 15,10.5 C15,8.01471863 12.9852814,6 10.5,6 C8.66116238,6 7.03779702,7.11297909 6.34791295,8.76123609 C7.34903712,9.48824913 8,10.6681043 8,12 C8,12.2761424 7.77614237,12.5 7.5,12.5 C7.22385763,12.5 7,12.2761424 7,12 C7,10.3431458 5.65685425,9 4,9 C2.34314575,9 1,10.3431458 1,12 C1,13.6568542 2.34314575,15 4,15 Z M9.69118222,5.05939327 C9.13621169,3.82983527 7.90059172,3 6.5,3 C4.56700338,3 3,4.56700338 3,6.5 C3,7.04681795 3.12529266,7.57427865 3.36125461,8.05072309 C3.56924679,8.01734375 3.78259797,8 4,8 C4.51800602,8 5.01301412,8.0984658 5.46732834,8.27770144 C6.22579284,6.55947236 7.82085903,5.33623457 9.69118222,5.05939327 Z" />
    </svg>
)

const ProductIcon = () => (
    <svg
        xmlns="http://www.w3.org/2000/svg"
        width="12"
        height="12"
        viewBox="0 0 16 16"
        fill="currentColor"
    >
        <path d="M0 6.5l.004-.228c.039-1.186.678-2.31 1.707-2.956L6.293.57a3.512 3.512 0 013.414 0l4.582 2.746c1.029.646 1.668 1.77 1.707 2.956L16 6.5v3l-.004.228c-.039 1.186-.678 2.31-1.707 2.956l-4.582 2.746a3.512 3.512 0 01-3.414 0L1.711 12.684C.682 12.038.043 10.914.004 9.728L0 9.5v-3zM8 1.5a2.5 2.5 0 00-1.222.318L2.196 4.564A2.5 2.5 0 001 6.732V9.268a2.5 2.5 0 001.196 2.168l4.582 2.746a2.5 2.5 0 002.444 0l4.582-2.746A2.5 2.5 0 0015 9.268V6.732a2.5 2.5 0 00-1.196-2.168L9.222 1.818A2.5 2.5 0 008 1.5z" />
    </svg>
)

const PackageIcon = () => (
    <svg
        xmlns="http://www.w3.org/2000/svg"
        width="12"
        height="12"
        viewBox="0 0 16 16"
        fill="currentColor"
    >
        <path d="M14.4472,3.72363l-6-3a.99965.99965,0,0,0-.8944,0l-6,3A.99992.99992,0,0,0,1,4.618v6.764a1.00012,1.00012,0,0,0,.5528.89441l6,3a1,1,0,0,0,.8944,0l6-3A1.00012,1.00012,0,0,0,15,11.382V4.618A.99992.99992,0,0,0,14.4472,3.72363ZM5.87085,5.897l5.34357-2.67181L13.372,4.304,8,6.94293ZM8,1.618,10.09637,2.6662,4.74292,5.343,2.628,4.30408ZM2,5.10968,4.25,6.215V9a.5.5,0,0,0,1,0V6.70618L7.5,7.81146V14.132L2,11.382ZM8.5,14.132V7.81146L14,5.10968V11.382Z" />
    </svg>
)

// Type labels
const TYPE_LABELS: Record<string, string> = {
    doc: 'Documentation',
    api: 'OpenAPI Reference',
}

// Facet configuration for display
const FACET_CONFIG: Record<
    keyof FullPageSearchFilters | 'version',
    {
        label: string
        icon: ReactNode
        getDisplayValue: (
            value: string,
            aggregations?: SearchAggregations
        ) => string
    }
> = {
    type: {
        label: 'Type',
        icon: <TypeIcon />,
        getDisplayValue: (value: string) => TYPE_LABELS[value] ?? value,
    },
    navigationSection: {
        label: 'Section',
        icon: <LayersIcon />,
        getDisplayValue: (value: string) => value,
    },
    deploymentType: {
        label: 'Deployment',
        icon: <CloudIcon />,
        getDisplayValue: (value: string) => value,
    },
    product: {
        label: 'Product',
        icon: <ProductIcon />,
        getDisplayValue: (value: string, aggregations?: SearchAggregations) =>
            aggregations?.product?.[value]?.displayName ?? value,
    },
    version: {
        label: 'Version',
        icon: <PackageIcon />,
        getDisplayValue: (value: string) => value,
    },
}

// Active filter chip component (smaller version)
const ActiveFilterChip = ({
    facetType,
    facetIcon,
    displayValue,
    onRemove,
}: {
    facetType: string
    facetIcon: ReactNode
    displayValue: string
    onRemove: () => void
}) => {
    const { euiTheme } = useEuiTheme()

    return (
        <div
            css={css`
                display: inline-flex;
                align-items: stretch;
                background: ${euiTheme.colors.primary};
                border-radius: ${euiTheme.border.radius.small};
                overflow: hidden;
                font-size: 12px;
            `}
        >
            <div
                css={css`
                    display: flex;
                    align-items: center;
                    gap: 4px;
                    padding: 4px 8px;
                    background: rgba(255, 255, 255, 0.15);
                    color: #fff;
                    font-weight: ${euiTheme.font.weight.medium};
                `}
            >
                <span
                    css={css`
                        display: flex;
                        color: rgba(255, 255, 255, 0.8);
                    `}
                >
                    {facetIcon}
                </span>
                <span>{facetType}</span>
            </div>
            <div
                css={css`
                    display: flex;
                    align-items: center;
                    padding: 4px 8px;
                    color: #fff;
                    font-weight: ${euiTheme.font.weight.semiBold};
                `}
            >
                {displayValue}
            </div>
            <button
                onClick={onRemove}
                css={css`
                    display: flex;
                    align-items: center;
                    padding: 0 8px;
                    background: rgba(0, 0, 0, 0.1);
                    border: none;
                    cursor: pointer;
                    color: #fff;
                    transition: background 0.15s ease;

                    &:hover {
                        background: rgba(0, 0, 0, 0.2);
                    }
                `}
                aria-label={`Remove ${facetType} filter: ${displayValue}`}
            >
                <EuiIcon type="cross" size="s" />
            </button>
        </div>
    )
}

interface ActiveFiltersProps {
    filters: FullPageSearchFilters
    version: string
    aggregations?: SearchAggregations
    onRemoveFilter: (key: keyof FullPageSearchFilters, value: string) => void
    onClearAll: () => void
    onResetVersion: () => void
}

const ActiveFilters = ({
    filters,
    version,
    aggregations,
    onRemoveFilter,
    onClearAll,
    onResetVersion,
}: ActiveFiltersProps) => {
    const { euiTheme } = useEuiTheme()

    const allFilters: {
        key: keyof FullPageSearchFilters | 'version'
        value: string
    }[] = []

    // Add version filter if not default
    if (version !== '9.0+') {
        allFilters.push({ key: 'version', value: version })
    }

    // Add other filters
    Object.entries(filters).forEach(([key, values]) => {
        ;(values as string[]).forEach((value: string) => {
            allFilters.push({
                key: key as keyof FullPageSearchFilters,
                value,
            })
        })
    })

    if (allFilters.length === 0) return null

    return (
        <EuiFlexGroup
            alignItems="center"
            gutterSize="s"
            wrap
            css={css`
                margin-bottom: ${euiTheme.size.m};
            `}
        >
            {allFilters.map((filter, idx) => {
                const config = FACET_CONFIG[filter.key]
                return (
                    <EuiFlexItem key={idx} grow={false}>
                        <ActiveFilterChip
                            facetType={config.label}
                            facetIcon={config.icon}
                            displayValue={config.getDisplayValue(
                                filter.value,
                                aggregations
                            )}
                            onRemove={() => {
                                if (filter.key === 'version') {
                                    onResetVersion()
                                } else {
                                    onRemoveFilter(
                                        filter.key as keyof FullPageSearchFilters,
                                        filter.value
                                    )
                                }
                            }}
                        />
                    </EuiFlexItem>
                )
            })}
            {allFilters.length > 1 && (
                <EuiFlexItem grow={false}>
                    <button
                        onClick={onClearAll}
                        css={css`
                            background: none;
                            border: none;
                            color: ${euiTheme.colors.subduedText};
                            cursor: pointer;
                            font-size: 12px;
                            display: flex;
                            align-items: center;
                            gap: 4px;

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
            )}
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
    aggregations?: SearchAggregations
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
    aggregations,
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
                aggregations={aggregations}
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
