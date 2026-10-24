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

export type NavigableResult =
    | { kind: 'operation'; result: SearchResultItem }
    | {
          kind: 'property'
          result: SearchResultItem
          property: ApiPropertyResult
      }

export const navigableSearchResults = (
    results: SearchResultItem[]
): NavigableResult[] =>
    results.flatMap((result): NavigableResult[] => [
        { kind: 'operation', result },
        ...(result.type === 'api' ? (result.properties ?? []) : []).map(
            (property): NavigableResult => ({
                kind: 'property',
                result,
                property,
            })
        ),
    ])
