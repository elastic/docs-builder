import { initPagesNavScroll } from './pages-nav-scroll'
import { throttle } from 'lodash'
import { $optional, $$optional } from 'select-dom'

const NAV_STATE_KEY = 'nav-expanded'

function expandedStorageKey(nav: ParentNode) {
    return `${NAV_STATE_KEY}:${navSurfaceKey(nav)}`
}

function saveNavState(nav: HTMLElement) {
    const ids = new Set<string>()
    const collect = (root: ParentNode) => {
        root.querySelectorAll('input[type="checkbox"]:checked').forEach(
            (el) => {
                if (el.id) {
                    ids.add(el.id)
                }
            }
        )
    }
    collect(nav)
    detachedPanels.get(nav)?.forEach((panel) => collect(panel))
    const expanded = [...ids]
    try {
        sessionStorage.setItem(
            expandedStorageKey(nav),
            JSON.stringify(expanded)
        )
    } catch {
        /* private mode */
    }
}

function findFolderInput(nav: HTMLElement, id: string) {
    const live = $optional(`#${CSS.escape(id)}`, nav)
    if (live instanceof HTMLInputElement) {
        return live
    }
    const store = detachedPanels.get(nav)
    if (!store) {
        return null
    }
    for (const panel of store.values()) {
        const input = panel.querySelector(`#${CSS.escape(id)}`)
        if (input instanceof HTMLInputElement) {
            return input
        }
    }
    return null
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
            const input = findFolderInput(nav, id)
            if (input) {
                input.checked = true
            }
        }
    } catch {
        /* ignore corrupt storage */
    }
}

function clearNavState(nav: ParentNode) {
    try {
        sessionStorage.removeItem(expandedStorageKey(nav))
    } catch {
        /* private mode */
    }
}

export function ensureSubtreeClips(nav: HTMLElement) {
    $$optional('li.nav-folder > ul.nav-subtree', nav).forEach((ul) => {
        if (!(ul instanceof HTMLElement)) {
            return
        }
        const clip = ul.ownerDocument.createElement('div')
        clip.className = 'nav-subtree-clip'
        ul.replaceWith(clip)
        clip.append(ul)
    })
}

const detachedPanels = new WeakMap<HTMLElement, Map<string, HTMLElement>>()

function panelStore(nav: HTMLElement) {
    let store = detachedPanels.get(nav)
    if (!store) {
        store = new Map()
        detachedPanels.set(nav, store)
    }
    return store
}

function folderFromInput(input: HTMLInputElement) {
    return input.closest('li.nav-folder')
}

function navFromInput(input: HTMLInputElement) {
    return (
        input.closest<HTMLElement>('#pages-nav') ??
        document.querySelector<HTMLElement>('#pages-nav')
    )
}

function connectedPanel(folder: Element) {
    return folder.querySelector<HTMLElement>(
        ':scope > .nav-subtree-clip, :scope > ul.nav-subtree'
    )
}

function insertFolderPanel(folder: Element, panel: HTMLElement) {
    const peer = folder.querySelector(':scope > .peer')
    if (peer) {
        peer.after(panel)
        return
    }
    folder.append(panel)
}

const FOLDER_ANIM_MS = 320
const FOLDER_ANIM_EASE = 'cubic-bezier(0.4, 0, 0.2, 1)'
const animatingPanels = new WeakSet<HTMLElement>()
const pendingPlayOpen = new Set<string>()
let userFolderGesture = false
let suppressFolderSnapUntil = 0

function shouldSuppressFolderSnap() {
    return Date.now() < suppressFolderSnapUntil
}

function beginUserFolderGesture(nav: HTMLElement) {
    userFolderGesture = true
    suppressFolderSnapUntil = Date.now() + FOLDER_ANIM_MS + 80
    ensureSubtreeClips(nav)
}

function queueFolderOpenAfterSwap(input: HTMLInputElement) {
    if (input.id) {
        pendingPlayOpen.add(input.id)
    }
}

function flushPendingFolderOpens(nav: HTMLElement) {
    const ids = [...pendingPlayOpen]
    pendingPlayOpen.clear()
    for (const id of ids) {
        const input = findFolderInput(nav, id)
        if (!(input instanceof HTMLInputElement)) {
            continue
        }
        input.checked = true
        const panel = attachFolderPanel(input)
        if (!panel || animatingPanels.has(panel)) {
            continue
        }
        if (
            panel.classList.contains('nav-subtree-clip--open') &&
            !panel.style.height
        ) {
            continue
        }
        playFolderOpen(panel)
    }
    if (ids.length > 0) {
        saveNavState(nav)
    }
}

