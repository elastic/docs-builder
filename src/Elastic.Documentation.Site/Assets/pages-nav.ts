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

    // Calculate target position: center of nav container
    const targetPosition = navRect.height / 2 - currentNavItemRect.height / 2

    // Calculate how much we need to scroll to position the item at the target
    const currentPositionInNav = currentNavItemRect.top - navRect.top
    const scrollOffset = currentPositionInNav - targetPosition

    // Apply the scroll, clamping to valid scroll range
    const newScrollTop = Math.max(0, nav.scrollTop + scrollOffset)

    nav.scrollTop = newScrollTop
}

// Throttle with leading: true, trailing: false - only executes the first call within the window
export const scrollCurrentNaviItemIntoView = throttle(
    scrollCurrentNaviItemIntoViewImpl,
    100,
    { leading: true, trailing: false }
)

function setDropdown(dropdown: HTMLElement) {
    if (dropdown) {
        const anchors = $$('a', dropdown)
        anchors.forEach((a) => {
            a.addEventListener('mousedown', (e) => {
                e.preventDefault()
            })
            a.addEventListener('mouseup', () => {
                if (document.activeElement instanceof HTMLElement) {
                    document.activeElement.blur()
                }
            })
        })
    }
}

export function initNav() {
    const pagesNav = $('#pages-nav')
    if (!pagesNav) {
        return
    }

    const pagesDropdown = $('#pages-dropdown')
    if (pagesDropdown) {
        setDropdown(pagesDropdown)
    }
    const pageVersionDropdown = $('#page-version-dropdown')
    if (pageVersionDropdown) {
        setDropdown(pageVersionDropdown)
    }
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
