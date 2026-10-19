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

    it('moves between the shown chips with the arrow keys and picks each one, as tabs do', () => {
        const press = (from: string, key: string) =>
            chip(from).dispatchEvent(
                new KeyboardEvent('keydown', { key, bubbles: true })
            )
        chip('eql').hidden = true

        press('query', 'ArrowRight')
        expect(shown(['query', 'eql', 'esql'])).toEqual(['esql'])
        expect(document.activeElement).toBe(chip('esql'))
        expect(chip('esql').tabIndex).toBe(0)
        expect(chip('query').tabIndex).toBe(-1)

        press('esql', 'ArrowRight')
        expect(shown(['query', 'eql', 'esql'])).toEqual(['query'])

        press('query', 'End')
        expect(shown(['query', 'eql', 'esql'])).toEqual(['esql'])
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

function contentTypeBody(id: string, types: string[]): string {
    const options = types
        .map(
            (t, i) =>
                `<option value="${id}-${i}"${i === 0 ? ' selected' : ''}>${t}</option>`
        )
        .join('')
    const panels = types
        .map(
            (t, i) =>
                `<div class="content-type-panel" id="${id}-${i}"${i === 0 ? '' : ' hidden="until-found"'}><p id="${id}-${i}-text">${t} body</p></div>`
        )
        .join('')
    return `
        <span data-content-type-current="${id}">${types[0]}</span>
        <select data-content-type-select="${id}">${options}</select>
        <div class="content-type-panels" id="${id}">${panels}</div>`
}

describe('content type panels', () => {
    const picker = (id: string) =>
        document.querySelector<HTMLSelectElement>(
            `select[data-content-type-select="${id}"]`
        )!
    const tag = (id: string) =>
        document.querySelector<HTMLElement>(
            `[data-content-type-current="${id}"]`
        )!

    beforeEach(() => {
        document.body.innerHTML =
            contentTypeBody('req', ['application/zip', 'application/gzip']) +
            contentTypeBody('res', ['application/json', 'application/yaml'])
        initUnionChips()
    })

    const pick = (id: string, value: string) => {
        picker(id).value = value
        picker(id).dispatchEvent(new Event('change', { bubbles: true }))
    }

    it('shows the panel the select picks and keeps the others findable', () => {
        pick('req', 'req-1')

        expect(shown(['req-0', 'req-1'])).toEqual(['req-1'])
        expect(byId('req-0').getAttribute('hidden')).toBe('until-found')
    })

    it('updates the content type tag to the picked content type', () => {
        pick('res', 'res-1')

        expect(tag('res').textContent).toBe('application/yaml')
        expect(shown(['res-0', 'res-1'])).toEqual(['res-1'])
    })

    it("leaves another body's panel and select untouched", () => {
        pick('res', 'res-1')

        expect(shown(['res-0', 'res-1'])).toEqual(['res-1'])
        expect(shown(['req-0', 'req-1'])).toEqual(['req-0'])
        expect(picker('req').value).toBe('req-0')
        expect(tag('req').textContent).toBe('application/zip')
    })

    it('shows the first panel again when the select goes back to it', () => {
        pick('req', 'req-1')
        expect(shown(['req-0', 'req-1'])).toEqual(['req-1'])

        pick('req', 'req-0')

        expect(shown(['req-0', 'req-1'])).toEqual(['req-0'])
        expect(tag('req').textContent).toBe('application/zip')
    })

    it('moves the picker and tag to a panel the browser revealed for a link before the script ran', () => {
        // The browser unhides the target's panel itself for a fragment, leaving the first panel open as well.
        byId('req-1').removeAttribute('hidden')

        revealVariantsAround(byId('req-1-text'))

        expect(shown(['req-0', 'req-1'])).toEqual(['req-1'])
        expect(picker('req').value).toBe('req-1')
        expect(tag('req').textContent).toBe('application/gzip')
    })
})

describe('union variant search box', () => {
    const proto = HTMLElement.prototype as unknown as Record<string, unknown>

    beforeEach(() => {
        proto.hidePopover = jest.fn()
        proto.showPopover = jest.fn()
        document.body.innerHTML = chipList('list', ['query', 'eql', 'esql'])
        document
            .querySelector('[data-chip-menu]')!
            .insertAdjacentHTML(
                'beforebegin',
                '<input type="search" data-chip-filter />'
            )
        initUnionChips()
    })

    afterEach(() => {
        delete proto.hidePopover
        delete proto.showPopover
    })

    const type = (value: string) => {
        const input =
            document.querySelector<HTMLInputElement>('[data-chip-filter]')!
        input.value = value
        input.dispatchEvent(new Event('input', { bubbles: true }))
    }
    const listed = () =>
        Array.from(
            document.querySelectorAll<HTMLElement>(
                '[data-chip-menu] [data-chip]'
            )
        )
            .filter((item) => !item.hidden)
            .map((item) => item.dataset.chip)

    const open = () => {
        const menu = document.querySelector<HTMLElement>('[data-chip-menu]')!
        menu.id = 'menu'
        menu.setAttribute('popover', 'manual')
        jest.spyOn(menu, 'matches').mockImplementation(
            (selector: string) =>
                selector === ':popover-open' ||
                Element.prototype.matches.call(menu, selector)
        )
        const input =
            document.querySelector<HTMLInputElement>('[data-chip-filter]')!
        input.focus()
        type('eql')
        const toggle = Object.assign(new Event('toggle'), { newState: 'open' })
        menu.dispatchEvent(toggle)
        return { menu, input }
    }

    it('moves the focus into the matches on ArrowDown', () => {
        const { input } = open()

        input.dispatchEvent(
            new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true })
        )

        expect(document.activeElement).toBe(
            document.querySelector('[data-chip-menu] [data-chip="eql"]')
        )
    })

    it('picks the best match on Enter, closes the menu and clears the query', () => {
        const { menu, input } = open()

        input.dispatchEvent(
            new KeyboardEvent('keydown', { key: 'Enter', bubbles: true })
        )
        menu.dispatchEvent(
            Object.assign(new Event('toggle'), { newState: 'closed' })
        )

        expect(shown(['query', 'eql', 'esql'])).toEqual(['eql'])
        expect(proto.hidePopover).toHaveBeenCalled()
        expect(input.value).toBe('')
    })

    it('keeps the menu open when the search box is pressed, and closes it for a press elsewhere', () => {
        const { input } = open()

        input.dispatchEvent(new Event('pointerdown', { bubbles: true }))
        expect(proto.hidePopover).not.toHaveBeenCalled()

        document.body.dispatchEvent(new Event('pointerdown', { bubbles: true }))
        expect(proto.hidePopover).toHaveBeenCalled()
    })

    it('opens the menu with every match, the ones in the row included', () => {
        type('eql')

        expect(proto.showPopover).toHaveBeenCalled()
        expect(listed()).toEqual(['eql', 'esql'])
    })
})
