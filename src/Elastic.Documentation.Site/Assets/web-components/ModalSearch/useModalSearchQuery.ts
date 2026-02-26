import { config } from '../../config'
import { logInfo, logWarn } from '../../telemetry/logging'
import {
    ATTR_NAVIGATION_SEARCH_QUERY,
    ATTR_NAVIGATION_SEARCH_QUERY_LENGTH,
    ATTR_NAVIGATION_SEARCH_RESULTS_TOTAL,
    ATTR_NAVIGATION_SEARCH_RETRY_AFTER,
    ATTR_ERROR_TYPE,
} from '../../telemetry/semconv'
import { traceSpan } from '../../telemetry/tracing'
import {
    useIsNavigationSearchAwaitingNewInput,
    useNavigationSearchCooldownActions,
    useIsNavigationSearchCooldownActive,
} from '../NavigationSearch/useNavigationSearchCooldown'
import {
    createApiErrorFromResponse,
    shouldRetry,
    isApiError,
    isRateLimitError,
} from '../shared/errorHandling'
import { ApiError } from '../shared/errorHandling'
import { usePageNumber, useSearchTerm } from './modalSearch.store'
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
    type: z.enum(['doc', 'api']),
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

const SearchResponse = z.object({
    results: z.array(SearchResultItem),
    totalResults: z.number(),
    pageCount: z.number(),
    pageNumber: z.number(),
    pageSize: z.number(),
    aggregations: SearchAggregations.optional(),
})

export type SearchResponse = z.infer<typeof SearchResponse>

export const useModalSearchQuery = () => {
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

    const query = useQuery<SearchResponse, ApiError>({
        queryKey: [
            'modal-search',
            {
                searchTerm: debouncedSearchTerm.toLowerCase(),
                pageNumber,
            },
        ],
        queryFn: async ({ signal }) => {
            if (!debouncedSearchTerm || debouncedSearchTerm.length < 1) {
                return SearchResponse.parse({
                    results: [],
                    totalResults: 0,
                    pageCount: 0,
                    pageNumber: pageNumber,
                    pageSize: 20,
                })
            }

            return traceSpan('modal_search', async (span) => {
                span.setAttribute(
                    ATTR_NAVIGATION_SEARCH_QUERY,
                    debouncedSearchTerm
                )
                span.setAttribute('modal_search.page', pageNumber)

                const params = new URLSearchParams({
                    q: debouncedSearchTerm,
                    page: pageNumber.toString(),
                })

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

                span.setAttribute(
                    ATTR_NAVIGATION_SEARCH_RESULTS_TOTAL,
                    searchResponse.totalResults
                )
                span.setAttribute(
                    'modal_search.results.count',
                    searchResponse.results.length
                )
                span.setAttribute(
                    'modal_search.page.count',
                    searchResponse.pageCount
                )

                if (searchResponse.totalResults === 0) {
                    logInfo('modal_search_zero_results', {
                        [ATTR_NAVIGATION_SEARCH_QUERY]: debouncedSearchTerm,
                        [ATTR_NAVIGATION_SEARCH_QUERY_LENGTH]:
                            debouncedSearchTerm.length,
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
                'modal-search',
                {
                    searchTerm: debouncedSearchTerm.toLowerCase(),
                    pageNumber,
                },
            ],
        })
    }, [queryClient, debouncedSearchTerm, pageNumber])

    useEffect(() => {
        if (query.error && isApiError(query.error)) {
            if (isRateLimitError(query.error)) {
                logWarn('modal_search_rate_limited', {
                    [ATTR_NAVIGATION_SEARCH_QUERY]: debouncedSearchTerm,
                    [ATTR_NAVIGATION_SEARCH_RETRY_AFTER]:
                        query.error.retryAfter ?? 0,
                })
            } else {
                logWarn('modal_search_error', {
                    [ATTR_NAVIGATION_SEARCH_QUERY]: debouncedSearchTerm,
                    [ATTR_ERROR_TYPE]: `${query.error.statusCode}`,
                    'error.message': query.error.message,
                })
            }
        }
    }, [query.error, debouncedSearchTerm])

    return {
        ...query,
        cancelQuery,
    }
}
