/**
 * Close behaviour for the top-bar dropdowns, and active-tab sync after htmx swaps.
 *
 * Native <details> opens and closes on summary clicks, but it does not close when
 * the user clicks elsewhere or presses Escape, which for a nav menu leaves a panel
 * stranded over the page. These are delegated document listeners, so they survive
 * the htmx body swaps that replace the nav on every navigation.
 *
 * The top bar itself is hx-preserve'd (same tabs on every page). After a swap we
 * restyle --active from meta[name="docs:current-section"] so the highlight follows
 * the new page without remounting icons. That meta lives in <head> because boosted
 * navigations swap body innerHTML and would leave a body data-* attribute stale.
 */

const DROPDOWN = 'details.secondary-nav-dropdown, details.nav-select-dropdown'
const ACTIVE = 'secondary-nav-item--active'
const OPEN = 'is-open'
const CLOSING = 'is-closing'
/** Matches EuiPopover opacity duration (`animation.slow`). */
const CLOSE_MS = 350

const closingTimers = new WeakMap<HTMLDetailsElement, number>()
const openRafs = new WeakMap<HTMLDetailsElement, number>()

function cancelOpenRaf(dropdown: HTMLDetailsElement) {
    const raf = openRafs.get(dropdown)
    if (raf === undefined) return
    cancelAnimationFrame(raf)
    openRafs.delete(dropdown)
}

function clearClosing(dropdown: HTMLDetailsElement) {
    dropdown.classList.remove(CLOSING)
    const timer = closingTimers.get(dropdown)
    if (timer === undefined) return
    window.clearTimeout(timer)
    closingTimers.delete(dropdown)
}

function beginClosing(dropdown: HTMLDetailsElement) {
    clearClosing(dropdown)
    dropdown.classList.add(CLOSING)
    const timer = window.setTimeout(() => {
        dropdown.classList.remove(CLOSING)
        closingTimers.delete(dropdown)
    }, CLOSE_MS)
    closingTimers.set(dropdown, timer)
}

function prefersReducedMotion() {
    return (
        typeof window.matchMedia === 'function' &&
        window.matchMedia('(prefers-reduced-motion: reduce)').matches
    )
}

function setMenuOpen(dropdown: HTMLDetailsElement) {
    cancelOpenRaf(dropdown)
    clearClosing(dropdown)
    if (prefersReducedMotion()) {
        dropdown.classList.add(OPEN)
        return
    }
    // Paint once at opacity 0, then add is-open so the EUI transition can run.
    dropdown.classList.remove(OPEN)
    const raf = requestAnimationFrame(() => {
        dropdown.classList.add(OPEN)
        openRafs.delete(dropdown)
    })
    openRafs.set(dropdown, raf)
}

function setMenuClosed(dropdown: HTMLDetailsElement) {
    cancelOpenRaf(dropdown)
    dropdown.classList.remove(OPEN)
    beginClosing(dropdown)
}

function openDropdowns(): HTMLDetailsElement[] {
    return Array.from(
        document.querySelectorAll<HTMLDetailsElement>(`${DROPDOWN}[open]`)
    )
}

function closeAllExcept(keep?: HTMLDetailsElement) {
    for (const dropdown of openDropdowns()) {
        if (dropdown === keep) continue
        dropdown.open = false
        setMenuClosed(dropdown)
    }
}

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
    document.addEventListener(
        'toggle',
        (event: Event) => {
            const dropdown = event.target
            if (
                !(dropdown instanceof HTMLDetailsElement) ||
                !dropdown.matches(DROPDOWN)
            )
                return
            if (dropdown.open) setMenuOpen(dropdown)
            else setMenuClosed(dropdown)
        },
        true
    )

    document.addEventListener('click', (event: MouseEvent) => {
        const target = event.target as HTMLElement | null
        const tab = target?.closest('#secondary-nav .secondary-nav-item')
        if (tab && event.button === 0) {
            previewSecondaryNavActive(tab)
        }
        // A click on a summary toggles its own dropdown; only the siblings close here,
        // otherwise we would fight the native toggle.
        const clicked =
            target?.closest<HTMLDetailsElement>(DROPDOWN) ?? undefined
        closeAllExcept(clicked)
    })

    document.addEventListener('keydown', (event: KeyboardEvent) => {
        if (event.key !== 'Escape') return
        const open = openDropdowns()
        if (open.length === 0) return
        const active = (
            document.activeElement as HTMLElement | null
        )?.closest<HTMLDetailsElement>(DROPDOWN)
        closeAllExcept()
        // Focus would otherwise be lost on the removed panel, stranding keyboard users.
        active?.querySelector('summary')?.focus()
    })

    document.addEventListener('htmx:load', () => {
        syncSecondaryNavActive(currentSectionId())
    })

    syncSecondaryNavActive(currentSectionId())
}
