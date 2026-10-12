// Shows the panel of a details popover in the browser's top layer, next to its trigger.
// Without this the details element still opens and the panel is absolutely positioned; this removes the stacking and clipping limits.
// Hooks are data attributes: [data-popover] on the details element, [data-popover-panel] on the panel.
const gap = 4
const margin = 8

function placeBeside(trigger: HTMLElement, panel: HTMLElement) {
    const anchor = trigger.getBoundingClientRect()
    const width = panel.offsetWidth

    const left = Math.min(
        Math.max(anchor.left, margin),
        Math.max(margin, window.innerWidth - width - margin)
    )
    const below = anchor.bottom + gap
    const fitsBelow = below + panel.offsetHeight <= window.innerHeight - margin
    const top = fitsBelow
        ? below
        : Math.max(margin, anchor.top - gap - panel.offsetHeight)

    panel.style.left = `${left}px`
    panel.style.top = `${top}px`
}

function place(details: HTMLDetailsElement, panel: HTMLElement) {
    const trigger = details.querySelector('summary')
    if (trigger) placeBeside(trigger, panel)
}

function show(details: HTMLDetailsElement, panel: HTMLElement) {
    if (!panel.matches(':popover-open')) panel.showPopover()
    place(details, panel)
}

function hide(panel: HTMLElement) {
    if (panel.matches(':popover-open')) panel.hidePopover()
}

export function initPopovers(scope: ParentNode = document) {
    if (!HTMLElement.prototype.showPopover) return

    scope
        .querySelectorAll<HTMLDetailsElement>(
            'details[data-popover]:not([data-popover-bound])'
        )
        .forEach((details) => {
            const panel = details.querySelector<HTMLElement>(
                '[data-popover-panel]'
            )
            if (!panel) return

            details.dataset.popoverBound = ''
            // Manual popovers are not dismissed by the browser; the menu enhancement closes the details, which hides the panel.
            panel.setAttribute('popover', 'manual')

            const reposition = () => {
                if (details.open) place(details, panel)
            }
            details.addEventListener('toggle', () => {
                if (details.open) {
                    show(details, panel)
                    window.addEventListener('scroll', reposition, {
                        passive: true,
                    })
                    window.addEventListener('resize', reposition)
                } else {
                    hide(panel)
                    window.removeEventListener('scroll', reposition)
                    window.removeEventListener('resize', reposition)
                }
            })
        })
}

// The inline applicability badge sits in a paragraph, so it is a button and spans rather than a details element. Script makes the button
// toggle the panel, shows it in the top layer beside the button where the browser has popovers (so a table's overflow cannot clip it and
// it stays on screen), and lets Escape or a click elsewhere close it. Hooks: [data-applies-popover] on the wrapper,
// [data-applies-trigger] on the button and [data-applies-panel] on the panel.
const inlineClosers = new WeakMap<HTMLElement, () => void>()
let inlineOutsideBound = false

// One listener for the whole page: it closes each open badge the click was not inside, and never touches a badge that has left the page.
function closeInlinePopoversOutside(event: Event) {
    document
        .querySelectorAll<HTMLElement>('[data-applies-popover][data-open]')
        .forEach((root) => {
            if (!root.contains(event.target as Node))
                inlineClosers.get(root)?.()
        })
}

export function initInlinePopovers(scope: ParentNode = document) {
    if (!inlineOutsideBound) {
        inlineOutsideBound = true
        document.addEventListener('click', closeInlinePopoversOutside)
    }
    scope
        .querySelectorAll<HTMLElement>(
            '[data-applies-popover]:not([data-applies-bound])'
        )
        .forEach((root) => {
            const trigger = root.querySelector<HTMLButtonElement>(
                '[data-applies-trigger]'
            )
            const panel = root.querySelector<HTMLElement>(
                '[data-applies-panel]'
            )
            if (!trigger || !panel) return
            root.dataset.appliesBound = ''

            const topLayer = Boolean(HTMLElement.prototype.showPopover)
            // Manual popovers are not dismissed by the browser; Escape and the outside click below do it.
            if (topLayer) panel.setAttribute('popover', 'manual')

            const reposition = () => placeBeside(trigger, panel)
            const setOpen = (open: boolean) => {
                if (open) root.dataset.open = ''
                else delete root.dataset.open
                trigger.setAttribute('aria-expanded', String(open))
                if (!topLayer) return
                if (open) {
                    if (!panel.matches(':popover-open')) panel.showPopover()
                    reposition()
                    window.addEventListener('scroll', reposition, {
                        passive: true,
                    })
                    window.addEventListener('resize', reposition)
                } else {
                    if (panel.matches(':popover-open')) panel.hidePopover()
                    window.removeEventListener('scroll', reposition)
                    window.removeEventListener('resize', reposition)
                }
            }
            trigger.addEventListener('click', () =>
                setOpen(!('open' in root.dataset))
            )
            root.addEventListener('keydown', (event) => {
                if (event.key !== 'Escape') return
                setOpen(false)
                trigger.focus()
            })
            inlineClosers.set(root, () => setOpen(false))
        })
}
