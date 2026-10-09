// The full-page search. The server renders the form and templates; this reads the address, queries the same API as the dialog,
// draws facets and results, and keeps the address in step so a search can be shared. Hooks are data attributes.
import { config } from '../config'
import { appendExcerpt, querySearch } from './search'

const PAGE_SIZE = 20

export interface SearchPageState {
    query: string
    page: number
    sort: string
    filters: Record<'type' | 'section' | 'product', string[]>
}

interface FullSearchResult {
    url: string
    title: string
    description: string
    parents: Array<{ url: string; title: string }>
}

interface FullSearchResponse {
    results: FullSearchResult[]
    totalResults: number
    pageCount: number
    pageNumber: number
    aggregations?: {
        type?: Record<string, number>
        navigationSection?: Record<string, number>
        product?: Record<string, { count: number; displayName: string }>
    }
}

export function parseState(search: string): SearchPageState {
    const params = new URLSearchParams(search)
    const page = Number(params.get('page') ?? '1')
    return {
        query: params.get('q') ?? '',
        page: Number.isFinite(page) && page >= 1 ? Math.floor(page) : 1,
        sort: params.get('sort') ?? 'relevance',
        filters: {
            type: params.getAll('type'),
            section: params.getAll('section'),
            product: params.getAll('product'),
        },
    }
}

export function toSearch(state: SearchPageState, size?: number): string {
    const params = new URLSearchParams()
    if (state.query) params.set('q', state.query)
    if (state.page > 1) params.set('page', String(state.page))
    if (state.sort !== 'relevance') params.set('sort', state.sort)
    if (size) params.set('size', String(size))
    for (const key of ['type', 'section', 'product'] as const)
        state.filters[key].forEach((value) => params.append(key, value))
    return params.toString()
}

export function initSearchPage(scope: ParentNode = document) {
    scope
        .querySelectorAll<HTMLElement>(
            '[data-search-page]:not([data-search-page-bound])'
        )
        .forEach((root) => {
            root.dataset.searchPageBound = ''
            bindSearchPage(root)
        })
}

