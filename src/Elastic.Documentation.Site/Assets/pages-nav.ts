import { throttle } from 'lodash'
import { $, $$ } from 'select-dom'

const NAV_STATE_KEY = 'nav-expanded'

function isDevMode() {
    return !!document.querySelector('diagnostics-panel')
}

function saveNavState(nav: HTMLElement) {
    const expanded = $$('input[type="checkbox"]:checked', nav)
        .map((el) => el.id)
        .filter(Boolean)
    sessionStorage.setItem(NAV_STATE_KEY, JSON.stringify(expanded))
}

function restoreNavState(nav: HTMLElement) {
    const raw = sessionStorage.getItem(NAV_STATE_KEY)
    if (!raw) return
    try {
        const ids: string[] = JSON.parse(raw)
        for (const id of ids) {
            const input = $(`#${CSS.escape(id)}`, nav)
            if (input instanceof HTMLInputElement) {
                input.checked = true
            }
        }
    } catch {
        /* ignore corrupt storage */
    }
}

function expandAllParents(navItem: HTMLElement) {
    let parent: HTMLLIElement | null | undefined = navItem?.closest('li')
    while (parent) {
        const input = parent.querySelector('input')
        if (input instanceof HTMLInputElement) {
            input.checked = true
        }
        parent = parent.parentElement?.closest('li')
    }
}

function scrollCurrentNaviItemIntoViewImpl(nav: HTMLElement) {
    const currentNavItem = $('.current', nav)

    if (!currentNavItem) {
        return
    }

    expandAllParents(currentNavItem)

    const navRect = nav.getBoundingClientRect()
    const currentNavItemRect = currentNavItem.getBoundingClientRect()

    // Get the sticky element's height to account for content hidden under it
    // The sticky element contains the search and dropdown, staying fixed at top when scrolling
    const stickyElement = $('.sticky', nav)
    const stickyHeight = stickyElement?.getBoundingClientRect().height ?? 0

    // The effective visible top of the nav is below the sticky element
    const effectiveNavTop = navRect.top + stickyHeight

    // Check if the item is already fully visible in the nav container's viewport
    // Account for sticky element that may be covering the top portion
    if (
        currentNavItemRect.top >= effectiveNavTop &&
        currentNavItemRect.bottom <= navRect.bottom
    ) {
        return
    }

    // Calculate target position: center of nav container (accounting for sticky area)
    const visibleNavHeight = navRect.height - stickyHeight
    const targetPosition =
        stickyHeight + visibleNavHeight / 2 - currentNavItemRect.height / 2

    // Calculate how much we need to scroll to position the item at the target
    const currentPositionInNav = currentNavItemRect.top - navRect.top
    const scrollOffset = currentPositionInNav - targetPosition

    // Apply the scroll, clamping to valid scroll range
    const newScrollTop = Math.max(0, nav.scrollTop + scrollOffset)

    nav.scrollTop = newScrollTop
}

// Throttle with leading: false, trailing: true - only executes the last call within the window
// This ensures that when multiple initNav() calls happen in quick succession (e.g., from multiple
// htmx:load events), only the final call executes after the delay, ensuring the nav tree is fully ready
export const scrollCurrentNaviItemIntoView = throttle(
    scrollCurrentNaviItemIntoViewImpl,
    100,
    { leading: false, trailing: true }
)

/**
 * Prevents focus-based dropdowns from closing before link navigation completes.
 * Without this, clicking a link inside the dropdown would transfer focus away,
 * causing the dropdown to close via CSS :focus-within before navigation happens.
 */
function preventFocusLossOnLinkClick(anchor: HTMLAnchorElement) {
    anchor.addEventListener('mousedown', (e) => {
        e.preventDefault()
    })
    // Close dropdown after click completes
    anchor.addEventListener('mouseup', () => {
        if (document.activeElement instanceof HTMLElement) {
            document.activeElement.blur()
        }
    })
}

export function initNav() {
    const pagesNav = $('#pages-nav')
    if (!pagesNav) {
        return
    }

    const dropdownActiveAnchor = $('#pages-dropdown a.pages-dropdown_active')
    if (dropdownActiveAnchor) {
        preventFocusLossOnLinkClick(dropdownActiveAnchor)
    }

    if (isDevMode()) {
        restoreNavState(pagesNav)
    }

    // Remove current class from all nav items before marking new ones
    const currentNavItems = $$('.current', pagesNav)
    currentNavItems.forEach((el) => {
        el.classList.remove('current')
    })

    // Normalize pathname by removing trailing slash to handle both URL variants
    const pathname = window.location.pathname.replace(/\/$/, '')
    const navItems = $$(
        'a[href="' + pathname + '"], a[href="' + pathname + '/"]',
        pagesNav
    )
    navItems.forEach((el) => {
        el.classList.add('current')
    })
    scrollCurrentNaviItemIntoView(pagesNav)

    if (isDevMode()) {
        saveNavState(pagesNav)
        for (const cb of $$('input[type="checkbox"]', pagesNav)) {
            cb.addEventListener('change', () => saveNavState(pagesNav))
        }
    }
}
