import {
    LOCATION_LABELS,
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

it.each([
    ['path', 'path'],
    ['query', 'query'],
    ['header', 'header'],
    ['request', 'request body'],
    ['response', 'response body'],
] as const)('labels the %s location as %s', (location, label) => {
    expect(LOCATION_LABELS[location]).toBe(label)
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
    ['index', [], '/{index}/_search'],
    ['in', [], '/{index}/_search'],
    ['scroll', ['scroll'], '/_search/scroll'],
    ['knn', [], '/{index}/_search'],
    ['get', [], '/{index}/_search'],
    ['GET /_search', ['_search'], '/_search'],
    ['a', [], '/{index}/_search'],
])('picks the route for %s', (searchTerm, tokens, path) => {
    const matched = primaryRoute(routes, searchTerm)
    expect(matched.tokens).toEqual(tokens)
    expect(matched.route?.path).toBe(path)
})

it("falls back to the page's primary route when no query word names one", () => {
    const primary = { path: '/_search', methods: ['POST', 'GET'] }
    const matched = primaryRoute(
        [primary, { path: '/{index}/_search', methods: ['POST', 'GET'] }],
        'retriever'
    )
    expect(matched.route).toBe(primary)
    expect(matched.tokens).toEqual([])
})

it('highlights nothing on the fallback route and the query words on a named one', () => {
    const fallback = primaryRoute(
        [
            { path: '/{index}/_search', methods: ['POST'] },
            { path: '/_search', methods: ['POST'] },
        ],
        'index'
    )
    expect(fallback).toEqual({
        route: { path: '/{index}/_search', methods: ['POST'] },
        tokens: [],
    })
    expect(primaryRoute(routes, 'scroll').tokens).toEqual(['scroll'])
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
