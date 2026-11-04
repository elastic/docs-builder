import {
    childLabel,
    fieldSearchTerms,
    groupSearchResults,
    navigableRowCount,
    primaryRoute,
} from './apiPropertyResults'
import type { SearchResultItem } from './useNavigationSearchQuery'

const property = (path: string, location: 'query' | 'request') => ({
    name: path.split('.').pop() ?? path,
    path,
    type: 'boolean',
    description: 'Count hits',
    anchor: `${location}-${path}`,
    location,
    url: `/docs/api/doc/elasticsearch/operation/operation-search#${location}-${path}`,
    children: [],
})

const search: SearchResultItem = {
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

it('numbers rows in keyboard order: each operation, then its properties', () => {
    const groups = groupSearchResults([search, page])
    expect(
        groups.map((group) => [
            group.index,
            group.properties.map((row) => row.index),
        ])
    ).toEqual([
        [0, [1, 2, 3]],
        [4, []],
    ])
    expect(groups.map((group) => group.rank)).toEqual([0, 1])
    expect(navigableRowCount(groups)).toBe(5)
})

it('shows a location only when the same path appears in two locations', () => {
    const [group] = groupSearchResults([search])
    expect(
        group.properties.map((row) => [
            row.property.path,
            row.property.location,
            row.showLocation,
        ])
    ).toEqual([
        ['track_total_hits', 'query', true],
        ['track_total_hits', 'request', true],
        ['size', 'request', false],
    ])
})

it('lists no properties under a docs page even when the payload carries some', () => {
    const [group] = groupSearchResults([
        { ...page, properties: [property('size', 'request')] },
    ])
    expect(group.properties).toEqual([])
    expect(navigableRowCount(groupSearchResults([page]))).toBe(1)
})

const routes = [
    { path: '/{index}/_search', methods: ['POST', 'GET'] },
    { path: '/_search', methods: ['POST', 'GET'] },
    { path: '/_search/scroll', methods: ['POST', 'GET'] },
]

it.each([
    ['_search', ['_search'], '/_search'],
    ['/_search', ['_search'], '/_search'],
    ['GET /_search knn', ['_search', 'knn'], '/_search'],
    ['search', ['search'], '/{index}/_search'],
    ['index', ['index'], undefined],
    ['in', ['in'], undefined],
    ['scroll', ['scroll'], '/_search/scroll'],
    ['knn', ['knn'], undefined],
    ['get', ['get'], undefined],
    ['GET /_search', ['_search'], '/_search'],
    ['a', [], undefined],
])('picks the route for %s', (searchTerm, tokens, path) => {
    const matched = primaryRoute(routes, searchTerm)
    expect(matched.tokens).toEqual(tokens)
    expect(matched.route?.path).toBe(path)
})

it.each([
    ['knn search', ['knn', 'search']],
    ['search knn', ['search', 'knn']],
    ['knn /_search', ['knn']],
    ['GET /_search knn.field', ['knn', 'field']],
    ['_search knn', ['_search', 'knn']],
    ['/_search', []],
    ['options', ['options']],
    ['_bulk delete', ['_bulk', 'delete']],
    ['get knn', ['get', 'knn']],
])('takes the field terms of %s', (searchTerm, terms) => {
    expect(fieldSearchTerms(searchTerm)).toEqual(terms)
})

it.each([
    ['.query', 'query'],
    ['[].query', '[].query'],
    ['.<string>.terms', '<string>.terms'],
])('labels the child suffix %s as %s', (suffix, label) => {
    expect(childLabel(suffix)).toBe(label)
})
