import {
    ATTR_SEARCH_QUERY,
    ATTR_SEARCH_PAGE,
    ATTR_SEARCH_RESULTS_TOTAL,
    ATTR_SEARCH_RESULTS_COUNT,
    ATTR_SEARCH_PAGE_COUNT,
} from '../../../telemetry/semconv'
import { traceSpan } from '../../../telemetry/tracing'
import { createApiErrorFromResponse, shouldRetry } from '../errorHandling'
import { ApiError } from '../errorHandling'
import { usePageNumber, useSearchTerm } from './search.store'
import {
    useIsSearchAwaitingNewInput,
    useSearchCooldownActions,
    useIsSearchCooldownActive,
} from './useSearchCooldown'
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
    type: z.string().default('doc'),
    url: z.string(),
    title: z.string(),
    description: z.string(),
    score: z.number(),
    parents: z.array(SearchResultItemParent),
    highlightedBody: z.string().nullish(),
})

export type SearchResultItem = z.infer<typeof SearchResultItem>

const SearchResponse = z.object({
    results: z.array(SearchResultItem),
    totalResults: z.number(),
    pageCount: z.number(),
    pageNumber: z.number(),
    pageSize: z.number(),
})

export type SearchResponse = z.infer<typeof SearchResponse>

export const useSearchQuery = () => {
    const searchTerm = useSearchTerm()
    const pageNumber = usePageNumber() + 1
    const trimmedSearchTerm = searchTerm.trim()
    const debouncedSearchTerm = useDebounce(trimmedSearchTerm, 300)
    const isCooldownActive = useIsSearchCooldownActive()
    const awaitingNewInput = useIsSearchAwaitingNewInput()
    const { acknowledgeCooldownFinished } = useSearchCooldownActions()
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
            'search',
            { searchTerm: debouncedSearchTerm.toLowerCase(), pageNumber },
        ],
        queryFn: async ({ signal }) => {
            // Don't create span for empty searches
            if (!debouncedSearchTerm || debouncedSearchTerm.length < 1) {
                return SearchResponse.parse({
                    results: [],
                    totalResults: 0,
                })
            }

            return traceSpan('execute search', async (span) => {
                // Track frontend search (even if backend response is cached by CloudFront)
                span.setAttribute(ATTR_SEARCH_QUERY, debouncedSearchTerm)
                span.setAttribute(ATTR_SEARCH_PAGE, pageNumber)

                const params = new URLSearchParams({
                    q: debouncedSearchTerm,
                    page: pageNumber.toString(),
                })

                const response = await fetch(
                    '/docs/_api/v1/search?' + params.toString(),
                    { signal }
                )
                if (!response.ok) {
                    throw await createApiErrorFromResponse(response)
                }
                const data = await response.json()
                const searchResponse = SearchResponse.parse(data)

                // Add result metrics to span
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
        refetchOnMount: !isCooldownActive,
        placeholderData: keepPreviousData,
        staleTime: 1000 * 60 * 5, // 5 minutes
        retry: shouldRetry,
    })

    const cancelQuery = useCallback(() => {
        queryClient.cancelQueries({
            queryKey: [
                'search',
                { searchTerm: debouncedSearchTerm.toLowerCase(), pageNumber },
            ],
        })
    }, [queryClient, debouncedSearchTerm, pageNumber])

    return {
        ...query,
        cancelQuery,
    }
}
