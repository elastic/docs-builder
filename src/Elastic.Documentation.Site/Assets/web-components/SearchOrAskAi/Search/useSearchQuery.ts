import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { useDebounce } from '@uidotdev/usehooks'
import * as z from 'zod'

const SearchResultItemParent = z.object({
    url: z.string(),
    title: z.string(),
})

const SearchResultItem = z.object({
    url: z.string(),
    title: z.string(),
    description: z.string(),
    score: z.number(),
    parents: z.array(SearchResultItemParent),
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

type Props = {
    searchTerm: string
    pageNumber?: number
}

export const useSearchQuery = ({ searchTerm, pageNumber = 1 }: Props) => {
    const trimmedSearchTerm = searchTerm.trim()
    const debouncedSearchTerm = useDebounce(trimmedSearchTerm, 300)
    return useQuery<SearchResponse>({
        queryKey: [
            'search',
            { searchTerm: debouncedSearchTerm.toLowerCase(), pageNumber },
        ],
        queryFn: async () => {
            if (!debouncedSearchTerm || debouncedSearchTerm.length < 1) {
                return SearchResponse.parse({ results: [], totalResults: 0 })
            }
            const params = new URLSearchParams({
                q: debouncedSearchTerm,
                page: pageNumber.toString(),
            })

            const response = await fetch(
                '/docs/_api/v1/search?' + params.toString()
            )
            if (!response.ok) {
                throw new Error(
                    'Failed to fetch search results: ' + response.statusText
                )
            }
            const data = await response.json()
            return SearchResponse.parse(data)
        },
        enabled: !!trimmedSearchTerm && trimmedSearchTerm.length >= 1,
        refetchOnWindowFocus: false,
        placeholderData: keepPreviousData,
        staleTime: 1000 * 60 * 5, // 5 minutes
    })
}
