import { useTypeFilter, useSearchActions } from '../search.store'
import { useEuiTheme, EuiButton, EuiSkeletonRectangle } from '@elastic/eui'
import { css } from '@emotion/react'
import { useRef, useCallback, MutableRefObject } from 'react'

interface SearchFiltersProps {
    counts: {
        apiResultsCount: number
        docsResultsCount: number
        totalCount: number
    }
    isLoading: boolean
    inputRef?: React.RefObject<HTMLInputElement>
    itemRefs?: MutableRefObject<(HTMLAnchorElement | null)[]>
    resultsCount?: number
}

export const SearchFilters = ({
    counts,
    isLoading,
    inputRef,
    itemRefs,
    resultsCount = 0,
}: SearchFiltersProps) => {
    const { euiTheme } = useEuiTheme()
    const selectedFilter = useTypeFilter()
    const { setTypeFilter } = useSearchActions()
    const { apiResultsCount, docsResultsCount, totalCount } = counts

    const filterRefs = useRef<(HTMLButtonElement | null)[]>([])

    const handleFilterKeyDown = useCallback(
        (e: React.KeyboardEvent<HTMLButtonElement>, filterIndex: number) => {
            const filterCount = 3 // ALL, DOCS, API

            if (e.key === 'ArrowUp') {
                e.preventDefault()
                // Go back to input
                inputRef?.current?.focus()
            } else if (e.key === 'ArrowDown') {
                e.preventDefault()
                // Go to first result if available
                if (resultsCount > 0) {
                    itemRefs?.current[0]?.focus()
                }
            } else if (e.key === 'ArrowLeft') {
                e.preventDefault()
                if (filterIndex > 0) {
                    filterRefs.current[filterIndex - 1]?.focus()
                }
            } else if (e.key === 'ArrowRight') {
                e.preventDefault()
                if (filterIndex < filterCount - 1) {
                    filterRefs.current[filterIndex + 1]?.focus()
                }
            }
        },
        [inputRef, itemRefs, resultsCount]
    )

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
            role="group"
            aria-label="Search filters"
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
                    onClick={() => setTypeFilter('all')}
                    onKeyDown={(e: React.KeyboardEvent<HTMLButtonElement>) =>
                        handleFilterKeyDown(e, 0)
                    }
                    buttonRef={(el: HTMLButtonElement | null) => {
                        filterRefs.current[0] = el
                    }}
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
                    onClick={() => setTypeFilter('doc')}
                    onKeyDown={(e: React.KeyboardEvent<HTMLButtonElement>) =>
                        handleFilterKeyDown(e, 1)
                    }
                    buttonRef={(el: HTMLButtonElement | null) => {
                        filterRefs.current[1] = el
                    }}
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
                    onClick={() => setTypeFilter('api')}
                    onKeyDown={(e: React.KeyboardEvent<HTMLButtonElement>) =>
                        handleFilterKeyDown(e, 2)
                    }
                    buttonRef={(el: HTMLButtonElement | null) => {
                        filterRefs.current[2] = el
                    }}
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
