import {
    autoUpdate,
    computePosition,
    flip,
    offset,
    shift,
    size,
    type Placement,
} from '@floating-ui/dom'

/**
 * Places every popover next to the element that opens it.
 *
 * A menu is a `[popover]` opened by a `[popovertarget]` button. The popover draws it in the top layer, so no
 * ancestor's overflow, stacking context or z-index can clip it, and gives click-outside dismissal and Escape.
 * Floating UI places it beside its anchor, flipping and shifting to stay on screen, and follows the anchor while
 * the menu is open. `data-placement` picks the side (default `bottom-start`); `data-anchor` names the id of an
 * anchor other than the button. `data-hover` also opens the menu while a mouse rests on its button or on the menu
 * itself, and a click on the button keeps it open after the mouse leaves; touch and keyboard still use the click.
 * Choosing a link closes the menu it is in. The menu fires a bubbling
 * `floating:placed` event the first time it is placed; it is invisible, so unfocusable, until then. Listeners are
 * delegated on the document, so they survive htmx swaps.
 */

const PLACED = 'data-placed'
const FLOATING = 'data-floating'
const VIEWPORT_PADDING = 8

const stopTracking = new Map<HTMLElement, () => void>()

function invokers(popover: HTMLElement): HTMLElement[] {
    if (!popover.id) return []
    return Array.from(
        document.querySelectorAll<HTMLElement>(
            `[popovertarget="${CSS.escape(popover.id)}"]`
        )
    )
}

function anchorOf(popover: HTMLElement): HTMLElement | null {
    const named = popover.dataset.anchor
    if (named) return document.getElementById(named)
    return invokers(popover)[0] ?? null
}

/**
 * Puts `floating` beside `anchor`. Sets `--floating-anchor-width` and `--floating-available-height` on it, so its
 * own CSS decides how wide and tall it may grow.
 */
function place(
    anchor: Element,
    floating: HTMLElement,
    placement: Placement
): Promise<void> {
    return computePosition(anchor, floating, {
        strategy: 'fixed',
        placement,
        middleware: [
            offset(6),
            flip({ padding: VIEWPORT_PADDING }),
            shift({ padding: VIEWPORT_PADDING }),
            size({
                padding: VIEWPORT_PADDING,
                apply({ availableHeight, rects, elements }) {
                    elements.floating.style.setProperty(
                        '--floating-anchor-width',
                        `${rects.reference.width}px`
                    )
                    elements.floating.style.setProperty(
                        '--floating-available-height',
                        `${Math.floor(availableHeight)}px`
                    )
                },
            }),
        ],
    }).then(({ x, y }) => {
        const first = !floating.hasAttribute(PLACED)
        Object.assign(floating.style, { left: `${x}px`, top: `${y}px` })
        // Hidden until placed (floating.css), so it never flashes at the wrong spot.
        floating.setAttribute(PLACED, '')
        // A hidden element cannot take focus, so what wants to focus the menu waits for this.
        if (first)
            floating.dispatchEvent(
                new Event('floating:placed', { bubbles: true })
            )
    })
}

/** Follows the anchor through scrolling, resizing and the menu's own size changes until `release` is called. */
function track(
    anchor: Element,
    floating: HTMLElement,
    placement: Placement
): () => void {
    return autoUpdate(anchor, floating, () => {
        void place(anchor, floating, placement)
    })
}

function release(popover: HTMLElement) {
    stopTracking.get(popover)?.()
    stopTracking.delete(popover)
    popover.removeAttribute(PLACED)
}

function placementOf(popover: HTMLElement): Placement {
    return (
        (popover.dataset.placement as Placement | undefined) ?? 'bottom-start'
    )
}

/** Runs before the popover shows, so it is hidden until placed from its first frame. */
function onBeforeToggle(event: Event) {
    const popover = event.target
    if (!(popover instanceof HTMLElement) || !popover.hasAttribute('popover'))
        return
    if ((event as ToggleEvent).newState !== 'open' || !anchorOf(popover)) return
    popover.setAttribute(FLOATING, '')
}

