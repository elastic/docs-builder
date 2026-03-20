import { $$ } from 'select-dom'

/**
 * Returns all sibling top-level accordion checkboxes for a given checkbox.
 * Siblings are other checkboxes inside [data-v2-accordion] elements at the
 * same nesting level as the given checkbox's ancestor accordion.
 */
function getSiblingAccordionCheckboxes(
    checkbox: HTMLInputElement
): HTMLInputElement[] {
    const accordion = checkbox.closest('[data-v2-accordion]')
    if (!accordion) return []

    const parent = accordion.parentElement
    if (!parent) return []

    return Array.from(
        parent.querySelectorAll<HTMLInputElement>(
            '[data-v2-accordion] > .peer input[type=checkbox]'
        )
    ).filter((cb) => cb !== checkbox)
}

/**
 * Accordion behaviour: when a top-level section is opened,
 * collapse all its siblings so only one section is expanded at a time.
 */
function initAccordion(nav: HTMLElement) {
    nav.querySelectorAll<HTMLInputElement>(
        '[data-v2-accordion] > .peer input[type=checkbox]'
    ).forEach((cb) => {
        cb.addEventListener('change', (e) => {
            const target = e.target as HTMLInputElement
            if (target.checked) {
                getSiblingAccordionCheckboxes(target).forEach((sibling) => {
                    sibling.checked = false
                })
            }
        })
    })
}

/**
 * Mark the current page's nav link with the "current" CSS class.
 * Unlike the V1 nav, we do NOT auto-expand parent sections —
 * progressive disclosure keeps all sections collapsed until the user opens them.
 */
function markCurrentPage(nav: HTMLElement) {
    // Remove stale current markers
    $$('.current', nav).forEach((el) => el.classList.remove('current'))

    const pathname = window.location.pathname.replace(/\/$/, '')
    $$(`a[href="${pathname}"], a[href="${pathname}/"]`, nav).forEach((el) =>
        el.classList.add('current')
    )
}

/**
 * Initialize all V2 nav behaviours on the given sidebar element.
 * Call this on every htmx:load when [data-nav-v2] is present.
 */
export function initNavV2(nav: HTMLElement) {
    initAccordion(nav)
    markCurrentPage(nav)
}
