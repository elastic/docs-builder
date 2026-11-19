import {
    autoUpdate,
    computePosition,
    flip,
    offset,
    shift,
    size,
    type ComputePositionReturn,
    type Placement,
} from '@floating-ui/dom'

/**
 * Places every popover next to the element that opens it.
 *
 * A menu is a `[popover]` opened by a `[popovertarget]` button. The popover draws it in the top layer, so no
 * ancestor's overflow, stacking context or z-index can clip it, and gives click-outside dismissal and Escape.
 * Floating UI places it beside its anchor, flipping and shifting to stay on screen, and follows the anchor while
 * the menu is open. `data-placement` picks the side (default `bottom-start`); `data-anchor` names the id of an
 * anchor other than the button; `data-boundary` is a selector for an ancestor the menu must also stay inside
 * sideways, for a menu whose button can sit at either end of its container. The menu fires a bubbling `floating:placed` event the first time it is placed; it is
 * invisible, so unfocusable, until then. Listeners are delegated on the document, so they survive htmx swaps.
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

/** The ancestor `data-boundary` names, or none: the menu also slides sideways to stay inside it. */
function boundaryOf(popover: HTMLElement): Element | undefined {
    const selector = popover.dataset.boundary
    return (selector && popover.closest(selector)) || undefined
}

/**
 * Puts `floating` beside `anchor`. Sets `--floating-anchor-width` and `--floating-available-height` on it, so its
 * own CSS decides how wide and tall it may grow. Resolves with where it went, which flip may have changed.
 */
export function place(
    anchor: Element,
    floating: HTMLElement,
    placement: Placement,
    boundary?: Element
): Promise<ComputePositionReturn> {
    return computePosition(anchor, floating, {
        strategy: 'fixed',
        placement,
        middleware: [
            offset(6),
            flip({ padding: VIEWPORT_PADDING }),
            // Only the sideways slide knows the boundary: a menu opens below its button, outside a container only as
            // tall as that button, so flipping and sizing keep to the screen.
            shift({ padding: VIEWPORT_PADDING, boundary }),
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
    }).then((position) => {
        const { x, y } = position
        const first = !floating.hasAttribute(PLACED)
        Object.assign(floating.style, { left: `${x}px`, top: `${y}px` })
        // Hidden until placed (floating.css), so it never flashes at the wrong spot.
        floating.setAttribute(PLACED, '')
        // A hidden element cannot take focus, so what wants to focus the menu waits for this.
        if (first)
            floating.dispatchEvent(
                new Event('floating:placed', { bubbles: true })
            )
        return position
    })
}

/** Follows the anchor through scrolling, resizing and the menu's own size changes until `release` is called. */
function track(
    anchor: Element,
    floating: HTMLElement,
    placement: Placement,
    boundary?: Element
): () => void {
    return autoUpdate(anchor, floating, () => {
        void place(anchor, floating, placement, boundary)
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
    const anchor = anchorOf(popover)
    if (!open || !anchor) return
    stopTracking.set(
        popover,
        track(anchor, popover, placementOf(popover), boundaryOf(popover))
    )
}

/** A swap removes the page's popovers; close them first so their tracking is released. */
function closeAll() {
    for (const popover of [...stopTracking.keys()]) {
        if (popover.isConnected && popover.matches(':popover-open'))
            popover.hidePopover()
        release(popover)
    }
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
}
