import { $$optional } from 'select-dom'
import tippy from 'tippy.js'
import type { Instance } from 'tippy.js'

const navV2CollapsedStorageKey = 'docs-builder-nav-v2-collapsed-ids'
const navV2SelectedLocationsStorageKey =
    'docs-builder-nav-v2-selected-locations'

let navV2FolderLinkToggleBound = false
let navV2OptimisticNavigateBound = false

let navV2TruncationTippyInstances: Instance[] = []
const navV2ActiveBranchScrollTimeouts = new WeakMap<HTMLElement, number[]>()
const navV2ActiveBranchScrollObservers = new WeakMap<
    HTMLElement,
    ResizeObserver
>()

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

function readSelectedNavLocations(): Record<string, string> {
    try {
        const raw = sessionStorage.getItem(navV2SelectedLocationsStorageKey)
        if (!raw) {
            return {}
        }

        const parsed = JSON.parse(raw) as unknown
        if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) {
            return {}
        }

        return Object.fromEntries(
            Object.entries(parsed).filter(
                (entry): entry is [string, string] =>
                    typeof entry[0] === 'string' && typeof entry[1] === 'string'
            )
        )
    } catch {
        return {}
    }
}

function rememberSelectedNavLocation(pathnameRaw: string, location?: string) {
    if (!location) {
        return
    }

    const locations = readSelectedNavLocations()
    locations[normalizeDocPathname(pathnameRaw)] = location
    sessionStorage.setItem(
        navV2SelectedLocationsStorageKey,
        JSON.stringify(locations)
    )
}

function selectedNavLocationForPath(pathnameRaw: string): string | null {
    return readSelectedNavLocations()[normalizeDocPathname(pathnameRaw)] ?? null
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

/**
 * Returns true when the current page is the section root URL.
 * Section root pages should not get current-page highlighting in the sidebar
 * because the section URL is a tab target, not a page within the nav tree.
 */
function isOnSectionRootPage(nav: HTMLElement): boolean {
    const sectionUrl = nav.dataset.sectionUrl
    if (!sectionUrl) {
        return false
    }

    return (
        normalizeDocPathname(window.location.pathname) ===
        normalizeDocPathname(sectionUrl)
    )
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
 * Primary click on a folder row link:
 * - When the current page matches the folder row href, toggle expand/collapse only (preventDefault).
 * - Otherwise, ensure the folder is expanded and allow navigation to the row href (e.g. section index).
 *   This keeps re-activation single-click after a manual collapse; collapsing comes on the next click
 *   once the folder row becomes current.
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

            if (
                a.classList.contains('current') &&
                linkPathMatchesCurrentPage(a)
            ) {
                cb.checked = !cb.checked
                cb.dispatchEvent(new Event('change', { bubbles: true }))
                persistFolderCheckboxCollapsedState(cb)
                e.preventDefault()
                e.stopPropagation()
                return
            }

            if (!cb.checked) {
                cb.checked = true
                cb.dispatchEvent(new Event('change', { bubbles: true }))
                persistFolderCheckboxCollapsedState(cb)
            }
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
            if (
                folderRowInLi === a &&
                a.classList.contains('current') &&
                linkPathMatchesCurrentPage(a)
            ) {
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

            const selectedLocation = a.dataset.navV2Location
            const previousSelectedLocation = selectedNavLocationForPath(path)
            rememberSelectedNavLocation(path, selectedLocation)

            const here = normalizeDocPathname(window.location.pathname)
            const pathStripped = normalizeDocPathname(path)
            if (
                pathStripped === here &&
                (!selectedLocation ||
                    selectedLocation === previousSelectedLocation)
            ) {
                return
            }

            markCurrentPageForPath(nav, path)
            expandToCurrentPageForPath(nav, path)
            applyActiveSubtreeHighlight(nav)
            scheduleActiveBranchScroll(nav)
        },
        true
    )
}

/**
 * Returns all sibling folder checkboxes at the same nesting level.
 */
