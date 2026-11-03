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

const property = (path: string, location: 'query' | 'request') => ({
    name: path.split('.').pop() ?? path,
    path,
    type: 'boolean',
    location,
    description: 'Count hits',
    anchor: `${location}-${path}`,
    url: `/docs/api/doc/elasticsearch/operation/operation-search#${location}-${path}`,
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
    renderWithProviders(
        <SearchResultsList
            results={groups}
            isLoading={false}
            isKeyboardNavigating={{ current: isKeyboardNavigating }}
            onMouseMove={onMouseMove}
            onResultClick={vi.fn()}
            typeFilter={typeFilter}
        />
    )
    return { onMouseMove }
}

describe('property rows', () => {
    beforeEach(() => {
        navigationSearchStore.getState().actions.setSearchTerm('track_total')
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

    it('shows the location only when the same path appears in two locations', () => {
        renderList()
        const query = screen.getByRole('link', {
            name: 'track_total_hits boolean query Count hits',
        })
        const request = screen.getByRole('link', {
            name: 'track_total_hits boolean request Count hits',
        })
        const size = screen.getByRole('link', {
            name: 'size boolean request Count hits',
        })
        expect(query.querySelector('.property-location')).toHaveTextContent(
            'query'
        )
        expect(request.querySelector('.property-location')).toHaveTextContent(
            'request'
        )
        expect(size.querySelector('.property-location')).toBeNull()
        expect(size.querySelector('.sr-only')).toHaveTextContent('request')
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
