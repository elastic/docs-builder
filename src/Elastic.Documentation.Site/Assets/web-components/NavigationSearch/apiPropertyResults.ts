import type { SearchResultItem } from './useNavigationSearchQuery'
import * as z from 'zod'

export const ApiPropertyResult = z.object({
    name: z.string(),
    path: z.string(),
    type: z.string(),
    description: z.string(),
    anchor: z.string(),
    location: z.enum(['path', 'query', 'header', 'request', 'response']),
    url: z.string(),
})

export type ApiPropertyResult = z.infer<typeof ApiPropertyResult>

export const LOCATION_LABELS: Record<ApiPropertyResult['location'], string> = {
    path: 'path',
    query: 'query',
    header: 'header',
    request: 'request body',
    response: 'response body',
}

export const ApiRoute = z.object({
    path: z.string(),
    methods: z.array(z.string()),
})

export type ApiRoute = z.infer<typeof ApiRoute>

const HTTP_METHODS = new Set([
    'get',
    'post',
    'put',
    'patch',
    'delete',
    'head',
    'options',
])

/** A path as a query word can name it: lower case, no leading slash, and a `{parameter}` stands as `*` so a word never matches a placeholder. */
const pathKey = (path: string) =>
    path
        .toLowerCase()
        .replace(/\{[^}]*\}/g, '*')
        .replace(/^\/+/, '')

const words = (searchTerm: string) =>
    searchTerm.trim().split(/\s+/).filter(Boolean)

/** `GET` right before `/_search` is a method; on its own, `get` or `options` may well be a field name. */
const isMethodBeforePath = (all: string[], i: number) =>
    HTTP_METHODS.has(all[i].toLowerCase()) &&
    (all[i + 1]?.startsWith('/') ?? false)

/** The parts of a query that can name a field, split on dots, in either word order; what a property row highlights. */
export const fieldSearchTerms = (searchTerm: string) => {
    const all = words(searchTerm)
    return Array.from(
        new Set(
            all
                .filter(
                    (word, i) =>
                        !word.startsWith('/') && !isMethodBeforePath(all, i)
                )
                .flatMap((word) => word.split('.'))
                .filter(Boolean)
        )
    )
}

/**
 * The query words that can name a path, as typed so a leading slash highlights, and the one route
 * to show for them: the route whose whole
 * path is a query word, else the first route in page order whose path contains one, else the
 * page's primary route, which no query word named and so highlights nothing. Routes arrive with
 * the page's primary route first, so that one wins among the partial matches.
 */
export const primaryRoute = (routes: ApiRoute[], searchTerm: string) => {
    const all = words(searchTerm)
    const pathWords = all
        .filter((_, i) => !isMethodBeforePath(all, i))
        .map((word) => ({ word, key: pathKey(word) }))
        .filter(({ key }) => key.length > 1)
    const keys = pathWords.map(({ key }) => key)
    const containing = routes.filter((route) =>
        keys.some((key) => pathKey(route.path).includes(key))
    )
    const named =
        containing.find((route) => keys.includes(pathKey(route.path))) ??
        containing[0]
    return named
        ? { tokens: pathWords.map(({ word }) => word), route: named }
        : { tokens: [], route: routes[0] }
}

export const GO_TO_LABELS = {
    page: 'Go to page',
    operation: 'Go to operation',
    property: 'Go to property',
} as const

export interface PropertyRow {
    property: ApiPropertyResult
    /** Position among every navigable row; the keyboard selection index. */
    index: number
}

export interface ResultGroup {
    result: SearchResultItem
    index: number
    /** Position among the results; what a click reports, for the operation and for each of its properties. */
    rank: number
    properties: PropertyRow[]
}

/** One box per result, with rows numbered in keyboard order: the operation, then its properties. */
export const groupSearchResults = (
    results: SearchResultItem[]
): ResultGroup[] => {
    let index = 0
    return results.map((result, rank) => {
        const group: ResultGroup = {
            result,
            index: index++,
            rank,
            properties: [],
        }
        const properties =
            result.type === 'api' ? (result.properties ?? []) : []
        group.properties = properties.map((property) => ({
            property,
            index: index++,
        }))
        return group
    })
}

export const navigableRowCount = (groups: ResultGroup[]) =>
    groups.reduce((count, group) => count + 1 + group.properties.length, 0)
