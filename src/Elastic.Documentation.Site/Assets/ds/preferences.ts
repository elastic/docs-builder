// The reader's display preferences: theme, contrast and motion. Each is one of its choices or "system", which follows the device.
// The head script has already applied the saved choices before first paint; this module is the control that changes them.
// The control is plain radios (data-pref names the preference, value the choice) inside .theme-switch, so it does nothing without script.
export type Pref = 'theme' | 'contrast' | 'motion'

const storageKeys: Record<Pref, string> = {
    theme: 'docs-theme',
    contrast: 'docs-contrast',
    motion: 'docs-motion',
}

const choices: Record<Pref, readonly string[]> = {
    theme: ['light', 'dark', 'black'],
    contrast: ['normal', 'more'],
    motion: ['full', 'reduce'],
}

function matches(query: string): boolean {
    return window.matchMedia?.(query).matches ?? false
}

export function readPref(pref: Pref): string {
    try {
        const stored = localStorage.getItem(storageKeys[pref])
        return stored && choices[pref].includes(stored) ? stored : 'system'
    } catch {
        return 'system'
    }
}

// Theme with "system" is left to the stylesheet, which follows prefers-color-scheme; the other two are resolved here, as the head script does.
function resolve(pref: Pref, choice: string): string | null {
    if (choice !== 'system') return choice
    if (pref === 'theme') return null
    if (pref === 'contrast')
        return matches('(prefers-contrast: more)') ? 'more' : 'normal'
    return matches('(prefers-reduced-motion: reduce)') ? 'reduce' : 'full'
}

export function applyPref(
    pref: Pref,
    choice: string,
    root: HTMLElement = document.documentElement
) {
    const resolved = resolve(pref, choice)
    if (resolved === null) delete root.dataset[pref]
    else root.dataset[pref] = resolved
    try {
        if (choice === 'system') localStorage.removeItem(storageKeys[pref])
        else localStorage.setItem(storageKeys[pref], choice)
    } catch {
        // storage unavailable: the choice still applies for this page view
    }
}

let followingSystem = false

// With "system" chosen, the page follows the device as its setting changes.
function followSystem() {
    if (followingSystem) return
    followingSystem = true
    const follow = (pref: Pref, query: string) =>
        window.matchMedia?.(query).addEventListener?.('change', () => {
            if (readPref(pref) === 'system') applyPref(pref, 'system')
        })
    follow('contrast', '(prefers-contrast: more)')
    follow('motion', '(prefers-reduced-motion: reduce)')
}

export function initPreferences(scope: ParentNode = document) {
    followSystem()
    scope
        .querySelectorAll<HTMLElement>('.theme-switch:not([data-pref-bound])')
        .forEach((control) => {
            control.dataset.prefBound = ''
            control
                .querySelectorAll<HTMLInputElement>('input[data-pref]')
                .forEach((radio) => {
                    const pref = radio.dataset.pref as Pref
                    radio.checked = readPref(pref) === radio.value
                    radio.addEventListener('change', () => {
                        if (radio.checked) applyPref(pref, radio.value)
                    })
                })

            const open = control.querySelector<HTMLInputElement>(
                '.theme-switch__open'
            )
            if (!open) return
            document.addEventListener('keydown', (event) => {
                if (event.key === 'Escape') open.checked = false
            })
            document.addEventListener('click', (event) => {
                if (!control.contains(event.target as Node))
                    open.checked = false
            })
        })
}
