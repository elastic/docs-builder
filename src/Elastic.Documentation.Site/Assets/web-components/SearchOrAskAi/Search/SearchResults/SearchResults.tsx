import { SearchOrAskAiErrorCallout } from '../../SearchOrAskAiErrorCallout'
import { useSearchActions, useSearchTerm } from '../search.store'
import { useSearchFilters, type TypeFilter } from '../useSearchFilters'
import { useSearchQuery } from '../useSearchQuery'
import { SearchResultListItem } from './SearchResultsListItem'
import {
    useEuiOverflowScroll,
    EuiSpacer,
    useEuiTheme,
    EuiHorizontalRule,
    EuiButton,
    EuiText,
    EuiSkeletonRectangle,
    EuiSkeletonLoading,
    EuiSkeletonText,
    EuiSkeletonTitle,
} from '@elastic/eui'
import { css } from '@emotion/react'
import { useDebounce } from '@uidotdev/usehooks'
import { useEffect, useRef, useCallback } from 'react'

interface SearchResultsProps {
    onKeyDown?: (
        e: React.KeyboardEvent<HTMLAnchorElement>,
        index: number
    ) => void
    setItemRef?: (element: HTMLAnchorElement | null, index: number) => void
}

export const SearchResults = ({
    onKeyDown,
    setItemRef,
}: SearchResultsProps) => {
    const { euiTheme } = useEuiTheme()
    const searchTerm = useSearchTerm()
    const { setPageNumber: setActivePage } = useSearchActions()
    const debouncedSearchTerm = useDebounce(searchTerm, 300)
    const scrollContainerRef = useRef<HTMLDivElement>(null)

    const scrollbarStyle = css`
        max-height: 400px;
        padding-block: ${euiTheme.size.base};
        margin-right: ${euiTheme.size.s};
        ${useEuiOverflowScroll('y', true)}
    `

    const resetScrollToTop = useCallback(() => {
        if (scrollContainerRef.current) {
            scrollContainerRef.current.scrollTop = 0
        }
    }, [])

    useEffect(() => {
        setActivePage(0)
    }, [debouncedSearchTerm, setActivePage])

    useEffect(() => {
        resetScrollToTop()
    }, [debouncedSearchTerm, resetScrollToTop])

    const { data, error, isLoading } = useSearchQuery()

    const { selectedFilter, handleFilterClick, filteredResults, counts } =
        useSearchFilters({
            results: data?.results ?? [],
            aggregations: data?.aggregations,
        })

    const isInitialLoading = isLoading && !data

    if (!searchTerm) {
        return null
    }

    return (
        <>
            <SearchOrAskAiErrorCallout
                error={error}
                domain="search"
                title="Error loading search results"
            />
            {error && <EuiSpacer size="s" />}

            {!error && (
                <>
                    <Filter
                        selectedFilter={selectedFilter}
                        onFilterClick={handleFilterClick}
                        counts={counts}
                        isLoading={isInitialLoading}
                    />

                    <EuiSpacer size="m" />
                    <EuiHorizontalRule margin="none" />

                    {!isInitialLoading && filteredResults.length === 0 && (
                        <EuiText
                            size="s"
                            color="subdued"
                            css={css`
                                margin: ${euiTheme.size.base};
                            `}
                        >
                            <i>No results</i>
                        </EuiText>
                    )}

                    <div
                        data-search-results
                        ref={scrollContainerRef}
                        css={scrollbarStyle}
                    >
                        <EuiSkeletonLoading
                            isLoading={isInitialLoading}
                            loadingContent={
                                <ul>
                                    {[1, 2, 3].map((i) => (
                                        <li
                                            key={i}
                                            css={css`
                                                padding: ${euiTheme.size.m}
                                                    ${euiTheme.size.base};
                                                padding-right: calc(
                                                    2 * ${euiTheme.size.base}
                                                );
                                                margin-inline: ${euiTheme.size
                                                    .base};
                                            `}
                                        >
                                            <div
                                                css={css`
                                                    display: grid;
                                                    grid-template-columns: auto 1fr;
                                                    gap: ${euiTheme.size.base};
                                                `}
                                            >
                                                <div
                                                    css={css`
                                                        display: flex;
                                                        justify-content: center;
                                                        align-items: center;
                                                    `}
                                                >
                                                    <EuiSkeletonRectangle
                                                        height={16}
                                                        width={16}
                                                        borderRadius="m"
                                                    />
                                                </div>
                                                <div>
                                                    <EuiSkeletonTitle size="xxxs" />
                                                    <EuiSpacer size="s" />
                                                    <EuiSkeletonText
                                                        lines={1}
                                                        size="xs"
                                                    />
                                                    <EuiSpacer size="xs" />
                                                    <div
                                                        css={css`
                                                            width: 80%;
                                                        `}
                                                    >
                                                        <EuiSkeletonText
                                                            lines={2}
                                                            size="xs"
                                                        />
                                                    </div>
                                                </div>
                                            </div>
                                        </li>
                                    ))}
                                </ul>
                            }
                            loadedContent={
                                data ? (
                                    <ul>
                                        {filteredResults.map(
                                            (result, index) => (
                                                <SearchResultListItem
                                                    item={result}
                                                    key={result.url}
                                                    index={index}
                                                    pageNumber={data.pageNumber}
                                                    pageSize={data.pageSize}
                                                    onKeyDown={onKeyDown}
                                                    setRef={setItemRef}
                                                />
                                            )
                                        )}
                                    </ul>
                                ) : (
                                    <div>No results</div>
                                )
                            }
                        />
                    </div>
                </>
            )}
        </>
    )
}