function onToggle(event: Event) {
    const popover = event.target
    if (!(popover instanceof HTMLElement) || !popover.hasAttribute(FLOATING))
        return
    const open = (event as ToggleEvent).newState === 'open'
    for (const invoker of invokers(popover))
        invoker.setAttribute('aria-expanded', String(open))
    release(popover)
    if (!open) openedByHover.delete(popover)
    const anchor = anchorOf(popover)
    if (!open || !anchor) return
    stopTracking.set(popover, track(anchor, popover, placementOf(popover)))
}

/** A swap removes the page's popovers; close them first so their tracking is released. */
function closeAll() {
    for (const popover of [...stopTracking.keys()]) {
        if (popover.isConnected && popover.matches(':popover-open'))
            popover.hidePopover()
        release(popover)
    }
}

const HOVER_CLOSE_MS = 150
const hoverTimers = new WeakMap<HTMLElement, number>()
/** Menus open only because the mouse rests on them: leaving closes them, and a click on the button keeps them open. Both collections are weak, so a menu an htmx swap detaches is not kept alive by its hover state. */
const openedByHover = new WeakSet<HTMLElement>()

/** The `data-hover` menu that `target` is inside of, or opens. */
function hoverMenuAt(target: EventTarget | null): HTMLElement | null {
    if (!(target instanceof Element)) return null
    const inside = target.closest<HTMLElement>('[popover][data-hover]')
    if (inside) return inside
    const id = target.closest('[popovertarget]')?.getAttribute('popovertarget')
    const menu = id ? document.getElementById(id) : null
    return menu?.hasAttribute('data-hover') ? menu : null
}

function onPointerOver(event: PointerEvent) {
    if (event.pointerType !== 'mouse') return
    const menu = hoverMenuAt(event.target)
    if (!menu) return
    window.clearTimeout(hoverTimers.get(menu))
    // `pointerover` also fires between the children of the button, so a menu the reader just closed with a click must not reopen.
    if (hoverMenuAt(event.relatedTarget) === menu) return
    if (menu.matches(':popover-open')) return
    menu.showPopover()
    openedByHover.add(menu)
}

/** Leaving the button for the menu (or back) crosses a gap, so the close waits long enough for the pointer to arrive. */
function onPointerOut(event: PointerEvent) {
    if (event.pointerType !== 'mouse') return
    const menu = hoverMenuAt(event.target)
    if (!menu || !openedByHover.has(menu)) return
    if (hoverMenuAt(event.relatedTarget) === menu) return
    window.clearTimeout(hoverTimers.get(menu))
    hoverTimers.set(
        menu,
        window.setTimeout(() => {
            if (menu.isConnected && menu.matches(':popover-open'))
                menu.hidePopover()
        }, HOVER_CLOSE_MS)
    )
}

/** The click that follows hovering a `data-hover` button would toggle the open menu shut; it keeps the menu open instead. */
function keepHoverMenuOpen(event: MouseEvent) {
    if (!(event.target instanceof Element)) return
    const id = event.target
        .closest('[popovertarget]')
        ?.getAttribute('popovertarget')
    const menu = id ? document.getElementById(id) : null
    if (!menu || !openedByHover.delete(menu)) return
    window.clearTimeout(hoverTimers.get(menu))
    // Cancelling the click cancels the button's popover toggle.
    event.preventDefault()
}

/** A link that does not swap the page, such as a download or a new tab, would otherwise leave its menu open. */
function onClick(event: MouseEvent) {
    if (!(event.target instanceof Element)) return
    const menu = event.target
        .closest('a[href]')
        ?.closest<HTMLElement>(`[popover][${FLOATING}]`)
    if (menu?.matches(':popover-open')) menu.hidePopover()
}

let initialized = false

export function initFloating() {
    if (initialized) return
    // Without the popover API the menus stay hidden, so there is nothing to place.
    if (typeof HTMLElement.prototype.showPopover !== 'function') return
    initialized = true
    // `beforetoggle` and `toggle` do not bubble, so listen in the capture phase.
    document.addEventListener('beforetoggle', onBeforeToggle, true)
    document.addEventListener('toggle', onToggle, true)
    document.addEventListener('htmx:beforeSwap', closeAll)
    document.addEventListener('pointerover', onPointerOver)
    document.addEventListener('pointerout', onPointerOut)
    document.addEventListener('click', keepHoverMenuOpen, true)
    document.addEventListener('click', onClick)
}
