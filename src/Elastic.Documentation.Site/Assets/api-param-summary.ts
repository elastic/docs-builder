/**
 * Fits collapsed parameter summaries into one line. Names wrap inside a clipped list so the
 * browser finds where the line ends; the wrapped ones are then hidden, which shrinks the list
 * to its content and puts the "N more" badge right after the last visible name.
 */

interface Summary {
    root: HTMLElement
    list: HTMLElement
    items: HTMLElement[]
    more: HTMLElement
    moreCount: HTMLElement
    noun: string | undefined
}

function partsOf(root: HTMLElement): Summary {
    return {
        root,
        list: root.querySelector<HTMLElement>('.api-param-summary-list')!,
        items: [
            ...root.querySelectorAll<HTMLElement>('.api-param-summary-item'),
        ],
        more: root.querySelector<HTMLElement>('[data-param-more]')!,
        moreCount: root.querySelector<HTMLElement>('[data-param-more-count]')!,
        noun: root.dataset.paramMoreNoun,
    }
}

function wrappedItems({ list, items }: Summary): HTMLElement[] {
    // An empty summary has nothing to measure; throwing here would abort the whole batch
    if (items.length === 0) return []
    // A first name wider than the whole line would be clipped mid-word, so it folds away too
    if (items[0].offsetWidth > list.clientWidth) return items
    const firstRowTop = items[0].offsetTop
    return items.filter((item) => item.offsetTop > firstRowTop)
}

/** Writes, one read across every summary, then writes: one layout however many summaries. */
export function fitParamSummaries(summaries: Iterable<HTMLElement>): void {
    // A hidden (expanded) summary has no width and nothing to fit
    const shown = [...summaries]
        .filter((root) => root.clientWidth > 0)
        .map(partsOf)

    // Measure beside the badge at its widest label: a name that fits there fits beside any shorter one
    for (const { items, more, moreCount, noun } of shown) {
        items.forEach((item) => (item.hidden = false))
        more.hidden = false
        moreCount.textContent = moreLabel(items.length, noun)
    }

    const wrapped = shown.map(wrappedItems)

    shown.forEach(({ root, more, moreCount, noun }, i) => {
        wrapped[i].forEach((item) => (item.hidden = true))
        more.hidden = wrapped[i].length === 0
        if (wrapped[i].length > 0)
            moreCount.textContent = moreLabel(wrapped[i].length, noun)
        root.classList.add('is-fitted')
    })
}

/** "3 more" for fields; "3 more variants" when the summary names what it folds away. */
export function moreLabel(hidden: number, noun?: string): string {
    if (!noun) return `${hidden} more`
    return `${hidden} more ${hidden === 1 ? noun : `${noun}s`}`
}