function getSiblingAccordionCheckboxes(
    checkbox: HTMLInputElement
): HTMLInputElement[] {
    const group = checkbox.closest('li.group-navigation')
    if (!group) {
        return []
    }

    const parent = group.parentElement
    if (!parent) {
        return []
    }

    return Array.from(
        parent.querySelectorAll<HTMLInputElement>(
            ':scope > li.group-navigation > .nav-folder-peer input[type=checkbox]'
        )
    ).filter((c) => c !== checkbox)
}

function isDefaultExpandedCheckbox(checkbox: HTMLInputElement) {
    return checkbox.dataset.navV2ExpandedDefault === 'true'
}

function shouldKeepDefaultExpanded(
    checkbox: HTMLInputElement,
    collapsedIds: Set<string>
) {
    return (
        isDefaultExpandedCheckbox(checkbox) &&
        (!checkbox.id || !collapsedIds.has(checkbox.id))
    )
}

/**
 * Accordion behaviour: when a folder is opened, collapse its siblings so
 * only one branch stays expanded at that nesting level. Default-expanded
 * siblings stay open unless the user explicitly collapsed them this session.
 */
function initAccordion(nav: HTMLElement) {
    nav.querySelectorAll<HTMLInputElement>(
        'li.group-navigation > .nav-folder-peer input[type=checkbox]'
    ).forEach((cb) => {
        if (cb.dataset.navV2AccordionBound === 'true') {
            return
        }

        cb.dataset.navV2AccordionBound = 'true'
        cb.addEventListener('change', (e) => {
            const target = e.target as HTMLInputElement
            if (target.checked) {
                const collapsedIds = readCollapsedFolderIds()
                getSiblingAccordionCheckboxes(target).forEach((sibling) => {
                    if (shouldKeepDefaultExpanded(sibling, collapsedIds)) {
                        sibling.checked = true
                        return
                    }

                    sibling.checked = false
                })
            }
        })
    })
}

function getAllFolderCheckboxes(nav: HTMLElement): HTMLInputElement[] {
    return Array.from(
        nav.querySelectorAll<HTMLInputElement>(
            'li.group-navigation > .nav-folder-peer input[type=checkbox]'
        )
    )
}

function folderContainsMatchingPath(
    checkbox: HTMLInputElement,
    pathnameRaw: string
) {
    const group = checkbox.closest('li.group-navigation')
    if (!group) {
        return false
    }

    return getAnchorsMatchingPath(group, pathnameRaw).length > 0
}

function collapseInactiveFolders(
    nav: HTMLElement,
    activeCheckboxes: Set<HTMLInputElement>,
    pathnameRaw?: string
) {
    const collapsedIds = readCollapsedFolderIds()
    getAllFolderCheckboxes(nav).forEach((cb) => {
        if (activeCheckboxes.has(cb)) {
            cb.checked = true
            return
        }

        if (
            shouldKeepDefaultExpanded(cb, collapsedIds) &&
            (!pathnameRaw || !folderContainsMatchingPath(cb, pathnameRaw))
        ) {
            cb.checked = true
            return
        }

        cb.checked = false
    })
}

function findNavV2ScrollContainer(nav: HTMLElement): HTMLElement | null {
    return nav.closest<HTMLElement>('.pages-nav-menu')
}

function pickActiveBranchScrollTarget(
    nav: HTMLElement,
    current: HTMLAnchorElement
): HTMLElement | null {
    let target = current.closest<HTMLElement>('li')
    let walk = target
    while (walk && walk !== nav) {
        if (walk.matches('li.group-navigation')) {
            target = walk
        }

        walk = walk.parentElement?.closest<HTMLElement>('li') ?? null
    }

    return target
}

function scrollActiveBranchIntoView(nav: HTMLElement) {
    const container = findNavV2ScrollContainer(nav)
    if (!container) {
        return
    }

    const current = deepestCurrentSidebarLink(nav)
    if (!current) {
        return
    }

    const target = pickActiveBranchScrollTarget(nav, current)
    if (!target) {
        return
    }

    const containerRect = container.getBoundingClientRect()
    const targetRect = target.getBoundingClientRect()
    const topPadding = 8
    const bottomPadding = 16
    const visibleTop = containerRect.top + topPadding
    const visibleBottom = containerRect.bottom - bottomPadding

    if (targetRect.top >= visibleTop && targetRect.bottom <= visibleBottom) {
        return
    }

    const targetTop =
        targetRect.top - containerRect.top + container.scrollTop - topPadding
    container.scrollTop = Math.max(0, targetTop)
}

