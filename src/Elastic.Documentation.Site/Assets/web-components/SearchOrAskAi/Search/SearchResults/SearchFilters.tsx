import { useTypeFilter, useSearchActions } from '../search.store'
import { useEuiTheme, EuiButton, EuiSkeletonRectangle } from '@elastic/eui'
import { css } from '@emotion/react'

interface SearchFiltersProps {
    counts: {
        apiResultsCount: number
        docsResultsCount: number
        totalCount: number
    }
    isLoading: boolean
}

export const SearchFilters = ({ counts, isLoading }: SearchFiltersProps) => {
    const { euiTheme } = useEuiTheme()
    const selectedFilter = useTypeFilter()
    const { setTypeFilter } = useSearchActions()
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
                    iconType="globe"
                    iconSize="s"
                    // @ts-expect-error: xs is valid size according to EuiButton docs
                    size="xs"
                    fill={selectedFilter === 'all'}
                    isLoading={isLoading}
                    onClick={() => setTypeFilter('all')}
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
                    iconType="document"
                    iconSize="s"
                    // @ts-expect-error: xs is valid size according to EuiButton docs
                    size="xs"
                    fill={selectedFilter === 'doc'}
                    isLoading={isLoading}
                    onClick={() => setTypeFilter('doc')}
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
                    iconType="code"
                    iconSize="s"
                    // @ts-expect-error: xs is valid size according to EuiButton docs
                    size="xs"
                    fill={selectedFilter === 'api'}
                    isLoading={isLoading}
                    onClick={() => setTypeFilter('api')}
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
