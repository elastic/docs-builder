import { applyParamSummaryFit, moreLabel } from './api-param-summary'

function summary(rows: number[], noun?: string, width = 300): HTMLElement {
    document.body.innerHTML = `
        <span class="api-param-summary" data-param-summary ${noun ? `data-param-more-noun="${noun}"` : ''}>
            ${rows.map((_, i) => `<code class="api-param-summary-item">n${i}</code>`).join('')}
            <span data-param-more hidden><span data-param-more-count></span></span>
        </span>`
    const el = document.querySelector<HTMLElement>('[data-param-summary]')!
    Object.defineProperty(el, 'clientWidth', { value: width })
    el.querySelectorAll<HTMLElement>('.api-param-summary-item').forEach(
        (item, i) =>
            Object.defineProperty(item, 'offsetTop', { value: rows[i] * 24 })
    )
    return el
}

const badge = (el: HTMLElement) =>
    el.querySelector<HTMLElement>('[data-param-more]')!

describe('applyParamSummaryFit', () => {
    it('keeps the badge hidden when every name fits on one row', () => {
        const el = summary([0, 0, 0])
        applyParamSummaryFit(el)
        expect(badge(el).hidden).toBe(true)
    })

    it('counts the names that wrapped out of the first row', () => {
        const el = summary([0, 0, 1, 1, 2])
        applyParamSummaryFit(el)
        expect(badge(el).hidden).toBe(false)
        expect(el.querySelector('[data-param-more-count]')!.textContent).toBe(
            '3 more'
        )
        const items = [
            ...el.querySelectorAll<HTMLElement>('.api-param-summary-item'),
        ]
        expect(items.map((item) => item.hidden)).toEqual([
            false,
            false,
            true,
            true,
            true,
        ])
    })

    it('names what a union summary folds away', () => {
        const el = summary([0, 1], 'variant')
        applyParamSummaryFit(el)
        expect(el.querySelector('[data-param-more-count]')!.textContent).toBe(
            '1 more variant'
        )
    })

    it('leaves a summary with no width alone', () => {
        const el = summary([0, 1], undefined, 0)
        applyParamSummaryFit(el)
        expect(badge(el).hidden).toBe(true)
    })
})

describe('moreLabel', () => {
    it('keeps the short form for fields', () => {
        expect(moreLabel(3)).toBe('3 more')
    })

    it('names variants, singular and plural', () => {
        expect(moreLabel(1, 'variant')).toBe('1 more variant')
        expect(moreLabel(3, 'variant')).toBe('3 more variants')
    })
})
