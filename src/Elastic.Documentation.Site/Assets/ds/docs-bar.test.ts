import { syncDocsBar } from './docs-bar'

const bar = (current: string) =>
    `<nav id="docs-bar"><a href="/a" ${current === 'a' ? 'aria-current="page"' : ''}>A</a><a href="/b" ${current === 'b' ? 'aria-current="page"' : ''}>B</a><details data-menu></details></nav>`

describe('docs bar sync', () => {
    beforeEach(() => {
        document.body.innerHTML = bar('a')
    })

    it('takes the current tab from the next page', () => {
        const changed = syncDocsBar(
            `<html><body>${bar('b')}<main></main></body></html>`
        )

        expect(changed).toBe(true)
        expect(
            document.querySelector('[aria-current="page"]')?.textContent
        ).toBe('B')
        expect(document.querySelectorAll('#docs-bar')).toHaveLength(1)
    })

    it('binds the menus of the new bar', () => {
        syncDocsBar(`<html><body>${bar('b')}</body></html>`)

        expect(
            document.querySelector('details')?.hasAttribute('data-menu-bound')
        ).toBe(true)
    })

    it('leaves the bar alone when the response has none', () => {
        const changed = syncDocsBar('<div>a search result</div>')

        expect(changed).toBe(false)
        expect(
            document.querySelector('[aria-current="page"]')?.textContent
        ).toBe('A')
    })
})
