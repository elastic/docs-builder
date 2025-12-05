import { useSelectedIndex, NO_SELECTION } from './search.store'
import { useAskAiFromSearch } from './useAskAiFromSearch'
import { useRef, MutableRefObject } from 'react'

interface SearchKeyboardNavigationReturn {
    inputRef: React.RefObject<HTMLInputElement>
    buttonRef: React.RefObject<HTMLButtonElement>
    itemRefs: MutableRefObject<(HTMLAnchorElement | null)[]>
    handleInputKeyDown: (e: React.KeyboardEvent<HTMLInputElement>) => void
    focusLastAvailable: () => void
}

export const useSearchKeyboardNavigation = (
    resultsCount: number
): SearchKeyboardNavigationReturn => {
    const inputRef = useRef<HTMLInputElement>(null)
    const buttonRef = useRef<HTMLButtonElement>(null)
    const itemRefs = useRef<(HTMLAnchorElement | null)[]>([])
    const { askAi } = useAskAiFromSearch()
    const selectedIndex = useSelectedIndex()

    const focusLastAvailable = () => {
        if (resultsCount > 0) {
            itemRefs.current[resultsCount - 1]?.focus()
        } else {
            inputRef.current?.focus()
        }
    }

    const handleInputKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
        if (e.key === 'Enter') {
            e.preventDefault()
            if (resultsCount > 0) {
                // Navigate to selected result
                itemRefs.current[selectedIndex]?.click()
            } else {
                askAi()
            }
        } else if (e.key === 'ArrowDown') {
            e.preventDefault()
            if (resultsCount > 1) {
                // First item is already visually selected, so go to second item
                const targetIndex = Math.min(
                    selectedIndex + 1,
                    resultsCount - 1
                )
                itemRefs.current[targetIndex]?.focus()
            } else {
                // Only 1 or 0 results, go to button
                buttonRef.current?.focus()
            }
        } else if (
            e.key === 'Tab' &&
            e.shiftKey &&
            selectedIndex !== NO_SELECTION
        ) {
            // When Shift+Tab from input with a selected item,
            // focus the selected result and let browser handle the rest
            itemRefs.current[selectedIndex]?.focus()
            // Don't preventDefault - let browser handle Shift+Tab from the focused result
        }
        // ArrowUp from input does nothing (already at top)
    }

    return {
        inputRef,
        buttonRef,
        itemRefs,
        handleInputKeyDown,
        focusLastAvailable,
    }
}
