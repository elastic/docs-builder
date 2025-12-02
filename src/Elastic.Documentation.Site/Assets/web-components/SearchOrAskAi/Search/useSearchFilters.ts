import { SearchResponse } from './useSearchQuery'
import { useState, useMemo } from 'react'
import type { MouseEvent } from 'react'

export type FilterType = 'all' | 'doc' | 'api'

interface UseSearchFiltersOptions {
    results: SearchResponse['results']
}

export const useSearchFilters = ({ results }: UseSearchFiltersOptions) => {
    const [selectedFilters, setSelectedFilters] = useState<Set<FilterType>>(
        new Set(['all'])
    )

    const isMultiSelectModifierPressed = (event?: MouseEvent): boolean => {
        return !!(event && (event.metaKey || event.altKey || event.ctrlKey))
    }

    const toggleFilter = (
        currentFilters: Set<FilterType>,
        filter: FilterType
    ): Set<FilterType> => {
        const newFilters = new Set(currentFilters)
        newFilters.delete('all')
        if (newFilters.has(filter)) {
            newFilters.delete(filter)
        } else {
            newFilters.add(filter)
        }
        return newFilters.size === 0 ? new Set(['all']) : newFilters
    }

    const handleFilterClick = (filter: FilterType, event?: MouseEvent) => {
        if (filter === 'all') {
            setSelectedFilters(new Set(['all']))
            return
        }

        if (isMultiSelectModifierPressed(event)) {
            setSelectedFilters((prev) => toggleFilter(prev, filter))
        } else {
            setSelectedFilters(new Set([filter]))
        }
    }

    const filteredResults = useMemo(() => {
        if (selectedFilters.has('all')) {
            return results
        }
        return results.filter((result) => selectedFilters.has(result.type))
    }, [results, selectedFilters])

    const counts = useMemo(() => {
        const apiResultsCount = results.filter((r) => r.type === 'api').length
        const docsResultsCount = results.filter((r) => r.type === 'doc').length
        const totalCount = docsResultsCount + apiResultsCount
        return { apiResultsCount, docsResultsCount, totalCount }
    }, [results])

    return {
        selectedFilters,
        handleFilterClick,
        filteredResults,
        counts,
    }
}
