import { config } from '../../config'
import { logError, logInfo, logWarn } from '../../telemetry/logging'
import {
    ATTR_NAVIGATION_SEARCH_QUERY,
    ATTR_NAVIGATION_SEARCH_QUERY_LENGTH,
    ATTR_NAVIGATION_SEARCH_RESULTS_TOTAL,
    ATTR_NAVIGATION_SEARCH_RETRY_AFTER,
    ATTR_ERROR_TYPE,
} from '../../telemetry/semconv'
import { traceSpan } from '../../telemetry/tracing'
import {
    createApiErrorFromResponse,
    shouldRetry,
    isApiError,
    isRateLimitError,
} from '../shared/errorHandling'
import { ApiError } from '../shared/errorHandling'
import { usePageNumber, useSearchTerm } from './navigationSearch.store'
import { parseApiVersionScope } from './parseApiVersionScope'
import {
    useIsNavigationSearchAwaitingNewInput,
    useNavigationSearchCooldownActions,
    useIsNavigationSearchCooldownActive,
} from './useNavigationSearchCooldown'
import {
    keepPreviousData,
    useQuery,
    useQueryClient,
} from '@tanstack/react-query'
import { useDebounce } from '@uidotdev/usehooks'
import { useRef, useEffect, useCallback } from 'react'
import * as z from 'zod'

const SearchResultItemParent = z.object({
    url: z.string(),
    title: z.string(),
})

const SearchResultItem = z.object({
    type: z.enum(['docs', 'api']),
    url: z.string(),
    title: z.string(),
    description: z.string(),
    score: z.number(),
    parents: z.array(SearchResultItemParent),
})

export type SearchResultItem = z.infer<typeof SearchResultItem>

const SearchAggregations = z.object({
    type: z.record(z.string(), z.number()).optional(),
})

export const SearchResponse = z.object({
    results: z.array(SearchResultItem),
    totalResults: z.number(),
    pageCount: z.number(),
    pageNumber: z.number(),
    pageSize: z.number(),
    aggregations: SearchAggregations.optional(),
})

export type SearchResponse = z.infer<typeof SearchResponse>

export type TypeFilter = 'all' | 'docs' | 'api'

