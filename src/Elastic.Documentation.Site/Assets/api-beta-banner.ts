/**
 * The API beta banner sits outside `#main-container`, so no boosted swap carries it: a move within an API swaps
 * `#api-content-grid`, and the design-system bundle swaps `#main-container` between docs and API pages too. After each
 * boosted navigation the banner is made to match the new page: replaced, inserted before `#main-container`, or removed.
 *
 * Where page feedback runs, the banner's "let us know" link opens the feedback widget in a dialog inside the banner. The
 * link stays the way to report when script or `<dialog>` is missing.
 */
const BANNER_ID = 'api-beta-banner'

/** The banner's markup in a page's HTML, or `null` when the page has none. Avoids parsing a large API page whole. */
export function bannerMarkup(pageHtml: string): string | null {
    const id = pageHtml.indexOf(`id="${BANNER_ID}"`)
    if (id < 0) return null
    const start = pageHtml.lastIndexOf('<aside', id)
    const end = pageHtml.indexOf('</aside>', id)
    if (start < 0 || end < 0) return null
    return pageHtml.slice(start, end + '</aside>'.length)
}

export function syncApiBetaBanner(
    pageHtml: string,
    onChange?: (banner: Element) => void
): void {
    const current = document.getElementById(BANNER_ID)
    const markup = bannerMarkup(pageHtml)
    if (markup === null) {
        current?.remove()
        return
    }
    const template = document.createElement('template')
    template.innerHTML = markup
    const incoming = template.content.firstElementChild
    if (!incoming) return
    if (current) current.replaceWith(incoming)
    else document.getElementById('main-container')?.before(incoming)
    if (incoming.isConnected) onChange?.(incoming)
}

const OPEN_FEEDBACK = '[data-api-beta-feedback-open]'
/** One listener for every banner the page ever holds, so a banner that navigation replaces needs no binding. */
function onClick(event: MouseEvent): void {
    const target = event.target
    if (!(target instanceof Element)) return

    // A click on the backdrop lands on the dialog itself; the dialog has no padding, so nothing else does.
    if (
        target instanceof HTMLDialogElement &&
        target.matches('.api-beta-feedback')
    ) {
        target.close()
        return
    }

    const trigger = target.closest<HTMLElement>(OPEN_FEEDBACK)
    if (!trigger || event.defaultPrevented) return
    // Open in a new tab, as for any link.
    if (event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey)
        return
    const dialog = document.getElementById(
        trigger.dataset.apiBetaFeedbackOpen ?? ''
    )
    if (
        !(dialog instanceof HTMLDialogElement) ||
        typeof dialog.showModal !== 'function'
    )
        return
    event.preventDefault()
    dialog.showModal()
}

type AfterSwapDetail = {
    boosted?: boolean
    successful?: boolean
    xhr?: { response?: unknown }
}

/**
 * After the swap, not before: htmx saves the outgoing page to history between the two, banner included.
 * `onChange` receives a banner that navigation put in the page, for bundles that bind behaviour per element.
 */
export function bindApiBetaBanner(onChange?: (banner: Element) => void): void {
    document.addEventListener('click', onClick)
    document.addEventListener('htmx:afterSwap', (event) => {
        const detail = (event as CustomEvent<AfterSwapDetail>).detail
        // Widgets with their own hx-get return fragments, not pages.
        if (!detail?.boosted || detail.successful === false) return
        const html = detail.xhr?.response
        if (typeof html === 'string') syncApiBetaBanner(html, onChange)
    })
}
