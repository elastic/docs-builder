import { useSelectedIndex, useSearchActions } from './search.store'
import { useAskAiFromSearch } from './useAskAiFromSearch'
import { useRef, useCallback, MutableRefObject } from 'react'

interface SearchKeyboardNavigationReturn {
    inputRef: React.RefObject<HTMLInputElement>
    buttonRef: React.RefObject<HTMLButtonElement>
    itemRefs: MutableRefObject<(HTMLAnchorElement | null)[]>
    filterRefs: MutableRefObject<(HTMLButtonElement | null)[]>
    handleInputKeyDown: (e: React.KeyboardEvent<HTMLInputElement>) => void
}

export const useSearchKeyboardNavigation = (
    resultsCount: number
): SearchKeyboardNavigationReturn => {
    const inputRef = useRef<HTMLInputElement>(null)
    const buttonRef = useRef<HTMLButtonElement>(null)
    const itemRefs = useRef<(HTMLAnchorElement | null)[]>([])
    const filterRefs = useRef<(HTMLButtonElement | null)[]>([])
    const { askAi } = useAskAiFromSearch()
    const selectedIndex = useSelectedIndex()
    const { setSelectedIndex } = useSearchActions()

    const handleInputKeyDown = useCallback(
        (e: React.KeyboardEvent<HTMLInputElement>) => {
            if (e.key === 'Enter') {
                e.preventDefault()
                if (resultsCount > 0 && selectedIndex >= 0) {
                    // Navigate to selected result
                    itemRefs.current[selectedIndex]?.click()
                } else if (resultsCount > 0) {
                    // No selection, click first result
                    itemRefs.current[0]?.click()
                } else {
                    // No results, ask AI
                    askAi()
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
                    // Scroll into view
                    itemRefs.current[nextIndex]?.scrollIntoView({
                        block: 'nearest',
                    })
                }
            } else if (e.key === 'ArrowUp') {
                e.preventDefault()
                if (resultsCount > 0 && selectedIndex > 0) {
                    // Move selection up
                    const prevIndex = selectedIndex - 1
                    setSelectedIndex(prevIndex)
                    // Scroll into view
                    itemRefs.current[prevIndex]?.scrollIntoView({
                        block: 'nearest',
                    })
                }
            }
            // Tab works naturally - goes to filters, then button
        },
        [resultsCount, selectedIndex, setSelectedIndex, askAi]
    )

    return {
        inputRef,
        buttonRef,
        itemRefs,
        filterRefs,
        handleInputKeyDown,
    }
}