export const useNavigationSearchQuery = (typeFilter: TypeFilter) => {
    const searchTerm = useSearchTerm()
    const pageNumber = usePageNumber() + 1
    const trimmedSearchTerm = searchTerm.trim()
    const debouncedSearchTerm = useDebounce(trimmedSearchTerm, 300)
    const isCooldownActive = useIsNavigationSearchCooldownActive()
    const awaitingNewInput = useIsNavigationSearchAwaitingNewInput()
    const { acknowledgeCooldownFinished } = useNavigationSearchCooldownActions()
    const previousSearchTermRef = useRef(debouncedSearchTerm)
    const queryClient = useQueryClient()

    useEffect(() => {
        if (previousSearchTermRef.current !== debouncedSearchTerm) {
            if (awaitingNewInput) {
                acknowledgeCooldownFinished()
            }
        }
        previousSearchTermRef.current = debouncedSearchTerm
    }, [debouncedSearchTerm, awaitingNewInput, acknowledgeCooldownFinished])

    const shouldEnable =
        !!trimmedSearchTerm &&
        trimmedSearchTerm.length >= 1 &&
        !isCooldownActive &&
        !awaitingNewInput

    const pathname =
        typeof window === 'undefined' ? '' : window.location.pathname
    const scoped =
        typeFilter === 'api'
            ? parseApiVersionScope(debouncedSearchTerm, pathname)
            : { query: debouncedSearchTerm, apiVersion: undefined }

    const query = useQuery<SearchResponse, ApiError>({
        queryKey: [
            'navigation-search',
            {
                searchTerm: scoped.query.toLowerCase(),
                pageNumber,
                typeFilter,
                apiVersion: scoped.apiVersion,
            },
        ],
        queryFn: async ({ signal }) => {
            // Return an empty page rather than letting keepPreviousData show stale results.
            if (!scoped.query) {
                return {
                    results: [],
                    totalResults: 0,
                    pageCount: 0,
                    pageNumber,
                    pageSize: 0,
                } satisfies SearchResponse
            }

            return traceSpan('navigation_search', async (span) => {
                // Track Navigation Search query (even if backend response is cached by CloudFront)
                span.setAttribute(ATTR_NAVIGATION_SEARCH_QUERY, scoped.query)
                span.setAttribute('navigation_search.page', pageNumber)

                const params = new URLSearchParams({
                    q: scoped.query,
                    page: pageNumber.toString(),
                })

                // Only add type filter if not 'all'
                if (typeFilter !== 'all') {
                    params.set('type', typeFilter)
                }

                if (scoped.apiVersion) {
                    params.set('api_version', scoped.apiVersion)
                }

                const response = await fetch(
                    `${config.apiBasePath}/v1/navigation-search?` +
                        params.toString(),
                    { signal }
                )
                if (!response.ok) {
                    throw await createApiErrorFromResponse(response)
                }
                const data = await response.json()
                const searchResponse = SearchResponse.parse(data)

                // Add result metrics to span
                span.setAttribute(
                    ATTR_NAVIGATION_SEARCH_RESULTS_TOTAL,
                    searchResponse.totalResults
                )
                span.setAttribute(
                    'navigation_search.results.count',
                    searchResponse.results.length
                )
                span.setAttribute(
                    'navigation_search.page.count',
                    searchResponse.pageCount
                )

                // Track zero results for quality analysis
                if (searchResponse.totalResults === 0) {
                    logInfo('navigation_search_zero_results', {
                        [ATTR_NAVIGATION_SEARCH_QUERY]: scoped.query,
                        [ATTR_NAVIGATION_SEARCH_QUERY_LENGTH]:
                            scoped.query.length,
                        [ATTR_NAVIGATION_SEARCH_RESULTS_TOTAL]: 0,
                    })
                }

                return searchResponse
            })
        },
        enabled: shouldEnable,
        refetchOnWindowFocus: false,
        refetchOnMount: !isCooldownActive,
        placeholderData: keepPreviousData,
        staleTime: 1000 * 60 * 5, // 5 minutes
        retry: shouldRetry,
    })

    const cancelQuery = useCallback(() => {
        queryClient.cancelQueries({
            queryKey: [
                'navigation-search',
                {
                    searchTerm: scoped.query.toLowerCase(),
                    pageNumber,
                    typeFilter,
                    apiVersion: scoped.apiVersion,
                },
            ],
        })
    }, [queryClient, scoped.query, scoped.apiVersion, pageNumber, typeFilter])

    // Track errors for observability
    useEffect(() => {
        if (query.error && isApiError(query.error)) {
            if (isRateLimitError(query.error)) {
                logWarn('navigation_search_rate_limited', {
                    [ATTR_NAVIGATION_SEARCH_QUERY]: scoped.query,
                    [ATTR_NAVIGATION_SEARCH_RETRY_AFTER]:
                        query.error.retryAfter ?? 0,
                })
            } else {
                logWarn('navigation_search_error', {
                    [ATTR_NAVIGATION_SEARCH_QUERY]: scoped.query,
                    [ATTR_ERROR_TYPE]: `${query.error.statusCode}`,
                    'error.message': query.error.message,
                })
            }
        } else if (query.error) {
            const err = query.error as Error
            logError('navigation_search_parse_error', {
                [ATTR_NAVIGATION_SEARCH_QUERY]: scoped.query,
                [ATTR_ERROR_TYPE]: err.name,
                'error.message': err.message,
            })
            console.error(
                '[navigation-search] failed to parse search response',
                err
            )
        }
    }, [query.error, scoped.query])

    return {
        ...query,
        cancelQuery,
    }
}
