import { SearchOrAskAiErrorCallout } from '../../SearchOrAskAiErrorCallout'
import { useSearchActions, useSearchTerm } from '../search.store'
import { useSearchQuery } from '../useSearchQuery'
import { SearchFilters } from './SearchFilters'
import { SearchResultsList } from './SearchResultsList'
import {
    EuiSpacer,
    useEuiTheme,
    EuiHorizontalRule,
    EuiText,
} from '@elastic/eui'
import { css } from '@emotion/react'
import { useDebounce } from '@uidotdev/usehooks'
import { useEffect, MutableRefObject } from 'react'

interface SearchResultsProps {
    inputRef?: React.RefObject<HTMLInputElement>
    buttonRef?: React.RefObject<HTMLButtonElement>
    itemRefs?: MutableRefObject<(HTMLAnchorElement | null)[]>
}

export const SearchResults = ({
    inputRef,
    buttonRef,
    itemRefs,
}: SearchResultsProps) => {
    const { euiTheme } = useEuiTheme()
    const searchTerm = useSearchTerm()
    const { setPageNumber } = useSearchActions()
    const debouncedSearchTerm = useDebounce(searchTerm, 300)

    // Reset to first page when search term changes
    useEffect(() => {
        setPageNumber(0)
    }, [debouncedSearchTerm, setPageNumber])

    const { data, error, isLoading } = useSearchQuery()

    const results = data?.results ?? []
    const typeAggregations = data?.aggregations?.type
    const counts = {
        apiResultsCount: typeAggregations?.['api'] ?? 0,
        docsResultsCount: typeAggregations?.['doc'] ?? 0,
        totalCount:
            (typeAggregations?.['api'] ?? 0) + (typeAggregations?.['doc'] ?? 0),
    }

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
                    <SearchFilters
                        counts={counts}
                        isLoading={isInitialLoading}
                        inputRef={inputRef}
                        itemRefs={itemRefs}
                        resultsCount={results.length}
                    />

                    <EuiHorizontalRule margin="none" />

                    {!isInitialLoading && results.length === 0 && (
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

                    {data && results.length > 0 && (
                        <SearchResultsList
                            results={results}
                            pageNumber={data.pageNumber}
                            pageSize={data.pageSize}
                            isLoading={isInitialLoading}
                            searchTerm={debouncedSearchTerm}
                            inputRef={inputRef}
                            buttonRef={buttonRef}
                            itemRefs={itemRefs}
                        />
                    )}

                    {isInitialLoading && (
                        <SearchResultsList
                            results={[]}
                            pageNumber={0}
                            pageSize={10}
                            isLoading={true}
                            searchTerm={debouncedSearchTerm}
                            inputRef={inputRef}
                            buttonRef={buttonRef}
                            itemRefs={itemRefs}
                        />
                    )}
                </>
            )}
        </>
    )
}
