import { useSearchTerm } from '../search.store'
import { useQuery } from '@tanstack/react-query'
import { useDebounce } from '@uidotdev/usehooks'
import * as z from 'zod'

const SearchResultItem = z.object({
    url: z.string(),
    title: z.string(),
    description: z.string(),
    score: z.number(),
})

const SearchResponse = z.object({
    results: z.array(SearchResultItem),
    totalResults: z.number(),
})

type SearchResponse = z.infer<typeof SearchResponse>

export const useSearchQuery = () => {
    const searchTerm = useSearchTerm()
    const trimmedSearchTerm = searchTerm.trim()
    const debouncedSearchTerm = useDebounce(trimmedSearchTerm, 300)
    return useQuery<SearchResponse>({
        queryKey: ['search', { searchTerm: debouncedSearchTerm }],
        queryFn: async () => {
            if (!debouncedSearchTerm || debouncedSearchTerm.length < 1) {
                return SearchResponse.parse({ results: [], totalResults: 0 })
            }

            const response = await fetch(
                '/docs/_api/v1/search?q=' +
                    encodeURIComponent(debouncedSearchTerm)
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
        staleTime: 1000 * 60 * 10, // 10 minutes
    })
}
