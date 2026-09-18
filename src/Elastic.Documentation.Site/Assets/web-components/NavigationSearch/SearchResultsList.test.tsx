import {
    emptyStateCopy,
    navigationSearchBreadcrumbs,
} from './SearchResultsList'

describe('navigationSearchBreadcrumbs', () => {
    it('uses every parent for api rows without a Docs prefix', () => {
        expect(
            navigationSearchBreadcrumbs(
                [
                    { url: '/docs/api', title: 'API Reference' },
                    { url: '/docs/api/elasticsearch', title: 'Elasticsearch' },
                ],
                'api',
                'assembler'
            )
        ).toEqual(['API Reference', 'Elasticsearch'])
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