const Filter = ({
    selectedFilter,
    onFilterClick,
    counts,
    isLoading,
}: {
    selectedFilter: TypeFilter
    onFilterClick: (filter: TypeFilter) => void
    counts: {
        apiResultsCount: number
        docsResultsCount: number
        totalCount: number
    }
    isLoading: boolean
}) => {
    const { euiTheme } = useEuiTheme()
    const { apiResultsCount, docsResultsCount, totalCount } = counts

    const buttonStyle = css`
        border-radius: 99999px;
        padding-inline: ${euiTheme.size.m};
        min-inline-size: auto;
    `

    const skeletonStyle = css`
        border-radius: 99999px;
    `

    return (
        <div
            css={css`
                display: flex;
                gap: ${euiTheme.size.s};
                padding-inline: ${euiTheme.size.base};
            `}
        >
            <EuiSkeletonRectangle
                isLoading={isLoading}
                width="73.0547px"
                css={skeletonStyle}
            >
                <EuiButton
                    color="text"
                    // @ts-expect-error: xs is valid size according to EuiButton docs
                    size="xs"
                    fill={selectedFilter === 'all'}
                    isLoading={isLoading}
                    onClick={() => onFilterClick('all')}
                    css={buttonStyle}
                    aria-label={`Show all results, ${totalCount} total`}
                    aria-pressed={selectedFilter === 'all'}
                >
                    {isLoading ? 'ALL' : `ALL (${totalCount})`}
                </EuiButton>
            </EuiSkeletonRectangle>
            <EuiSkeletonRectangle
                isLoading={isLoading}
                width="87.4375px"
                css={skeletonStyle}
            >
                <EuiButton
                    color="text"
                    // @ts-expect-error: xs is valid size according to EuiButton docs
                    size="xs"
                    fill={selectedFilter === 'doc'}
                    isLoading={isLoading}
                    onClick={() => onFilterClick('doc')}
                    css={buttonStyle}
                    aria-label={`Filter to documentation results, ${docsResultsCount} available`}
                    aria-pressed={selectedFilter === 'doc'}
                >
                    {isLoading ? 'DOCS' : `DOCS (${docsResultsCount})`}
                </EuiButton>
            </EuiSkeletonRectangle>
            <EuiSkeletonRectangle
                isLoading={isLoading}
                width="65.0547px"
                css={skeletonStyle}
            >
                <EuiButton
                    color="text"
                    // @ts-expect-error: xs is valid size according to EuiButton docs
                    size="xs"
                    fill={selectedFilter === 'api'}
                    isLoading={isLoading}
                    onClick={() => onFilterClick('api')}
                    css={buttonStyle}
                    aria-label={`Filter to API results, ${apiResultsCount} available`}
                    aria-pressed={selectedFilter === 'api'}
                >
                    {isLoading ? 'API' : `API (${apiResultsCount})`}
                </EuiButton>
            </EuiSkeletonRectangle>
        </div>
    )
}
