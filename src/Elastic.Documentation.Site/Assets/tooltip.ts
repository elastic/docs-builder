import { place } from './floating'
import { autoUpdate } from '@floating-ui/dom'

/**
 * Tooltips: any element with a non-empty `data-tooltip` shows that text in one shared tooltip on hover, on keyboard
 * focus and on a touch hold, where the native title waits about a second. Listeners are delegated on the document, so
 * they cover elements added later and survive htmx swaps. The tooltip is a manual popover: it sits in the top layer, so
 * it shows above an open modal dialog, and it never closes an open menu the way an auto popover would. floating.ts places
 * it above its target, below when there is no room, and keeps it on screen.
 *
 * `data-tooltip-delay="slow"` waits 500ms instead of 80ms, for lists the mouse crosses. `data-tooltip-theme="mono"` sets
 * the text in a monospace font, for JSON paths. A property name on an API page shows its JSON path the same way, read from
 * the permalink's label beside it, so the row does not print the path twice.
 */

// A property name carries no data-tooltip: its JSON path is in the permalink's label just before it (propertyPath).
const PROPERTY_NAME = '.property-list dt > a.property-anchor + code'
const TARGET = `[data-tooltip]:not([data-tooltip=""]), ${PROPERTY_NAME}`
// A click on a copy button swaps the text in place (flashTooltip); hiding the tooltip would make it flicker.
const KEEP_ON_CLICK = '.copybtn, .api-code-preview-btn, .property-anchor'
const DELAY_MS = 80
const SLOW_DELAY_MS = 500
const TOUCH_HOLD_MS = 300
const TOOLTIP_ID = 'site-tooltip'

type State =
    | { kind: 'idle' }
    | { kind: 'pending'; target: HTMLElement; timer: number }
    | {
          kind: 'shown'
          target: HTMLElement
          describedBy: string | null
          stopTracking: () => void
      }

let state: State = { kind: 'idle' }
let tooltip: HTMLElement | null = null
// A target whose tooltip Escape or a click put away. It stays away while the pointer is on the target, even when the
// target's content changes under the pointer (a copy button swaps its icon), and shows again once the pointer leaves.
let dismissed: HTMLElement | null = null
// The target the pointer is on, so Escape on a tooltip that keyboard focus opened does not block a later hover.
let hovered: HTMLElement | null = null

function tooltipElement(): HTMLElement {
    // A swap of the whole body takes the tooltip with it, so it is made again.
    if (tooltip?.isConnected) return tooltip
    tooltip = document.createElement('div')
    tooltip.id = TOOLTIP_ID
    tooltip.className = 'tooltip'
    tooltip.setAttribute('popover', 'manual')
    tooltip.setAttribute('role', 'tooltip')
    document.body.append(tooltip)
    return tooltip
}

const PERMALINK_LABEL_PREFIX = 'Link to '

/** A property name's JSON path. The row prints it once, in its permalink's label, to keep large API pages small. */
function propertyPath(name: HTMLElement): string {
    const label = name.previousElementSibling?.getAttribute('aria-label') ?? ''
    if (!label.startsWith(PERMALINK_LABEL_PREFIX)) return ''
    const path = label.slice(PERMALINK_LABEL_PREFIX.length)
    return path === name.textContent ? '' : path
}

function targetOf(node: EventTarget | null): HTMLElement | null {
    return node instanceof Element ? node.closest<HTMLElement>(TARGET) : null
}

function current(): HTMLElement | null {
    return state.kind === 'idle' ? null : state.target
}

function hide() {
    if (state.kind === 'pending') window.clearTimeout(state.timer)
    if (state.kind === 'shown') {
        state.stopTracking()
        if (state.describedBy === null)
            state.target.removeAttribute('aria-describedby')
        else state.target.setAttribute('aria-describedby', state.describedBy)
        tooltip?.hidePopover()
    }
    state = { kind: 'idle' }
}

/** Places the tooltip and points its arrow at the middle of the target, wherever shift moved the tooltip. */
async function position(target: HTMLElement, el: HTMLElement) {
    const { placement, x } = await place(target, el, 'top')
    const anchor = target.getBoundingClientRect()
    el.dataset.side = placement.split('-')[0]
    el.style.setProperty(
        '--tooltip-arrow-x',
        `${anchor.left + anchor.width / 2 - x}px`
    )
}

