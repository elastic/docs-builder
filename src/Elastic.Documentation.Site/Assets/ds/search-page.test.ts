import { initSearchPage, isQuestion, parseState, toSearch } from './search-page'

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

describe('search page state', () => {
    it('reads the address', () => {
        expect(
            parseState(
                '?q=shards&page=2&sort=recent&type=doc&type=api&product=es'
            )
        ).toEqual({
            query: 'shards',
            page: 2,
            sort: 'recent',
            filters: { type: ['doc', 'api'], section: [], product: ['es'] },
        })
    })

    it('falls back to the first page for a bad page number', () => {
        expect(parseState('?q=a&page=-3').page).toBe(1)
        expect(parseState('?q=a&page=x').page).toBe(1)
    })

    it('leaves defaults out of the address and round-trips the rest', () => {
        const state = parseState('?q=shards&page=2&sort=recent&section=Deploy')

        expect(toSearch(state)).toBe(
            'q=shards&page=2&sort=recent&section=Deploy'
        )
        expect(toSearch(parseState('?q=a'))).toBe('q=a')
    })

    it('adds the page size only when asked', () => {
        expect(toSearch(parseState('?q=a'), 20)).toBe('q=a&size=20')
    })
})

describe('questions', () => {
    it('treats questions and long phrases as worth an answer', () => {
        expect(isQuestion('how do I index logs')).toBe(true)
        expect(isQuestion('shards?')).toBe(true)
        expect(isQuestion('index lifecycle policy rollover settings')).toBe(
            true
        )
    })

    it('leaves keyword lookups alone', () => {
        expect(isQuestion('shards')).toBe(false)
        expect(isQuestion('data stream')).toBe(false)
    })
})

describe('search page', () => {
    const markup = `
<div data-search-page>
  <form data-search-page-form><input data-search-page-query></form>
  <aside data-search-page-filters hidden></aside>
  <p data-search-page-status></p>
  <select data-search-page-sort><option value="relevance">r</option><option value="recent">m</option></select>
  <ol data-search-page-results></ol>
  <nav data-search-page-pager hidden><button data-search-page-prev></button><span data-search-page-page></span><button data-search-page-next></button></nav>
  <section data-search-page-answer hidden><div data-search-page-answer-body></div></section>
  <template data-search-page-result><li><p data-search-page-path></p><a data-search-page-title></a><p data-search-page-excerpt></p></li></template>
  <template data-search-page-facet><fieldset><legend></legend><div class="search-facet__options"></div></fieldset></template>
</div>`

    beforeEach(() => {
        document.body.innerHTML = markup
        window.history.replaceState(null, '', '/?q=shards')
        global.fetch = jest.fn().mockResolvedValue({
            ok: true,
            json: async () => ({
                results: [
                    {
                        url: '/a',
                        title: 'Shards',
                        description: 'about <mark>shards</mark>',
                        parents: [{ url: '/x', title: 'Deploy' }],
                    },
                ],
                totalResults: 41,
                pageCount: 3,
                pageNumber: 1,
                aggregations: { type: { doc: 30, api: 11 } },
            }),
        }) as unknown as typeof fetch
    })

    it('searches the address query and draws results, facets and the pager', async () => {
        initSearchPage()
        await new Promise((r) => setTimeout(r, 0))

        expect(
            document.querySelector('[data-search-page-status]')!.textContent
        ).toBe('41 results.')
        expect(
            document.querySelector('[data-search-page-title]')!.textContent
        ).toBe('Shards')
        expect(
            document.querySelector('[data-search-page-excerpt] mark')!
                .textContent
        ).toBe('shards')
        expect(document.querySelectorAll('.search-facet__option')).toHaveLength(
            2
        )
        expect(
            document.querySelector<HTMLElement>('[data-search-page-pager]')!
                .hidden
        ).toBe(false)
        expect(
            document.querySelector('[data-search-page-page]')!.textContent
        ).toBe('Page 1 of 3')
    })

    it('asks for the next page and keeps the address in step', async () => {
        initSearchPage()
        await new Promise((r) => setTimeout(r, 0))
        document
            .querySelector<HTMLButtonElement>('[data-search-page-next]')!
            .click()
        await new Promise((r) => setTimeout(r, 0))

        expect(location.search).toBe('?q=shards&page=2')
    })

    it('keeps the answer panel hidden for a keyword search', async () => {
        initSearchPage()
        await new Promise((r) => setTimeout(r, 0))

        expect(
            document.querySelector<HTMLElement>('[data-search-page-answer]')!
                .hidden
        ).toBe(true)
    })

    it('shows the answer panel for a question', async () => {
        window.history.replaceState(null, '', '/?q=how+do+I+index+logs')
        initSearchPage()
        await new Promise((r) => setTimeout(r, 0))

        expect(
            document.querySelector<HTMLElement>('[data-search-page-answer]')!
                .hidden
        ).toBe(false)
    })

    it('shows a message when the search fails', async () => {
        ;(global.fetch as jest.Mock).mockResolvedValue({
            ok: false,
            status: 500,
        })
        initSearchPage()
        await new Promise((r) => setTimeout(r, 0))

        expect(
            document.querySelector('[data-search-page-status]')!.textContent
        ).toBe('Search is unavailable right now.')
    })
})