function scheduleActiveBranchScroll(nav: HTMLElement) {
    const existingTimeouts = navV2ActiveBranchScrollTimeouts.get(nav)
    if (existingTimeouts !== undefined) {
        existingTimeouts.forEach((timeoutId) => window.clearTimeout(timeoutId))
    }

    const run = () => {
        if (!nav.isConnected) {
            return
        }

        scrollActiveBranchIntoView(nav)
    }

    requestAnimationFrame(() => {
        requestAnimationFrame(() => {
            run()
        })
    })

    const timeoutIds = [260, 520].map((delay, index) =>
        window.setTimeout(() => {
            run()
            if (index === 1) {
                navV2ActiveBranchScrollTimeouts.delete(nav)
            }
        }, delay)
    )
    navV2ActiveBranchScrollTimeouts.set(nav, timeoutIds)
}

function initActiveBranchScrollObserver(nav: HTMLElement) {
    if (
        typeof ResizeObserver === 'undefined' ||
        navV2ActiveBranchScrollObservers.has(nav)
    ) {
        return
    }

    const observer = new ResizeObserver(() => {
        if (!nav.isConnected) {
            observer.disconnect()
            navV2ActiveBranchScrollObservers.delete(nav)
            return
        }

        scheduleActiveBranchScroll(nav)
    })
    navV2ActiveBranchScrollObservers.set(nav, observer)

    const container = findNavV2ScrollContainer(nav)
    const stickyChrome =
        container?.previousElementSibling instanceof HTMLElement
            ? container.previousElementSibling
            : null

    if (container) {
        observer.observe(container)
    }
    if (stickyChrome) {
        observer.observe(stickyChrome)
    }
}

function warmFolderSubtreeLayoutFromPeer(peer: HTMLElement) {
    const li = peer.parentElement
    if (!li?.matches('li.group-navigation')) {
        return
    }

    const input = li.querySelector<HTMLInputElement>(
        ':scope > .nav-folder-peer input[type=checkbox]'
    )
    if (input?.checked) {
        return
    }

    const ul = li.querySelector<HTMLElement>(
        ':scope > .docs-sidebar-nav-v2__folder-clip .docs-sidebar-nav-v2__folder-children'
    )
    if (ul) {
        void ul.scrollHeight
    }
}

function primeNavV2FolderLayoutsSync(nav: HTMLElement, maxCount: number) {
    const uls = nav.querySelectorAll<HTMLUListElement>(
        'ul.docs-sidebar-nav-v2__folder-children'
    )
    const n = Math.min(maxCount, uls.length)
    for (let i = 0; i < n; i++) {
        const ul = uls[i]
        const li = ul.closest('li.group-navigation')
        const input = li?.querySelector<HTMLInputElement>(
            ':scope > .nav-folder-peer input[type=checkbox]'
        )
        if (!input?.checked) {
            void ul.scrollHeight
        }
    }
}

/**
 * Spreads first-open layout cost off the interaction path: idle batches measure collapsed
 * folder lists; pointer events on the folder row warm right before click (see init).
 */
function scheduleNavV2CollapsedFolderLayoutWarmup(
    nav: HTMLElement,
    startIndex: number
) {
    const uls = nav.querySelectorAll<HTMLUListElement>(
        'ul.docs-sidebar-nav-v2__folder-children'
    )
    let index = startIndex
    const chunkSize = 6

    const schedule = (cb: () => void) => {
        if (typeof requestIdleCallback !== 'undefined') {
            requestIdleCallback(
                () => {
                    cb()
                },
                { timeout: 2000 }
            )
        } else {
            setTimeout(cb, 0)
        }
    }

    const step = () => {
        if (!nav.isConnected) {
            return
        }

        const end = Math.min(index + chunkSize, uls.length)
        for (; index < end; index++) {
            const ul = uls[index]
            const li = ul.closest('li.group-navigation')
            const input = li?.querySelector<HTMLInputElement>(
                ':scope > .nav-folder-peer input[type=checkbox]'
            )
            if (!input?.checked) {
                void ul.scrollHeight
            }
        }

        if (index < uls.length) {
            schedule(step)
        }
    }

    schedule(step)
}

