import { delegate, type DelegateInstance, type Instance } from 'tippy.js'

let delegateInstance: DelegateInstance | null = null

// tippy's delegate keeps every tooltip it creates, and with it the hovered element, until the delegate itself is
// destroyed; destroying it on each swap is what releases the previous page.
document.addEventListener('htmx:afterSwap', () => {
    delegateInstance?.destroy()
    delegateInstance = null
})

/**
 * Tooltips for the API pages: any element with data-tippy-content gets a tippy tooltip that shows at once,
 * where the native title waits about a second. One delegated listener covers every target, including the
 * ones scripts add later, and the tooltip is created on first hover rather than per element at load.
 * Inside the code preview dialog the tooltip mounts in the dialog, or the top layer would cover it.
 */
export function initTooltips(): void {
    if (delegateInstance) return
    delegateInstance = delegate(document.body, {
        // Templates may emit the attribute empty; an empty title shows nothing natively, so neither does this.
        target: '[data-tippy-content]:not([data-tippy-content=""])',
        delay: [80, 0],
        placement: 'top',
        touch: ['hold', 300],
        // A click on a copy button swaps the text in place (flashTooltip); hiding the tooltip would make it flicker.
        onCreate: (instance) => {
            if (
                instance.reference.matches(
                    '.copybtn, .api-code-preview-btn, .api-code-wrap-btn, .property-anchor-icon'
                )
            )
                instance.setProps({ hideOnClick: false })
        },
        appendTo: (reference) => reference.closest('dialog') ?? document.body,
    })
}

/** Sets a tooltip's text, updating an already created tippy too (it caches its first content). */
export function setTooltip(el: HTMLElement, text: string): void {
    el.dataset.tippyContent = text
    ;(el as HTMLElement & { _tippy?: Instance })._tippy?.setContent(text)
}

const flashes = new WeakMap<HTMLElement, { original: string; timer: number }>()

/** Swaps a tooltip's text for a moment (e.g. "Copied!"), updating an already created tippy too. */
export function flashTooltip(el: HTMLElement, text: string, ms = 1500): void {
    const pending = flashes.get(el)
    if (pending) window.clearTimeout(pending.timer)
    const original = pending?.original ?? el.dataset.tippyContent ?? ''
    setTooltip(el, text)
    const timer = window.setTimeout(() => {
        setTooltip(el, original)
        flashes.delete(el)
    }, ms)
    flashes.set(el, { original, timer })
}
