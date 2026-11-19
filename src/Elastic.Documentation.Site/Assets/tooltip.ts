import { delegate, type DelegateInstance, type Instance } from 'tippy.js'

/** For lists the mouse crosses: the page's 80ms delay would open a tooltip on every row. */
export const SLOW_TOOLTIP_DELAY = '[500, 0]'

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
        target: `[data-tippy-content]:not([data-tippy-content=""]), ${PROPERTY_NAME}`,
        // Only property names reach this: everything else carries data-tippy-content, which tippy reads first.
        content: (reference) => propertyPath(reference),
        delay: [80, 0],
        placement: 'top',
        touch: ['hold', 300],
        onCreate: (instance) => {
            // A click on a copy button swaps the text in place (flashTooltip); hiding the tooltip would make it flicker.
            if (
                instance.reference.matches(
                    '.copybtn, .api-code-preview-btn, .property-anchor'
                )
            )
                instance.setProps({ hideOnClick: false })
            if (instance.reference.matches('.property-list dt > code'))
                instance.setProps({ theme: 'mono' })
        },
        // A top-level field's path is its name, which the row already shows.
        onShow: (instance) => (instance.props.content ? undefined : false),
        appendTo: (reference) => reference.closest('dialog') ?? document.body,
    })
}

const PROPERTY_NAME = '.property-list dt > a.property-anchor + code'
const PERMALINK_LABEL_PREFIX = 'Link to '

/** A property name's JSON path. The row prints it once, in its permalink's label, to keep large API pages small. */
function propertyPath(name: Element): string {
    const label = name.previousElementSibling?.getAttribute('aria-label') ?? ''
    if (!label.startsWith(PERMALINK_LABEL_PREFIX)) return ''
    const path = label.slice(PERMALINK_LABEL_PREFIX.length)
    return path === name.textContent ? '' : path
}

const flashes = new WeakMap<HTMLElement, { original: string; timer: number }>()

/** Swaps a tooltip's text for a moment (e.g. "Copied!"), updating an already created tippy too. */
export function flashTooltip(el: HTMLElement, text: string, ms = 1500): void {
    const pending = flashes.get(el)
    if (pending) window.clearTimeout(pending.timer)
    const original = pending?.original ?? el.dataset.tippyContent ?? ''
    const set = (value: string) => {
        el.dataset.tippyContent = value
        ;(el as HTMLElement & { _tippy?: Instance })._tippy?.setContent(value)
    }
    set(text)
    const timer = window.setTimeout(() => {
        set(original)
        flashes.delete(el)
    }, ms)
    flashes.set(el, { original, timer })
}
