/**
 * The API beta banner sits outside `#main-container`, so no boosted swap carries it: a move within an API swaps
 * `#api-content-grid`, and the design-system bundle swaps `#main-container` between docs and API pages too. After each
 * boosted navigation the banner is made to match the new page: replaced, inserted before `#main-container`, or removed.
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

export function syncApiBetaBanner(pageHtml: string): void {
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
    if (current) {
        current.replaceWith(incoming)
        return
    }
    document.getElementById('main-container')?.before(incoming)
}

type AfterSwapDetail = {
    boosted?: boolean
    successful?: boolean
    xhr?: { response?: unknown }
}

/** After the swap, not before: htmx saves the outgoing page to history between the two, banner included. */
export function bindApiBetaBanner(): void {
    document.addEventListener('htmx:afterSwap', (event) => {
        const detail = (event as CustomEvent<AfterSwapDetail>).detail
        // Widgets with their own hx-get return fragments, not pages.
        if (!detail?.boosted || detail.successful === false) return
        const html = detail.xhr?.response
        if (typeof html === 'string') syncApiBetaBanner(html)
    })
}
