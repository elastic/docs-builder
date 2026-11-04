import { SearchResponse } from './useNavigationSearchQuery'

const parent = { url: '/docs', title: 'Docs' }

describe('SearchResponse', () => {
    it('parses a mixed docs and api navigation-search response', () => {
        const parsed = SearchResponse.parse({
            results: [
                {
                    type: 'docs',
                    url: '/docs/elasticsearch/guide',
                    title: 'Elasticsearch Guide',
                    description: 'Learn about Elasticsearch',
                    score: 0.95,
                    parents: [parent],
                },
                {
                    type: 'api',
                    url: '/docs/api/elasticsearch/_bulk',
                    title: 'Bulk API',
                    description: 'Index multiple documents',
                    score: 0.9,
                    parents: [
                        { url: '/docs/api', title: 'API Reference' },
                        {
                            url: '/docs/api/elasticsearch',
                            title: 'Elasticsearch',
                        },
                    ],
                },
            ],
            totalResults: 2,
            pageCount: 1,
            pageNumber: 1,
            pageSize: 20,
        })

        expect(parsed.results.map((item) => item.type)).toEqual(['docs', 'api'])
    })
})
