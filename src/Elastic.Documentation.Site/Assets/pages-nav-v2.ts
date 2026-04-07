import { $$ } from 'select-dom'
import tippy from 'tippy.js'
import type { Instance } from 'tippy.js'

const navV2CollapsedStorageKey = 'docs-builder-nav-v2-collapsed-ids'

let navV2FolderLinkToggleBound = false
let navV2OptimisticNavigateBound = false

let navV2TruncationTippyInstances: Instance[] = []

function readCollapsedFolderIds(): Set<string> {
    try {
        const raw = sessionStorage.getItem(navV2CollapsedStorageKey)
        if (!raw) {
            return new Set()
        }

        const parsed = JSON.parse(raw) as unknown
        if (!Array.isArray(parsed)) {
            return new Set()
        }

        return new Set(parsed.filter((x): x is string => typeof x === 'string'))
    } catch {
        return new Set()
    }
}

function writeCollapsedFolderIds(ids: Set<string>) {
    sessionStorage.setItem(navV2CollapsedStorageKey, JSON.stringify([...ids]))
}

function persistFolderCheckboxCollapsedState(cb: HTMLInputElement) {
    if (!cb.id) {
        return
    }

    const ids = readCollapsedFolderIds()
    if (!cb.checked) {
        ids.add(cb.id)
    } else {
        ids.delete(cb.id)
    }

    writeCollapsedFolderIds(ids)
}

function normalizeDocPathname(pathname: string) {
    const p = pathname.replace(/\/$/, '')
    return p === '' ? '/' : p
}

/** Matches {@link markCurrentPage} / {@link expandToCurrentPage} href selectors (not root-normalized). */
function stripTrailingSlashForNavHref(pathname: string) {
    return pathname.replace(/\/$/, '')
}

function linkPathMatchesCurrentPage(anchor: HTMLAnchorElement) {
    const href = anchor.getAttribute('href')
    if (!href) {
        return false
    }

    const linkPath = normalizeDocPathname(
        new URL(href, window.location.href).pathname
    )
    const currentPath = normalizeDocPathname(window.location.pathname)
    return linkPath === currentPath
}

/**
 * Primary click on a folder row link: same-URL toggles expand/collapse only (preventDefault);
 * navigating to another URL keeps the folder open (checkbox checked) and follows href.
 * Skips modified clicks (new tab, etc.). Collapsed folder ids are stored for expandToCurrentPage.
 */
function ensureNavV2FolderLinkToggle() {
    if (navV2FolderLinkToggleBound) {
        return
    }

    navV2FolderLinkToggleBound = true

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
                '[data-nav-v2] li.group-navigation > .nav-folder-peer > a.sidebar-link'
            ) as HTMLAnchorElement | null

            if (!a) {
                return
            }

            const peer = a.parentElement
            const cb = peer?.querySelector<HTMLInputElement>(
                ':scope > input[type="checkbox"]'
            )

            if (!cb) {
                return
            }

            if (linkPathMatchesCurrentPage(a)) {
                cb.checked = !cb.checked
                cb.dispatchEvent(new Event('change', { bubbles: true }))
                persistFolderCheckboxCollapsedState(cb)
                e.preventDefault()
                e.stopPropagation()
                return
            }

            cb.checked = true
            cb.dispatchEvent(new Event('change', { bubbles: true }))
            persistFolderCheckboxCollapsedState(cb)
        },
        true
    )
}

/**
 * Apply current + subtree highlight from the clicked href before HTMX finishes (hx-boost),
 * so the sidebar does not wait for the network response to update.
 */
function ensureNavV2OptimisticCurrentOnNavigate() {
    if (navV2OptimisticNavigateBound) {
        return
    }

    navV2OptimisticNavigateBound = true

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
                'nav[data-nav-v2] a.sidebar-link'
            ) as HTMLAnchorElement | null

            if (!a || a.hasAttribute('hx-disable')) {
                return
            }

            const nav = a.closest('[data-nav-v2]') as HTMLElement | null
            if (!nav) {
                return
            }

            const li = a.closest('li.group-navigation')
            const folderRowInLi =
                li?.querySelector<HTMLAnchorElement>(
                    ':scope > .nav-folder-peer > a.sidebar-link'
                ) ?? null
            if (folderRowInLi === a && linkPathMatchesCurrentPage(a)) {
                return
            }

            const href = a.getAttribute('href')
            if (!href) {
                return
            }

            let path: string
            try {
                path = new URL(href, window.location.href).pathname
            } catch {
                return
            }

            const here = window.location.pathname.replace(/\/$/, '')
            const pathStripped = path.replace(/\/$/, '')
            if (pathStripped === here) {
                return
            }

            markCurrentPageForPath(nav, path)
            expandToCurrentPageForPath(nav, path)
            applyActiveSubtreeHighlight(nav)
        },
        true
    )
}

/**
 * Returns all sibling top-level accordion checkboxes for a given checkbox.
 * Siblings are other checkboxes inside [data-v2-accordion] elements at the
 * same nesting level as the given checkbox's ancestor accordion.
 */
