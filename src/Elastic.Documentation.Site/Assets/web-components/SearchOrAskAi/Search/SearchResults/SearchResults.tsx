import { SearchOrAskAiErrorCallout } from '../../SearchOrAskAiErrorCallout'
import { usePageNumber, useSearchActions, useSearchTerm } from '../search.store'
import { useSearchQuery } from '../useSearchQuery'
import { SearchResultListItem } from './SearchResultsListItem'
import { EuiPagination, EuiSpacer } from '@elastic/eui'
import { css } from '@emotion/react'
import { useDebounce } from '@uidotdev/usehooks'
import { useEffect } from 'react'

interface SearchResultsProps {
    onKeyDown?: (e: React.KeyboardEvent<HTMLLIElement>, index: number) => void
    setItemRef?: (element: HTMLAnchorElement | null, index: number) => void
}

export const SearchResults = ({
    onKeyDown,
    setItemRef,
}: SearchResultsProps) => {
    const searchTerm = useSearchTerm()
    const activePage = usePageNumber()
    const { setPageNumber: setActivePage } = useSearchActions()
    const debouncedSearchTerm = useDebounce(searchTerm, 300)

    useEffect(() => {
        setActivePage(0)
    }, [debouncedSearchTerm])

    const { data, error } = useSearchQuery()

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
                <div data-search-results>
                    <EuiSpacer size="s" />
                    {data && (
                        <>
                            <ul>
                                {data.results.map((result, index) => (
                                    <SearchResultListItem
                                        item={result}
                                        key={result.url}
                                        index={index}
                                        onKeyDown={onKeyDown}
                                        setRef={setItemRef}
                                    />
                                ))}
                            </ul>
                            <EuiSpacer size="m" />
                            <div
                                css={css`
                                    display: flex;
                                    justify-content: center;
                                `}
                            >
                                <EuiPagination
                                    aria-label="Search results pages"
                                    pageCount={Math.min(data.pageCount, 10)}
                                    activePage={activePage}
                                    onPageClick={(activePage) =>
                                        setActivePage(activePage)
                                    }
                                />
                            </div>
                        </>
                    )}
                </div>
            )}
        </>
    )
}
