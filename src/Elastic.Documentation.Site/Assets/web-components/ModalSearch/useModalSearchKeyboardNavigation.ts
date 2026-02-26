import {
    useSelectedIndex,
    useModalSearchActions,
    useSearchTerm,
} from './modalSearch.store'
import { useModalSearchTelemetry } from './useModalSearchTelemetry'
import { useRef, useCallback, useEffect } from 'react'

interface Options {
    resultsCount: number
    isLoading: boolean
    onClose: () => void
    onNavigate: () => void
}

interface Result {
    inputRef: React.RefObject<HTMLInputElement>
    panelRef: React.RefObject<HTMLDivElement>
    isKeyboardNavigating: React.MutableRefObject<boolean>
    handleInputKeyDown: (e: React.KeyboardEvent<HTMLInputElement>) => void
    handleMouseMove: () => void
}

export const useModalSearchKeyboardNavigation = ({
    resultsCount,
    isLoading,
    onClose,
    onNavigate,
}: Options): Result => {
    const inputRef = useRef<HTMLInputElement>(null)
    const panelRef = useRef<HTMLDivElement>(null)
    const isKeyboardNavigating = useRef(false)
    const selectedIndex = useSelectedIndex()
    const searchTerm = useSearchTerm()
    const { setSelectedIndex } = useModalSearchActions()
    const { trackNavigation } = useModalSearchTelemetry()

    const handleMouseMove = useCallback(() => {
        isKeyboardNavigating.current = false
    }, [])

    const navigateToResult = useCallback(
        (index: number) => {
            const element = document.querySelector<HTMLAnchorElement>(
                `[data-search-result-index="${index}"]`
            )
            if (element) {
                onNavigate()
                element.click()
            }
        },
        [onNavigate]
    )

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
            navigateToResult,
            trackNavigation,
        ]
    )

    useEffect(() => {
        const panel = panelRef.current
        if (!panel) return

        const handlePanelKeyDown = (e: KeyboardEvent) => {
            if (e.target === inputRef.current) return

            const target = e.target as HTMLElement
            const isInteractive =
                target.tagName === 'BUTTON' ||
                target.tagName === 'A' ||
                !!target.closest('button, a')

            switch (e.key) {
                case 'ArrowDown':
                    e.preventDefault()
                    isKeyboardNavigating.current = true
                    if (resultsCount > 0) {
                        const nextIndex =
                            selectedIndex < 0
                                ? 0
                                : Math.min(selectedIndex + 1, resultsCount - 1)
                        setSelectedIndex(nextIndex)
                    }
                    break

                case 'ArrowUp':
                    e.preventDefault()
                    isKeyboardNavigating.current = true
                    if (resultsCount > 0 && selectedIndex > 0) {
                        const prevIndex = selectedIndex - 1
                        setSelectedIndex(prevIndex)
                    }
                    break

                case 'Enter':
                    if (isInteractive) return
                    e.preventDefault()
                    if (isLoading || resultsCount === 0) return
                    navigateToResult(selectedIndex >= 0 ? selectedIndex : 0)
                    break

                case 'Escape':
                    e.preventDefault()
                    onClose()
                    break
            }
        }

        panel.addEventListener('keydown', handlePanelKeyDown)
        return () => panel.removeEventListener('keydown', handlePanelKeyDown)
    }, [
        resultsCount,
        isLoading,
        selectedIndex,
        setSelectedIndex,
        navigateToResult,
        onClose,
    ])

    return {
        inputRef,
        panelRef,
        isKeyboardNavigating,
        handleInputKeyDown,
        handleMouseMove,
    }
}