function prefersReducedMotion() {
    return (
        typeof window.matchMedia === 'function' &&
        window.matchMedia('(prefers-reduced-motion: reduce)').matches
    )
}

function clearFolderAnim(panel: HTMLElement) {
    if (typeof panel.getAnimations === 'function') {
        panel.getAnimations().forEach((anim) => anim.cancel())
    }
    panel.style.removeProperty('height')
    panel.style.removeProperty('transition')
    animatingPanels.delete(panel)
}

function snapFolderOpen(panel: HTMLElement) {
    clearFolderAnim(panel)
    panel.classList.add('nav-subtree-clip--open')
}

function snapFolderClosed(panel: HTMLElement) {
    clearFolderAnim(panel)
    panel.classList.remove('nav-subtree-clip--open')
}

export function attachFolderPanel(input: HTMLInputElement) {
    const folder = folderFromInput(input)
    const nav = navFromInput(input)
    if (!folder) {
        return null
    }
    const live = connectedPanel(folder)
    if (live) {
        return live
    }
    const stored = (nav && panelStore(nav).get(input.id)) ?? null
    if (!stored) {
        return null
    }
    stored.classList.remove('nav-subtree-clip--open')
    stored.style.height = '0px'
    insertFolderPanel(folder, stored)
    return stored
}

export function detachFolderPanel(input: HTMLInputElement) {
    const folder = folderFromInput(input)
    const nav = navFromInput(input)
    const live = folder ? connectedPanel(folder) : null
    const panel =
        live ?? (nav && input.id ? panelStore(nav).get(input.id) : null)
    if (!panel) {
        return
    }
    if (nav && input.id) {
        panelStore(nav).set(input.id, panel)
    }
    snapFolderClosed(panel)
    panel.remove()
}

function folderPanelForSync(folder: Element, input: HTMLInputElement) {
    const nav = navFromInput(input)
    return (
        connectedPanel(folder) ??
        (nav && input.id ? panelStore(nav).get(input.id) : null) ??
        null
    )
}

export function syncFolderPanels(
    root: ParentNode,
    options?: { detachClosed?: boolean }
) {
    if (root instanceof HTMLElement) {
        ensureSubtreeClips(root)
    }
    const detachClosed = options?.detachClosed === true
    for (const folder of root.querySelectorAll('li.nav-folder')) {
        const input = folder.querySelector<HTMLInputElement>(
            ':scope > .peer input[type="checkbox"]'
        )
        if (!input) {
            continue
        }
        const existing = folderPanelForSync(folder, input)
        if (existing && animatingPanels.has(existing)) {
            continue
        }
        if (input.checked) {
            const panel = attachFolderPanel(input)
            if (panel) {
                snapFolderOpen(panel)
            }
        } else if (detachClosed) {
            detachFolderPanel(input)
        } else if (existing?.isConnected) {
            snapFolderClosed(existing)
        }
    }
}

const pendingClose = new WeakMap<HTMLInputElement, number>()
let folderCloseSeq = 0

function runHeightAnim(
    panel: HTMLElement,
    fromPx: number,
    toPx: number,
    done: () => void
) {
    let settled = false
    const finish = () => {
        if (settled) {
            return
        }
        settled = true
        panel.removeEventListener('transitionend', onEnd)
        done()
    }
    const onEnd = (event: TransitionEvent) => {
        if (event.target === panel && event.propertyName === 'height') {
            finish()
        }
    }
    animatingPanels.add(panel)
    panel.classList.remove('nav-subtree-clip--open')
    panel.style.transition = 'none'
    panel.style.height = `${fromPx}px`
    void panel.offsetHeight
    panel.style.transition = `height ${FOLDER_ANIM_MS}ms ${FOLDER_ANIM_EASE}`
    panel.style.height = `${toPx}px`
    panel.addEventListener('transitionend', onEnd)
    window.setTimeout(finish, FOLDER_ANIM_MS + 80)
}

