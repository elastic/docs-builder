import {
    getTheme,
    toggleTheme as doToggleTheme,
    type Theme,
} from '../../theme-switch'
import { useCallback, useEffect, useState } from 'react'

export function useTheme(): { theme: Theme; toggleTheme: () => void } {
    const [theme, setThemeState] = useState<Theme>(() => getTheme())

    useEffect(() => {
        const handler = (e: CustomEvent<Theme>) => setThemeState(e.detail)
        document.addEventListener('theme-change', handler as EventListener)
        return () =>
            document.removeEventListener(
                'theme-change',
                handler as EventListener
            )
    }, [])

    const toggleTheme = useCallback(() => {
        const next = doToggleTheme()
        setThemeState(next)
    }, [])

    return { theme, toggleTheme }
}
