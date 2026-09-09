/**
 * Fits a collapsed parameter-name summary into one line.
 * Hides trailing names and shows an "N more" badge instead of an ellipsis.
 */

export function hiddenCountToFit(
    total: number,
    overflows: (hidden: number) => boolean
): number {
    if (total <= 0 || !overflows(0)) return 0

    for (let hidden = 1; hidden < total; hidden++)
        if (!overflows(hidden)) return hidden

    return total
}

export function applyParamSummaryFit(summary: HTMLElement): void {
    const section = summary.closest<HTMLElement>('[data-param-section]')
    if (section?.classList.contains('expanded')) return

    const line = summary.querySelector<HTMLElement>('.api-param-summary-line')
    const items = [
        ...summary.querySelectorAll<HTMLElement>('[data-param-item]'),
    ]
    const more = summary.querySelector<HTMLElement>('[data-param-more]')
    const moreCount = summary.querySelector<HTMLElement>(
        '[data-param-more-count]'
    )
    if (!line || !more || !moreCount || line.clientWidth === 0) return

    const overflows = (hidden: number): boolean => {
        applyHiddenState(items, more, moreCount, hidden)
        return line.scrollWidth > line.clientWidth + 1
    }

    const hidden = hiddenCountToFit(items.length, overflows)
    applyHiddenState(items, more, moreCount, hidden)
}

function applyHiddenState(
    items: HTMLElement[],
    more: HTMLElement,
    moreCount: HTMLElement,
    hidden: number
): void {
    const visible = items.length - hidden
    for (let i = 0; i < items.length; i++) items[i].hidden = i >= visible

    if (hidden <= 0) {
        more.hidden = true
        more.classList.remove('has-leading-sep')
        moreCount.textContent = ''
        return
    }

    more.hidden = false
    more.classList.toggle('has-leading-sep', visible > 0)
    moreCount.textContent = `${hidden} more`
}