function show(target: HTMLElement) {
    hide()
    if (!target.isConnected) return
    const text = target.dataset.tooltip ?? propertyPath(target)
    // A top-level property's path is its name, which the row already shows.
    if (!text) return
    const el = tooltipElement()
    el.textContent = text
    const theme =
        target.dataset.tooltipTheme ??
        (target.matches(PROPERTY_NAME) ? 'mono' : undefined)
    if (theme) el.dataset.theme = theme
    else delete el.dataset.theme
    // Hidden until placed for this target, so it never flashes where the last one was.
    el.removeAttribute('data-placed')
    // Shown last, so it goes above whatever entered the top layer since, such as a modal dialog.
    el.showPopover()
    const describedBy = target.getAttribute('aria-describedby')
    target.setAttribute(
        'aria-describedby',
        describedBy ? `${describedBy} ${TOOLTIP_ID}` : TOOLTIP_ID
    )
    const stopTracking = autoUpdate(target, el, () => {
        // React can remove a hovered row, and a resize can drop a chip's text once it fits; the tooltip goes with either.
        if (target.isConnected && target.matches(TARGET))
            void position(target, el)
        else hide()
    })
    state = { kind: 'shown', target, describedBy, stopTracking }
}

function schedule(target: HTMLElement, ms: number) {
    hide()
    state = {
        kind: 'pending',
        target,
        timer: window.setTimeout(() => show(target), ms),
    }
}

function delayOf(target: HTMLElement): number {
    return target.dataset.tooltipDelay === 'slow' ? SLOW_DELAY_MS : DELAY_MS
}

/** Whether the event moves the pointer or focus from inside `el` to outside it. */
function leaves(el: HTMLElement, event: PointerEvent | FocusEvent): boolean {
    if (!(event.target instanceof Node) || !el.contains(event.target))
        return false
    return !(
        event.relatedTarget instanceof Node && el.contains(event.relatedTarget)
    )
}

function onPointerOver(event: PointerEvent) {
    const target = targetOf(event.target)
    hovered = target ?? hovered
    if (!target || target === current() || target === dismissed) return
    // A touch starts with pointerover and ends with pointerout, so holding for the delay is what shows it.
    schedule(
        target,
        event.pointerType === 'touch' ? TOUCH_HOLD_MS : delayOf(target)
    )
}

function onPointerOut(event: PointerEvent) {
    if (dismissed && leaves(dismissed, event)) dismissed = null
    if (hovered && leaves(hovered, event)) hovered = null
    const target = current()
    if (target && leaves(target, event)) hide()
}

function onPointerDown(event: PointerEvent) {
    if (event.pointerType === 'touch') return
    const target = targetOf(event.target)
    if (target && target === current() && target.matches(KEEP_ON_CLICK)) return
    dismissed = target
    hide()
}

function onFocusIn(event: FocusEvent) {
    const target = targetOf(event.target)
    // A click focuses too; only keyboard focus shows the tooltip, and at once, since the delay is for a passing mouse.
    // A tooltip the pointer only queued is promoted: show() clears its timer.
    // :focus-visible belongs to the element that took focus, which may be a link inside the target.
    if (!target || !(event.target as Element).matches(':focus-visible')) return
    if (state.kind === 'shown' && state.target === target) return
    show(target)
}

function onFocusOut(event: FocusEvent) {
    const target = current()
    if (target && leaves(target, event)) hide()
}

function onKeydown(event: KeyboardEvent) {
    if (event.key !== 'Escape') return
    const target = current()
    // With the pointer elsewhere nothing would clear `dismissed`, and the first hover would show no tooltip.
    // A pointer on a nested target (a chip's count inside the chip) is still inside the dismissed one.
    dismissed = target && hovered && target.contains(hovered) ? target : null
    hide()
}

function onSwap() {
    dismissed = null
    hovered = null
    hide()
}

let initialized = false

export function initTooltips(): void {
    if (initialized) return
    // Without the popover API the tooltip would never leave the page's flow; the targets keep their accessible names.
    if (typeof HTMLElement.prototype.showPopover !== 'function') return
    initialized = true
    document.addEventListener('pointerover', onPointerOver)
    document.addEventListener('pointerout', onPointerOut)
    document.addEventListener('pointerdown', onPointerDown)
    document.addEventListener('focusin', onFocusIn)
    document.addEventListener('focusout', onFocusOut)
    document.addEventListener('keydown', onKeydown)
    // Nothing keeps a hovered element of the old page once it is swapped out.
    document.addEventListener('htmx:beforeSwap', onSwap)
}

const flashes = new WeakMap<HTMLElement, { original: string; timer: number }>()

/** Swaps a tooltip's text for a moment (e.g. "Copied!"), in the tooltip too when it is showing for `el`. */
export function flashTooltip(el: HTMLElement, text: string, ms = 1500): void {
    const pending = flashes.get(el)
    if (pending) window.clearTimeout(pending.timer)
    const original = pending?.original ?? el.dataset.tooltip ?? ''
    const set = (value: string) => {
        el.dataset.tooltip = value
        if (state.kind === 'shown' && state.target === el && tooltip)
            tooltip.textContent = value
    }
    set(text)
    const timer = window.setTimeout(() => {
        set(original)
        flashes.delete(el)
    }, ms)
    flashes.set(el, { original, timer })
}
