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

    it('binds the theme switch of the new bar so its choices apply', () => {
        const withSwitch = `<nav id="docs-bar"><div class="theme-switch"><input type="radio" name="theme" data-pref="theme" value="dark"><input type="radio" name="theme" data-pref="theme" value="light"></div></nav>`
        document.documentElement.removeAttribute('data-theme')
        localStorage.clear()
        syncDocsBar(`<html><body>${withSwitch}</body></html>`)

        const dark = document.querySelector<HTMLInputElement>(
            'input[value="dark"]'
        )!
        dark.checked = true
        dark.dispatchEvent(new Event('change'))

        expect(document.documentElement.dataset.theme).toBe('dark')
    })

    it('leaves the bar alone when the response has none', () => {
        const changed = syncDocsBar('<div>a search result</div>')

        expect(changed).toBe(false)
        expect(
            document.querySelector('[aria-current="page"]')?.textContent
        ).toBe('A')
    })
})
