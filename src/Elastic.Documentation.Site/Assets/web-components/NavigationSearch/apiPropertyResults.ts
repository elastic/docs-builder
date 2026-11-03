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
