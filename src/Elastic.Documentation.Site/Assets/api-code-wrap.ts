import { iconButton } from './api-icon-button'
import { initTooltips, setTooltip } from './tooltip'

export const apiCodeWrapStorageKey = 'api-code-wrap'

const cardSelector = '#api-examples-panel [data-code-card]'

const wrapIcon = `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true"><path d="M2 3h12v1H2V3Zm0 4h9.5a2.5 2.5 0 0 1 0 5H9.7l1.15 1.15-.7.7L7.8 11.5l2.35-2.35.7.7L9.7 11h1.8a1.5 1.5 0 0 0 0-3H2V7Zm0 5h4v1H2v-1Z" fill="currentColor"/></svg>`

function readPreference(): boolean {
    try {
        return window.localStorage.getItem(apiCodeWrapStorageKey) === 'true'
    } catch {
        return false
    }
}

function writePreference(wrapped: boolean): void {
    try {
        window.localStorage.setItem(apiCodeWrapStorageKey, String(wrapped))
    } catch {
        // Private mode or blocked storage: the choice lasts for this page only.
    }
}

function isWrapped(): boolean {
    return document.documentElement.hasAttribute('data-api-code-wrap')
}

function syncButtons(): void {
    const wrapped = isWrapped()
    document
        .querySelectorAll<HTMLButtonElement>('[data-code-wrap-toggle]')
        .forEach((button) => {
            button.setAttribute('aria-pressed', String(wrapped))
            setTooltip(button, wrapped ? 'Disable line wrapping' : 'Wrap lines')
        })
}

function toggleWrap(): void {
    const wrapped = !isWrapped()
    document.documentElement.toggleAttribute('data-api-code-wrap', wrapped)
    writePreference(wrapped)
    syncButtons()
}

function createButton(): HTMLButtonElement {
    const button = iconButton('api-code-wrap-btn', 'Wrap lines', wrapIcon)
    button.dataset.codeWrapToggle = ''
    button.addEventListener('click', toggleWrap)
    return button
}

/**
 * Adds a "Wrap lines" toggle to the header of every code card in the examples rail. The choice applies to
 * every card in the rail (CSS keys on html[data-api-code-wrap]) and is remembered across pages.
 * Safe to call again after an HTMX swap.
 */
export function initApiCodeWrap(root: ParentNode = document): void {
    if (readPreference()) {
        document.documentElement.setAttribute('data-api-code-wrap', '')
    }
    root.querySelectorAll<HTMLElement>(cardSelector).forEach((card) => {
        const actions = card.querySelector<HTMLElement>('[data-code-actions]')
        if (!actions || actions.querySelector('[data-code-wrap-toggle]')) return
        if (!card.querySelector('pre code')) return
        actions.prepend(createButton())
    })
    syncButtons()
    initTooltips()
}
