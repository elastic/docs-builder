import { useSelectedIndex, useSearchActions } from '../search.store'
import { type SearchResultItem } from '../useSearchQuery'
import { SearchResultListItem } from './SearchResultsListItem'
import { useEuiOverflowScroll, useEuiTheme } from '@elastic/eui'
import { css } from '@emotion/react'
import { useRef, useCallback, useEffect, MutableRefObject } from 'react'

interface SearchResultsListProps {
    results: SearchResultItem[]
    pageNumber: number
    pageSize: number
    isLoading: boolean
    searchTerm: string
    itemRefs?: MutableRefObject<(HTMLAnchorElement | null)[]>
}

export const SearchResultsList = ({
    results,
    pageNumber,
    pageSize,
    isLoading,
    searchTerm,
    itemRefs,
}: SearchResultsListProps) => {
    if (isLoading) {
        return null
    }
    const { euiTheme } = useEuiTheme()
    const selectedIndex = useSelectedIndex()
    const { setSelectedIndex } = useSearchActions()
    const scrollContainerRef = useRef<HTMLDivElement>(null)

    const scrollbarStyle = css`
        max-height: 400px;
        padding-block-start: ${euiTheme.size.base};
        padding-block-end: ${euiTheme.size.base};
        padding-inline-end: ${euiTheme.size.xs};
        margin-inline-end: ${euiTheme.size.xs};
        ${useEuiOverflowScroll('y', false)}
    `

    const resetScrollToTop = useCallback(() => {
        if (scrollContainerRef.current) {
            scrollContainerRef.current.scrollTop = 0
        }
    }, [])

    useEffect(() => {
        resetScrollToTop()
    }, [searchTerm, resetScrollToTop])

    // Roving tabindex: only one item is tabbable
    const getTabIndex = useCallback(
        (index: number): 0 | -1 => {
            const effectiveIndex = selectedIndex >= 0 ? selectedIndex : 0
            return index === effectiveIndex ? 0 : -1
        },
        [selectedIndex]
    )

    // Handle arrow keys when focus is on a result item
    const handleItemKeyDown = useCallback(
        (e: React.KeyboardEvent<HTMLAnchorElement>, currentIndex: number) => {
            if (e.key === 'ArrowDown') {
                e.preventDefault()
                if (currentIndex < results.length - 1) {
                    const nextIndex = currentIndex + 1
                    setSelectedIndex(nextIndex)
                    itemRefs?.current[nextIndex]?.focus()
                }
            } else if (e.key === 'ArrowUp') {
                e.preventDefault()
                if (currentIndex > 0) {
                    const prevIndex = currentIndex - 1
                    setSelectedIndex(prevIndex)
                    itemRefs?.current[prevIndex]?.focus()
                }
            }
        },
        [results.length, setSelectedIndex, itemRefs]
    )

    return (
        <div data-search-results ref={scrollContainerRef} css={scrollbarStyle}>
            <ul role="listbox" aria-label="Search results">
                {results.map((result, index) => (
                    <SearchResultListItem
                        item={result}
                        key={result.url}
                        index={index}
                        pageNumber={pageNumber}
                        pageSize={pageSize}
                        isSelected={index === selectedIndex}
                        tabIndex={getTabIndex(index)}
                        onSelect={setSelectedIndex}
                        onKeyDown={handleItemKeyDown}
                        setRef={(el) => {
                            if (itemRefs) {
                                itemRefs.current[index] = el
                            }
                        }}
                    />
                ))}
            </ul>
        </div>
    )
}