function measurePanelHeight(panel: HTMLElement) {
    const wasOpen = panel.classList.contains('nav-subtree-clip--open')
    const prevHeight = panel.style.height
    const prevTransition = panel.style.transition
    panel.style.transition = 'none'
    panel.classList.add('nav-subtree-clip--open')
    panel.style.height = 'auto'
    let height = panel.getBoundingClientRect().height || panel.scrollHeight
    if (!height) {
        const inner = panel.querySelector<HTMLElement>(':scope > .nav-subtree')
        if (inner) {
            const style =
                inner.ownerDocument.defaultView?.getComputedStyle(inner)
            height =
                Math.max(inner.scrollHeight, inner.offsetHeight) +
                (style ? parseFloat(style.marginTop) || 0 : 0) +
                (style ? parseFloat(style.marginBottom) || 0 : 0)
        }
    }
    if (!wasOpen) {
        panel.classList.remove('nav-subtree-clip--open')
    }
    panel.style.height = prevHeight
    panel.style.transition = prevTransition
    void panel.offsetHeight
    return height
}

function playFolderOpen(panel: HTMLElement) {
    if (!panel.classList.contains('nav-subtree-clip')) {
        snapFolderOpen(panel)
        return
    }
    if (prefersReducedMotion()) {
        snapFolderOpen(panel)
        return
    }
    animatingPanels.add(panel)
    suppressFolderSnapUntil = Date.now() + FOLDER_ANIM_MS + 80
    panel.classList.remove('nav-subtree-clip--open')
    panel.style.transition = 'none'
    panel.style.height = '0px'
    void panel.offsetHeight
    const to = measurePanelHeight(panel)
    if (to === 0) {
        snapFolderOpen(panel)
        syncFolderPanels(panel, { detachClosed: true })
        return
    }
    runHeightAnim(panel, 0, to, () => {
        snapFolderOpen(panel)
        syncFolderPanels(panel, { detachClosed: true })
    })
}

function finishFolderClose(input: HTMLInputElement, token: number) {
    if (pendingClose.get(input) !== token || input.checked) {
        return
    }
    pendingClose.delete(input)
    detachFolderPanel(input)
}

function playFolderClose(input: HTMLInputElement, nav: HTMLElement) {
    const token = ++folderCloseSeq
    pendingClose.set(input, token)
    const folder = folderFromInput(input)
    const panel = folder ? connectedPanel(folder) : null
    if (!panel || nav.classList.contains('nav-no-folder-anim')) {
        finishFolderClose(input, token)
        return
    }
    if (prefersReducedMotion()) {
        finishFolderClose(input, token)
        return
    }
    const from = panel.offsetHeight || panel.scrollHeight
    if (from === 0) {
        finishFolderClose(input, token)
        return
    }
    runHeightAnim(panel, from, 0, () => finishFolderClose(input, token))
}

function onFolderCheckboxChange(input: HTMLInputElement) {
    const nav = navFromInput(input)
    if (!nav) {
        return
    }
    if (input.checked) {
        pendingClose.delete(input)
        const animate =
            userFolderGesture || !nav.classList.contains('nav-no-folder-anim')
        userFolderGesture = false
        const panel = attachFolderPanel(input)
        if (!panel) {
            return
        }
        if (animate) {
            playFolderOpen(panel)
        } else {
            snapFolderOpen(panel)
            syncFolderPanels(panel, { detachClosed: true })
        }
        return
    }
    playFolderClose(input, nav)
}

export function collapseAllFolders(nav: HTMLElement) {
    const uncheck = (el: Element) => {
        if (el instanceof HTMLInputElement) {
            el.checked = false
        }
    }
    $$optional('input[type="checkbox"]:checked', nav).forEach(uncheck)
    detachedPanels.get(nav)?.forEach((panel) => {
        panel
            .querySelectorAll('input[type="checkbox"]:checked')
            .forEach(uncheck)
    })
    clearNavState(nav)
    syncFolderPanels(nav, { detachClosed: true })
}

function expandAllParents(navItem: HTMLElement) {
    let parent: HTMLLIElement | null | undefined = navItem?.closest('li')
    while (parent) {
        const input = parent.querySelector<HTMLInputElement>(
            ':scope > .peer input[type="checkbox"], :scope > input[type="checkbox"]'
        )
        if (input) {
            input.checked = true
            const panel = attachFolderPanel(input)
            if (panel && !animatingPanels.has(panel)) {
                snapFolderOpen(panel)
            }
        }
        parent = parent.parentElement?.closest('li')
    }
}

