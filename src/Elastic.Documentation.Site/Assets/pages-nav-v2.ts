import { $$optional } from 'select-dom'
import tippy from 'tippy.js'
import type { Instance } from 'tippy.js'

const navV2CollapsedStorageKey = 'docs-builder-nav-v2-collapsed-ids'

let navV2FolderLinkToggleBound = false
let navV2OptimisticNavigateBound = false
let navV2ScrollViewportBound = false

let navV2TruncationTippyInstances: Instance[] = []

/** Latest pages-nav aside / scrollport for viewport clamp + edge fades. */
let navV2ScrollViewportAside: HTMLElement | null = null
let navV2ScrollViewportScrollEl: HTMLElement | null = null

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

/**
 * Normalize a docs pathname for nav matching: resolve {@code .}/{@code ..},
 * drop trailing slash, strip a trailing {@code .md}. Uses the URL parser so
 * hrefs like {@code /docs/extend/kibana/./getting-started} match the live path.
 */
function normalizeDocPathname(pathname: string) {
    let p: string
    try {
        p = new URL(pathname, 'https://docs.local').pathname
    } catch {
        p = pathname
    }
    p = p.replace(/\/$/, '')
    if (p.endsWith('.md')) {
        p = p.slice(0, -3)
    }
    return p === '' ? '/' : p
}

function anchorMatchesPath(anchor: HTMLAnchorElement, pathnameRaw: string) {
    const href = anchor.getAttribute('href')
    if (!href) {
        return false
    }
    try {
        return (
            normalizeDocPathname(new URL(href, window.location.href).pathname) ===
            normalizeDocPathname(pathnameRaw)
        )
    } catch {
        return false
    }
}

/**
 * True when the section tab URL is also a normal sidebar link (e.g. Reference index).
 */
function sectionRootHasSidebarDestination(nav: HTMLElement): boolean {
    const sectionUrl = nav.dataset.sectionUrl
    if (!sectionUrl) {
        return false
    }

    return $$optional('a.sidebar-link[href]', nav).some(
        (el) =>
            el instanceof HTMLAnchorElement &&
            anchorMatchesPath(el, sectionUrl)
    )
}

/**
 * Returns true when the current page is the section root URL with no sidebar row for it.
 * Tab-only section roots stay unhighlighted; roots that appear in the tree (Reference, Extend)
 * keep normal current-page styling.
 */
function isOnSectionRootPage(nav: HTMLElement): boolean {
    const shell = nav.closest<HTMLElement>('.pages-nav-v2-shell')
    if (shell?.dataset.navIsolated === 'true') {
        return false
    }

    const sectionUrl = nav.dataset.sectionUrl
    if (!sectionUrl) {
        return false
    }

    const onRoot =
        normalizeDocPathname(window.location.pathname) ===
        normalizeDocPathname(sectionUrl)

    if (!onRoot) {
        return false
    }

    return !sectionRootHasSidebarDestination(nav)
}

function linkPathMatchesCurrentPage(anchor: HTMLAnchorElement) {
    return anchorMatchesPath(anchor, window.location.pathname)
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

            if (linkPathMatchesCurrentPage(a)) {
                setNavV2FolderOpen(cb, !cb.checked, { animate: true })
                e.preventDefault()
                e.stopPropagation()
                return
            }

            if (!cb.checked) {
                setNavV2FolderOpen(cb, true, { animate: true })
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

            if (
                normalizeDocPathname(path) ===
                normalizeDocPathname(window.location.pathname)
            ) {
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
                    if (sibling.checked) {
                        setNavV2FolderOpen(sibling, false, { animate: true })
                    }
                })
            }
        })
    })
}

function prefersReducedMotion(): boolean {
    return window.matchMedia('(prefers-reduced-motion: reduce)').matches
}

type NavV2FolderClipEls = {
    clip: HTMLElement
    inner: HTMLElement
}

