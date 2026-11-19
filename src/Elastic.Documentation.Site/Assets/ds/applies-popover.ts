// The applies-to badge opens its panel as a popover: the browser opens and closes it, and floating.ts places it beside the button.
// A page renders the badge many times, from Markdown, tables and API pages that share no counter, so the server cannot give each
// panel a unique id. Binding gives it one and points the button at it. Unbound, the panel shows while the button has focus.
// Hooks: [data-applies-popover] on the wrapper, [data-applies-trigger] on the button and [data-applies-panel] on the panel.
let count = 0

export function initAppliesPopovers(scope: ParentNode = document) {
    if (typeof HTMLElement.prototype.showPopover !== 'function') return
    scope
        .querySelectorAll<HTMLElement>(
            '[data-applies-popover]:not([data-applies-bound])'
        )
        .forEach((root) => {
            const trigger = root.querySelector('[data-applies-trigger]')
            const panel = root.querySelector('[data-applies-panel]')
            if (!trigger || !panel) return
            panel.id = `applies-popover-${++count}`
            panel.setAttribute('popover', '')
            trigger.setAttribute('popovertarget', panel.id)
            trigger.setAttribute('aria-haspopup', 'dialog')
            root.dataset.appliesBound = ''
        })
}

/** A cloned badge carries the original's panel id and `popovertarget`, so it would open the original panel. Unbind it, then `initAppliesPopovers` binds the copy. */
export function unbindAppliesPopovers(scope: ParentNode) {
    scope
        .querySelectorAll<HTMLElement>('[data-applies-bound]')
        .forEach((root) => {
            const trigger = root.querySelector('[data-applies-trigger]')
            const panel = root.querySelector('[data-applies-panel]')
            panel?.removeAttribute('id')
            panel?.removeAttribute('popover')
            trigger?.removeAttribute('popovertarget')
            trigger?.removeAttribute('aria-haspopup')
            delete root.dataset.appliesBound
        })
}