function currentInNav(nav: HTMLElement) {
    const live = $optional('.current', nav)
    if (live instanceof HTMLElement) {
        return live
    }
    const store = detachedPanels.get(nav)
    if (!store) {
        return null
    }
    for (const panel of store.values()) {
        const found = panel.querySelector('.current')
        if (found instanceof HTMLElement) {
            return found
        }
    }
    return null
}

function getNavScrollContainer(nav: HTMLElement) {
    return nav.querySelector<HTMLElement>('.pages-nav-v2__scroll') ?? nav
}

function scrollCurrentNaviItemIntoViewImpl(nav: HTMLElement) {
    const currentNavItem = currentInNav(nav)

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

function ancestorFoldersForCurrent(nav: HTMLElement, current: Element) {
    const wanted = new Set<Element>()
    const hostLi = current.closest('li')
    let walk: Element | null = hostLi?.parentElement ?? null
    while (walk && walk !== nav) {
        if (walk.matches('li.nav-folder')) {
            const row = walk.querySelector<HTMLAnchorElement>(
                ':scope > .nav-folder-peer > a.sidebar-link'
            )
            if (row && row !== current) {
                wanted.add(walk)
            }
        }
        walk = walk.parentElement
    }
    return wanted
}

function applyAncestorHighlight(nav: HTMLElement) {
    const current = $optional('a.sidebar-link.current', nav)
    const wanted = current
        ? ancestorFoldersForCurrent(nav, current)
        : new Set<Element>()

    $$optional('.nav-v2-active-ancestor', nav).forEach((el) => {
        if (!wanted.has(el)) {
            el.classList.remove('nav-v2-active-ancestor')
        }
    })
    wanted.forEach((el) => {
        el.classList.add('nav-v2-active-ancestor')
    })
}

export function markCurrentPage(nav: HTMLElement) {
    const pathname = window.location.pathname.replace(/\/$/, '')
    const navActiveMeta = document.querySelector<HTMLMetaElement>(
        'meta[name="docs:nav-active"]'
    )
    const activePathname = navActiveMeta?.content ?? pathname

    const next = new Set<Element>()
    const consider = (el: Element) => {
        if (
            el instanceof HTMLAnchorElement &&
            anchorMatchesPath(el, activePathname)
        ) {
            next.add(el)
        }
    }
    $$optional('a.sidebar-link[href]', nav).forEach(consider)
    detachedPanels.get(nav)?.forEach((panel) => {
        if (panel.isConnected) {
            return
        }
        panel.querySelectorAll('a.sidebar-link[href]').forEach(consider)
    })

    $$optional('.current', nav).forEach((el) => {
        if (!next.has(el)) {
            el.classList.remove(
                'current',
                'nav-v2-current-ready',
                'nav-v2-hold-hover'
            )
        }
    })
    next.forEach((el) => {
        if (!el.classList.contains('current')) {
            el.classList.add('current')
            el.classList.remove('nav-v2-current-ready')
            currentColorPending = true
        }
    })
    applyAncestorHighlight(nav)
}

export const CURRENT_COLOR_DELAY_MS = 300
export const CURRENT_COLOR_DELAY_REDUCED_MS = 150

export function currentColorDelayMs() {
    if (typeof window.matchMedia !== 'function') {
        return CURRENT_COLOR_DELAY_MS
    }
    return window.matchMedia('(prefers-reduced-motion: reduce)').matches
        ? CURRENT_COLOR_DELAY_REDUCED_MS
        : CURRENT_COLOR_DELAY_MS
}

let settleCurrentTimer = 0

export function settleCurrentPage(
    nav: HTMLElement,
    options?: { delay?: boolean }
) {
    const apply = () => {
        settleCurrentTimer = 0
        nav.querySelectorAll('a.sidebar-link.current').forEach((el) => {
            el.classList.add('nav-v2-current-ready')
        })
    }
    if (options?.delay) {
        window.clearTimeout(settleCurrentTimer)
        settleCurrentTimer = window.setTimeout(apply, currentColorDelayMs())
        return
    }
    if (settleCurrentTimer) {
        return
    }
    apply()
}

function holdHover(el: Element) {
    if (!(el instanceof HTMLElement)) {
        return
    }
    el.classList.add('nav-v2-hold-hover')
    const release = () => {
        el.classList.remove('nav-v2-hold-hover')
    }
    el.addEventListener('pointerleave', release, { once: true })
}

function previewCurrentLink(anchor: HTMLAnchorElement) {
    const nav = anchor.closest<HTMLElement>('#pages-nav')
    if (!nav || anchor.classList.contains('current')) {
        return
    }
    currentColorPending = true
    nav.querySelectorAll('a.sidebar-link.current').forEach((el) => {
        el.classList.remove(
            'current',
            'nav-v2-current-ready',
            'nav-v2-hold-hover'
        )
    })
    anchor.classList.add('current')
    holdHover(anchor)
    applyAncestorHighlight(nav)
}

let folderRowClickBound = false
let sectionResetClickBound = false
let navStatePersistBound = false
let lastSwapHtml = ''
let canRecenterNav = true
let pinnedNavScrollTop: number | null = null
let pendingFolderReset = false
let currentColorPending = false

export function pinPagesNavScroll(root: ParentNode = document) {
    const nav = root.querySelector('#pages-nav')
    if (!(nav instanceof HTMLElement)) {
        return
    }
    pinnedNavScrollTop = getNavScrollContainer(nav).scrollTop
}

function restorePagesNavScroll(root: ParentNode = document) {
    if (pinnedNavScrollTop == null) {
        return
    }
    const nav = root.querySelector('#pages-nav')
    if (!(nav instanceof HTMLElement)) {
        return
    }
    getNavScrollContainer(nav).scrollTop = pinnedNavScrollTop
}

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
 * expanded folders do not flash closed. Leaving a section forgets its
 * accordion state so coming back (Guides → Reference → Guides) starts collapsed.
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
        canRecenterNav = false
        scrollCurrentNaviItemIntoView.cancel()
        return false
    }
    canRecenterNav = true
    pinnedNavScrollTop = null
    pendingFolderReset = true
    pendingPlayOpen.clear()
    if (current instanceof HTMLElement) {
        clearNavState(current)
    }
    const liveDocument = current.ownerDocument ?? document
    const next = liveDocument.importNode(incoming, true)
    current.replaceWith(next)
    if (next instanceof HTMLElement) {
        next.classList.add('nav-no-folder-anim')
        ensureSubtreeClips(next)
        clearNavState(next)
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

function htmlContainsPagesNav(html: string) {
    return html.includes('id="pages-nav"') || html.includes("id='pages-nav'")
}

function responseHtmlFromSwap(event: Event): string {
    const detail = (event as CustomEvent).detail as
        { serverResponse?: string; xhr?: { response?: string } } | undefined
    const xhr =
        typeof detail?.xhr?.response === 'string' ? detail.xhr.response : ''
    const server =
        typeof detail?.serverResponse === 'string' ? detail.serverResponse : ''
    // hx-preserve can strip #pages-nav from the settled serverResponse.
    if (xhr && htmlContainsPagesNav(xhr)) {
        return xhr
    }
    if (server && htmlContainsPagesNav(server)) {
        return server
    }
    return xhr || server || lastSwapHtml
}

function onBeforeSwap(event: Event) {
    lastSwapHtml = responseHtmlFromSwap(event)
    pinPagesNavScroll()
}

function onAfterSwap(event: Event) {
    const html = responseHtmlFromSwap(event) || lastSwapHtml
    lastSwapHtml = ''
    const replaced = html ? syncPagesNavFromResponse(html) : false
    if (!replaced) {
        canRecenterNav = false
        scrollCurrentNaviItemIntoView.cancel()
        restorePagesNavScroll()
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
                '#pages-nav a.sidebar-link'
            ) as HTMLAnchorElement | null
            if (!a) {
                return
            }

            const folderRow = a.closest('.nav-folder-peer')
            const cb = folderRow?.parentElement?.classList.contains(
                'nav-folder'
            )
                ? folderCheckboxForRow(a)
                : null

            if (anchorMatchesPath(a, window.location.pathname)) {
                if (cb) {
                    const nav = a.closest<HTMLElement>('#pages-nav')
                    if (nav) {
                        beginUserFolderGesture(nav)
                    }
                    cb.checked = !cb.checked
                    cb.dispatchEvent(new Event('change', { bubbles: true }))
                }
                e.preventDefault()
                e.stopPropagation()
                return
            }

            previewCurrentLink(a)
            if (cb && !cb.checked) {
                const nav = a.closest<HTMLElement>('#pages-nav')
                if (nav) {
                    beginUserFolderGesture(nav)
                }
                // hx-preserve moves #pages-nav on swap and cancels an in-flight
                // height transition. Open after initNav so the first expand plays.
                queueFolderOpenAfterSwap(cb)
            }
        },
        true
    )
}

