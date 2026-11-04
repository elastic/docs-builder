import { renderWithProviders } from '../../test-utils/render'
import {
    SearchResultsList,
    emptyStateCopy,
    formatApiProductCrumb,
    navigationSearchBreadcrumbs,
} from './SearchResultsList'
import { groupSearchResults, type ResultGroup } from './apiPropertyResults'
import { navigationSearchStore } from './navigationSearch.store'
import type { SearchResultItem, TypeFilter } from './useNavigationSearchQuery'
import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import htmx from 'htmx.org'

vi.mock('../../telemetry/logging', async () =>
    (await import('../../test-utils/mocks')).logging()
)

describe('navigationSearchBreadcrumbs', () => {
    it('uses API plus the product label, not the product key', () => {
        expect(
            navigationSearchBreadcrumbs(
                [
                    { url: '/docs/api', title: 'API Reference' },
                    { url: '/docs/api/elasticsearch', title: 'elasticsearch' },
                ],
                'api',
                'assembler'
            )
        ).toEqual(['API', 'Elasticsearch API'])
    })

    it('keeps an already labelled product crumb', () => {
        expect(
            navigationSearchBreadcrumbs(
                [
                    { url: '/docs/api', title: 'API' },
                    {
                        url: '/docs/api/kibana',
                        title: 'Kibana API',
                    },
                ],
                'api',
                'assembler'
            )
        ).toEqual(['API', 'Kibana API'])
    })

    it('keeps the group crumb after the product as written', () => {
        expect(
            navigationSearchBreadcrumbs(
                [
                    { url: '/docs/api', title: 'API' },
                    {
                        url: '/docs/api/doc/elasticsearch',
                        title: 'elasticsearch',
                    },
                    {
                        url: '/docs/api/doc/elasticsearch/group/endpoint-ml-anomaly',
                        title: 'Machine learning anomaly detection',
                    },
                ],
                'api',
                'assembler'
            )
        ).toEqual([
            'API',
            'Elasticsearch API',
            'Machine learning anomaly detection',
        ])
    })

    it('returns only API when the product crumb is missing', () => {
        expect(
            navigationSearchBreadcrumbs(
                [{ url: '/docs/api', title: 'API' }],
                'api',
                'assembler'
            )
        ).toEqual(['API'])
    })

    it('keeps the Docs prefix and drops the first parent when type is omitted', () => {
        expect(
            navigationSearchBreadcrumbs(
                [
                    { url: '/docs', title: 'Docs' },
                    { url: '/docs/elasticsearch', title: 'Elasticsearch' },
                ],
                'all',
                'assembler'
            )
        ).toEqual(['Docs', 'Elasticsearch'])
    })
})

describe('formatApiProductCrumb', () => {
    it('turns a product key into a display label', () => {
        expect(formatApiProductCrumb('elasticsearch')).toBe('Elasticsearch API')
        expect(formatApiProductCrumb('kibana')).toBe('Kibana API')
    })
})

describe('emptyStateCopy', () => {
    it('does not say it could not find a page for an API empty state', () => {
        expect(emptyStateCopy('api')).toBe(
            "We couldn't find an API that matches your search"
        )
        expect(emptyStateCopy('api')).not.toContain('page')
        expect(emptyStateCopy('all')).toBe(
            "We couldn't find a page that matches your search"
        )
    })
})

const property = (
    path: string,
    location: 'query' | 'request',
    { parent = '' } = {}
) => ({
    name: path.split('.').pop() ?? path,
    path,
    type: 'boolean',
    location,
    description: 'Count hits',
    anchor: `${location}-${path}`,
    url: `/docs/api/doc/elasticsearch/operation/operation-search#${location}-${path}`,
    parent,
})

const operation: SearchResultItem = {
    type: 'api',
    url: '/docs/api/doc/elasticsearch/operation/operation-search',
    title: 'Search',
    description: 'Run a search',
    score: 5,
    parents: [],
    properties: [
        property('track_total_hits', 'query'),
        property('track_total_hits', 'request'),
        property('size', 'request'),
    ],
}

const page: SearchResultItem = {
    type: 'docs',
    url: '/docs/elasticsearch',
    title: 'Elasticsearch',
    description: 'Learn about Elasticsearch',
    score: 1,
    parents: [],
}

const renderList = ({
    groups = groupSearchResults([operation]),
    isKeyboardNavigating = false,
    typeFilter = 'api',
}: {
    groups?: ResultGroup[]
    isKeyboardNavigating?: boolean
    typeFilter?: TypeFilter
} = {}) => {
    const onMouseMove = vi.fn()
    const { container } = renderWithProviders(
        <SearchResultsList
            results={groups}
            isLoading={false}
            isKeyboardNavigating={{ current: isKeyboardNavigating }}
            onMouseMove={onMouseMove}
            onResultClick={vi.fn()}
            typeFilter={typeFilter}
        />
    )
    return { onMouseMove, container }
}

