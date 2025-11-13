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

function scrollCurrentNaviItemIntoView(nav: HTMLElement) {
    const currentNavItem = $('.current', nav)

    if (currentNavItem) {
        expandAllParents(currentNavItem)
    }

    if (currentNavItem && !isElementInViewport(nav, currentNavItem)) {
        const navRect = nav.getBoundingClientRect()
        const currentNavItemRect = currentNavItem.getBoundingClientRect()
        // Calculate the offset needed to scroll the current navigation item into view.
        // The offset is determined by the difference between the top of the current navigation item and the top of the navigation container,
        // adjusted by one-third of the height of the navigation container and half the height of the current navigation item.
        const offset =
            currentNavItemRect.top -
            navRect.top -
            navRect.height / 3 +
            currentNavItemRect.height / 2

        // Scroll the navigation container by the calculated offset to bring the current navigation item into view.
        nav.scrollTop = nav.scrollTop + offset
    }
}

function isElementInViewport(parent: HTMLElement, child: HTMLElement): boolean {
    const childRect = child.getBoundingClientRect()
    const parentRect = parent.getBoundingClientRect()
    return (
        childRect.top >= parentRect.top &&
        childRect.left >= parentRect.left &&
        childRect.bottom <= parentRect.bottom &&
        childRect.right <= parentRect.right
    )
}

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
