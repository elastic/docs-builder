import {
    useSelectedIndex,
    useSearchActions,
    useSearchTerm,
} from './navigationSearch.store'
import { type TypeFilter } from './useNavigationSearchQuery'
import { useNavigationSearchTelemetry } from './useNavigationSearchTelemetry'
import { useRef, useCallback } from 'react'

interface Options {
    resultsCount: number
    isLoading: boolean
    onClose: () => void
    typeFilter: TypeFilter
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
    typeFilter,
}: Options): Result => {
    const inputRef = useRef<HTMLInputElement>(null)
    const isKeyboardNavigating = useRef(false)
    const selectedIndex = useSelectedIndex()
    const searchTerm = useSearchTerm()
    const { setSelectedIndex } = useSearchActions()
    const { trackNavigation } = useNavigationSearchTelemetry(typeFilter)

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
        // The row's onClick closes the dropdown or waits for the htmx request, as for a mouse click.
        element?.click()
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
                        trackNavigation({
                            method: 'keyboard',
                            direction: 'down',
                            query: searchTerm,
                        })
                    }
                    break

                case 'ArrowUp':
                    e.preventDefault()
                    isKeyboardNavigating.current = true
                    if (resultsCount > 0 && selectedIndex > 0) {
                        const prevIndex = selectedIndex - 1
                        setSelectedIndex(prevIndex)
                        scrollToItem(prevIndex)
                        trackNavigation({
                            method: 'keyboard',
                            direction: 'up',
                            query: searchTerm,
                        })
                    }
                    break
            }
        },
        [
            resultsCount,
            isLoading,
            selectedIndex,
            searchTerm,
            setSelectedIndex,
            onClose,
            trackNavigation,
        ]
    )

    return {
        inputRef,
        isKeyboardNavigating,
        handleInputKeyDown,
        handleMouseMove,
    }
}