describe('property rows', () => {
    beforeEach(() => {
        navigationSearchStore.getState().actions.setSearchTerm('track_total')
    })

    it('shows the primary route a query named instead of the description', () => {
        const withRoutes: SearchResultItem = {
            ...operation,
            description: 'Returns search hits',
            routes: [
                { path: '/{index}/_search', methods: ['POST', 'GET'] },
                { path: '/_search', methods: ['POST', 'GET'] },
            ],
        }
        navigationSearchStore.getState().actions.setSearchTerm('search')
        const { container } = renderList({
            groups: groupSearchResults([withRoutes]),
        })
        const lines = container.querySelectorAll('.route-line')
        expect(lines).toHaveLength(1)
        expect(lines[0]).toHaveTextContent('POST')
        expect(lines[0]).not.toHaveTextContent('GET')
        expect(lines[0]).toHaveTextContent('/{index}/_search')
        expect(lines[0].querySelector('mark')).toHaveTextContent('search')
        expect(screen.queryByText('Returns search hits')).toBeNull()
    })

    it('shows the route a query names in full over the primary route', () => {
        const withRoutes: SearchResultItem = {
            ...operation,
            routes: [
                { path: '/{index}/_search', methods: ['POST', 'GET'] },
                { path: '/_search', methods: ['POST', 'GET'] },
            ],
        }
        navigationSearchStore.getState().actions.setSearchTerm('/_search')
        const { container } = renderList({
            groups: groupSearchResults([withRoutes]),
        })
        expect(container.querySelector('.route-line')).toHaveTextContent(
            'POST/_search'
        )
    })

    it('shows the primary route when the query names no route', () => {
        const withRoutes: SearchResultItem = {
            ...operation,
            description: 'Returns search hits',
            routes: [{ path: '/_search', methods: ['POST', 'GET'] }],
        }
        navigationSearchStore.getState().actions.setSearchTerm('knn')
        const { container } = renderList({
            groups: groupSearchResults([withRoutes]),
        })
        expect(container.querySelector('.route-line')).toHaveTextContent(
            '/_search'
        )
        expect(screen.queryByText('Returns search hits')).toBeNull()
    })

    it('keeps the description for a row without routes', () => {
        navigationSearchStore.getState().actions.setSearchTerm('knn')
        const { container } = renderList({
            groups: groupSearchResults([{ ...operation, routes: [] }, page]),
            typeFilter: 'all',
        })
        expect(container.querySelector('.route-line')).toBeNull()
        expect(screen.getByText('Run a search')).toBeInTheDocument()
        expect(
            screen.getByText('Learn about Elasticsearch')
        ).toBeInTheDocument()
    })

    it('names the Enter action after each row destination', () => {
        renderList({
            groups: groupSearchResults([operation, page]),
            typeFilter: 'all',
        })
        expect(screen.getAllByText('Go to operation')).toHaveLength(1)
        expect(screen.getAllByText('Go to property')).toHaveLength(3)
        expect(screen.getAllByText('Go to page')).toHaveLength(1)
    })

    it('shows the location on every property row', () => {
        const { container } = renderList()
        expect(
            Array.from(
                container.querySelectorAll('[data-search-result-index]'),
                (row) => row.querySelector('.property-location')?.textContent
            )
        ).toEqual([undefined, 'query', 'request body', 'request body'])
    })

    it('boosts the rendered rows once per render', () => {
        vi.mocked(htmx.process).mockClear()
        const { container } = renderList()
        const list = container.querySelector('[data-search-result-index]')
            ?.parentElement?.parentElement
        expect(htmx.process).toHaveBeenCalledTimes(1)
        expect(htmx.process).toHaveBeenCalledWith(list)
    })

    it('links each row to the URL as given', () => {
        const { container } = renderList()
        expect(
            Array.from(
                container.querySelectorAll('[data-search-result-index]'),
                (row) => row.getAttribute('href')
            )
        ).toEqual([
            operation.url,
            `${operation.url}#query-track_total_hits`,
            `${operation.url}#request-track_total_hits`,
            `${operation.url}#request-size`,
        ])
    })

    it('selects the property row under the mouse', async () => {
        renderList()
        const row = screen.getByRole('link', {
            name: 'size boolean request Count hits',
        })
        await userEvent.hover(row)
        expect(navigationSearchStore.getState().selectedIndex).toBe(3)
        expect(row).toHaveAttribute('data-selected', 'true')
    })

    it('hands a mouse move back to the parent while the keyboard is driving', async () => {
        const { onMouseMove } = renderList({ isKeyboardNavigating: true })
        const row = screen.getByRole('link', {
            name: 'size boolean request Count hits',
        })
        await userEvent.hover(row)
        expect(onMouseMove).toHaveBeenCalled()
        expect(navigationSearchStore.getState().selectedIndex).toBe(3)
    })
})