function bindSearchPage(root: HTMLElement) {
    const q = <T extends HTMLElement>(selector: string) =>
        root.querySelector<T>(selector)!
    const form = q<HTMLFormElement>('[data-search-page-form]')
    const query = q<HTMLInputElement>('[data-search-page-query]')
    const sort = q<HTMLSelectElement>('[data-search-page-sort]')
    const status = q('[data-search-page-status]')
    const list = q('[data-search-page-results]')
    const pager = q('[data-search-page-pager]')
    const filters = q('[data-search-page-filters]')
    const resultTemplate = q<HTMLTemplateElement>(
        'template[data-search-page-result]'
    )
    const facetTemplate = q<HTMLTemplateElement>(
        'template[data-search-page-facet]'
    )

    let state = parseState(location.search)
    let controller: AbortController | null = null

    async function run() {
        query.value = state.query
        sort.value = state.sort
        history.replaceState(null, '', `?${toSearch(state)}`)
        controller?.abort()
        if (!state.query) {
            status.textContent = 'Type a search term to begin.'
            list.replaceChildren()
            pager.hidden = true
            filters.hidden = true
            return
        }
        controller = new AbortController()
        status.textContent = 'Searching…'
        try {
            if (config.buildType === 'isolated') {
                const results = await querySearch(
                    state.query,
                    controller.signal
                )
                render({
                    results,
                    totalResults: results.length,
                    pageCount: 1,
                    pageNumber: 1,
                })
                return
            }
            const response = await fetch(
                `${config.apiBasePath}/v1/search?${toSearch(state, PAGE_SIZE)}`,
                { signal: controller.signal }
            )
            if (!response.ok) throw new Error(String(response.status))
            render((await response.json()) as FullSearchResponse)
        } catch (error) {
            if ((error as Error).name === 'AbortError') return
            list.replaceChildren()
            pager.hidden = true
            status.textContent = 'Search is unavailable right now.'
        }
    }

    function render(response: FullSearchResponse) {
        status.textContent =
            response.totalResults === 0
                ? 'No results.'
                : `${response.totalResults} ${response.totalResults === 1 ? 'result' : 'results'}.`
        list.replaceChildren(
            ...response.results.map((result) => {
                const item = (
                    resultTemplate.content.cloneNode(true) as DocumentFragment
                ).querySelector<HTMLElement>('li')!
                item.querySelector('[data-search-page-path]')!.textContent =
                    result.parents.map((p) => p.title).join(' / ')
                const link = item.querySelector<HTMLAnchorElement>(
                    '[data-search-page-title]'
                )!
                link.href = result.url
                link.textContent = result.title
                appendExcerpt(
                    item.querySelector<HTMLElement>(
                        '[data-search-page-excerpt]'
                    )!,
                    result.description ?? ''
                )
                return item
            })
        )
        renderPager(response)
        renderFacets(response.aggregations)
    }

    function renderPager(response: FullSearchResponse) {
        pager.hidden = response.pageCount <= 1
        q('[data-search-page-page]').textContent =
            `Page ${response.pageNumber} of ${response.pageCount}`
        q<HTMLButtonElement>('[data-search-page-prev]').disabled =
            response.pageNumber <= 1
        q<HTMLButtonElement>('[data-search-page-next]').disabled =
            response.pageNumber >= response.pageCount
    }

    function renderFacets(aggregations: FullSearchResponse['aggregations']) {
        const groups: Array<{
            key: 'type' | 'section' | 'product'
            title: string
            options: Array<{ value: string; label: string; count: number }>
        }> = [
            {
                key: 'type',
                title: 'Type',
                options: Object.entries(aggregations?.type ?? {}).map(
                    ([value, count]) => ({ value, label: value, count })
                ),
            },
            {
                key: 'section',
                title: 'Section',
                options: Object.entries(
                    aggregations?.navigationSection ?? {}
                ).map(([value, count]) => ({ value, label: value, count })),
            },
            {
                key: 'product',
                title: 'Product',
                options: Object.entries(aggregations?.product ?? {}).map(
                    ([value, bucket]) => ({
                        value,
                        label: bucket.displayName,
                        count: bucket.count,
                    })
                ),
            },
        ]

        const fieldsets = groups
            .filter((group) => group.options.length > 0)
            .map((group) => {
                const fieldset = (
                    facetTemplate.content.cloneNode(true) as DocumentFragment
                ).querySelector<HTMLElement>('fieldset')!
                fieldset.querySelector('legend')!.textContent = group.title
                const options = fieldset.querySelector(
                    '.search-facet__options'
                )!
                group.options.forEach((option, index) => {
                    const id = `${root.id || 'search-page'}-${group.key}-${index}`
                    const label = document.createElement('label')
                    label.className = 'search-facet__option'
                    label.htmlFor = id
                    const input = document.createElement('input')
                    input.type = 'checkbox'
                    input.id = id
                    input.value = option.value
                    input.checked = state.filters[group.key].includes(
                        option.value
                    )
                    input.addEventListener('change', () => {
                        const values = new Set(state.filters[group.key])
                        if (input.checked) values.add(option.value)
                        else values.delete(option.value)
                        state = {
                            ...state,
                            page: 1,
                            filters: {
                                ...state.filters,
                                [group.key]: [...values],
                            },
                        }
                        void run()
                    })
                    const text = document.createElement('span')
                    text.textContent = option.label
                    const count = document.createElement('span')
                    count.className = 'search-facet__count'
                    count.textContent = String(option.count)
                    label.append(input, text, count)
                    options.append(label)
                })
                return fieldset
            })
        filters.replaceChildren(...fieldsets)
        filters.hidden = fieldsets.length === 0
    }

    form.addEventListener('submit', (event) => {
        event.preventDefault()
        state = { ...state, query: query.value.trim(), page: 1 }
        void run()
    })
    sort.addEventListener('change', () => {
        state = { ...state, sort: sort.value, page: 1 }
        void run()
    })
    q('[data-search-page-prev]').addEventListener('click', () => {
        state = { ...state, page: Math.max(1, state.page - 1) }
        void run()
    })
    q('[data-search-page-next]').addEventListener('click', () => {
        state = { ...state, page: state.page + 1 }
        void run()
    })

    void run()
}
