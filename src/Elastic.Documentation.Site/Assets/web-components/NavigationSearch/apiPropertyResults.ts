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
    /** What extends `path` to each direct child's path: `.query`, `[].query`, `.<string>.terms`. */
    children: z.array(z.string()).default([]),
})

export type ApiPropertyResult = z.infer<typeof ApiPropertyResult>

export const childLabel = (suffix: string) => suffix.replace(/^\./, '')

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
 * The query words that can name a path, and the one route to show for them: the route whose whole
 * path is a query word, else the first route in page order whose path contains one. Routes arrive
 * with the page's primary route first, so that one wins among the partial matches.
 */
export const primaryRoute = (routes: ApiRoute[], searchTerm: string) => {
    const all = words(searchTerm)
    const tokens = all
        .filter((_, i) => !isMethodBeforePath(all, i))
        .map(pathKey)
        .filter((word) => word.length > 1)
    const containing = routes.filter((route) =>
        tokens.some((token) => pathKey(route.path).includes(token))
    )
    return {
        tokens,
        route:
            containing.find((route) => tokens.includes(pathKey(route.path))) ??
            containing[0],
    }
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
    /** Set when the same path appears in two locations, so the row says which one it is. */
    showLocation: boolean
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
            showLocation: properties.some(
                (other) =>
                    other.path === property.path &&
                    other.location !== property.location
            ),
        }))
        return group
    })
}

export const navigableRowCount = (groups: ResultGroup[]) =>
    groups.reduce((count, group) => count + 1 + group.properties.length, 0)
