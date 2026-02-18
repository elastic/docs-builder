/**
 * Theme switching utilities for dark mode.
 * Used by isolated and codex builds only (not assembler).
 */

export type Theme = 'light' | 'dark'

const STORAGE_KEY = 'theme'

export function getTheme(): Theme {
    if (typeof document === 'undefined') return 'light'
    const stored = document.documentElement.dataset.theme
    if (stored === 'dark' || stored === 'light') return stored
    const fromStorage = localStorage.getItem(STORAGE_KEY)
    if (fromStorage === 'dark' || fromStorage === 'light') return fromStorage
    return window.matchMedia('(prefers-color-scheme: dark)').matches
        ? 'dark'
        : 'light'
}

export function setTheme(theme: Theme): void {
    document.documentElement.dataset.theme = theme
    localStorage.setItem(STORAGE_KEY, theme)
    document.dispatchEvent(
        new CustomEvent<Theme>('theme-change', { detail: theme })
    )
}

export function toggleTheme(): Theme {
    const next: Theme = getTheme() === 'dark' ? 'light' : 'dark'
    setTheme(next)
    return next
}
