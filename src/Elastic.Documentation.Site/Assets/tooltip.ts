import { delegate } from 'tippy.js'

let bound = false

/**
 * Tooltips for the API pages: any element with data-tippy-content gets a tippy tooltip that shows at once,
 * where the native title waits about a second. One delegated listener covers every target, including the
 * ones scripts add later, and the tooltip is created on first hover rather than per element at load.
 * Inside the code preview dialog the tooltip mounts in the dialog, or the top layer would cover it.
 */
export function initTooltips(): void {
    if (bound) return
    bound = true
    delegate(document.body, {
        target: '[data-tippy-content]',
        delay: [80, 0],
        placement: 'top',
        touch: ['hold', 300],
        appendTo: (reference) => reference.closest('dialog') ?? document.body,
        // An empty title shows nothing natively; keep that.
        onShow: (instance) => (instance.props.content ? undefined : false),
    })
}
