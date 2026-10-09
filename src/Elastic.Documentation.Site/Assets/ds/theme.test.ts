import {
    applyTheme,
    initThemeToggles,
    nextTheme,
    readTheme,
    themeStorageKey,
} from './theme'

describe('theme', () => {
    beforeEach(() => {
        localStorage.clear()
        delete document.documentElement.dataset.theme
        document.body.innerHTML =
            '<button data-theme-toggle hidden><span data-theme-label>System</span></button>'
    })

    it('cycles system, light, dark', () => {
        expect(nextTheme('system')).toBe('light')
        expect(nextTheme('light')).toBe('dark')
        expect(nextTheme('dark')).toBe('system')
    })

    it('stores a pinned choice and clears it for system', () => {
        applyTheme('dark')
        expect(localStorage.getItem(themeStorageKey)).toBe('dark')
        expect(document.documentElement.dataset.theme).toBe('dark')
        expect(readTheme()).toBe('dark')

        applyTheme('system')
        expect(localStorage.getItem(themeStorageKey)).toBeNull()
        expect(document.documentElement.dataset.theme).toBeUndefined()
    })

    it('reveals a hidden toggle and names the current choice', () => {
        initThemeToggles()
        const button = document.querySelector<HTMLButtonElement>(
            '[data-theme-toggle]'
        )!

        expect(button.hidden).toBe(false)
        expect(button.querySelector('[data-theme-label]')!.textContent).toBe(
            'System'
        )
    })

    it('changes the theme on click and updates the label', () => {
        initThemeToggles()
        const button = document.querySelector<HTMLButtonElement>(
            '[data-theme-toggle]'
        )!
        button.click()

        expect(document.documentElement.dataset.theme).toBe('light')
        expect(button.querySelector('[data-theme-label]')!.textContent).toBe(
            'Light'
        )
        expect(button.getAttribute('aria-label')).toContain('light')
    })
})
