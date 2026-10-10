import { initUnionChips, revealVariantsAround } from './api-union-chips'

function chipList(id: string, variants: string[], inner = ''): string {
    const chips = variants
        .map(
            (v, i) =>
                `<button class="api-example-chip${i === 0 ? ' is-active' : ''}" data-chip="${v}" aria-selected="${i === 0}"><span data-chip-title>${v}</span></button>`
        )
        .join('')
    const items = variants
        .map(
            (v) =>
                `<button role="menuitem" class="api-example-menu-item" data-chip="${v}" hidden><span data-chip-item-title>${v}</span></button>`
        )
        .join('')
    const panels = variants
        .map(
            (v, i) =>
                `<div class="union-variant-item" id="${v}"${i === 0 ? '' : ' hidden="until-found"'}>${i === 1 ? inner : ''}</div>`
        )
        .join('')
    return `
        <div class="union-variants-container union-variant-chips" id="${id}">
            <div class="api-example-chips-row" data-chip-row>
                <div class="api-example-chips" role="tablist">${chips}</div>
                <button data-chip-more hidden><span data-chip-more-label></span></button>
                <div data-chip-menu><div role="menu">${items}</div></div>
            </div>
            <div class="union-variants">${panels}</div>
        </div>`
}

const byId = (id: string) => document.getElementById(id)!
const chip = (id: string) =>
    document.querySelector<HTMLElement>(`.api-example-chip[data-chip="${id}"]`)!
const shown = (ids: string[]) =>
    ids.filter((id) => !byId(id).hasAttribute('hidden'))

describe('union variant chips', () => {
    const proto = HTMLElement.prototype as unknown as Record<string, unknown>

    beforeEach(() => {
        proto.hidePopover = jest.fn()
        document.body.innerHTML = chipList(
            'outer',
            ['query', 'eql', 'esql'],
            chipList('inner', ['a', 'b', 'c'], '<dt id="deep">deep</dt>')
        )
        initUnionChips()
    })

    afterEach(() => {
        delete proto.hidePopover
    })

    it('shows the variant whose chip is clicked and keeps the others findable', () => {
        chip('esql').click()

        expect(shown(['query', 'eql', 'esql'])).toEqual(['esql'])
        expect(byId('query').getAttribute('hidden')).toBe('until-found')
        expect(chip('esql').classList.contains('is-active')).toBe(true)
        expect(chip('esql').getAttribute('aria-selected')).toBe('true')
        expect(chip('query').getAttribute('aria-selected')).toBe('false')
    })

    it('shows a variant picked from the more menu and closes the menu', () => {
        document
            .querySelector<HTMLElement>('[data-chip-menu] [data-chip="eql"]')!
            .click()

        expect(shown(['query', 'eql', 'esql'])).toEqual(['eql'])
        expect(proto.hidePopover).toHaveBeenCalled()
    })

    it('leaves the outer list alone when a nested list switches', () => {
        chip('eql').click()
        chip('c').click()

        expect(shown(['query', 'eql', 'esql'])).toEqual(['eql'])
        expect(shown(['a', 'b', 'c'])).toEqual(['c'])
    })

    it('switches every list around a find-in-page match, outermost first', () => {
        byId('deep').dispatchEvent(new Event('beforematch', { bubbles: true }))

        expect(shown(['query', 'eql', 'esql'])).toEqual(['eql'])
        expect(shown(['a', 'b', 'c'])).toEqual(['b'])
    })

    it('moves the chip to a variant the browser revealed for a link before the script ran', () => {
        byId('eql').removeAttribute('hidden')

        expect(revealVariantsAround(byId('deep'))).toBe(true)
        expect(chip('eql').classList.contains('is-active')).toBe(true)
        expect(shown(['query', 'eql', 'esql'])).toEqual(['eql'])
    })

    it('releases the height kept for the scroll position on the next switch', () => {
        const list = byId('outer').querySelector<HTMLElement>(
            ':scope > .union-variants'
        )!
        list.style.minHeight = '900px'
        // jsdom has no layout: give the page room below the scroll position, as a page that is not at its bottom has.
        Object.defineProperty(document.documentElement, 'scrollHeight', {
            configurable: true,
            get: () => 5000,
        })

        chip('esql').click()

        expect(list.style.minHeight).toBe('')
        delete (document.documentElement as unknown as Record<string, unknown>)
            .scrollHeight
    })

    it('reports whether a link target was inside a hidden variant', () => {
        expect(revealVariantsAround(byId('deep'))).toBe(true)
        expect(revealVariantsAround(byId('deep'))).toBe(false)
    })
})
