import { throttle } from 'lodash'
import { $optional, $$optional } from 'select-dom'

const NAV_STATE_KEY = 'nav-expanded'
let controlsInitialized = false
let pagesNavTrigger: HTMLButtonElement | null = null

function isDevMode() {
    return !!document.querySelector('diagnostics-panel')
}

function saveNavState(nav: HTMLElement) {
    const expanded = $$optional<HTMLButtonElement>(
        'button[data-nav-toggle][aria-expanded="true"]',
        nav
    )
        .map((el) => el.getAttribute('aria-controls'))
        .filter(Boolean)
    sessionStorage.setItem(NAV_STATE_KEY, JSON.stringify(expanded))
}

function setExpanded(button: HTMLButtonElement, expanded: boolean) {
    const controlledId = button.getAttribute('aria-controls')
    if (!controlledId) return

    const controlled = document.getElementById(controlledId)
    if (!controlled) return

    button.setAttribute('aria-expanded', expanded.toString())
    controlled.hidden = !expanded
}

function restoreNavState(nav: HTMLElement) {
    const raw = sessionStorage.getItem(NAV_STATE_KEY)
    if (!raw) return
    try {
        const ids: string[] = JSON.parse(raw)
        for (const id of ids) {
            const button =
                $optional(
                    `button[data-nav-toggle][aria-controls="${CSS.escape(id)}"]`,
                    nav
                ) ??
                $optional(
                    `button[data-nav-toggle][aria-controls="${CSS.escape(`nav-subtree-${id}`)}"]`,
                    nav
                )
            if (button instanceof HTMLButtonElement) setExpanded(button, true)
        }
    } catch {
        /* ignore corrupt storage */
    }
}

function expandAllParents(navItem: HTMLElement) {
    let parent: HTMLLIElement | null | undefined = navItem?.closest('li')
    while (parent) {
        const button = parent.querySelector(
            ':scope > div > button[data-nav-toggle]'
        )
        if (button instanceof HTMLButtonElement) setExpanded(button, true)
        parent = parent.parentElement?.closest('li')
    }
}

function setPagesNavOpen(open: boolean, restoreFocus = false) {
    const panel = document.querySelector('[data-pages-nav-panel]')
    const backdrop = document.querySelector('[data-pages-nav-backdrop]')
    if (!(panel instanceof HTMLElement)) return

    panel.dataset.open = open.toString()
    if (backdrop instanceof HTMLButtonElement) backdrop.hidden = !open
    document.body.classList.toggle('overflow-hidden', open)
    $$optional<HTMLButtonElement>('[data-pages-nav-open]').forEach((button) =>
        button.setAttribute('aria-expanded', open.toString())
    )

    if (open) {
        const closeButton = panel.querySelector('[data-pages-nav-close]')
        if (closeButton instanceof HTMLButtonElement) closeButton.focus()
    } else if (restoreFocus) {
        pagesNavTrigger?.focus()
    }
}

function initializeControls() {
    if (controlsInitialized) return
    controlsInitialized = true

    document.addEventListener('click', (event) => {
        if (!(event.target instanceof Element)) return

        const openDropdown = document.querySelector(
            '[data-pages-dropdown-toggle][aria-expanded="true"]'
        )
        if (
            openDropdown instanceof HTMLButtonElement &&
            !event.target.closest('#pages-dropdown')
        ) {
            setExpanded(openDropdown, false)
        }

        const openButton = event.target.closest('[data-pages-nav-open]')
        if (openButton instanceof HTMLButtonElement) {
            pagesNavTrigger = openButton
            setPagesNavOpen(true)
            return
        }

        if (
            event.target.closest('[data-pages-nav-close]') ||
            event.target.closest('[data-pages-nav-backdrop]')
        ) {
            setPagesNavOpen(false, true)
            return
        }

        const navToggle = event.target.closest('[data-nav-toggle]')
        if (navToggle instanceof HTMLButtonElement) {
            setExpanded(
                navToggle,
                navToggle.getAttribute('aria-expanded') !== 'true'
            )
            const pagesNav = $optional('#pages-nav')
            if (isDevMode() && pagesNav) saveNavState(pagesNav)
            return
        }

        const dropdownToggle = event.target.closest(
            '[data-pages-dropdown-toggle]'
        )
        if (dropdownToggle instanceof HTMLButtonElement) {
            setExpanded(
                dropdownToggle,
                dropdownToggle.getAttribute('aria-expanded') !== 'true'
            )
        }
    })

    document.addEventListener('focusin', (event) => {
        if (
            event.target instanceof Element &&
            !event.target.closest('#pages-dropdown')
        ) {
            const openDropdown = document.querySelector(
                '[data-pages-dropdown-toggle][aria-expanded="true"]'
            )
            if (openDropdown instanceof HTMLButtonElement) {
                setExpanded(openDropdown, false)
            }
        }
    })

    document.addEventListener('keydown', (event) => {
        if (event.key !== 'Escape') return

        if (document.querySelector('dialog[data-image-dialog][open]')) return

        const openDropdown = document.querySelector(
            '[data-pages-dropdown-toggle][aria-expanded="true"]'
        )
        if (openDropdown instanceof HTMLButtonElement) {
            setExpanded(openDropdown, false)
            openDropdown.focus()
            return
        }

        const panel = document.querySelector('[data-pages-nav-panel]')
        if (panel instanceof HTMLElement && panel.dataset.open === 'true') {
            setPagesNavOpen(false, true)
        }
    })
}

function scrollCurrentNaviItemIntoViewImpl(nav: HTMLElement) {
    const currentNavItem = $optional('.current', nav)

    if (!currentNavItem) {
        return
    }

    expandAllParents(currentNavItem)

    const navRect = nav.getBoundingClientRect()
    const currentNavItemRect = currentNavItem.getBoundingClientRect()

    // Get the sticky element's height to account for content hidden under it
    // The sticky element contains the search and dropdown, staying fixed at top when scrolling
    const stickyElement = $optional('.sticky', nav)
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

export function initNav() {
    initializeControls()

    const pagesNav = $optional('#pages-nav')
    if (!pagesNav) {
        return
    }

    if (isDevMode()) {
        restoreNavState(pagesNav)
    }

    // Remove current class from all nav items before marking new ones
    const currentNavItems = $$optional('.current', pagesNav)
    currentNavItems.forEach((el) => {
        el.classList.remove('current')
    })

    // Normalize pathname by removing trailing slash to handle both URL variants
    const pathname = window.location.pathname.replace(/\/$/, '')

    // When the page is a hidden nav item (e.g. an individual detection rule), the server
    // emits docs:nav-active pointing to the nearest visible ancestor so we can highlight it.
    const navActiveMeta = document.querySelector<HTMLMetaElement>(
        'meta[name="docs:nav-active"]'
    )
    const activePathname = navActiveMeta?.content ?? pathname

    const navItems = $$optional(
        'a[href="' + activePathname + '"], a[href="' + activePathname + '/"]',
        pagesNav
    )
    navItems.forEach((el) => {
        el.classList.add('current')
    })
    scrollCurrentNaviItemIntoView(pagesNav)

    if (isDevMode()) {
        saveNavState(pagesNav)
    }
}
