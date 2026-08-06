/**
 * Close behaviour for the top-bar dropdowns.
 *
 * Native <details> opens and closes on summary clicks, but it does not close when
 * the user clicks elsewhere or presses Escape, which for a nav menu leaves a panel
 * stranded over the page. These are delegated document listeners, so they survive
 * the htmx body swaps that replace the nav on every navigation.
 */

const DROPDOWN = 'details.secondary-nav-dropdown'

function openDropdowns(): HTMLDetailsElement[] {
    return Array.from(
        document.querySelectorAll<HTMLDetailsElement>(`${DROPDOWN}[open]`)
    )
}

function closeAllExcept(keep?: HTMLDetailsElement) {
    for (const dropdown of openDropdowns()) {
        if (dropdown !== keep) dropdown.open = false
    }
}

export function initSecondaryNav() {
    document.addEventListener('click', (event: MouseEvent) => {
        const target = event.target as HTMLElement | null
        // A click on a summary toggles its own dropdown; only the siblings close here,
        // otherwise we would fight the native toggle.
        const clicked = target?.closest<HTMLDetailsElement>(DROPDOWN) ?? undefined
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
}
