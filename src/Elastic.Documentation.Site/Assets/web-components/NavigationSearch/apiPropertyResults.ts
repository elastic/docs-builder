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

const pathKey = (path: string) => path.toLowerCase().replace(/^\/+/, '')

/**
 * The query words that can name a path, and the one route to show for them: the route whose whole
 * path is a query word, else the first route in page order whose path contains one. Routes arrive
 * with the page's primary route first, so that one wins among the partial matches.
 */
export const primaryRoute = (routes: ApiRoute[], searchTerm: string) => {
    const tokens = searchTerm
        .toLowerCase()
        .split(/\s+/)
        .map(pathKey)
        .filter((word) => word.length > 1 && !HTTP_METHODS.has(word))
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
    properties: PropertyRow[]
}

/** One box per result, with rows numbered in keyboard order: the operation, then its properties. */
export const groupSearchResults = (
    results: SearchResultItem[]
): ResultGroup[] => {
    let index = 0
    return results.map((result) => {
        const group: ResultGroup = { result, index: index++, properties: [] }
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
