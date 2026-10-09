import { appendExcerpt, initSearch, bindSearchShortcut } from './search'

jest.mock('../config', () => ({
    config: {
        buildType: 'assembler',
        apiBasePath: '/docs/_api',
        rootPath: '/docs',
    },
}))
jest.mock('../web-components/ModalSearch/pagefind', () => ({
    searchPagefind: jest.fn(),
}))

const markup = `
<button data-search-open hidden>Search</button>
<dialog data-search-dialog>
  <form method="dialog">
    <input data-search-input role="combobox" aria-expanded="false">
  </form>
  <p data-search-status role="status">Type to search.</p>
  <ol data-search-results role="listbox"></ol>
  <template data-search-result>
    <li role="option"><a href="#"><span data-search-path></span><span data-search-title></span><span data-search-excerpt></span></a></li>
  </template>
</dialog>`

const results = {
    results: [
        {
            url: '/docs/a',
            title: 'Alpha',
            description: 'about <mark>alpha</mark> things',
            parents: [{ title: 'Docs', url: '/docs' }],
        },
        {
            url: '/docs/b',
            title: 'Beta',
            description: 'plain text',
            parents: [],
        },
    ],
}

describe('appendExcerpt', () => {
    it('turns mark tags into mark elements and keeps the rest as text', () => {
        const target = document.createElement('span')
        appendExcerpt(target, 'about <mark>alpha</mark> things')

        expect(target.querySelector('mark')!.textContent).toBe('alpha')
        expect(target.textContent).toBe('about alpha things')
    })

    it('never parses other markup from the index', () => {
        const target = document.createElement('span')
        appendExcerpt(target, '<img src=x onerror=alert(1)> <mark>hit</mark>')

        expect(target.querySelector('img')).toBeNull()
        expect(target.querySelector('mark')!.textContent).toBe('hit')
    })
})

describe('search dialog', () => {
    let fetchMock: jest.Mock

    beforeEach(() => {
        jest.useFakeTimers()
        document.body.innerHTML = markup
        const dialog = document.querySelector('dialog') as HTMLDialogElement
        dialog.showModal = function () {
            this.setAttribute('open', '')
        }
        dialog.close = function () {
            this.removeAttribute('open')
            this.dispatchEvent(new Event('close'))
        }
        fetchMock = jest.fn(async () => ({
            ok: true,
            json: async () => results,
        }))
        global.fetch = fetchMock as unknown as typeof fetch
        initSearch()
        bindSearchShortcut()
    })

    afterEach(() => jest.useRealTimers())

    const type = async (value: string) => {
        const input = document.querySelector<HTMLInputElement>(
            '[data-search-input]'
        )!
        input.value = value
        input.dispatchEvent(new Event('input'))
        await jest.advanceTimersByTimeAsync(350)
    }

    it('reveals the trigger and opens the dialog from it', () => {
        const trigger =
            document.querySelector<HTMLButtonElement>('[data-search-open]')!
        expect(trigger.hidden).toBe(false)

        trigger.click()

        expect(document.querySelector('dialog')!.hasAttribute('open')).toBe(
            true
        )
    })

    it('opens from the keyboard shortcut', () => {
        document.dispatchEvent(
            new KeyboardEvent('keydown', {
                key: 'k',
                ctrlKey: true,
                bubbles: true,
            })
        )

        expect(document.querySelector('dialog')!.hasAttribute('open')).toBe(
            true
        )
    })

    it('queries the search api and draws the results', async () => {
        document.querySelector<HTMLButtonElement>('[data-search-open]')!.click()
        await type('alpha')

        expect(fetchMock.mock.calls[0][0]).toBe(
            '/docs/_api/v1/navigation-search?q=alpha&page=1'
        )
        const items = document.querySelectorAll('[role="option"]')
        expect(items).toHaveLength(2)
        expect(items[0].querySelector('[data-search-title]')!.textContent).toBe(
            'Alpha'
        )
        expect(items[0].querySelector('[data-search-path]')!.textContent).toBe(
            'Docs'
        )
        expect(items[0].querySelector('a')!.getAttribute('href')).toBe(
            '/docs/a'
        )
        expect(
            document.querySelector('[data-search-status]')!.textContent
        ).toBe('2 results.')
    })

    it('moves the selection with the arrow keys', async () => {
        document.querySelector<HTMLButtonElement>('[data-search-open]')!.click()
        await type('alpha')
        const input = document.querySelector<HTMLInputElement>(
            '[data-search-input]'
        )!

        input.dispatchEvent(
            new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true })
        )

        const items = document.querySelectorAll('[role="option"]')
        expect(items[1].getAttribute('aria-selected')).toBe('true')
        expect(input.getAttribute('aria-activedescendant')).toBe(
            'search-result-1'
        )
    })

    it('says so when nothing matches', async () => {
        fetchMock.mockImplementation(async () => ({
            ok: true,
            json: async () => ({ results: [] }),
        }))
        document.querySelector<HTMLButtonElement>('[data-search-open]')!.click()
        await type('zzz')

        expect(
            document.querySelector('[data-search-status]')!.textContent
        ).toBe('No results.')
    })

    it('reports a failing backend', async () => {
        fetchMock.mockImplementation(async () => ({ ok: false, status: 500 }))
        document.querySelector<HTMLButtonElement>('[data-search-open]')!.click()
        await type('alpha')

        expect(
            document.querySelector('[data-search-status]')!.textContent
        ).toBe('Search is unavailable right now.')
    })

    it('clears the search when the dialog closes', async () => {
        document.querySelector<HTMLButtonElement>('[data-search-open]')!.click()
        await type('alpha')

        document.querySelector('dialog')!.dispatchEvent(new Event('close'))

        expect(
            document.querySelector<HTMLInputElement>('[data-search-input]')!
                .value
        ).toBe('')
        expect(document.querySelectorAll('[role="option"]')).toHaveLength(0)
    })
})
