import { useEffect } from 'react'

const EDITABLE_SELECTOR =
    'input, textarea, select, [contenteditable=""], [contenteditable="true"]'

interface Options {
    enabled: boolean
    inputRef: React.RefObject<HTMLInputElement>
}

const isEditableTarget = (target: EventTarget | null) =>
    target instanceof Element && target.closest(EDITABLE_SELECTOR) !== null

export const useSlashToFocus = ({ enabled, inputRef }: Options) => {
    useEffect(() => {
        if (!enabled) return

        const handleKeyDown = (e: KeyboardEvent) => {
            // Shift is allowed: "/" is Shift+7 on some keyboard layouts.
            if (e.key !== '/' || e.ctrlKey || e.metaKey || e.altKey) return
            if (e.defaultPrevented || isEditableTarget(e.target)) return

            const input = inputRef.current
            if (!input || input.disabled) return

            e.preventDefault()
            input.focus()
            input.select()
        }

        window.addEventListener('keydown', handleKeyDown)
        return () => window.removeEventListener('keydown', handleKeyDown)
    }, [enabled, inputRef])
}
