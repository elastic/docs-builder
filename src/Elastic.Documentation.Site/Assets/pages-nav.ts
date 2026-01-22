import { throttle } from 'lodash'
import { $, $$ } from 'select-dom'

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

    // Check if the item is already fully visible in the nav container's viewport
    // If it's already visible, don't scroll to avoid unnecessary scrolling
    if (
        currentNavItemRect.top >= navRect.top &&
        currentNavItemRect.bottom <= navRect.bottom
    ) {
        return
    }

    // Calculate target position: center of nav container
    const targetPosition = navRect.height / 2 - currentNavItemRect.height / 2

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

    // Remove current class from all nav items before marking new ones
    const currentNavItems = $$('.current', pagesNav)
    currentNavItems.forEach((el) => {
        el.classList.remove('current')
    })

    const navItems = $$(
        'a[href="' +
            window.location.pathname +
            '"], a[href="' +
            window.location.pathname +
            '/"]',
        pagesNav
    )
    navItems.forEach((el) => {
        el.classList.add('current')
    })
    scrollCurrentNaviItemIntoView(pagesNav)
}
