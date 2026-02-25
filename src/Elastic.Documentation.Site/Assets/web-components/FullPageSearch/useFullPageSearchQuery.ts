import { config } from '../../config'
import {
    ATTR_SEARCH_QUERY,
    ATTR_SEARCH_PAGE,
    ATTR_SEARCH_RESULTS_TOTAL,
    ATTR_SEARCH_RESULTS_COUNT,
    ATTR_SEARCH_PAGE_COUNT,
} from '../../telemetry/semconv'
import { traceSpan } from '../../telemetry/tracing'
import {
    createApiErrorFromResponse,
    shouldRetry,
} from '../shared/errorHandling'
import { ApiError } from '../shared/errorHandling'
import {
    useFullPageSearchQuery as useSearchQuery,
    usePage,
    usePageSize,
    useSortBy,
    useVersion,
    useFilters,
    useHasSearched,
} from './fullPageSearch.store'
import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { useDebounce } from '@uidotdev/usehooks'
import * as z from 'zod'

// Demo mode: Read fail parameter once at module load to ensure it persists
const getFailMode = () => {
    if (typeof window === 'undefined') return null
    const params = new URLSearchParams(window.location.search)
    return params.get('fail')
}
const FAIL_MODE = getFailMode()

const SearchResultParent = z.object({
    url: z.string(),
    title: z.string(),
})

const SearchProduct = z.object({
    id: z.string(),
    displayName: z.string(),
})

const SearchResultItem = z.object({
    type: z.string(),
    url: z.string(),
    title: z.string(),
    description: z.string(),
    score: z.number(),
    parents: z.array(SearchResultParent),
    aiShortSummary: z.string().nullable().optional(),
    aiRagOptimizedSummary: z.string().nullable().optional(),
    navigationSection: z.string().nullable().optional(),
    lastUpdated: z.string().nullable().optional(),
    product: SearchProduct.nullable().optional(),
    relatedProducts: z.array(SearchProduct).nullable().optional(),
})

export type SearchResultItem = z.infer<typeof SearchResultItem>

const ProductAggregationBucket = z.object({
    count: z.number(),
    displayName: z.string(),
})

export type ProductAggregationBucket = z.infer<typeof ProductAggregationBucket>

const SearchAggregations = z.object({
    type: z.record(z.string(), z.number()).optional(),
    navigationSection: z.record(z.string(), z.number()).optional(),
    deploymentType: z.record(z.string(), z.number()).optional(),
    product: z.record(z.string(), ProductAggregationBucket).optional(),
})

export type SearchAggregations = z.infer<typeof SearchAggregations>

const FullSearchResponse = z.object({
    results: z.array(SearchResultItem),
    totalResults: z.number(),
    pageCount: z.number(),
    pageNumber: z.number(),
    pageSize: z.number(),
    aggregations: SearchAggregations.optional(),
    isSemanticQuery: z.boolean(),
})

export type FullSearchResponse = z.infer<typeof FullSearchResponse>

// Semantic query detection (matches backend logic)
const SEMANTIC_KEYWORDS =
    /^(how|why|what|when|where|can|should|is it|do i|does|will|would|could)/i

export function isSemanticQuery(query: string): boolean {
    const trimmed = query.trim()
    const wordCount = trimmed.split(/\s+/).length
    return (
        SEMANTIC_KEYWORDS.test(trimmed) ||
        trimmed.endsWith('?') ||
        wordCount > 3
    )
}

export const useFullPageSearch = () => {
    const query = useSearchQuery()
    const page = usePage()
    const pageSize = usePageSize()
    const sortBy = useSortBy()
    const version = useVersion()
    const filters = useFilters()
    const hasSearched = useHasSearched()

    const debouncedQuery = useDebounce(query.trim(), 300)

    const shouldEnable =
        hasSearched && !!debouncedQuery && debouncedQuery.length >= 1

    return useQuery<FullSearchResponse, ApiError>({
        queryKey: [
            'full-search',
            {
                query: debouncedQuery.toLowerCase(),
                page,
                pageSize,
                sortBy,
                version,
                filters,
                failMode: FAIL_MODE,
            },
        ],
        queryFn: async ({ signal }) => {
            if (!debouncedQuery || debouncedQuery.length < 1) {
                return FullSearchResponse.parse({
                    results: [],
                    totalResults: 0,
                    pageCount: 0,
                    pageNumber: 1,
                    pageSize,
                    isSemanticQuery: false,
                })
            }

            // Demo mode: fail=slow simulates slow search results (3 second delay)
            if (FAIL_MODE === 'slow') {
                await new Promise((resolve) => setTimeout(resolve, 3000))
            }

            // Demo mode: fail=slow_error simulates slow search followed by server error
            if (FAIL_MODE === 'slow_error') {
                await new Promise((resolve) => setTimeout(resolve, 3000))
                const error = new Error(
                    'Service temporarily unavailable. Please try again later.'
                ) as ApiError
                error.name = 'ApiError'
                error.statusCode = 500
                throw error
            }

            return traceSpan('execute full search', async (span) => {
                span.setAttribute(ATTR_SEARCH_QUERY, debouncedQuery)
                span.setAttribute(ATTR_SEARCH_PAGE, page)

                const params = new URLSearchParams({
                    q: debouncedQuery,
                    page: page.toString(),
                    size: pageSize.toString(),
                    sort: sortBy,
                })

                // Add version filter if not default
                if (version !== '9.0+') {
                    params.set('version', version)
                }

                // Add type filters
                filters.type.forEach((t) => params.append('type', t))

                // Add section filters
                filters.navigationSection.forEach((s) =>
                    params.append('section', s)
                )

                // Add deployment filters
                filters.deploymentType.forEach((d) =>
                    params.append('deployment', d)
                )

                // Add product filters
                filters.product.forEach((p) => params.append('product', p))

                const response = await fetch(
                    `${config.apiBasePath}/search?` + params.toString(),
                    { signal }
                )

                if (!response.ok) {
                    throw await createApiErrorFromResponse(response)
                }

                const data = await response.json()
                const searchResponse = FullSearchResponse.parse(data)

                span.setAttribute(
                    ATTR_SEARCH_RESULTS_TOTAL,
                    searchResponse.totalResults
                )
                span.setAttribute(
                    ATTR_SEARCH_RESULTS_COUNT,
                    searchResponse.results.length
                )
                span.setAttribute(
                    ATTR_SEARCH_PAGE_COUNT,
                    searchResponse.pageCount
                )

                return searchResponse
            })
        },
        enabled: shouldEnable,
        refetchOnWindowFocus: false,
        placeholderData: keepPreviousData,
        staleTime: 1000 * 60 * 5, // 5 minutes
        retry: shouldRetry,
    })
}