function getNavV2FolderClipEls(
    cb: HTMLInputElement
): NavV2FolderClipEls | null {
    const peer = cb.closest('.nav-folder-peer')
    const li = peer?.parentElement
    if (!li?.matches('li.group-navigation')) {
        return null
    }

    const clip = li.querySelector<HTMLElement>(
        ':scope > .docs-sidebar-nav-v2__folder-clip'
    )
    const inner = clip?.querySelector<HTMLElement>(
        ':scope > .docs-sidebar-nav-v2__folder-clip-inner'
    )
    if (!clip || !inner) {
        return null
    }

    return { clip, inner }
}

const navV2FolderClipAnimToken = new WeakMap<HTMLElement, number>()
const navV2FolderClipOpenAnimation = new WeakMap<HTMLElement, Animation>()

function clearNavV2FolderClipInlineAnim(clip: HTMLElement) {
    navV2FolderClipOpenAnimation.get(clip)?.cancel()
    navV2FolderClipOpenAnimation.delete(clip)
    clip.style.height = ''
    clip.style.minHeight = ''
    clip.style.overflow = ''
    clip.style.transition = ''
    clip.style.gridTemplateRows = ''
    clip.style.display = ''
}

/** Children ul scrollHeight while the clip is at 0fr (inner is often 0). */
function measureNavV2FolderContentHeight(clip: HTMLElement): number {
    const ul = clip.querySelector<HTMLElement>(
        ':scope > .docs-sidebar-nav-v2__folder-clip-inner > .docs-sidebar-nav-v2__folder-children'
    )
    if (ul && ul.scrollHeight > 0) {
        return ul.scrollHeight
    }
    const inner = clip.querySelector<HTMLElement>(
        ':scope > .docs-sidebar-nav-v2__folder-clip-inner'
    )
    return inner?.scrollHeight ?? 0
}

/**
 * Open/close a folder checkbox.
 * - Close: CSS {@code 1fr→0fr} (already reliable).
 * - Open: Web Animations height 0→N (CSS {@code 0fr→1fr} / style transitions
 *   snap on the real click path because of style coalescing + grid).
 * Pending opens set {@code data-nav-v2-pending-open} so optimistic
 * {@link expandToCurrentPageForPath} does not force {@code checked} mid-tween.
 */
function setNavV2FolderOpen(
    cb: HTMLInputElement,
    open: boolean,
    options: { animate?: boolean } = {}
) {
    const animate = options.animate !== false
    // Ignore duplicate opens while a tween is already in flight.
    if (open && cb.dataset.navV2PendingOpen === 'true') {
        return
    }
    if (cb.checked === open) {
        return
    }

    const els = getNavV2FolderClipEls(cb)
    if (els) {
        clearNavV2FolderClipInlineAnim(els.clip)
    }

    const applyChecked = (next: boolean) => {
        delete cb.dataset.navV2PendingOpen
        cb.checked = next
        cb.dispatchEvent(new Event('change', { bubbles: true }))
        persistFolderCheckboxCollapsedState(cb)
    }

    if (!animate || !els || prefersReducedMotion()) {
        applyChecked(open)
        return
    }

    // Close: let CSS grid-template-rows animate 1fr → 0fr.
    if (!open) {
        applyChecked(false)
        return
    }

    const { clip } = els
    const targetPx = measureNavV2FolderContentHeight(clip)
    if (targetPx <= 0 || typeof clip.animate !== 'function') {
        applyChecked(true)
        return
    }

    const token = (navV2FolderClipAnimToken.get(clip) ?? 0) + 1
    navV2FolderClipAnimToken.set(clip, token)
    cb.dataset.navV2PendingOpen = 'true'

    clip.style.display = 'block'
    clip.style.overflow = 'hidden'
    clip.style.minHeight = '0'
    clip.style.height = '0px'

    cb.checked = true
    cb.dispatchEvent(new Event('change', { bubbles: true }))

    const animation = clip.animate(
        [{ height: '0px' }, { height: `${targetPx}px` }],
        {
            duration: 220,
            easing: 'cubic-bezier(0.25, 0.1, 0.25, 1)',
            fill: 'forwards',
        }
    )
    navV2FolderClipOpenAnimation.set(clip, animation)

    const finish = () => {
        if (navV2FolderClipAnimToken.get(clip) !== token) {
            return
        }
        navV2FolderClipOpenAnimation.delete(clip)
        animation.cancel()
        clip.style.height = ''
        clip.style.minHeight = ''
        clip.style.overflow = ''
        clip.style.display = ''
        delete cb.dataset.navV2PendingOpen
        persistFolderCheckboxCollapsedState(cb)
    }

    void animation.finished.then(finish).catch(finish)
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
 * Apply #f6f9fc on the deepest {@code li.group-navigation} that contains the current page
 * (not every expanded folder, not outer ancestors). Folder index → that group; leaf →
 * immediate parent group. Ancestor rows still get weight via {@code nav-v2-active-ancestor}.
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
     * Walk up: every ancestor group-navigation gets heading weight; background goes only
     * on the deepest one (closest to current), never on outer wrappers.
     */
    const ancestorGroups: HTMLElement[] = []
    let walk: Element | null = hostLi.parentElement
    while (walk && walk !== nav) {
        if (walk.matches('li.group-navigation')) {
            const ancestorRow = walk.querySelector<HTMLAnchorElement>(
                ':scope > .nav-folder-peer > a.sidebar-link'
            )
            if (ancestorRow && ancestorRow !== current && walk instanceof HTMLElement) {
                walk.classList.add('nav-v2-active-ancestor')
                ancestorGroups.push(walk)
            }
        }

        walk = walk.parentElement
    }

    if (
        ancestorGroups.length > 0 &&
        !hostLi.classList.contains('nav-v2-active-subtree')
    ) {
        ancestorGroups[0].classList.add('nav-v2-active-parent')
    }
}

