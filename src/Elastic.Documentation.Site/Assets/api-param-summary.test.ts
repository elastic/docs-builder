import { fitParamSummaries, moreLabel } from './api-param-summary'

interface Layout {
    /** The row each name lands on; a function gets whether the badge is showing. */
    rows: (number | ((badgeShown: boolean) => number))[]
    noun?: string
    width?: number
    firstNameWidth?: number
}

function summary({
    rows,
    noun,
    width = 300,
    firstNameWidth = 50,
}: Layout): HTMLElement {
    document.body.innerHTML = `
        <span class="api-param-summary" data-param-summary ${noun ? `data-param-more-noun="${noun}"` : ''}>
            <span class="api-param-summary-list">
                ${rows.map((_, i) => `<code class="api-param-summary-item">n${i}</code>`).join('')}
            </span>
            <span data-param-more hidden><span data-param-more-count></span></span>
        </span>`
    const el = document.querySelector<HTMLElement>('[data-param-summary]')!
    const list = el.querySelector<HTMLElement>('.api-param-summary-list')!
    const more = el.querySelector<HTMLElement>('[data-param-more]')!
    Object.defineProperty(el, 'clientWidth', { value: width })
    Object.defineProperty(list, 'clientWidth', { value: width })
    items(el).forEach((item, i) => {
        Object.defineProperty(item, 'offsetWidth', {
            value: i === 0 ? firstNameWidth : 50,
        })
        Object.defineProperty(item, 'offsetTop', {
            get: () => {
                const row = rows[i]
                return (typeof row === 'number' ? row : row(!more.hidden)) * 24
            },
        })
    })
    return el
}

const items = (el: HTMLElement) => [
    ...el.querySelectorAll<HTMLElement>('.api-param-summary-item'),
]
const hiddenFlags = (el: HTMLElement) => items(el).map((item) => item.hidden)
const badge = (el: HTMLElement) =>
    el.querySelector<HTMLElement>('[data-param-more]')!
const count = (el: HTMLElement) =>
    el.querySelector<HTMLElement>('[data-param-more-count]')!.textContent

describe('fitParamSummaries', () => {
    it('keeps the badge hidden when every name fits on one row', () => {
        const el = summary({ rows: [0, 0, 0] })
        fitParamSummaries([el])
        expect(badge(el).hidden).toBe(true)
        expect(hiddenFlags(el)).toEqual([false, false, false])
    })

    it('hides the names that wrapped out of the first row and counts them', () => {
        const el = summary({ rows: [0, 0, 1, 1, 2] })
        fitParamSummaries([el])
        expect(hiddenFlags(el)).toEqual([false, false, true, true, true])
        expect(badge(el).hidden).toBe(false)
        expect(count(el)).toBe('3 more')
    })

    it('measures beside the badge, since its label takes room too', () => {
        // The last name wraps on its own; once the badge shows, the fourth wraps as well
        const el = summary({
            rows: [0, 0, 0, (badgeShown) => (badgeShown ? 1 : 0), 1],
        })
        fitParamSummaries([el])
        expect(hiddenFlags(el)).toEqual([false, false, false, true, true])
        expect(count(el)).toBe('2 more')
    })

    it('folds every name away when even the first is wider than the line', () => {
        const el = summary({ rows: [0, 1, 1], firstNameWidth: 400 })
        fitParamSummaries([el])
        expect(hiddenFlags(el)).toEqual([true, true, true])
        expect(count(el)).toBe('3 more')
    })

    it('names what a union summary folds away', () => {
        const el = summary({ rows: [0, 1], noun: 'variant' })
        fitParamSummaries([el])
        expect(count(el)).toBe('1 more variant')
    })

    it('marks a fitted summary so CSS can reveal it', () => {
        const el = summary({ rows: [0, 1] })
        fitParamSummaries([el])
        expect(el.classList.contains('is-fitted')).toBe(true)
    })

    it('leaves a summary with no width alone', () => {
        const el = summary({ rows: [0, 1], width: 0 })
        fitParamSummaries([el])
        expect(badge(el).hidden).toBe(true)
        expect(el.classList.contains('is-fitted')).toBe(false)
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
