import {
    emptyStateCopy,
    formatApiProductCrumb,
    navigationSearchBreadcrumbs,
    navigationSearchResultTitle,
} from './SearchResultsList'

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

describe('navigationSearchResultTitle', () => {
    it('strips the product suffix from API row titles', () => {
        expect(
            navigationSearchResultTitle(
                'Bulk index or delete documents - Elasticsearch API',
                'api'
            )
        ).toBe('Bulk index or delete documents')
        expect(
            navigationSearchResultTitle('Search dashboards - Kibana API', 'api')
        ).toBe('Search dashboards')
    })

    it('leaves docs titles alone', () => {
        expect(
            navigationSearchResultTitle('Search - Elasticsearch API', 'all')
        ).toBe('Search - Elasticsearch API')
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