const SECTION_RESET_LINK =
    '#secondary-nav a.secondary-nav-item__hit, #secondary-nav a.secondary-nav-dropdown-link, #pages-dropdown a.pages-dropdown_active'

function isSectionResetElement(el: Element | null) {
    return Boolean(el?.closest(SECTION_RESET_LINK))
}

function pathIsSectionHome(path: string) {
    const normalized = normalizeNavPathname(path)
    const links = document.querySelectorAll<HTMLAnchorElement>(
        '#secondary-nav a.secondary-nav-item__hit[href]'
    )
    for (const a of links) {
        const href = a.getAttribute('href')
        if (!href || href.startsWith('#') || /^https?:/i.test(href)) {
            continue
        }
        if (normalizeNavPathname(href) === normalized) {
            return true
        }
    }
    return false
}

function requestFolderReset(root: ParentNode = document) {
    pendingFolderReset = true
    const nav = root.querySelector('#pages-nav')
    if (nav instanceof HTMLElement) {
        clearNavState(nav)
    }
}

function ensureSectionResetClick() {
    if (sectionResetClickBound) {
        return
    }
    sectionResetClickBound = true
    document.addEventListener(
        'click',
        (e: MouseEvent) => {
            if (
                e.button !== 0 ||
                e.metaKey ||
                e.ctrlKey ||
                e.shiftKey ||
                e.altKey ||
                !(e.target instanceof Element) ||
                !isSectionResetElement(e.target)
            ) {
                return
            }
            requestFolderReset()
        },
        true
    )
    document.addEventListener(
        'htmx:beforeRequest',
        (event: Event) => {
            const detail = (event as CustomEvent).detail as
                | {
                      boosted?: boolean
                      elt?: EventTarget
                      requestConfig?: { path?: string }
                  }
                | undefined
            const elt = detail?.elt
            const path = detail?.requestConfig?.path
            if (elt instanceof Element && isSectionResetElement(elt)) {
                requestFolderReset()
                return
            }
            if (typeof path === 'string' && pathIsSectionHome(path)) {
                requestFolderReset()
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
        if (
            !(target instanceof HTMLInputElement) ||
            target.type !== 'checkbox'
        ) {
            return
        }
        const nav = target.closest<HTMLElement>('#pages-nav')
        if (nav) {
            onFolderCheckboxChange(target)
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
    ensureSectionResetClick()
    ensureNavStatePersist()
    ensureSubtreeClips(pagesNav)
    const resetFolders = pendingFolderReset
    pendingFolderReset = false
    // Same-tree HTMX re-runs initNav (sometimes twice). Do not kill an
    // in-progress first-open. Only snap on first paint or island reset.
    const holdFolderAnim = shouldSuppressFolderSnap()
    if ((resetFolders || canRecenterNav) && !holdFolderAnim) {
        pagesNav.classList.add('nav-no-folder-anim')
    }
    if (resetFolders && !holdFolderAnim) {
        collapseAllFolders(pagesNav)
    } else if (!resetFolders) {
        restoreNavState(pagesNav)
    }
    markCurrentPage(pagesNav)
    const currentNavItem = currentInNav(pagesNav)
    if (currentNavItem && !holdFolderAnim) {
        expandAllParents(currentNavItem)
    }
    if (!holdFolderAnim) {
        syncFolderPanels(pagesNav)
    }
    if (currentColorPending) {
        settleCurrentPage(pagesNav, { delay: true })
        currentColorPending = false
    } else {
        settleCurrentPage(pagesNav)
    }
    flushPendingFolderOpens(pagesNav)
    requestAnimationFrame(() => {
        requestAnimationFrame(() => {
            pagesNav.classList.remove('nav-no-folder-anim')
        })
    })
    if (canRecenterNav) {
        scrollCurrentNaviItemIntoView(pagesNav)
        canRecenterNav = false
    } else {
        scrollCurrentNaviItemIntoView.cancel()
        restorePagesNavScroll()
    }
    initPagesNavScroll(pagesNav)
}