/**
 * Mark all nav links whose href matches {@code pathname} with the "current" CSS class.
 */
function markCurrentPageForPath(nav: HTMLElement, pathnameRaw: string) {
    // $$ throws when empty; SSR has no .current yet, so use $$optional.
    $$optional('.current', nav).forEach((el) => el.classList.remove('current'))

    $$optional('a.sidebar-link[href]', nav).forEach((el) => {
        if (el instanceof HTMLAnchorElement && anchorMatchesPath(el, pathnameRaw)) {
            el.classList.add('current')
        }
    })
}

/**
 * Mark the current page's nav link with the "current" CSS class.
 * Skips marking when the current page is the section root URL.
 */
function markCurrentPage(nav: HTMLElement) {
    if (isOnSectionRootPage(nav)) {
        $$optional('.current', nav).forEach((el) =>
            el.classList.remove('current')
        )
        return
    }
    markCurrentPageForPath(nav, window.location.pathname)
}

function pickDeepestAnchorMatchingPath(
    nav: HTMLElement,
    pathnameRaw: string
): HTMLElement | null {
    const matches = $$optional('a.sidebar-link[href]', nav).filter(
        (el): el is HTMLAnchorElement =>
            el instanceof HTMLAnchorElement &&
            anchorMatchesPath(el, pathnameRaw)
    )
    if (matches.length === 0) {
        return null
    }

    let best: HTMLElement = matches[0]
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

                if (cb.dataset.navV2PendingOpen === 'true') {
                    // Click handler owns an animated open on this checkbox — do not snap it.
                } else if (collapsedIds.has(cb.id)) {
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
                if (cb.dataset.navV2PendingOpen !== 'true') {
                    cb.checked = true
                }
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
    if (isOnSectionRootPage(nav)) {
        // On the section root page, expand all top-level folders so the
        // section content is visible even though no specific page is current.
        nav.querySelectorAll<HTMLInputElement>(
            '#nav-tree > li > .peer > input[type="checkbox"]'
        ).forEach((cb) => {
            cb.checked = true
        })
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

function getNavV2ScrollOverflow(scrollEl: HTMLElement) {
    const { scrollTop, scrollHeight, clientHeight } = scrollEl
    const maxScroll = scrollHeight - clientHeight
    const eps = 1
    const canScroll = maxScroll > eps
    return {
        canScrollUp: canScroll && scrollTop > eps,
        canScrollDown: canScroll && scrollTop < maxScroll - eps,
    }
}

/**
 * Soft top/bottom edge fades on the pages-nav scrollport: only when content
 * overflows in that direction (so the first/last items stay sharp at rest).
 * Also toggles Figma scroll buttons (hover reveal is CSS; direction via data-visible).
 */
function updateNavV2ScrollFades(scrollEl: HTMLElement) {
    const { canScrollUp, canScrollDown } = getNavV2ScrollOverflow(scrollEl)
    scrollEl.dataset.navFadeTop = canScrollUp ? 'true' : 'false'
    scrollEl.dataset.navFadeBottom = canScrollDown ? 'true' : 'false'

    const { upBtn, downBtn } = findNavV2ScrollButtons(scrollEl)
    if (upBtn) {
        upBtn.dataset.visible = canScrollUp ? 'true' : 'false'
    }
    if (downBtn) {
        downBtn.dataset.visible = canScrollDown ? 'true' : 'false'
    }
}

/**
 * Buttons are siblings of the scrollport inside `.pages-nav-v2__menu` so they
 * stay pinned while the tree scrolls. Fallbacks cover older HTML shapes.
 */
function findNavV2ScrollButtons(scrollEl: HTMLElement) {
    const menu =
        scrollEl.closest<HTMLElement>('.pages-nav-v2__menu') ??
        scrollEl.parentElement
    const pagesNav = scrollEl.closest('#pages-nav')
    const upBtn =
        menu?.querySelector<HTMLButtonElement>(
            ':scope > .pages-nav-v2__scroll-btn--up'
        ) ??
        scrollEl.querySelector<HTMLButtonElement>(
            ':scope > .pages-nav-v2__scroll-btn--up'
        ) ??
        pagesNav?.querySelector<HTMLButtonElement>(
            ':scope > .pages-nav-v2__scroll-btn--up'
        ) ??
        null
    const downBtn =
        menu?.querySelector<HTMLButtonElement>(
            ':scope > .pages-nav-v2__scroll-btn--down'
        ) ??
        scrollEl.querySelector<HTMLButtonElement>(
            ':scope > .pages-nav-v2__scroll-btn--down'
        ) ??
        pagesNav?.querySelector<HTMLButtonElement>(
            ':scope > .pages-nav-v2__scroll-btn--down'
        ) ??
        null
    return { upBtn, downBtn }
}

function scrollNavV2ByPage(scrollEl: HTMLElement, direction: 'up' | 'down') {
    const delta = Math.max(120, Math.round(scrollEl.clientHeight * 0.75))
    scrollEl.scrollBy({
        top: direction === 'up' ? -delta : delta,
        behavior: 'smooth',
    })
}

function findSiteFooter(): HTMLElement | null {
    return (
        document.querySelector<HTMLElement>('footer.bg-ink-dark') ??
        document.querySelector<HTMLElement>('body > footer:last-of-type')
    )
}

function getOffsetTopPx() {
    const raw = getComputedStyle(document.documentElement)
        .getPropertyValue('--offset-top')
        .trim()
    const parsed = Number.parseFloat(raw)
    return Number.isFinite(parsed) ? parsed : 48
}

/**
 * Clamp the sticky host to the visible strip under chrome → viewport bottom or
 * footer. CSS padding (24px top / bottom, border-box) insets #pages-nav inside
 * that strip; scroll buttons overlay .pages-nav-v2__menu (pinned over the scrollport).
 *
 * `--offset-top` is only the sticky secondary nav (assembler) / isolated header.
 * While the Elastic global nav is still on screen (position:static, scrolls
 * away), the aside sits lower — use its live getBoundingClientRect().top so the
 * panel does not extend past the viewport bottom.
 */
function updatePagesNavAsideViewportHeight(aside: HTMLElement) {
    if (!window.matchMedia('(width >= 768px)').matches) {
        aside.style.removeProperty('--pages-nav-aside-height')
        return
    }

    const stickyTop = getOffsetTopPx()
    const layoutTop = aside.getBoundingClientRect().top
    const top = Number.isFinite(layoutTop)
        ? Math.max(stickyTop, Math.round(layoutTop))
        : stickyTop
    let bottom = window.innerHeight
    const footer = findSiteFooter()
    if (footer) {
        const footerTop = footer.getBoundingClientRect().top
        if (footerTop < bottom) {
            bottom = footerTop
        }
    }

    const height = Math.max(0, Math.round(bottom - top))
    aside.style.setProperty('--pages-nav-aside-height', `${height}px`)
}

function refreshNavV2ScrollViewport() {
    const aside = navV2ScrollViewportAside
    const scrollEl = navV2ScrollViewportScrollEl
    if (!aside || !scrollEl) {
        return
    }

    updatePagesNavAsideViewportHeight(aside)
    updateNavV2ScrollFades(scrollEl)
}

function initNavV2ScrollViewport(nav: HTMLElement) {
    const shell = nav.closest('.pages-nav-v2-shell')
    const scrollEl = shell?.querySelector<HTMLElement>('.pages-nav-v2__scroll')
    const aside =
        nav.closest<HTMLElement>('aside.sidebar') ??
        document.querySelector<HTMLElement>('aside.sidebar:has(#pages-nav)')
    if (!scrollEl || !aside) {
        return
    }

    navV2ScrollViewportAside = aside
    navV2ScrollViewportScrollEl = scrollEl

    if (!navV2ScrollViewportBound) {
        navV2ScrollViewportBound = true
        window.addEventListener('scroll', refreshNavV2ScrollViewport, {
            passive: true,
        })
        window.addEventListener('resize', refreshNavV2ScrollViewport, {
            passive: true,
        })
    }

    scrollEl.addEventListener(
        'scroll',
        () => updateNavV2ScrollFades(scrollEl),
        { passive: true }
    )
    // Folder open/close changes scrollHeight without resizing the scrollport.
    shell?.addEventListener('change', refreshNavV2ScrollViewport)

    const { upBtn, downBtn } = findNavV2ScrollButtons(scrollEl)
    if (upBtn && upBtn.dataset.navScrollBound !== 'true') {
        upBtn.dataset.navScrollBound = 'true'
        upBtn.addEventListener('click', () => scrollNavV2ByPage(scrollEl, 'up'))
    }
    if (downBtn && downBtn.dataset.navScrollBound !== 'true') {
        downBtn.dataset.navScrollBound = 'true'
        downBtn.addEventListener('click', () =>
            scrollNavV2ByPage(scrollEl, 'down')
        )
    }

    const content = scrollEl.querySelector('.pages-nav-v2__content')
    if (content) {
        const mo = new MutationObserver(refreshNavV2ScrollViewport)
        mo.observe(content, {
            childList: true,
            subtree: true,
            attributes: true,
            attributeFilter: ['class', 'style', 'open'],
        })
    }

    const ro = new ResizeObserver(refreshNavV2ScrollViewport)
    ro.observe(aside)
    ro.observe(scrollEl)
    // Assembler: elastic-nav.js injects the global header async and changes layout height.
    const elasticNav =
        document.querySelector<HTMLElement>('#elastic-nav') ??
        document.querySelector<HTMLElement>('#elastic-nav-wrapper')
    if (elasticNav) {
        ro.observe(elasticNav)
    }
    refreshNavV2ScrollViewport()
    requestAnimationFrame(refreshNavV2ScrollViewport)
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
    initNavV2FolderLayoutWarmup(nav)
    initNavV2ScrollViewport(nav)
    requestAnimationFrame(() => {
        requestAnimationFrame(() => initNavV2TruncationTooltips(nav))
    })
}

ensureNavV2FolderLinkToggle()
ensureNavV2OptimisticCurrentOnNavigate()
