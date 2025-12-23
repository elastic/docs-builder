import { useEffect } from 'react'

/**
 * Hook to listen for global keyboard shortcuts with Cmd/Ctrl modifier
 */
export const useGlobalKeyboardShortcut = (
    key: string,
    callback: () => void
) => {
    useEffect(() => {
        const handleKeyDown = (e: KeyboardEvent) => {
            if ((e.metaKey || e.ctrlKey) && e.key === key) {
                e.preventDefault()
                callback()
            }
        }
        window.addEventListener('keydown', handleKeyDown)
        return () => window.removeEventListener('keydown', handleKeyDown)
    }, [key, callback])
}
