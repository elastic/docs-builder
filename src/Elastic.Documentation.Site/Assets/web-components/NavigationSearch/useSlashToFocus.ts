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
            if (e.ctrlKey || e.metaKey || e.altKey) return
            // Shift is allowed for "/" (Shift+7 on some layouts) but not for "s", so a capital S is ignored.
            if (e.key !== '/' && !(e.key === 's' && !e.shiftKey)) return
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
