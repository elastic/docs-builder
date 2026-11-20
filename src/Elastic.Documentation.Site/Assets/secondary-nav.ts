/**
 * Active-tab sync for the top bar after htmx swaps.
 *
 * The top bar itself is hx-preserve'd (same tabs on every page). After a swap we
 * restyle --active from meta[name="docs:current-section"] so the highlight follows
 * the new page without remounting icons. That meta lives in <head> because boosted
 * navigations swap body innerHTML and would leave a body data-* attribute stale.
 */

const ACTIVE = 'secondary-nav-item--active'

function currentSectionId(): string | null {
    const meta = document.querySelector<HTMLMetaElement>(
        'meta[name="docs:current-section"]'
    )
    const value = meta?.content
    return value ? value : null
}

function itemMatchesSection(item: Element, sectionId: string): boolean {
    const ids = item.getAttribute('data-section-ids')
    if (!ids) return false
    return ids.split(/\s+/).includes(sectionId)
}

export function syncSecondaryNavActive(sectionId: string | null | undefined) {
    const items = document.querySelectorAll(
        '#secondary-nav .secondary-nav-item'
    )
    for (const item of items) {
        const active = Boolean(sectionId && itemMatchesSection(item, sectionId))
        item.classList.toggle(ACTIVE, active)
    }
}

function previewSecondaryNavActive(item: Element) {
    const items = document.querySelectorAll(
        '#secondary-nav .secondary-nav-item'
    )
    for (const el of items) {
        el.classList.toggle(ACTIVE, el === item)
    }
}

export function initSecondaryNav() {
    document.addEventListener('click', (event: MouseEvent) => {
        const target = event.target as HTMLElement | null
        const tab = target?.closest('#secondary-nav .secondary-nav-item')
        if (tab && event.button === 0) {
            previewSecondaryNavActive(tab)
        }
    })

    document.addEventListener('htmx:load', () => {
        syncSecondaryNavActive(currentSectionId())
    })

    syncSecondaryNavActive(currentSectionId())
}
