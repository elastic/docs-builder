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
    inputRef?: React.RefObject<HTMLInputElement>
    buttonRef?: React.RefObject<HTMLButtonElement>
    itemRefs?: MutableRefObject<(HTMLAnchorElement | null)[]>
}

export const SearchResultsList = ({
    results,
    pageNumber,
    pageSize,
    isLoading,
    searchTerm,
    inputRef,
    buttonRef,
    itemRefs,
}: SearchResultsListProps) => {
    if (isLoading) {
        return null
    }
    const { euiTheme } = useEuiTheme()
    const selectedIndex = useSelectedIndex()
    const { setSelectedIndex, clearSelection } = useSearchActions()
    const scrollContainerRef = useRef<HTMLDivElement>(null)

    const scrollbarStyle = css`
        max-height: 400px;
        padding-block-start: ${euiTheme.size.base};
        padding-block-end: ${euiTheme.size.base};
        margin-inline-end: ${euiTheme.size.s};
        ${useEuiOverflowScroll('y', true)}
        mask-image: none;
    `

    const resetScrollToTop = useCallback(() => {
        if (scrollContainerRef.current) {
            scrollContainerRef.current.scrollTop = 0
        }
    }, [])

    useEffect(() => {
        resetScrollToTop()
    }, [searchTerm, resetScrollToTop])

    // Scroll selected item into view when selection changes
    useEffect(() => {
        const selectedElement = itemRefs?.current[selectedIndex]
        // scrollIntoView may not exist in test environments (JSDOM)
        if (
            selectedElement &&
            typeof selectedElement.scrollIntoView === 'function'
        ) {
            selectedElement.scrollIntoView({ block: 'nearest' })
        }
    }, [selectedIndex, itemRefs])

    // Sync selectedIndex when an item receives focus (e.g., via click or tab)
    const handleItemFocus = useCallback(
        (index: number) => {
            setSelectedIndex(index)
        },
        [setSelectedIndex]
    )

    // Handle keyboard navigation when an item is focused
    const handleItemKeyDown = useCallback(
        (e: React.KeyboardEvent<HTMLAnchorElement>, currentIndex: number) => {
            if (e.key === 'ArrowDown') {
                e.preventDefault()
                if (currentIndex < results.length - 1) {
                    // Move to next item
                    itemRefs?.current[currentIndex + 1]?.focus()
                } else {
                    // At last item, go to button
                    buttonRef?.current?.focus()
                }
            } else if (e.key === 'ArrowUp') {
                e.preventDefault()
                if (currentIndex > 0) {
                    // Move to previous item
                    itemRefs?.current[currentIndex - 1]?.focus()
                } else {
                    // At first item, go back to input
                    inputRef?.current?.focus()
                }
            }
        },
        [results.length, inputRef, buttonRef, itemRefs]
    )

    // Clear selection when focus leaves the results list
    const handleListBlur = useCallback(
        (e: React.FocusEvent<HTMLUListElement>) => {
            const newFocusTarget = e.relatedTarget as HTMLElement | null
            const focusLeftList = !e.currentTarget.contains(newFocusTarget)
            if (focusLeftList) {
                clearSelection()
            }
        },
        [clearSelection]
    )

    return (
        <div data-search-results ref={scrollContainerRef} css={scrollbarStyle}>
            <ul onBlur={handleListBlur}>
                {results.map((result, index) => (
                    <SearchResultListItem
                        item={result}
                        key={result.url}
                        index={index}
                        pageNumber={pageNumber}
                        pageSize={pageSize}
                        isSelected={index === selectedIndex}
                        onFocus={handleItemFocus}
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
