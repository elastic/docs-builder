import type { Placement } from '@floating-ui/dom'

/**
 * Menus on API pages (product, version, spec download).
 *
 * The `popover` attribute draws the menu in the browser's top layer, so no ancestor's
 * overflow, stacking context or z-index can clip it. That attribute also gives click-outside
 * dismissal and Escape. Floating UI places it next to its button, flipping and shifting to
 * stay on screen; it loads on the first open, so pages without a menu never download it.
 * Listeners are delegated on the document, so they survive htmx swaps.
 */

const MENU = '[popover].api-menu'
const PLACED = 'data-placed'
const VIEWPORT_PADDING = 8

type FloatingUi = typeof import('@floating-ui/dom')

const openMenus = new Set<HTMLElement>()
const stopTracking = new WeakMap<HTMLElement, () => void>()

function triggerFor(menu: HTMLElement): HTMLElement | null {
    if (!menu.id) return null
    return document.querySelector<HTMLElement>(
        `[popovertarget="${CSS.escape(menu.id)}"]`
    )
}

function place(fui: FloatingUi, trigger: HTMLElement, menu: HTMLElement) {
    void fui
        .computePosition(trigger, menu, {
            strategy: 'fixed',
            placement:
                (menu.dataset.placement as Placement | undefined) ??
                'bottom-start',
            middleware: [
                fui.offset(6),
                fui.flip({ padding: VIEWPORT_PADDING }),
                fui.shift({ padding: VIEWPORT_PADDING }),
                fui.size({
                    padding: VIEWPORT_PADDING,
                    apply({ availableHeight, rects, elements }) {
                        Object.assign(elements.floating.style, {
                            minWidth: `${rects.reference.width}px`,
                            maxHeight: `${Math.max(120, availableHeight)}px`,
                        })
                    },
                }),
            ],
        })
        .then(({ x, y }) => {
            Object.assign(menu.style, { left: `${x}px`, top: `${y}px` })
            // Stays hidden until placed, so it never flashes at the wrong spot.
            menu.setAttribute(PLACED, '')
        })
}

async function track(trigger: HTMLElement, menu: HTMLElement) {
    const fui = await import('@floating-ui/dom')
    // It may have closed while the module loaded.
    if (!openMenus.has(menu)) return
    stopTracking.set(
        menu,
        fui.autoUpdate(trigger, menu, () => place(fui, trigger, menu))
    )
}

function untrack(menu: HTMLElement) {
    openMenus.delete(menu)
    stopTracking.get(menu)?.()
    stopTracking.delete(menu)
    menu.removeAttribute(PLACED)
}

function onToggle(event: Event) {
    const menu = event.target
    if (!(menu instanceof HTMLElement) || !menu.matches(MENU)) return
    const trigger = triggerFor(menu)
    if (!trigger) return

    const open = (event as ToggleEvent).newState === 'open'
    trigger.setAttribute('aria-expanded', String(open))
    untrack(menu)
    if (!open) return

    openMenus.add(menu)
    void track(trigger, menu)
}

/** A swap removes the page's menus; close them first so their position tracking is released. */
function closeOpenMenus() {
    for (const menu of openMenus) {
        if (menu.isConnected) menu.hidePopover()
    }
    openMenus.clear()
}

export function initApiMenus() {
    // Without the popover API the menus stay hidden, so there is nothing to place.
    if (typeof HTMLElement.prototype.showPopover !== 'function') return
    // `toggle` does not bubble, so listen in the capture phase.
    document.addEventListener('toggle', onToggle, true)
    document.addEventListener('htmx:beforeSwap', closeOpenMenus)
}
