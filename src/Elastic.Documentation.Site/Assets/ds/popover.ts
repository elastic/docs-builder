// Shows the panel of a details popover in the browser's top layer, next to its trigger.
// Without this the details element still opens and the panel is absolutely positioned; this removes the stacking and clipping limits.
// Hooks are data attributes: [data-popover] on the details element, [data-popover-panel] on the panel.
const gap = 4
const margin = 8

function place(details: HTMLDetailsElement, panel: HTMLElement) {
    const trigger = details.querySelector('summary')
    if (!trigger) return
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
