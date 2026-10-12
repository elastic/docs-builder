/**
 * Fits a collapsed parameter summary into one line. Names wrap inside a clipped list so the
 * browser finds where the line ends; the wrapped ones are then hidden, which shrinks the list
 * to its content and puts the "N more" badge right after the last visible name.
 */

function wrappedItems(items: HTMLElement[]): HTMLElement[] {
    const firstRowTop = items[0].offsetTop
    return items.filter((item) => item.offsetTop > firstRowTop)
}

export function applyParamSummaryFit(summary: HTMLElement): void {
    const items = [
        ...summary.querySelectorAll<HTMLElement>('.api-param-summary-item'),
    ]
    const more = summary.querySelector<HTMLElement>('[data-param-more]')
    const moreCount = summary.querySelector<HTMLElement>(
        '[data-param-more-count]'
    )
    // A hidden (expanded) summary has no width and nothing to fit
    if (!items.length || !more || !moreCount || summary.clientWidth === 0)
        return

    items.forEach((item) => (item.hidden = false))
    more.hidden = true
    if (wrappedItems(items).length === 0) return

    // The badge takes room from the list, so measure again with it in place
    more.hidden = false
    const wrapped = wrappedItems(items)
    wrapped.forEach((item) => (item.hidden = true))
    moreCount.textContent = moreLabel(
        wrapped.length,
        summary.dataset.paramMoreNoun
    )
}

/** "3 more" for fields; "3 more variants" when the summary names what it folds away. */
export function moreLabel(hidden: number, noun?: string): string {
    if (!noun) return `${hidden} more`
    return `${hidden} more ${hidden === 1 ? noun : `${noun}s`}`
}
