import { useSelectedIndex, useSearchActions } from './navigationSearch.store'
import { useRef, useCallback } from 'react'

interface Options {
    resultsCount: number
    isLoading: boolean
    onClose: () => void
    onNavigate: () => void
}

interface Result {
    inputRef: React.RefObject<HTMLInputElement>
    isKeyboardNavigating: React.MutableRefObject<boolean>
    handleInputKeyDown: (e: React.KeyboardEvent<HTMLInputElement>) => void
    handleMouseMove: () => void
}

export const useNavigationSearchKeyboardNavigation = ({
    resultsCount,
    isLoading,
    onClose,
    onNavigate,
}: Options): Result => {
    const inputRef = useRef<HTMLInputElement>(null)
    const isKeyboardNavigating = useRef(false)
    const selectedIndex = useSelectedIndex()
    const { setSelectedIndex } = useSearchActions()

    const handleMouseMove = useCallback(() => {
        isKeyboardNavigating.current = false
    }, [])

    const scrollToItem = (index: number) => {
        // Use data attribute to find and scroll to item
        const element = document.querySelector(
            `[data-search-result-index="${index}"]`
        )
        element?.scrollIntoView?.({ block: 'nearest' })
    }

    const navigateToResult = (index: number) => {
        // Click the anchor with the matching data attribute
        const element = document.querySelector<HTMLAnchorElement>(
            `[data-search-result-index="${index}"]`
        )
        if (element) {
            onNavigate()
            element.click()
        }
    }

    const handleInputKeyDown = useCallback(
        (e: React.KeyboardEvent<HTMLInputElement>) => {
            switch (e.key) {
                case 'Escape':
                    e.preventDefault()
                    onClose()
                    break

                case 'Enter':
                    e.preventDefault()
                    if (isLoading || resultsCount === 0) return
                    navigateToResult(selectedIndex >= 0 ? selectedIndex : 0)
                    break

                case 'ArrowDown':
                    e.preventDefault()
                    isKeyboardNavigating.current = true
                    if (resultsCount > 0) {
                        const nextIndex =
                            selectedIndex < 0
                                ? 0
                                : Math.min(selectedIndex + 1, resultsCount - 1)
                        setSelectedIndex(nextIndex)
                        scrollToItem(nextIndex)
                    }
                    break

                case 'ArrowUp':
                    e.preventDefault()
                    isKeyboardNavigating.current = true
                    if (resultsCount > 0 && selectedIndex > 0) {
                        const prevIndex = selectedIndex - 1
                        setSelectedIndex(prevIndex)
                        scrollToItem(prevIndex)
                    }
                    break
            }
        },
        [
            resultsCount,
            isLoading,
            selectedIndex,
            setSelectedIndex,
            onClose,
            onNavigate,
        ]
    )

    return {
        inputRef,
        isKeyboardNavigating,
        handleInputKeyDown,
        handleMouseMove,
    }
}
