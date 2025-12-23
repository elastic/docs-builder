import { useSelectedIndex, useSearchActions } from '../Search/search.store'
import { useRef, useCallback, MutableRefObject } from 'react'

interface Options {
    resultsCount: number
    isLoading: boolean
    onClose: () => void
}

interface Result {
    inputRef: React.RefObject<HTMLInputElement>
    itemRefs: MutableRefObject<(HTMLAnchorElement | null)[]>
    isKeyboardNavigating: MutableRefObject<boolean>
    handleInputKeyDown: (e: React.KeyboardEvent<HTMLInputElement>) => void
    handleMouseMove: () => void
}

export const useNavigationSearchKeyboardNavigation = ({
    resultsCount,
    isLoading,
    onClose,
}: Options): Result => {
    const inputRef = useRef<HTMLInputElement>(null)
    const itemRefs = useRef<(HTMLAnchorElement | null)[]>([])
    const isKeyboardNavigating = useRef(false)
    const selectedIndex = useSelectedIndex()
    const { setSelectedIndex } = useSearchActions()

    const handleMouseMove = useCallback(() => {
        isKeyboardNavigating.current = false
    }, [])

    const scrollToItem = (index: number) => {
        const element = itemRefs.current[index]
        element?.scrollIntoView?.({ block: 'nearest' })
    }

    const navigateToResult = (index: number) => {
        itemRefs.current[index]?.click()
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
        [resultsCount, isLoading, selectedIndex, setSelectedIndex, onClose]
    )

    return {
        inputRef,
        itemRefs,
        isKeyboardNavigating,
        handleInputKeyDown,
        handleMouseMove,
    }
}
