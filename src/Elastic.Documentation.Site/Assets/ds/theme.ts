export const themeStorageKey = 'docs-theme'

export type ThemeChoice = 'system' | 'light' | 'dark'

const order: ThemeChoice[] = ['system', 'light', 'dark']

export function readTheme(): ThemeChoice {
    try {
        const stored = localStorage.getItem(themeStorageKey)
        return stored === 'light' || stored === 'dark' ? stored : 'system'
    } catch {
        return 'system'
    }
}

export function applyTheme(
    choice: ThemeChoice,
    root: HTMLElement = document.documentElement
) {
    if (choice === 'system') {
        delete root.dataset.theme
    } else {
        root.dataset.theme = choice
    }
    try {
        if (choice === 'system') {
            localStorage.removeItem(themeStorageKey)
        } else {
            localStorage.setItem(themeStorageKey, choice)
        }
    } catch {
        // storage unavailable: the choice still applies for this page view
    }
}

export function nextTheme(choice: ThemeChoice): ThemeChoice {
    return order[(order.indexOf(choice) + 1) % order.length]
}

// Controls are plain buttons with data-theme-toggle; without JS they do nothing and the page follows the system.
export function initThemeToggles(scope: ParentNode = document) {
    scope
        .querySelectorAll<HTMLElement>(
            '[data-theme-toggle]:not([data-theme-bound])'
        )
        .forEach((button) => {
            button.dataset.themeBound = ''
            const sync = (choice: ThemeChoice) => {
                button.dataset.themeChoice = choice
                button.setAttribute(
                    'aria-label',
                    `Theme: ${choice}. Activate to change.`
                )
            }
            sync(readTheme())
            button.addEventListener('click', () => {
                const choice = nextTheme(readTheme())
                applyTheme(choice)
                sync(choice)
            })
        })
}
