import { useSelectedIndex, useSearchActions } from './search.store'
import { useRef, useCallback, MutableRefObject } from 'react'

interface SearchKeyboardNavigationOptions {
    resultsCount: number
    isLoading: boolean
}

interface SearchKeyboardNavigationReturn {
    inputRef: React.RefObject<HTMLInputElement>
    buttonRef: React.RefObject<HTMLButtonElement>
    itemRefs: MutableRefObject<(HTMLAnchorElement | null)[]>
    filterRefs: MutableRefObject<(HTMLButtonElement | null)[]>
    handleInputKeyDown: (e: React.KeyboardEvent<HTMLInputElement>) => void
}

export const useSearchKeyboardNavigation = ({
    resultsCount,
    isLoading,
}: SearchKeyboardNavigationOptions): SearchKeyboardNavigationReturn => {
    const inputRef = useRef<HTMLInputElement>(null)
    const buttonRef = useRef<HTMLButtonElement>(null)
    const itemRefs = useRef<(HTMLAnchorElement | null)[]>([])
    const filterRefs = useRef<(HTMLButtonElement | null)[]>([])
    const selectedIndex = useSelectedIndex()
    const { setSelectedIndex } = useSearchActions()

    const handleInputKeyDown = useCallback(
        (e: React.KeyboardEvent<HTMLInputElement>) => {
            if (e.key === 'Enter') {
                e.preventDefault()
                if (isLoading || resultsCount === 0) {
                    // Don't do anything while search is loading or if there are no results
                    return
                }
                if (selectedIndex >= 0) {
                    // Navigate to selected result
                    itemRefs.current[selectedIndex]?.click()
                } else {
                    // No selection, click first result
                    itemRefs.current[0]?.click()
                }
            } else if (e.key === 'ArrowDown') {
                e.preventDefault()
                if (resultsCount > 0) {
                    // Move selection down (or start at 0)
                    const nextIndex =
                        selectedIndex < 0
                            ? 0
                            : Math.min(selectedIndex + 1, resultsCount - 1)
                    setSelectedIndex(nextIndex)
                    // Scroll into view (guard for JSDOM)
                    const element = itemRefs.current[nextIndex]
                    if (
                        element &&
                        typeof element.scrollIntoView === 'function'
                    ) {
                        element.scrollIntoView({ block: 'nearest' })
                    }
                }
            } else if (e.key === 'ArrowUp') {
                e.preventDefault()
                if (resultsCount > 0 && selectedIndex > 0) {
                    // Move selection up
                    const prevIndex = selectedIndex - 1
                    setSelectedIndex(prevIndex)
                    // Scroll into view (guard for JSDOM)
                    const element = itemRefs.current[prevIndex]
                    if (
                        element &&
                        typeof element.scrollIntoView === 'function'
                    ) {
                        element.scrollIntoView({ block: 'nearest' })
                    }
                }
            }
            // Tab works naturally - goes to filters, then button
        },
        [resultsCount, isLoading, selectedIndex, setSelectedIndex]
    )

    return {
        inputRef,
        buttonRef,
        itemRefs,
        filterRefs,
        handleInputKeyDown,
    }
}