function getSiblingAccordionCheckboxes(
    checkbox: HTMLInputElement
): HTMLInputElement[] {
    const accordion = checkbox.closest('[data-v2-accordion]')
    if (!accordion) {
        return []
    }

    const parent = accordion.parentElement
    if (!parent) {
        return []
    }

    return Array.from(
        parent.querySelectorAll<HTMLInputElement>(
            '[data-v2-accordion] > .peer input[type=checkbox]'
        )
    ).filter((c) => c !== checkbox)
}

/**
 * Accordion behaviour: when a top-level section is opened,
 * collapse all its siblings so only one section is expanded at a time.
 */
function initAccordion(nav: HTMLElement) {
    nav.querySelectorAll<HTMLInputElement>(
        '[data-v2-accordion] > .peer input[type=checkbox]'
    ).forEach((cb) => {
        if (cb.dataset.navV2AccordionBound === 'true') {
            return
        }

        cb.dataset.navV2AccordionBound = 'true'
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

function clearActiveSubtreeHighlight(nav: HTMLElement) {
    nav.querySelectorAll(
        '.nav-v2-active-subtree, .nav-v2-active-leaf, .nav-v2-active-ancestor'
    ).forEach((el) => {
        el.classList.remove(
            'nav-v2-active-subtree',
            'nav-v2-active-leaf',
            'nav-v2-active-ancestor'
        )
    })
}

/**
 * Counts {@code li} ancestors from {@code anchor} up to (but not including) {@code nav}.
 */
function navListItemDepthFromAnchor(anchor: Element, nav: HTMLElement): number {
    let depth = 0
    let el: Element | null = anchor
    while (el && el !== nav) {
        if (el.matches('li')) {
            depth++
        }
        el = el.parentElement
    }
    return depth
}

/**
 * Prefer the deepest {@code a.sidebar-link.current} when several share the URL (folder index
 * and child, or duplicate toc entries). Otherwise {@code querySelector} picks the first in DOM
 * order (usually a parent folder) and subtree/ancestor classes apply to the wrong rows.
 */
function deepestCurrentSidebarLink(nav: HTMLElement): HTMLAnchorElement | null {
    const anchors = nav.querySelectorAll<HTMLAnchorElement>(
        'a.sidebar-link.current'
    )
    if (anchors.length === 0) {
        return null
    }

    let best = anchors[0]
    let bestDepth = navListItemDepthFromAnchor(best, nav)
    for (let i = 1; i < anchors.length; i++) {
        const candidate = anchors[i]
        const d = navListItemDepthFromAnchor(candidate, nav)
        if (d > bestDepth) {
            bestDepth = d
            best = candidate
        }
    }
    return best
}

/**
 * Apply #F1F6FF background per design: folder index → whole folder + visible children;
 * nested folder index → that folder + its children only; leaf → that row only.
 */
function applyActiveSubtreeHighlight(nav: HTMLElement) {
    clearActiveSubtreeHighlight(nav)
    const current = deepestCurrentSidebarLink(nav)
    if (!current || !nav.contains(current)) {
        return
    }

    const hostLi = current.closest('li')
    if (!hostLi || !nav.contains(hostLi)) {
        return
    }

    const folderRowLink = hostLi.querySelector<HTMLAnchorElement>(
        ':scope > .nav-folder-peer > a.sidebar-link'
    )
    const childUl = hostLi.querySelector(':scope > ul')

    if (
        hostLi.classList.contains('group-navigation') &&
        folderRowLink &&
        folderRowLink === current &&
        childUl
    ) {
        hostLi.classList.add('nav-v2-active-subtree')
    } else {
        hostLi.classList.add('nav-v2-active-leaf')
    }

    /*
     * Walk DOM ancestors: folder rows (li.group-navigation) whose own link is not .current
     * get nav-v2-active-ancestor (CSS: semibold + #1D2A3E on clickable rows only).
     */
    let walk: Element | null = hostLi
    while (walk && walk !== nav) {
        if (walk.matches('li.group-navigation')) {
            const ancestorRow = walk.querySelector<HTMLAnchorElement>(
                ':scope > .nav-folder-peer > a.sidebar-link'
            )
            if (ancestorRow && ancestorRow !== current) {
                walk.classList.add('nav-v2-active-ancestor')
            }
        }

        walk = walk.parentElement
    }
}

/**
 * Mark all nav links whose href matches {@code pathname} with the "current" CSS class.
 */
function markCurrentPageForPath(nav: HTMLElement, pathnameRaw: string) {
    $$('.current', nav).forEach((el) => el.classList.remove('current'))

    const pathname = stripTrailingSlashForNavHref(pathnameRaw)
    $$(`a[href="${pathname}"], a[href="${pathname}/"]`, nav).forEach((el) =>
        el.classList.add('current')
    )
}

/**
 * Mark the current page's nav link with the "current" CSS class.
 */
function markCurrentPage(nav: HTMLElement) {
    markCurrentPageForPath(nav, window.location.pathname)
}

function pickDeepestAnchorMatchingPath(
    nav: HTMLElement,
    pathnameRaw: string
): HTMLElement | null {
    const pathname = stripTrailingSlashForNavHref(pathnameRaw)
    const matches = nav.querySelectorAll<HTMLElement>(
        `a[href="${pathname}"], a[href="${pathname}/"]`
    )
    if (matches.length === 0) {
        return null
    }

    let best = matches[0]
    let bestDepth = navListItemDepthFromAnchor(best, nav)
    for (let i = 1; i < matches.length; i++) {
        const m = matches[i]
        const d = navListItemDepthFromAnchor(m, nav)
        if (d > bestDepth) {
            bestDepth = d
            best = m
        }
    }
    return best
}

/**
 * Expand all ancestor collapsible sections that contain the link for {@code pathnameRaw}.
 * Uses the deepest matching anchor when several share the URL.
 */
function expandToCurrentPageForPath(nav: HTMLElement, pathnameRaw: string) {
    const link = pickDeepestAnchorMatchingPath(nav, pathnameRaw)
    if (!link) {
        return
    }

    const collapsedIds = readCollapsedFolderIds()

    let el: Element | null = link.parentElement
    while (el && el !== nav) {
        if (el.matches('li')) {
            const cb = el.querySelector<HTMLInputElement>(
                ':scope > .peer input[type=checkbox]'
            )
            if (cb && cb.id) {
                const rowLink = el.querySelector<HTMLElement>(
                    ':scope > .nav-folder-peer > a.sidebar-link'
                )
                const currentIsThisFolderRow =
                    rowLink !== null && rowLink === link

                if (collapsedIds.has(cb.id)) {
                    if (currentIsThisFolderRow) {
                        // User collapsed this folder while its index is current; HTML swap often
                        // re-checks the input — force closed so a second click can stay collapsed.
                        cb.checked = false
                    } else {
                        collapsedIds.delete(cb.id)
                        writeCollapsedFolderIds(collapsedIds)
                        cb.checked = true
                    }
                } else {
                    cb.checked = true
                }
            } else if (cb) {
                cb.checked = true
            }
        }

        el = el.parentElement
    }
}

/**
 * Expand all ancestor collapsible sections that contain the current page link,
 * so that navigating directly to a URL reveals its location in the sidebar.
 * Does not re-open a folder row that the user collapsed while that folder index
 * is the current page (see session storage + folder row link match).
 */
function expandToCurrentPage(nav: HTMLElement) {
    expandToCurrentPageForPath(nav, window.location.pathname)
}

function destroyNavV2TruncationTooltips() {
    for (const instance of navV2TruncationTippyInstances) {
        instance.destroy()
    }

    navV2TruncationTippyInstances = []
}

function measureNavTextEl(ref: HTMLElement, textEl: HTMLElement) {
    return ref === textEl
        ? textEl
        : (ref.querySelector<HTMLElement>('.docs-sidebar-nav-v2__nav-text') ??
              textEl)
}

/**
 * Tippy tooltips only when text is truncated (single-line ellipsis). onShow returns false to cancel.
 */
function initNavV2TruncationTooltips(nav: HTMLElement) {
    destroyNavV2TruncationTooltips()

    const els = nav.querySelectorAll<HTMLElement>(
        '.docs-sidebar-nav-v2__nav-text'
    )
    for (const el of els) {
        const full = el.textContent?.trim() ?? ''
        if (!full) {
            continue
        }

        const ref: HTMLElement =
            el.parentElement?.matches('a.sidebar-link') === true
                ? (el.parentElement as HTMLElement)
                : el

        const instance = tippy(ref, {
            content: full,
            placement: 'right-start',
            offset: [0, 6],
            animation: 'fade',
            duration: [200, 150],
            arrow: true,
            maxWidth: 360,
            appendTo: () => document.body,
            theme: 'nav-v2-truncate',
            trigger: 'mouseenter focusin',
            hideOnClick: true,
            interactive: false,
            touch: ['hold', 500],
            aria: { content: 'describedby' },
            onShow() {
                const textEl = measureNavTextEl(ref, el)
                const label = textEl.textContent?.trim() ?? ''
                if (!label) {
                    return false
                }

                instance.setContent(label)
                if (textEl.scrollWidth <= textEl.clientWidth + 1) {
                    return false
                }
            },
        })

        navV2TruncationTippyInstances.push(instance)
    }
}

/**
 * Initialize all V2 nav behaviours on the given sidebar element.
 * Call this on every htmx:load when [data-nav-v2] is present.
 */
export function initNavV2(nav: HTMLElement) {
    initAccordion(nav)
    markCurrentPage(nav)
    expandToCurrentPage(nav)
    applyActiveSubtreeHighlight(nav)
    requestAnimationFrame(() => {
        requestAnimationFrame(() => initNavV2TruncationTooltips(nav))
    })
}

ensureNavV2FolderLinkToggle()
ensureNavV2OptimisticCurrentOnNavigate()
