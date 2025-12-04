import { useAskAiFromSearch } from './useAskAiFromSearch'
import { useRef, MutableRefObject } from 'react'

interface SearchKeyboardNavigationReturn {
    inputRef: React.RefObject<HTMLInputElement>
    buttonRef: React.RefObject<HTMLButtonElement>
    listItemRefs: MutableRefObject<(HTMLAnchorElement | null)[]>
    handleInputKeyDown: (e: React.KeyboardEvent<HTMLInputElement>) => void
    handleListItemKeyDown: (
        e: React.KeyboardEvent<HTMLAnchorElement>,
        currentIndex: number
    ) => void
    focusLastAvailable: () => void
    setItemRef: (element: HTMLAnchorElement | null, index: number) => void
}

export const useSearchKeyboardNavigation = (): SearchKeyboardNavigationReturn => {
    const inputRef = useRef<HTMLInputElement>(null)
    const buttonRef = useRef<HTMLButtonElement>(null)
    const listItemRefs = useRef<(HTMLAnchorElement | null)[]>([])
    const { askAi } = useAskAiFromSearch()

    const setItemRef = (element: HTMLAnchorElement | null, index: number) => {
        listItemRefs.current[index] = element
    }

    const focusFirstAvailable = () => {
        const firstItem = listItemRefs.current.find((item) => item !== null)
        if (firstItem) {
            firstItem.focus()
        } else if (buttonRef.current) {
            buttonRef.current.focus()
        }
    }

    const focusLastAvailable = () => {
        const lastItem = [...listItemRefs.current]
            .reverse()
            .find((item) => item !== null)
        if (lastItem) {
            lastItem.focus()
        } else if (inputRef.current) {
            inputRef.current.focus()
        }
    }

    const handleInputKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
        if (e.key === 'Enter') {
            e.preventDefault()
            // Navigate to first result if available, otherwise trigger Ask AI
            const firstItem = listItemRefs.current.find((item) => item !== null)
            if (firstItem) {
                firstItem.click()
            } else {
                askAi()
            }
        } else if (e.key === 'ArrowDown') {
            e.preventDefault()
            focusFirstAvailable()
        }
    }

    const handleListItemKeyDown = (
        e: React.KeyboardEvent<HTMLAnchorElement>,
        currentIndex: number
    ) => {
        if (e.key === 'ArrowDown') {
            e.preventDefault()
            const nextItem = listItemRefs.current[currentIndex + 1]
            if (nextItem) {
                nextItem.focus()
            } else {
                buttonRef.current?.focus()
            }
        } else if (e.key === 'ArrowUp') {
            e.preventDefault()
            const prevItem = listItemRefs.current[currentIndex - 1]
            if (prevItem) {
                prevItem.focus()
            } else {
                inputRef.current?.focus()
            }
        }
    }

    return {
        inputRef,
        buttonRef,
        listItemRefs,
        handleInputKeyDown,
        handleListItemKeyDown,
        focusLastAvailable,
        setItemRef,
    }
}
