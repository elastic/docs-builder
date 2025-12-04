import {
    useTypeFilter,
    useSearchActions,
    type TypeFilter,
} from './search.store'
import { SearchResponse } from './useSearchQuery'

interface UseSearchFiltersOptions {
    results: SearchResponse['results']
    aggregations?: SearchResponse['aggregations']
}

export const useSearchFilters = ({
    results,
    aggregations,
}: UseSearchFiltersOptions) => {
    const typeFilter = useTypeFilter()
    const { setTypeFilter } = useSearchActions()

    const handleFilterClick = (filter: TypeFilter) => {
        setTypeFilter(filter)
    }

    // Results come pre-filtered from the server, so we just return them directly
    const filteredResults = results

    const typeAggregations = aggregations?.type
    const apiResultsCount = typeAggregations?.['api'] ?? 0
    const docsResultsCount = typeAggregations?.['doc'] ?? 0
    const totalCount = docsResultsCount + apiResultsCount
    const counts = { apiResultsCount, docsResultsCount, totalCount }

    return {
        selectedFilter: typeFilter,
        handleFilterClick,
        filteredResults,
        counts,
    }
}

// Re-export TypeFilter for convenience
export type { TypeFilter }