function initNavV2FolderLayoutWarmup(nav: HTMLElement) {
    primeNavV2FolderLayoutsSync(nav, 14)

    nav.querySelectorAll<HTMLElement>(
        'li.group-navigation > .nav-folder-peer'
    ).forEach((peer) => {
        if (peer.dataset.navV2PointerWarmBound === 'true') {
            return
        }

        peer.dataset.navV2PointerWarmBound = 'true'
        const warm = () => {
            warmFolderSubtreeLayoutFromPeer(peer)
        }

        peer.addEventListener('pointerenter', warm, { passive: true })
        /*
         * Runs immediately before click (after hover path): pays layout once so the grid
         * transition is less likely to share a frame with the first full subtree measure.
         */
        peer.addEventListener('pointerdown', warm, { passive: true })
    })

    scheduleNavV2CollapsedFolderLayoutWarmup(nav, 14)
}

function clearActiveSubtreeHighlight(nav: HTMLElement) {
    nav.querySelectorAll(
        '.nav-v2-active-subtree, .nav-v2-active-leaf, .nav-v2-active-ancestor, .nav-v2-active-parent'
    ).forEach((el) => {
        el.classList.remove(
            'nav-v2-active-subtree',
            'nav-v2-active-leaf',
            'nav-v2-active-ancestor',
            'nav-v2-active-parent'
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

function deepestCurrentSidebarLink(nav: HTMLElement): HTMLAnchorElement | null {
    return nav.querySelector<HTMLAnchorElement>('a.sidebar-link.current')
}

/**
 * Apply #F1F6FF background per design: folder index → whole folder + visible children;
 * nested folder index → that folder + its children only; leaf → that row only.
 */
function applyActiveSubtreeHighlight(nav: HTMLElement) {
    clearActiveSubtreeHighlight(nav)
    if (isOnSectionRootPage(nav)) {
        return
    }
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
    const childUl = hostLi.querySelector(
        ':scope > .docs-sidebar-nav-v2__folder-clip .docs-sidebar-nav-v2__folder-children'
    )

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
     * Keep ancestor-state styling across the full chain, but mark only the nearest parent
     * with nav-v2-active-parent so background treatment can stay scoped to one level.
     */
    let walk: Element | null = hostLi
    let markedImmediateParent = false
    while (walk && walk !== nav) {
        if (walk.matches('li.group-navigation')) {
            const ancestorRow = walk.querySelector<HTMLAnchorElement>(
                ':scope > .nav-folder-peer > a.sidebar-link'
            )
            if (ancestorRow && ancestorRow !== current) {
                walk.classList.add('nav-v2-active-ancestor')
                if (!markedImmediateParent) {
                    walk.classList.add('nav-v2-active-parent')
                    markedImmediateParent = true
                }
            }
        }

        walk = walk.parentElement
    }
}

function markCurrentPageForPath(nav: HTMLElement, pathnameRaw: string) {
    $$optional('.current', nav).forEach((el) => el.classList.remove('current'))

    const current = pickAnchorMatchingPath(nav, pathnameRaw)
    current?.classList.add('current')
}

/**
 * Mark the current page's nav link with the "current" CSS class.
 * Skips marking when the current page is the section root URL.
 */
function markCurrentPage(nav: HTMLElement) {
    if (isOnSectionRootPage(nav)) {
        $$optional('.current', nav).forEach((el) => el.classList.remove('current'))
        return
    }
    markCurrentPageForPath(nav, window.location.pathname)
}

function getAnchorsMatchingPath(
    root: ParentNode,
    pathnameRaw: string
): HTMLAnchorElement[] {
    const pathname = stripTrailingSlashForNavHref(pathnameRaw)
    return Array.from(
        root.querySelectorAll<HTMLAnchorElement>(
            `a[href="${pathname}"], a[href="${pathname}/"]`
        )
    )
}

function pickCanonicalAnchor(
    nav: HTMLElement,
    anchors: HTMLAnchorElement[]
): HTMLAnchorElement | null {
    if (anchors.length === 0) {
        return null
    }

    let best = anchors[0]
    let bestDepth = navListItemDepthFromAnchor(best, nav)
    let bestLi = best.closest('li')
    for (let i = 1; i < anchors.length; i++) {
        const candidate = anchors[i]
        const candidateLi = candidate.closest('li')
        if (!bestLi || !candidateLi || !bestLi.contains(candidateLi)) {
            continue
        }

        const d = navListItemDepthFromAnchor(candidate, nav)
        if (d > bestDepth) {
            bestDepth = d
            best = candidate
            bestLi = candidateLi
        }
    }
    return best
}

function pickAnchorMatchingPath(
    nav: HTMLElement,
    pathnameRaw: string
): HTMLAnchorElement | null {
    const matches = getAnchorsMatchingPath(nav, pathnameRaw)
    if (matches.length === 0) {
        return null
    }

    const selectedLocation = selectedNavLocationForPath(pathnameRaw)
    if (selectedLocation) {
        const selected = matches.find(
            (m) => m.dataset.navV2Location === selectedLocation
        )
        if (selected) {
            return selected
        }
    }

    return pickCanonicalAnchor(nav, matches)
}

/**
 * Expand all ancestor collapsible sections that contain the link for {@code pathnameRaw}.
 * Uses the selected nav occurrence when available; direct loads fall back to the canonical occurrence.
 */
function expandToCurrentPageForPath(nav: HTMLElement, pathnameRaw: string) {
    const link = pickAnchorMatchingPath(nav, pathnameRaw)
    if (!link) {
        collapseInactiveFolders(nav, new Set())
        return
    }

    const collapsedIds = readCollapsedFolderIds()
    const activeCheckboxes = new Set<HTMLInputElement>()
    let wroteCollapsedIds = false

    let el: Element | null = link.parentElement
    while (el && el !== nav) {
        if (el.matches('li')) {
            const cb = el.querySelector<HTMLInputElement>(
                ':scope > .nav-folder-peer input[type=checkbox]'
            )
            if (cb && cb.id) {
                activeCheckboxes.add(cb)
                if (collapsedIds.has(cb.id)) {
                    collapsedIds.delete(cb.id)
                    wroteCollapsedIds = true
                }
                cb.checked = true
            } else if (cb) {
                activeCheckboxes.add(cb)
                cb.checked = true
            }
        }

        el = el.parentElement
    }

    if (wroteCollapsedIds) {
        writeCollapsedFolderIds(collapsedIds)
    }

    collapseInactiveFolders(nav, activeCheckboxes, pathnameRaw)
}

/**
 * Expand all ancestor collapsible sections that contain the current page link,
 * so that navigating directly to a URL reveals its location in the sidebar,
 * while collapsing folders that are outside the active branch.
 */
function expandToCurrentPage(nav: HTMLElement) {
    if (isOnSectionRootPage(nav)) {
        const activeCheckboxes = new Set<HTMLInputElement>()
        const topLevelFolders = nav.querySelectorAll<HTMLInputElement>(
            '#nav-tree > li.group-navigation > .nav-folder-peer > input[type="checkbox"]'
        )
        if (topLevelFolders.length === 1) {
            topLevelFolders[0].checked = true
            activeCheckboxes.add(topLevelFolders[0])
        }
        collapseInactiveFolders(nav, activeCheckboxes)
        return
    }
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
    initActiveBranchScrollObserver(nav)
    scheduleActiveBranchScroll(nav)
    initNavV2FolderLayoutWarmup(nav)
    requestAnimationFrame(() => {
        requestAnimationFrame(() => initNavV2TruncationTooltips(nav))
    })
}

ensureNavV2FolderLinkToggle()
ensureNavV2OptimisticCurrentOnNavigate()
