import { initPagesNavScroll } from './pages-nav-scroll'
import { throttle } from 'lodash'
import { $optional, $$optional } from 'select-dom'

const NAV_STATE_KEY = 'nav-expanded'

function expandedStorageKey(nav: ParentNode) {
    return `${NAV_STATE_KEY}:${navSurfaceKey(nav)}`
}

function saveNavState(nav: HTMLElement) {
    const expanded = $$optional('input[type="checkbox"]:checked', nav)
        .map((el) => el.id)
        .filter(Boolean)
    try {
        sessionStorage.setItem(expandedStorageKey(nav), JSON.stringify(expanded))
    } catch {
        /* private mode */
    }
}

function restoreNavState(nav: HTMLElement) {
    let raw: string | null
    try {
        raw = sessionStorage.getItem(expandedStorageKey(nav))
    } catch {
        return
    }
    if (!raw) return
    try {
        const ids: string[] = JSON.parse(raw)
        for (const id of ids) {
            const input = $optional(`#${CSS.escape(id)}`, nav)
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

function getNavScrollContainer(nav: HTMLElement) {
    return nav.querySelector<HTMLElement>('.pages-nav-v2__scroll') ?? nav
}

function scrollCurrentNaviItemIntoViewImpl(nav: HTMLElement) {
    const currentNavItem = $optional('.current', nav)

    if (!currentNavItem) {
        return
    }

    expandAllParents(currentNavItem)

    const scrollContainer = getNavScrollContainer(nav)
    const navRect = scrollContainer.getBoundingClientRect()
    const currentNavItemRect = currentNavItem.getBoundingClientRect()

    // Sticky chrome (dropdown / back) sits above the scrollport in the Figma shell.
    const stickyElement = $optional('.pages-nav-v2__chrome, .sticky', nav)
    const stickyHeight =
        scrollContainer === nav
            ? (stickyElement?.getBoundingClientRect().height ?? 0)
            : 0

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

    const newScrollTop = Math.max(0, scrollContainer.scrollTop + scrollOffset)

    scrollContainer.scrollTop = newScrollTop
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

function normalizeNavPathname(pathname: string) {
    let p: string
    try {
        p = new URL(pathname, window.location.href).pathname
    } catch {
        p = pathname
    }
    p = p.replace(/\/$/, '')
    return p === '' ? '/' : p
}

function anchorMatchesPath(anchor: HTMLAnchorElement, pathnameRaw: string) {
    const href = anchor.getAttribute('href')
    if (!href) {
        return false
    }
    try {
        return (
            normalizeNavPathname(
                new URL(href, window.location.href).pathname
            ) === normalizeNavPathname(pathnameRaw)
        )
    } catch {
        return false
    }
}

function folderCheckboxForRow(anchor: HTMLAnchorElement) {
    return anchor.parentElement?.querySelector<HTMLInputElement>(
        ':scope > input[type="checkbox"]'
    )
}

function clearAncestorHighlight(nav: HTMLElement) {
    $$optional('.nav-v2-active-ancestor', nav).forEach((el) => {
        el.classList.remove('nav-v2-active-ancestor')
    })
}

function applyAncestorHighlight(nav: HTMLElement) {
    clearAncestorHighlight(nav)
    const current = $optional('a.sidebar-link.current', nav)
    if (!current) {
        return
    }

    const hostLi = current.closest('li')
    let walk: Element | null = hostLi?.parentElement ?? null
    while (walk && walk !== nav) {
        if (walk.matches('li.nav-folder')) {
            const row = walk.querySelector<HTMLAnchorElement>(
                ':scope > .nav-folder-peer > a.sidebar-link'
            )
            if (row && row !== current) {
                walk.classList.add('nav-v2-active-ancestor')
            }
        }
        walk = walk.parentElement
    }
}

function markCurrentPage(nav: HTMLElement) {
    $$optional('.current', nav).forEach((el) => {
        el.classList.remove('current')
    })

    const pathname = window.location.pathname.replace(/\/$/, '')
    const navActiveMeta = document.querySelector<HTMLMetaElement>(
        'meta[name="docs:nav-active"]'
    )
    const activePathname = navActiveMeta?.content ?? pathname

    $$optional('a.sidebar-link[href]', nav).forEach((el) => {
        if (
            el instanceof HTMLAnchorElement &&
            anchorMatchesPath(el, activePathname)
        ) {
            el.classList.add('current')
        }
    })
    applyAncestorHighlight(nav)
}

let folderRowClickBound = false
let navStatePersistBound = false
let lastSwapHtml = ''

export function navSurfaceKey(nav: ParentNode): string {
    const heading =
        nav
            .querySelector('.pages-nav-v2-shell')
            ?.getAttribute('data-nav-heading') ??
        nav.querySelector('.pages-nav-v2__heading-text')?.textContent?.trim() ??
        ''
    const treeId = (nav.querySelector('[id^="nav-tree-"]')?.id ?? '').replace(
        /-outgoing$/,
        ''
    )
    return `${treeId}::${heading}`
}

/**
 * After a boosted swap, replace `#pages-nav` only when the island/section
 * surface changed. Same-tree navigations keep the live nav (hx-preserve) so
 * expanded folders do not flash closed.
 */
export function syncPagesNavFromResponse(
    responseHtml: string,
    root: ParentNode = document
): boolean {
    const current = root.querySelector('#pages-nav')
    if (!current || !responseHtml) {
        return false
    }
    const incoming = new DOMParser()
        .parseFromString(responseHtml, 'text/html')
        .querySelector('#pages-nav')
    if (!incoming) {
        return false
    }
    if (navSurfaceKey(current) === navSurfaceKey(incoming)) {
        return false
    }
    if (current instanceof HTMLElement) {
        saveNavState(current)
    }
    const liveDocument = current.ownerDocument ?? document
    const next = liveDocument.importNode(incoming, true)
    current.replaceWith(next)
    if (next instanceof HTMLElement) {
        restoreNavState(next)
    }
    return true
}

function clearHtmxHistoryCache() {
    try {
        sessionStorage.removeItem('htmx-history-cache')
    } catch {
        /* private mode */
    }
}

function responseHtmlFromSwap(event: Event): string {
    const detail = (event as CustomEvent).detail as
        { serverResponse?: string; xhr?: { response?: string } } | undefined
    if (typeof detail?.serverResponse === 'string' && detail.serverResponse) {
        return detail.serverResponse
    }
    if (typeof detail?.xhr?.response === 'string' && detail.xhr.response) {
        return detail.xhr.response
    }
    return lastSwapHtml
}

function onBeforeSwap(event: Event) {
    lastSwapHtml = responseHtmlFromSwap(event)
}

function onAfterSwap(event: Event) {
    const html = responseHtmlFromSwap(event) || lastSwapHtml
    lastSwapHtml = ''
    if (html) {
        syncPagesNavFromResponse(html)
    }
}

if (typeof document !== 'undefined') {
    clearHtmxHistoryCache()
    document.addEventListener('htmx:beforeSwap', onBeforeSwap, true)
    document.addEventListener('htmx:afterSwap', onAfterSwap, true)
}

/**
 * Folder row = label + chevron as one hit target (chevron lives inside the <a>).
 * First click on a collapsed folder expands it and navigates to its overview.
 * Clicking the same row while it is current toggles the group closed/open.
 */
function ensureFolderRowClick() {
    if (folderRowClickBound) {
        return
    }
    folderRowClickBound = true

    document.addEventListener(
        'click',
        (e: MouseEvent) => {
            if (!(e.target instanceof Element)) {
                return
            }
            if (
                e.defaultPrevented ||
                e.button !== 0 ||
                e.metaKey ||
                e.ctrlKey ||
                e.shiftKey ||
                e.altKey
            ) {
                return
            }

            const a = e.target.closest(
                '#pages-nav li.nav-folder > .nav-folder-peer > a.sidebar-link'
            ) as HTMLAnchorElement | null
            if (!a) {
                return
            }

            const cb = folderCheckboxForRow(a)
            if (!cb) {
                return
            }

            if (anchorMatchesPath(a, window.location.pathname)) {
                cb.checked = !cb.checked
                cb.dispatchEvent(new Event('change', { bubbles: true }))
                e.preventDefault()
                e.stopPropagation()
                return
            }

            if (!cb.checked) {
                cb.checked = true
                cb.dispatchEvent(new Event('change', { bubbles: true }))
            }
        },
        true
    )
}

function ensureNavStatePersist() {
    if (navStatePersistBound) {
        return
    }
    navStatePersistBound = true
    document.addEventListener('change', (e: Event) => {
        const target = e.target
        if (!(target instanceof HTMLInputElement) || target.type !== 'checkbox') {
            return
        }
        const nav = target.closest<HTMLElement>('#pages-nav')
        if (nav) {
            saveNavState(nav)
        }
    })
}

export function initNav() {
    const pagesNav = $optional('#pages-nav')
    if (!pagesNav) {
        return
    }

    const dropdownActiveAnchor = $optional(
        '#pages-dropdown a.pages-dropdown_active'
    )
    if (dropdownActiveAnchor) {
        preventFocusLossOnLinkClick(dropdownActiveAnchor)
    }

    ensureFolderRowClick()
    ensureNavStatePersist()
    restoreNavState(pagesNav)
    markCurrentPage(pagesNav)
    scrollCurrentNaviItemIntoView(pagesNav)
    initPagesNavScroll(pagesNav)
}
