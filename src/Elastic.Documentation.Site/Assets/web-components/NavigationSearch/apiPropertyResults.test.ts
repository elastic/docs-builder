import {
    goToLabel,
    navigableSearchResults,
    type ApiPropertyResult,
    type NavigableResult,
} from './apiPropertyResults'
import type { SearchResultItem } from './useNavigationSearchQuery'

const property: ApiPropertyResult = {
    name: 'knn',
    path: 'knn',
    type: 'object',
    description: 'Nearest neighbours',
    anchor: 'req-knn',
    location: 'request',
    url: '/docs/api/doc/elasticsearch/operation/operation-search#req-knn',
}

const operation: SearchResultItem = {
    type: 'api',
    url: '/docs/api/doc/elasticsearch/operation/operation-search',
    title: 'Search',
    description: 'Search documents',
    score: 5,
    parents: [],
    properties: [property],
}

const page: SearchResultItem = {
    type: 'docs',
    url: '/docs/elasticsearch',
    title: 'Elasticsearch',
    description: 'Learn about Elasticsearch',
    score: 1,
    parents: [],
}

const cases: Array<[string, NavigableResult, string]> = [
    [
        'an API operation',
        { kind: 'operation', result: operation },
        'Go to operation',
    ],
    ['a docs page', { kind: 'operation', result: page }, 'Go to page'],
    [
        'a property',
        { kind: 'property', result: operation, property },
        'Go to property',
    ],
]

it.each(cases)('labels the Enter action for %s', (_, entry, label) => {
    expect(goToLabel(entry)).toBe(label)
})

it('labels the operation and each of its properties in order', () => {
    expect(navigableSearchResults([operation, page]).map(goToLabel)).toEqual([
        'Go to operation',
        'Go to property',
        'Go to page',
    ])
})
